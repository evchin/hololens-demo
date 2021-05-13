using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AvatarController : MonoBehaviour
{
    public Shader mrtkShader;
    Animator animator;
    int isWalkingHash;
    int isReachingOutHash;
    int isSmilingHash;

    void Start()
    {
        animator = GetComponent<Animator>();
        isWalkingHash = Animator.StringToHash("isWalking");
        isReachingOutHash = Animator.StringToHash("isReachingOut");
        isSmilingHash = Animator.StringToHash("isSmiling");

        // hair
        SkinnedMeshRenderer hair = transform.Find("haircut_generated").GetComponent<SkinnedMeshRenderer>();
        hair.sharedMaterial.shader = mrtkShader;
        hair.sharedMaterial.SetFloat("_Mode", 2);
    }

    void Update()
    {
        bool isWalking = animator.GetBool(isWalkingHash);
        bool isReachingOut = animator.GetBool(isReachingOutHash);
        bool isSmiling = animator.GetBool(isSmilingHash);
        bool close = transform.position.z < 1.4f;

        if (!isSmiling)
        {
            animator.SetBool(isSmilingHash, true);
        }
        if (!isWalking && !close)
        {
            animator.SetBool(isWalkingHash, true);
        }
        if (isWalking && close)
        {
            animator.SetBool(isWalkingHash, false);
        }
        if (!isReachingOut && !isWalking && close)
        {
            animator.SetBool(isReachingOutHash, true);
        }
    }
}
