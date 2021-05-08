/* Copyright (C) Itseez3D, Inc. - All Rights Reserved
* You may not use this file except in compliance with an authorized license
* Unauthorized copying of this file, via any medium is strictly prohibited
* Proprietary and confidential
* UNLESS REQUIRED BY APPLICABLE LAW OR AGREED BY ITSEEZ3D, INC. IN WRITING, SOFTWARE DISTRIBUTED UNDER THE LICENSE IS DISTRIBUTED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OR
* CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED
* See the License for the specific language governing permissions and limitations under the License.
* Written by Itseez3D, Inc. <support@avatarsdk.com>, November 2020
*/

using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ItSeez3D.AvatarSdk.Core
{
	public class FullbodyPersistentStorage : BasePersistentStorage, IFullbodyPersistentStorage
	{
		private Dictionary<FullbodyAvatarFileType, string> avatarFiles = new Dictionary<FullbodyAvatarFileType, string>()
		{
			{ FullbodyAvatarFileType.Model, "model.gltf" },
			{ FullbodyAvatarFileType.Texture, "model.jpg" },
			{ FullbodyAvatarFileType.NormalMap, "normal_map.png" },
			{ FullbodyAvatarFileType.MetallnessMap, "metallic_map.png"},
			{ FullbodyAvatarFileType.RoughnessMap, "roughness_map.png"},
			{ FullbodyAvatarFileType.ExportDataJson, "export_data.json" },
			{ FullbodyAvatarFileType.ModelInfo, "model.json" }
		};

		private Dictionary<OutfitFileType, string> outfitFiles = new Dictionary<OutfitFileType, string>()
		{
			{ OutfitFileType.Model, "model.gltf"},
			{ OutfitFileType.Texture, "{0}.png" },
			{ OutfitFileType.NormalMap, "{0}_normal_map.png" },
			{ OutfitFileType.MetallnessMap, "{0}_metallic_map.png" },
			{ OutfitFileType.RoughnessMap, "{0}_roughness_map.png" },
			{ OutfitFileType.BodyVisibilityMask, "{0}_body_visibility_mask.png" }
		};

		public string GetAvatarsDirectory()
		{
			return EnsureDirectoryExists(Path.Combine(GetDataDirectory(), "avatars"));
		}

		public string GetCommonFilesDirectory()
		{
			return EnsureDirectoryExists(Path.Combine(GetDataDirectory(), "common_files"));
		}

		public string GetAvatarDirectory(string avatarCode)
		{
			return Path.Combine(GetAvatarsDirectory(), avatarCode);
		}

		public string GetAvatarFile(string avatarCode, FullbodyAvatarFileType fileType)
		{
			if (fileType == FullbodyAvatarFileType.ExportDataJson)
				return Path.Combine(GetAvatarDirectory(avatarCode), avatarFiles[fileType]);
			else
				return Path.Combine(GetAvatarDirectory(avatarCode), "avatar", avatarFiles[fileType]);
		}

		public string GetHaircutFile(string avatarCode, string haircutName, FullbodyHaircutFileType fileType)
		{
			return Path.Combine(GetAvatarDirectory(avatarCode), haircutName, GetHaircutFileName(haircutName, fileType));
		}

		public string GetHaircutFileInDir(string haircutDir, string haircutName, FullbodyHaircutFileType fileType)
		{
			return Path.Combine(haircutDir, GetHaircutFileName(haircutName, fileType));
		}

		public string GetOutfitFile(string avatarCode, string outfitName, OutfitFileType outfitFileType)
		{
			return Path.Combine(GetAvatarDirectory(avatarCode), outfitName, string.Format(outfitFiles[outfitFileType], outfitName));
		}

		public string GetOutfitFileInDir(string outfitDir, string outfitName, OutfitFileType outfitFileType)
		{
			return Path.Combine(outfitDir, string.Format(outfitFiles[outfitFileType], outfitName));
		}

		public string GetCommonFilePath(string relativePath)
		{
			return Path.Combine(GetCommonFilesDirectory(), relativePath);
		}

		private string GetHaircutFileName(string haircutName, FullbodyHaircutFileType fileType)
		{
			switch (fileType)
			{
				case FullbodyHaircutFileType.Model:
					return "model.gltf";
				case FullbodyHaircutFileType.Texture:
					return string.Format("{0}.png", haircutName);
				default:
					{
						Debug.LogErrorFormat("Unknown FullbodyHaircutFileType: {0}", fileType);
						return string.Empty;
					}
			}
		}
	}
}
