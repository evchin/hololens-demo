/* Copyright (C) Itseez3D, Inc. - All Rights Reserved
* You may not use this file except in compliance with an authorized license
* Unauthorized copying of this file, via any medium is strictly prohibited
* Proprietary and confidential
* UNLESS REQUIRED BY APPLICABLE LAW OR AGREED BY ITSEEZ3D, INC. IN WRITING, SOFTWARE DISTRIBUTED UNDER THE LICENSE IS DISTRIBUTED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OR
* CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED
* See the License for the specific language governing permissions and limitations under the License.
* Written by Itseez3D, Inc. <support@avatarsdk.com>, December 2020
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace ItSeez3D.AvatarSdk.Core
{
	public static class RenderingPipelineTraits
	{
		public enum RenderingPipeline { HDRP, LWRP, URP, DEFAULT }
		public enum ShaderKind { HairStrandsLit, HairSolidLit }
		public enum MaterialTemplate { Body, BodyPbr, Outfit, Haircut }
		private static Dictionary<MaterialTemplate, string> materialTemplateNames = new Dictionary<MaterialTemplate, string>();
		private static Dictionary<ShaderKind, string> shaderNames = new Dictionary<ShaderKind, string>();
		private static void GenerateTraits(RenderingPipeline renderingPipeLine)
		{
			string materialSuffix = "";
			string materialDirectory = "";
			string shaderSuffix = "";
			string separator = "";

			switch (renderingPipeLine)
			{
				case RenderingPipeline.LWRP:
				case RenderingPipeline.URP:
					materialSuffix = "_urp";
					shaderSuffix = "URP";
					separator = "/";
					materialDirectory = "urp";
					break;
				default: break;
			}
			materialTemplateNames = new Dictionary<MaterialTemplate, string>()
					{
						{ MaterialTemplate.Body, $"fullbody_materials/{materialDirectory}{separator}avatar_sdk_template_body_material{materialSuffix}" },
						{ MaterialTemplate.BodyPbr, $"fullbody_materials/{materialDirectory}{separator}avatar_sdk_template_body_pbr_material{materialSuffix}" },
						{ MaterialTemplate.Outfit, $"fullbody_materials/{materialDirectory}{separator}avatar_sdk_template_outfit_material{materialSuffix}" },
						{ MaterialTemplate.Haircut, $"fullbody_materials/{materialDirectory}{separator}avatar_sdk_template_haircut_material{materialSuffix}" }
					};
			shaderNames = new Dictionary<ShaderKind, string>() 
			{
				{ ShaderKind.HairStrandsLit, $"Avatar SDK/{shaderSuffix}{separator}HaircutStrandsLit{shaderSuffix}Shader" },
				{ ShaderKind.HairSolidLit, $"Avatar SDK/{shaderSuffix}{separator}HaircutSolidLit{shaderSuffix}Shader"  }
			};
		}
		public static RenderingPipeline GetRenderingPipeLine()
		{
			if (GraphicsSettings.renderPipelineAsset == null)
			{
				return RenderingPipeline.DEFAULT;
			}
			else if (GraphicsSettings.renderPipelineAsset.GetType().Name.Contains("HDRenderPipelineAsset"))
			{
				return RenderingPipeline.HDRP;
			}
			else if (GraphicsSettings.renderPipelineAsset.GetType().Name.Contains("LightweightRenderPipelineAsset"))
			{
				return RenderingPipeline.LWRP;
			}
			else if (GraphicsSettings.renderPipelineAsset.GetType().Name.Contains("UniversalRenderPipelineAsset"))
			{
				return RenderingPipeline.URP;
			}
			else
			{
				return RenderingPipeline.DEFAULT;
			}
		}
		static RenderingPipelineTraits()
		{
			GenerateTraits(GetRenderingPipeLine());
		}
		public static string GetMaterialName(MaterialTemplate materialTemplate)
		{
			if (materialTemplateNames == null)
			{
				throw new Exception("Incorrect initialization of material templates names: Template names is null");
			}
			else if (!materialTemplateNames.ContainsKey(materialTemplate))
			{
				throw new Exception("Incorrect initialization of material templates names: Key not found");
			}
			return materialTemplateNames[materialTemplate];
		}
		public static string GetShaderName(ShaderKind shaderKind)
		{
			if (shaderNames == null)
			{
				throw new Exception("Incorrect initialization of shader names: Shader names is null");
			}
			else if (!shaderNames.ContainsKey(shaderKind))
			{
				throw new Exception("Incorrect initialization of shader names: Key not found");
			}
			return shaderNames[shaderKind];
		}

		internal static string GetTextureName()
		{
			if(GetRenderingPipeLine() == RenderingPipeline.URP)
			{
				return "_BaseMap";
			}
			else
			{
				return "_MainTex";
			}
		}

		public static void SetUpTheLighting()
		{
			if(GetRenderingPipeLine() == RenderingPipeline.URP || GetRenderingPipeLine() == RenderingPipeline.LWRP)
			{
				UnityEngine.SceneManagement.Scene activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
				var directionalLightObj = activeScene.GetRootGameObjects().FirstOrDefault(o => o.GetComponent<Light>() != null && o.GetComponent<Light>().type == LightType.Directional);
				if (directionalLightObj == null) { return; }
				Light directionalLight = directionalLightObj.GetComponent<Light>();
				if (directionalLight == null) { return; }
				directionalLight.intensity *= 1.4f;
			}
		}
	}
}