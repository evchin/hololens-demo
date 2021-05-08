/* Copyright (C) Itseez3D, Inc. - All Rights Reserved
* You may not use this file except in compliance with an authorized license
* Unauthorized copying of this file, via any medium is strictly prohibited
* Proprietary and confidential
* UNLESS REQUIRED BY APPLICABLE LAW OR AGREED BY ITSEEZ3D, INC. IN WRITING, SOFTWARE DISTRIBUTED UNDER THE LICENSE IS DISTRIBUTED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OR
* CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED
* See the License for the specific language governing permissions and limitations under the License.
* Written by Itseez3D, Inc. <support@avatarsdk.com>, January 2021
*/

using ItSeez3D.AvatarSdk.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ItSeez3D.AvatarSdkSamples.Core
{
	public class FullbodyParametersConfigurationPanel : ParametersConfigurationPanel
	{
		public GameObject basePanel;

		public GameObject bodyShapePanel;
		public GameObject outfitsPanel;

		public Button bodyShapeButton;
		public Button outfitsButton;

		public Toggle textureSizeToggle;
		public InputField textureWidthInput;
		public InputField textureHeightInput;

		public Toggle embedTexturesToggle;

		private FullbodyBodyShapeSetter bodyShapeSetter;
		private FullbodyOutfitsParametersSetter outfitsParametersSetter;
		private FullbodyHaircutsParametersSetter haircutsParametersSetter;
		private FullbodyBlendshapesParametersSetter blendshapesParametersSetter;

		protected override void Start()
		{
			base.Start();

			if (bodyShapeButton != null && bodyShapePanel != null)
			{
				bodyShapeButton.onClick.AddListener(() => OnShowPanelButtonClick(bodyShapePanel));
				panels.Add(bodyShapePanel);
				bodyShapeSetter = bodyShapePanel.GetComponentInChildren<FullbodyBodyShapeSetter>();
			}
			if (outfitsButton != null && outfitsPanel != null)
			{
				outfitsButton.onClick.AddListener(() => OnShowPanelButtonClick(outfitsPanel));
				panels.Add(outfitsPanel);
				outfitsParametersSetter = outfitsPanel.GetComponentInChildren<FullbodyOutfitsParametersSetter>();
			}
			if (haircutsButton != null && haircutsPanel != null)
				haircutsParametersSetter = haircutsPanel.GetComponentInChildren<FullbodyHaircutsParametersSetter>();
			if (blendshapesButton != null && blendshapesPanel != null)
				blendshapesParametersSetter = blendshapesPanel.GetComponentInChildren<FullbodyBlendshapesParametersSetter>();
		}

		public void UpdateParameters(FullbodyAvatarComputationParameters computationParameters)
		{
			if (haircutsSelectingView != null)
				haircutsSelectingView.InitItems(computationParameters.haircuts.names, new List<string>() { "generated" });
			if (blendshapesSelectingView != null)
				blendshapesSelectingView.InitItems(computationParameters.blendshapes.names);
			if (outfitsParametersSetter != null)
			{
				outfitsParametersSetter.outfitsSelectingView.InitItems(computationParameters.outfits.names);
				outfitsParametersSetter.additionalTexturesSelectingView.InitItems(computationParameters.outfits.additionalTextures, computationParameters.outfits.additionalTextures);
			}
			if (additionalTexturesSelectingView != null)
				additionalTexturesSelectingView.InitItems(computationParameters.additionalTextures.names, computationParameters.additionalTextures.names);
			if (bodyShapeSetter != null)
				bodyShapeSetter.UpdateParameters(computationParameters.bodyShape, new BodyShapeGroup());
			if (modelInfoSetter != null)
				modelInfoSetter.UpdateParameters(computationParameters.modelInfo, new ModelInfoGroup());
			if (avatarModificationsSetter != null)
				avatarModificationsSetter.UpdateParameters(computationParameters.avatarModifications, new AvatarModificationsGroup());
		}

		public void ConfigureComputationParameters(FullbodyAvatarComputationParameters outComputationParameters)
		{
			if (textureSizeToggle.isOn)
				outComputationParameters.textureSize = ConvertionUtils.StrToTextureSize(textureWidthInput.text, textureHeightInput.text);

			if (embedTexturesToggle != null)
				outComputationParameters.embedTextures = embedTexturesToggle.isOn;

			if (haircutsSelectingView != null)
				outComputationParameters.haircuts = haircutsParametersSetter.GetParameters();
			if (blendshapesSelectingView != null)
				outComputationParameters.blendshapes = blendshapesParametersSetter.GetParameters();
			if (outfitsParametersSetter != null)
				outComputationParameters.outfits = outfitsParametersSetter.GetParameters();
			if (additionalTexturesSelectingView != null)
				outComputationParameters.additionalTextures.names = additionalTexturesSelectingView.CurrentSelection;
			if (bodyShapeSetter != null)
				outComputationParameters.bodyShape = bodyShapeSetter.GetParameters();
			if (modelInfoSetter != null)
				outComputationParameters.modelInfo = modelInfoSetter.GetParameters();
			if (avatarModificationsSetter != null)
				outComputationParameters.avatarModifications = avatarModificationsSetter.GetParameters();
		}

		public void SwitchActiveState(bool isActive)
		{
			if (basePanel != null)
				basePanel.SetActive(isActive);
		}
	}
}
