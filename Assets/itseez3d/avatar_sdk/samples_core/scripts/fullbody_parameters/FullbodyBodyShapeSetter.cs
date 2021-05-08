/* Copyright (C) Itseez3D, Inc. - All Rights Reserved
* You may not use this file except in compliance with an authorized license
* Unauthorized copying of this file, via any medium is strictly prohibited
* Proprietary and confidential
* UNLESS REQUIRED BY APPLICABLE LAW OR AGREED BY ITSEEZ3D, INC. IN WRITING, SOFTWARE DISTRIBUTED UNDER THE LICENSE IS DISTRIBUTED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OR
* CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED
* See the License for the specific language governing permissions and limitations under the License.
* Written by Itseez3D, Inc. <support@avatarsdk.com>, November 2020
*/

using ItSeez3D.AvatarSdk.Core;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;


namespace ItSeez3D.AvatarSdkSamples.Core
{
	public class FullbodyBodyShapeSetter : ComputationParametersPanel
	{
		public Toggle genderToggle;
		public Toggle heightToggle;
		public Toggle weightToggle;
		public Toggle chestToggle;
		public Toggle waistToggle;
		public Toggle hipsToggle;


		public Dropdown genderDropdown;
		public InputField heightInput;
		public InputField weightInput;
		public InputField chestInput;
		public InputField waistInput;
		public InputField hipsInput;

		private BodyShapeGroup allParameters = null;

		public override void DeselectAllParameters()
		{
			SetToggleValue(genderToggle, allParameters.gender.IsAvailable, false);
			SetToggleValue(heightToggle, allParameters.height.IsAvailable, false);
			SetToggleValue(weightToggle, allParameters.weight.IsAvailable, false);
			SetToggleValue(waistToggle, allParameters.waist.IsAvailable, false);
			SetToggleValue(chestToggle, allParameters.chest.IsAvailable, false);
			SetToggleValue(hipsToggle, allParameters.hips.IsAvailable, false);

		}

		private AvatarGender GetGender()
		{
			switch (genderDropdown.value)
			{
				case 0: return AvatarGender.Male;
				case 1: return AvatarGender.Female;
				case 2: return AvatarGender.NonBinary;
				default: return AvatarGender.Unknown;
			}
		}

		private float GetFloat(string str)
		{
			float result;
			if (float.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
			{
				return result;
			}
			else
			{
				str = str.Replace(',', '.');
				if (float.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
				{
					return result;
				}
				else
				{
					Debug.LogErrorFormat("Unable to parse string as float: {0}", str);
					return 0;
				}
			}
		}

		public BodyShapeGroup GetParameters()
		{
			BodyShapeGroup bodyShapeParams = new BodyShapeGroup();

			bodyShapeParams.gender = CreatePropertyAndSetValue(allParameters.gender, genderToggle, GetGender());
			bodyShapeParams.height = CreatePropertyAndSetValue(allParameters.height, heightToggle, GetFloat(heightInput.text));
			bodyShapeParams.weight = CreatePropertyAndSetValue(allParameters.weight, weightToggle, GetFloat(weightInput.text));
			bodyShapeParams.waist = CreatePropertyAndSetValue(allParameters.waist, waistToggle, GetFloat(waistInput.text));
			bodyShapeParams.chest = CreatePropertyAndSetValue(allParameters.chest, chestToggle, GetFloat(chestInput.text));
			bodyShapeParams.hips = CreatePropertyAndSetValue(allParameters.hips, hipsToggle, GetFloat(hipsInput.text));

			return bodyShapeParams;
		}

		public override void SelectAllParameters()
		{
			SetToggleValue(genderToggle, allParameters.gender.IsAvailable, allParameters.gender.IsAvailable);
			SetToggleValue(heightToggle, allParameters.height.IsAvailable, allParameters.height.IsAvailable);
			SetToggleValue(weightToggle, allParameters.weight.IsAvailable, allParameters.weight.IsAvailable);
			SetToggleValue(chestToggle, allParameters.chest.IsAvailable, allParameters.chest.IsAvailable);
			SetToggleValue(waistToggle, allParameters.waist.IsAvailable, allParameters.waist.IsAvailable);
			SetToggleValue(hipsToggle, allParameters.hips.IsAvailable, allParameters.hips.IsAvailable);

		}

		public override void SelectDefaultParameters()
		{
			DeselectAllParameters();
		}

		public void UpdateParameters(BodyShapeGroup allParameters, BodyShapeGroup defaultParameters)
		{
			this.allParameters = allParameters;
			SelectDefaultParameters();
		}
	}
}