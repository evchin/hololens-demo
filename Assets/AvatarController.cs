using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AvatarController : MonoBehaviour
{
    Animator animator;
    int isWalkingHash;
    int isReachingOutHash;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        isWalkingHash = Animator.StringToHash("isWalking");
        isReachingOutHash = Animator.StringToHash("isReachingOut");
    }

    // Update is called once per frame
    void Update()
    {
        bool isWalking = animator.GetBool(isWalkingHash);
        bool isReachingOut = animator.GetBool(isReachingOutHash);
        bool close = transform.position.z < 1.4f;

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
