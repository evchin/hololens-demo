/* Copyright (C) Itseez3D, Inc. - All Rights Reserved
* You may not use this file except in compliance with an authorized license
* Unauthorized copying of this file, via any medium is strictly prohibited
* Proprietary and confidential
* UNLESS REQUIRED BY APPLICABLE LAW OR AGREED BY ITSEEZ3D, INC. IN WRITING, SOFTWARE DISTRIBUTED UNDER THE LICENSE IS DISTRIBUTED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OR
* CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED
* See the License for the specific language governing permissions and limitations under the License.
* Written by Itseez3D, Inc. <support@avatarsdk.com>, May 2020
*/

using ItSeez3D.AvatarSdk.Core;
using ItSeez3D.AvatarSdkSamples.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ItSeez3D.AvatarSdkSamples.Cloud
{
	public class FullbodyParametersSample : GettingStartedSample
	{
		public FullbodyParametersConfigurationPanel parametersPanel = null;

		public GameObject baseControlsParent;

		public GameObject generatedAvatarControlsParent;

		public GameObject haircutsControlsParent;

		public GameObject outfitsControlsParent;

		public GameObject facialAnimationsControlsParent;

		public GameObject bodyAnimationsControlsParent;

		public GameObject createBodyAnimatorButton;

		public Text haircutNameText;

		public Text outfitNameText;

		public ItemsSelectingView haircutsSelectingView;

		public ItemsSelectingView outfitsSelectingView;

		public ModelInfoDataPanel modelInfoPanel;

		public AnimationManager facialAnimationManager;

		public AnimationManager bodyAnimationManager;

		private IFullbodyAvatarProvider fullbodyAvatarProvider = null;

		private bool isParametersPanelActive = false;

		private bool isAvatarDisplayed = false;

		private FullbodyAvatarLoader avatarLoader = null;

		private int currentHaircutIdx = -1;
		private List<string> haircuts = new List<string>();

		private int currentOutfitIdx = -1;
		private List<string> outfits = new List<string>();

		FullbodyAvatarComputationParameters computationParameters = new FullbodyAvatarComputationParameters();

		private readonly string generatedHaircutName = "generated";
		private readonly string baldHaircutName = "bald";
		private readonly string emptyOutfitName = "no outfit";

		protected override void Start()
		{
			base.Start();
			RenderingPipelineTraits.SetUpTheLighting();
		}

		#region public methods
		public FullbodyParametersSample()
		{
			selectedPipelineType = PipelineType.FULLBODY;
		}

		public void OnParametersButtonClick()
		{
			isParametersPanelActive = !isParametersPanelActive;
			parametersPanel.SwitchActiveState(isParametersPanelActive);

			progressText.gameObject.SetActive(!isParametersPanelActive);
			generatedAvatarControlsParent.SetActive(!isParametersPanelActive && isAvatarDisplayed);
		}

		public void OnNextHaircutButtonClick()
		{
			currentHaircutIdx++;
			if (currentHaircutIdx >= haircuts.Count)
				currentHaircutIdx = 0;
			StartCoroutine(ChangeHaircut(haircuts[currentHaircutIdx]));
		}

		public void OnPrevHaircutButtonClick()
		{
			currentHaircutIdx--;
			if (currentHaircutIdx < 0)
				currentHaircutIdx = haircuts.Count - 1;
			StartCoroutine(ChangeHaircut(haircuts[currentHaircutIdx]));
		}

		public void OnHaircutListButtonClick()
		{
			baseControlsParent.SetActive(false);
			string currentHaircutName = haircuts[currentHaircutIdx];
			haircutsSelectingView.Show(new List<string>() { currentHaircutName }, (list, isSelected) =>
			{
				baseControlsParent.SetActive(true);
				if (isSelected)
					StartCoroutine(ChangeHaircut(list[0]));
			});
		}

		public void OnNextOutfitButtonClick()
		{
			currentOutfitIdx++;
			if (currentOutfitIdx >= outfits.Count)
				currentOutfitIdx = 0;
			StartCoroutine(ChangeOutfit(outfits[currentOutfitIdx]));
		}

		public void OnPrevOutfitButtonClick()
		{
			currentOutfitIdx--;
			if (currentOutfitIdx < 0)
				currentOutfitIdx = outfits.Count - 1;
			StartCoroutine(ChangeOutfit(outfits[currentOutfitIdx]));
		}

		public void OnOutfitListButtonClick()
		{
			baseControlsParent.SetActive(false);
			string currentOutfitName = outfits[currentOutfitIdx];
			outfitsSelectingView.Show(new List<string>() { currentOutfitName }, (list, isSelected) =>
			{
				baseControlsParent.SetActive(true);
				if (isSelected)
					StartCoroutine(ChangeOutfit(list[0]));
			});
		}

		public void OnCreateBodyAnimatorButtonClick()
		{
			bodyAnimationManager.CreateHumanoidAnimator(avatarLoader.AvatarGameObject, avatarLoader.GetBodyMeshObject().GetComponentInChildren<SkinnedMeshRenderer>());
			bodyAnimationsControlsParent.SetActive(true);
			createBodyAnimatorButton.SetActive(false);
		}

		#endregion public methods

		#region base overrided methods

		protected override IEnumerator Initialize()
		{
			fullbodyAvatarProvider = AvatarSdkMgr.GetFullbodyAvatarProvider();
			avatarProvider = fullbodyAvatarProvider;
			yield return Await(avatarProvider.InitializeAsync());
			yield return CheckAvailablePipeline();
		}

		protected override IEnumerator GenerateAndDisplayHead(byte[] photoBytes, PipelineType pipeline)
		{
			isAvatarDisplayed = false;
			generatedAvatarPipeline = pipeline;
			generatedAvatarControlsParent.SetActive(false);

			if (isParametersPanelActive)
				OnParametersButtonClick();

			parametersPanel.ConfigureComputationParameters(computationParameters);

			// generate avatar from the photo and get its code in the Result of request
			var initializeRequest = fullbodyAvatarProvider.InitializeFullbodyAvatarAsync(photoBytes, computationParameters);
			yield return Await(initializeRequest);
			currentAvatarCode = initializeRequest.Result;

			StartCoroutine(SampleUtils.DisplayPhotoPreview(currentAvatarCode, photoPreview));

			//Await till avatar is calculated
			var calculateRequest = fullbodyAvatarProvider.StartAndAwaitAvatarCalculationAsync(currentAvatarCode);
			yield return Await(calculateRequest);

			string initialHaircutName = GetInitialHaircutName(computationParameters.haircuts.names);
			string initialOutfitName = GetInitialOutfitName(computationParameters.outfits.names);
			if (AvatarSdkMgr.GetSdkType() == SdkType.Cloud) 
			{
				//Downloading avatar model files
				var retrievingBodyModelRequest = fullbodyAvatarProvider.RetrieveBodyModelFromCloudAsync(currentAvatarCode);
				yield return Await(retrievingBodyModelRequest);

				//Downloading haircut if required
				if (!string.IsNullOrEmpty(initialHaircutName) && !computationParameters.haircuts.embed)
				{
					var retrievingHaircutRequest = fullbodyAvatarProvider.RetrieveHaircutModelFromCloudAsync(currentAvatarCode, initialHaircutName);
					yield return Await(retrievingHaircutRequest);
				}

				//Downloading outfit if required
				if (!string.IsNullOrEmpty(initialOutfitName) && !computationParameters.outfits.embed)
				{
					var retrievingOutfitRequest = fullbodyAvatarProvider.RetrieveOutfitModelFromCloudAsync(currentAvatarCode, initialOutfitName);
					yield return Await(retrievingOutfitRequest);
				}
			}

			yield return ShowAvatarOnScene(initialHaircutName, initialOutfitName, computationParameters.blendshapes.names.Count > 0);

			ConfigureHaircutsControls(initialHaircutName);
			ConfigureOutfitsControls(initialOutfitName);
			ConfigureBlendshapesControls(computationParameters.blendshapes.names);
			ConfigureBodyAnimationsControls();

			ModelInfo modelInfo = CoreTools.GetAvatarModelInfo(currentAvatarCode);
			modelInfoPanel.UpdateData(modelInfo);

			progressText.text = string.Empty;
			generatedAvatarControlsParent.SetActive(true);
			isAvatarDisplayed = true;
		}

		protected override void SetControlsInteractable(bool interactable)
		{
			base.SetControlsInteractable(interactable);
			parametersPanel.SetControlsInteractable(interactable);

			Selectable[] generatedControls = generatedAvatarControlsParent.GetComponentsInChildren<Selectable>(true);
			if (generatedControls != null)
			{
				foreach (Selectable c in generatedControls)
					c.interactable = interactable;
			}
		}
		#endregion

		#region private methods
		private IEnumerator CheckAvailablePipeline()
		{
			// Fullbody avatars are available on the Pro plan. Need to verify it.
			SetControlsInteractable(false);
			var pipelineAvailabilityRequest = avatarProvider.IsPipelineSupportedAsync(selectedPipelineType);
			yield return Await(pipelineAvailabilityRequest);
			if (pipelineAvailabilityRequest.IsError)
				yield break;

			if (pipelineAvailabilityRequest.Result == true)
			{
				yield return UpdateAvatarParameters();
				progressText.text = string.Empty;
				SetControlsInteractable(true);
			}
			else
			{
				string errorMsg = "You can't generate fullbody avatars.\nThis option is available on the PRO plan.";
				progressText.text = errorMsg;
				progressText.color = Color.red;
				Debug.LogError(errorMsg);
			}
		}

		private IEnumerator ShowAvatarOnScene(string haircutName, string outfitName, bool withBlendshapes)
		{
			GameObject avatarObject = new GameObject(AVATAR_OBJECT_NAME);
			avatarObject.SetActive(false);

			avatarLoader = new FullbodyAvatarLoader(fullbodyAvatarProvider);
			avatarLoader.AvatarGameObject = avatarObject;
			yield return avatarLoader.LoadAvatarAsync(currentAvatarCode);

			if (!string.IsNullOrEmpty(haircutName))
			{
				var showHaircutRequest = avatarLoader.ShowHaircutAsync(haircutName);
				yield return Await(showHaircutRequest);
			}

			if (!string.IsNullOrEmpty(outfitName))
			{
				var showOutfitRequest = avatarLoader.ShowOutfitAsync(outfitName);
				yield return Await(showOutfitRequest);
			}

			if (withBlendshapes)
				facialAnimationManager.CreateAnimator(avatarLoader.GetBodyMeshObject());

			avatarObject.AddComponent<MoveByMouse>();
			avatarObject.SetActive(true);
		}

		private void ConfigureHaircutsControls(string currentHaircutName)
		{
			haircuts = avatarLoader.GetHaircuts();
			haircuts.Sort();
			if (haircuts.Count > 0)
			{
				haircutsControlsParent.SetActive(true);

				haircuts.Insert(0, baldHaircutName);
				haircutsSelectingView.InitItems(haircuts);

				currentHaircutIdx = haircuts.IndexOf(currentHaircutName);
				if (currentHaircutIdx < 0)
					currentHaircutIdx = 0;
				haircutNameText.text = haircuts[currentHaircutIdx];
			}
			else
				haircutsControlsParent.SetActive(false);
		}

		private void ConfigureOutfitsControls(string currentOutfitName)
		{
			outfits = avatarLoader.GetOutfits();
			outfits.Sort();
			if (outfits.Count > 0)
			{
				outfitsControlsParent.SetActive(true);

				outfits.Insert(0, emptyOutfitName);
				outfitsSelectingView.InitItems(outfits);

				currentOutfitIdx = outfits.IndexOf(currentOutfitName);
				if (currentOutfitIdx < 0)
					currentOutfitIdx = 0;
				outfitNameText.text = outfits[currentOutfitIdx];
			}
			else
				outfitsControlsParent.SetActive(false);
		}

		private void ConfigureBlendshapesControls(List<string> blendshapesSets)
		{
			bool isMobileBlendshapesSetExist = blendshapesSets.Exists(s => s.Contains("mobile_51"));
			facialAnimationsControlsParent.SetActive(isMobileBlendshapesSetExist);
		}

		private void ConfigureBodyAnimationsControls()
		{
			createBodyAnimatorButton.SetActive(true);
			bodyAnimationsControlsParent.SetActive(false);
		}

		private IEnumerator UpdateAvatarParameters()
		{
			SetControlsInteractable(false);

			var parametersRequest = fullbodyAvatarProvider.GetAvailableComputationParametersAsync(selectedPipelineType);
			yield return Await(parametersRequest);
			if (parametersRequest.IsError)
			{
				Debug.LogError("Unable to get available computation parameters");
			}
			else
			{
				FullbodyAvatarComputationParameters availableParameters = parametersRequest.Result;
				parametersPanel.UpdateParameters(availableParameters);
				SetControlsInteractable(true);
			}
		}

		private string GetInitialHaircutName(List<string> haircutsList)
		{
			if (haircutsList == null || haircutsList.Count == 0)
				return string.Empty;

			if (haircutsList.Contains(generatedHaircutName))
				return generatedHaircutName;

			System.Random random = new System.Random(DateTime.Now.Millisecond);
			return haircutsList[random.Next(haircutsList.Count - 1)];
		}

		private string GetInitialOutfitName(List<string> outfitsList)
		{
			if (outfitsList == null || outfitsList.Count == 0)
				return string.Empty;

			System.Random random = new System.Random(DateTime.Now.Millisecond);
			return outfitsList[random.Next(outfitsList.Count - 1)];
		}

		private IEnumerator ChangeHaircut(string haircutName)
		{
			SetControlsInteractable(false);

			if (haircutName == baldHaircutName)
				avatarLoader.HideAllHaircuts();
			else
			{
				var showHaircutRequest = avatarLoader.ShowHaircutAsync(haircutName);
				yield return Await(showHaircutRequest);
			}

			progressText.text = string.Empty;
			haircutNameText.text = haircutName;
			currentHaircutIdx = haircuts.IndexOf(haircutName);

			SetControlsInteractable(true);
		}

		private IEnumerator ChangeOutfit(string outfitName)
		{
			SetControlsInteractable(false);

			if (outfitName == emptyOutfitName)
				avatarLoader.HideAllOutfits();
			else
			{
				var showOutfitRequest = avatarLoader.ShowOutfitAsync(outfitName);
				yield return Await(showOutfitRequest);
			}

			progressText.text = string.Empty;
			outfitNameText.text = outfitName;
			currentOutfitIdx = outfits.IndexOf(outfitName);

			SetControlsInteractable(true);
		}
		#endregion private methods
	}
}
