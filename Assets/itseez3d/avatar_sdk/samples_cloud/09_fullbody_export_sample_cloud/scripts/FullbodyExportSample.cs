/* Copyright (C) Itseez3D, Inc. - All Rights Reserved
* You may not use this file except in compliance with an authorized license
* Unauthorized copying of this file, via any medium is strictly prohibited
* Proprietary and confidential
* UNLESS REQUIRED BY APPLICABLE LAW OR AGREED BY ITSEEZ3D, INC. IN WRITING, SOFTWARE DISTRIBUTED UNDER THE LICENSE IS DISTRIBUTED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OR
* CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED
* See the License for the specific language governing permissions and limitations under the License.
* Written by Itseez3D, Inc. <support@avatarsdk.com>, May 2020
*/

using ItSeez3D.AvatarSdk.Cloud;
using ItSeez3D.AvatarSdk.Core;
using ItSeez3D.AvatarSdkSamples.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ItSeez3D.AvatarSdkSamples.Cloud
{
	public class FullbodyExportSample : GettingStartedSample
	{
		public FullbodyParametersConfigurationPanel parametersPanel = null;

		public Text meshFormatDropdownLabel;

		public Text infoText;

		private GameObject avatarObject = null;

		private IFullbodyAvatarProvider fullbodyAvatarProvider = null;

		private bool isParametersPanelActive = false;
		#region public methods
		public FullbodyExportSample()
		{
			selectedPipelineType = PipelineType.FULLBODY;			
		}

		public void OnParametersButtonClick()
		{
			isParametersPanelActive = !isParametersPanelActive;
			parametersPanel.SwitchActiveState(isParametersPanelActive);

			progressText.gameObject.SetActive(!isParametersPanelActive);
			infoText.gameObject.SetActive(!isParametersPanelActive);
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
			if (avatarObject != null)
				DestroyImmediate(avatarObject);

			if (isParametersPanelActive)
				OnParametersButtonClick();

			FullbodyAvatarComputationParameters computationParameters = new FullbodyAvatarComputationParameters();
			parametersPanel.ConfigureComputationParameters(computationParameters);
			computationParameters.meshFormat = MeshFormatExtensions.MeshFormatFromStr(meshFormatDropdownLabel.text);

			// generate avatar from the photo and get its code in the Result of request
			var initializeRequest = fullbodyAvatarProvider.InitializeFullbodyAvatarAsync(photoBytes, computationParameters);
			yield return Await(initializeRequest);
			currentAvatarCode = initializeRequest.Result;

			StartCoroutine(SampleUtils.DisplayPhotoPreview(currentAvatarCode, photoPreview));

			var calculateRequest = fullbodyAvatarProvider.StartAndAwaitAvatarCalculationAsync(currentAvatarCode);
			yield return Await(calculateRequest);

			var downloadRequest = fullbodyAvatarProvider.RetrieveAllAvatarDataFromCloudAsync(currentAvatarCode);
			yield return Await(downloadRequest);

			string avatarDirectory = AvatarSdkMgr.FullbodyStorage().GetAvatarDirectory(currentAvatarCode);
			progressText.text = string.Format("The generated avatar can be found in the directory:\n{0}", avatarDirectory);
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
			System.Diagnostics.Process.Start(avatarDirectory);
#endif
		}

		protected override void SetControlsInteractable(bool interactable)
		{
			base.SetControlsInteractable(interactable);
			parametersPanel.SetControlsInteractable(interactable);
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
		#endregion private methods
	}
}
