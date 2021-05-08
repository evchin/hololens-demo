/* Copyright (C) Itseez3D, Inc. - All Rights Reserved
* You may not use this file except in compliance with an authorized license
* Unauthorized copying of this file, via any medium is strictly prohibited
* Proprietary and confidential
* UNLESS REQUIRED BY APPLICABLE LAW OR AGREED BY ITSEEZ3D, INC. IN WRITING, SOFTWARE DISTRIBUTED UNDER THE LICENSE IS DISTRIBUTED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OR
* CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED
* See the License for the specific language governing permissions and limitations under the License.
* Written by Itseez3D, Inc. <support@avatarsdk.com>, April 2017
*/

using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using ItSeez3D.AvatarSdk.Core;

namespace ItSeez3D.AvatarSdkSamples.Core
{
	/// <summary>
	/// Helper class to deal with head blendshape animations.
	/// </summary>
	public class AnimationManager : MonoBehaviour
	{
		#region UI

		public Text currentAnimationText;

		public RuntimeAnimatorController animatorController;

		public string[] animations;

		#endregion

		// animations-related data
		private Animator animator = null;
		private int currentAnimationIdx = 0;

		public void CreateAnimator (GameObject obj)
		{
			ChangeCurrentAnimation(0);

			animator = obj.AddComponent<Animator>();
			animator.applyRootMotion = true;
			animator.runtimeAnimatorController = animatorController;
		}

		public void CreateHumanoidAnimator(GameObject obj, SkinnedMeshRenderer meshRenderer)
		{
			animator = obj.AddComponent<Animator>();
			animator.applyRootMotion = true;
			animator.runtimeAnimatorController = animatorController;
			animator.avatar = AvatarBuilder.BuildHumanAvatar(obj, BuildHumanDescription(meshRenderer));

			currentAnimationIdx = 0;
			if (currentAnimationText != null)
				currentAnimationText.text = animations[currentAnimationIdx].Replace('_', ' ');
		}

		public void OnPrevAnimation ()
		{
			ChangeCurrentAnimation (-1);
		}

		public void OnNextAnimation ()
		{
			ChangeCurrentAnimation (+1);
		}

		public void PlayCurrentAnimation ()
		{
			if (animator != null)
				animator.Play(animations[currentAnimationIdx]);
		}

		private void ChangeCurrentAnimation(int delta)
		{
			var newIdx = currentAnimationIdx + delta;
			if (newIdx < 0)
				newIdx = animations.Length - 1;
			if (newIdx >= animations.Length)
				newIdx = 0;

			currentAnimationIdx = newIdx;
			if (currentAnimationText != null)
				currentAnimationText.text = animations[currentAnimationIdx].Replace('_', ' ');

			PlayCurrentAnimation();
		}

		private HumanDescription BuildHumanDescription(SkinnedMeshRenderer meshRenderer)
		{
			HumanDescription description = new HumanDescription();
			description.armStretch = 0.05f;
			description.legStretch = 0.05f;
			description.upperArmTwist = 0.5f;
			description.lowerArmTwist = 0.5f;
			description.upperLegTwist = 0.5f;
			description.lowerLegTwist = 0.5f;
			description.feetSpacing = 0;

			List<HumanBone> humanBones = new List<HumanBone>();
			TextAsset humanBonesContent = Resources.Load<TextAsset>("human_bones");
			string[] lines = humanBonesContent.text.Split(new string[] { "\r\n" }, System.StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < lines.Length; i++)
			{
				string[] names = lines[i].Split(',');
				humanBones.Add(new HumanBone() { boneName = names[0], humanName = names[1], limit = new HumanLimit() { useDefaultValues = true } });
			}
			description.human = humanBones.ToArray();

			List<Transform> bones = meshRenderer.bones.ToList();
			Matrix4x4[] bindPoses = meshRenderer.sharedMesh.bindposes;
			List<SkeletonBone> skeletonBones = new List<SkeletonBone>();
			for (int i = 0; i < bones.Count; i++)
			{
				Matrix4x4 boneLocalPosition = bindPoses[i].inverse;
				int parentIdx = bones.FindIndex(b => b.name == bones[i].parent.name);
				if (parentIdx > 0)
					boneLocalPosition = boneLocalPosition * bindPoses[parentIdx];

				SkeletonBone bone = new SkeletonBone()
				{
					name = bones[i].name,
					position = boneLocalPosition.GetPosition(),
					rotation = boneLocalPosition.GetRotation(),
					scale = boneLocalPosition.GetScale()
				};

				skeletonBones.Add(bone);
			}
			description.skeleton = skeletonBones.ToArray();

			return description;
		}
	}
}

