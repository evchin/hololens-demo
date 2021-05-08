/* Copyright (C) Itseez3D, Inc. - All Rights Reserved
* You may not use this file except in compliance with an authorized license
* Unauthorized copying of this file, via any medium is strictly prohibited
* Proprietary and confidential
* UNLESS REQUIRED BY APPLICABLE LAW OR AGREED BY ITSEEZ3D, INC. IN WRITING, SOFTWARE DISTRIBUTED UNDER THE LICENSE IS DISTRIBUTED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OR
* CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED
* See the License for the specific language governing permissions and limitations under the License.
* Written by Itseez3D, Inc. <support@avatarsdk.com>, December 2020
*/

using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using static GLTF.CoroutineGLTFSceneImporter;

namespace ItSeez3D.AvatarSdk.Core
{
	public class FullbodyMaterialAdjuster
	{
		enum ShaderType
		{
			Standard,
			HaircutStrands,
			HaircutSolid
		}

		private static Dictionary<string, ShaderType> haircutsShaders = new Dictionary<string, ShaderType>()
		{
			{ "wavy_bob", ShaderType.HaircutStrands },
			{ "very_long", ShaderType.HaircutStrands },
			{ "shoulder_length", ShaderType.HaircutStrands },
			{ "short_parted", ShaderType.HaircutStrands },
			{ "short_curls", ShaderType.HaircutStrands },
			{ "roman", ShaderType.HaircutStrands },
			{ "rasta", ShaderType.HaircutStrands },
			{ "mid_length_straight2", ShaderType.HaircutStrands },
			{ "mid_length_ruffled", ShaderType.HaircutStrands },
			{ "corkscrew_curls", ShaderType.HaircutStrands },
			{ "bob_parted", ShaderType.HaircutStrands },

			{ "short_slick", ShaderType.HaircutSolid },
			{ "mid_length_wispy", ShaderType.HaircutSolid },
			{ "long_crimped", ShaderType.HaircutSolid },
			{ "balding", ShaderType.HaircutSolid },
			{ "short_disheveled", ShaderType.HaircutSolid },
			{ "ponytail_with_bangs", ShaderType.HaircutSolid },
			{ "mid_length_straight", ShaderType.HaircutSolid },
			{ "long_wavy", ShaderType.HaircutSolid },
			{ "long_disheveled", ShaderType.HaircutSolid },
			{ "short_simple", ShaderType.HaircutSolid },
			{ "generated", ShaderType.HaircutSolid }
		};

		private IFullbodyPersistentStorage persistentStorage = null;

		public FullbodyMaterialAdjuster()
		{
			persistentStorage = AvatarSdkMgr.FullbodyStorage();
		}

		public IEnumerator PrepareBodyMaterial(CoroutineResult<Material> executionResult, string avatarCode, bool withPbrTextures)
		{
			var materialType = withPbrTextures ? RenderingPipelineTraits.MaterialTemplate.BodyPbr : RenderingPipelineTraits.MaterialTemplate.Body;
			string templateMaterialName = RenderingPipelineTraits.GetMaterialName(materialType);
			Material templateMaterial = Resources.Load<Material>(templateMaterialName);

			if (templateMaterial == null)
			{
				Debug.LogError("Template body material isn't found!");
				executionResult.result = null;
				yield break;
			}
			Material bodyMaterial = new Material(templateMaterial);

			string bodyTextureFilename = persistentStorage.GetAvatarFile(avatarCode, FullbodyAvatarFileType.Texture);
			Texture2D bodyTexture = new Texture2D(0, 0);
			bodyTexture.LoadImage(File.ReadAllBytes(bodyTextureFilename));
			UpdateBodyMainTexture(bodyMaterial, bodyTexture);
			yield return null;

			if (withPbrTextures)
			{
				string metallnessTextureFilename = persistentStorage.GetAvatarFile(avatarCode, FullbodyAvatarFileType.MetallnessMap);
				string roughnessTextureFilename = persistentStorage.GetAvatarFile(avatarCode, FullbodyAvatarFileType.RoughnessMap);
				string normalTextureFilename = persistentStorage.GetAvatarFile(avatarCode, FullbodyAvatarFileType.NormalMap);

				yield return ConfigurePbrTextures(bodyMaterial, normalTextureFilename, metallnessTextureFilename, roughnessTextureFilename);
			}

			executionResult.result = bodyMaterial;
		}

		public Texture2D PrepareTransparentBodyTextureForOutfit(Texture2D opaqueBodyTexture, string outfitDir, string outfitName)
		{
			string alphaMaskTextureFilename = persistentStorage.GetOutfitFileInDir(outfitDir, outfitName, OutfitFileType.BodyVisibilityMask);
			if (File.Exists(alphaMaskTextureFilename))
			{
				Texture2D tranparentBodyTexture = new Texture2D(2, 2);
				tranparentBodyTexture.LoadImage(File.ReadAllBytes(alphaMaskTextureFilename));

				Color32[] transparentTexColors = tranparentBodyTexture.GetPixels32();
				Color32[] bodyTexColors = opaqueBodyTexture.GetPixels32();
				for (int i = 0; i < bodyTexColors.Length; i++)
					bodyTexColors[i].a = transparentTexColors[i].r;

				tranparentBodyTexture.SetPixels32(bodyTexColors);
				tranparentBodyTexture.Apply();

				return tranparentBodyTexture;
			}
			else
			{
				Debug.LogWarningFormat("Body visibility mask not found: {0}", alphaMaskTextureFilename);
				return null;
			}
		}

		public void UpdateBodyMainTexture(Material bodySharedMaterial, Texture2D bodyTexture)
		{
			bodySharedMaterial.SetTexture(RenderingPipelineTraits.GetTextureName(), bodyTexture);
		}

		public IEnumerator PrepareHairMaterial(CoroutineResult<Material> executionResult, string haircutDir, string haircutName)
		{
			string haircutTextureFilename = persistentStorage.GetHaircutFileInDir(haircutDir, haircutName, FullbodyHaircutFileType.Texture);
			Texture2D haircutTexture = new Texture2D(0, 0);
			haircutTexture.LoadImage(File.ReadAllBytes(haircutTextureFilename));
			yield return null;

			if (haircutsShaders.ContainsKey(haircutName))
			{
				if (haircutsShaders[haircutName] == ShaderType.HaircutSolid)
					executionResult.result = PrepareHairMaterialWithAvatarSdkSolidShader(haircutTexture);
				else
					executionResult.result = PrepareHairMaterialWithAvatarSdkStrandsShader(haircutTexture);
			}
			else
				executionResult.result = PrepareHairMaterialWithStandardShader(haircutTexture);
		}

		public IEnumerator PrepareOutfitMaterial(CoroutineResult<Material> executionResult, string outfitDir, string outfitName, bool withPbrTextures)
		{
			Material templateMaterial = Resources.Load<Material>(RenderingPipelineTraits.GetMaterialName(RenderingPipelineTraits.MaterialTemplate.Outfit));
			if (templateMaterial == null)
			{
				Debug.LogError("Template outfit material isn't found!");
				yield break;
			}

			Material outfitMaterial = new Material(templateMaterial);

			string outfitTextureFilename = persistentStorage.GetOutfitFileInDir(outfitDir, outfitName, OutfitFileType.Texture);
			Texture2D outfitTexture = new Texture2D(0, 0);
			outfitTexture.LoadImage(File.ReadAllBytes(outfitTextureFilename));
			outfitMaterial.SetTexture(RenderingPipelineTraits.GetTextureName(), outfitTexture);
			yield return null;

			if (withPbrTextures)
			{
				string metallnessTextureFilename = persistentStorage.GetOutfitFileInDir(outfitDir, outfitName, OutfitFileType.MetallnessMap);
				string roughnessTextureFilename = persistentStorage.GetOutfitFileInDir(outfitDir, outfitName, OutfitFileType.RoughnessMap);
				string normalTextureFilename = persistentStorage.GetOutfitFileInDir(outfitDir, outfitName, OutfitFileType.NormalMap);

				yield return ConfigurePbrTextures(outfitMaterial, normalTextureFilename, metallnessTextureFilename, roughnessTextureFilename);
			}

			executionResult.result = outfitMaterial;
		}

		private Material PrepareHairMaterialWithStandardShader(Texture2D mainTexture)
		{
			Material templateMaterial = Resources.Load<Material>(RenderingPipelineTraits.GetMaterialName(RenderingPipelineTraits.MaterialTemplate.Haircut));
			if (templateMaterial == null)
			{
				Debug.LogError("Template haircut material isn't found!");
				return null;
			}

			Material hairMaterial = new Material(templateMaterial);

			hairMaterial.SetTexture(RenderingPipelineTraits.GetTextureName(), mainTexture);
			hairMaterial.SetTexture("_EmissionMap", mainTexture);

			return hairMaterial;
		}

		private Material PrepareHairMaterialWithAvatarSdkSolidShader(Texture2D mainTexture)
		{
			string name = RenderingPipelineTraits.GetShaderName(RenderingPipelineTraits.ShaderKind.HairSolidLit);
			Shader solidShader = Shader.Find(name);
			if (solidShader == null)
			{
				Debug.LogErrorFormat("{0} shader wasn't found. Use Standard shader for haircut.", ShadersUtils.haircutSolidLitShaderName);
				return PrepareHairMaterialWithStandardShader(mainTexture);
			}

			Material hairMaterial = new Material(solidShader);
			hairMaterial.shader = solidShader;
			hairMaterial.renderQueue = (int)RenderQueue.Transparent;
			hairMaterial.SetTexture("_MainTex", mainTexture);
			return hairMaterial;
		}

		private Material PrepareHairMaterialWithAvatarSdkStrandsShader(Texture2D mainTexture)
		{
			string name = RenderingPipelineTraits.GetShaderName(RenderingPipelineTraits.ShaderKind.HairStrandsLit);
			Shader strandShader = Shader.Find(name);
			if (strandShader == null)
			{
				Debug.LogErrorFormat("{0} shader wasn't found. Use Standard shader for haircut.", ShadersUtils.haircutStrandLitShaderName);
				return PrepareHairMaterialWithStandardShader(mainTexture);
			}

			Material hairMaterial = new Material(strandShader);
			hairMaterial.shader = strandShader;
			hairMaterial.SetTexture("_MainTex", mainTexture);
			return hairMaterial;
		}

		private IEnumerator ConfigurePbrTextures(Material material, string normalMapFilename, string metallicMapFilename, string roughnessMapFilename)
		{
			int metallicWithRoughnessTextureWidth = 0;
			int metallicWithRoughnessTextureHeight = 0;

			Color32[] metallnessColors = null;
			if (File.Exists(metallicMapFilename))
			{
				Texture2D metallnessTexture = new Texture2D(0, 0);
				metallnessTexture.LoadImage(File.ReadAllBytes(metallicMapFilename));
				metallicWithRoughnessTextureWidth = metallnessTexture.width;
				metallicWithRoughnessTextureHeight = metallnessTexture.height;
				metallnessColors = metallnessTexture.GetPixels32();
				Object.DestroyImmediate(metallnessTexture);
				yield return null;
			}
			else
				Debug.LogWarningFormat("Texture not found: {0}", metallicMapFilename);

			Color32[] roughnessColors = null;
			if (File.Exists(roughnessMapFilename))
			{
				Texture2D roughnessTexture = new Texture2D(0, 0);
				roughnessTexture.LoadImage(File.ReadAllBytes(roughnessMapFilename));
				metallicWithRoughnessTextureWidth = roughnessTexture.width;
				metallicWithRoughnessTextureHeight = roughnessTexture.height;
				roughnessColors = roughnessTexture.GetPixels32();
				Object.DestroyImmediate(roughnessTexture);
				yield return null;
			}
			else
				Debug.LogWarningFormat("Texture not found: {0}", roughnessMapFilename);

			if (metallicWithRoughnessTextureWidth > 0 && metallicWithRoughnessTextureHeight > 0)
			{
				Texture2D metallicWithRoughnessTexture = new Texture2D(metallicWithRoughnessTextureWidth, metallicWithRoughnessTextureHeight);
				Color32[] metallicWithRoughnessTextureColors = metallicWithRoughnessTexture.GetPixels32();
				for (int i = 0; i < metallicWithRoughnessTextureColors.Length; i++)
				{
					byte metallValue = metallnessColors == null ? (byte)0 : metallnessColors[i].r;
					metallicWithRoughnessTextureColors[i].r = metallValue;
					metallicWithRoughnessTextureColors[i].g = metallValue;
					metallicWithRoughnessTextureColors[i].b = metallValue;

					metallicWithRoughnessTextureColors[i].a = 255;
					if (roughnessColors != null)
						metallicWithRoughnessTextureColors[i].a -= roughnessColors[i].r;
				}
				metallicWithRoughnessTexture.SetPixels32(metallicWithRoughnessTextureColors);
				metallicWithRoughnessTexture.Apply(true, true);
				yield return null;

				material.SetTexture("_MetallicGlossMap", metallicWithRoughnessTexture);
			}

			if (File.Exists(normalMapFilename))
			{
				Texture2D normalTexture = new Texture2D(0, 0, TextureFormat.DXT5, true, true);
				normalTexture.LoadImage(File.ReadAllBytes(normalMapFilename));
				yield return null;
				material.SetTexture("_BumpMap", normalTexture);
			}

			Resources.UnloadUnusedAssets();
			yield return null;
		}
	}
}
