using ItSeez3D.AvatarSdk.Core;
using ItSeez3D.AvatarSdkSamples.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class LoadAvatar : MonoBehaviour
{
    public SdkType sdkType;
    private GameObject avatarObject = null;
    public RuntimeAnimatorController controller;
    public Avatar avatar;
    string photoPath;
    public TextMeshProUGUI progressText;
    protected IAvatarProvider avatarProvider = null;
    private IFullbodyAvatarProvider fullbodyAvatarProvider = null;
    private readonly string generatedHaircutName = "generated";
    PipelineType selectedPipelineType = PipelineType.FULLBODY;
    protected string currentAvatarCode = string.Empty;
    protected readonly string AVATAR_OBJECT_NAME = "Avatar";
    string outfitName;

    protected void OnEnable()
    {
        if (!AvatarSdkMgr.IsInitialized)
        {
            AvatarSdkMgr.Init(sdkType: sdkType);
        }
    }

    private void Start() 
    {
        outfitName = "outfit_1";
        photoPath = $"{Application.persistentDataPath}/Files/photo.jpg";
        StartCoroutine(Initialize());
        RenderingPipelineTraits.SetUpTheLighting();
        StartCoroutine(FetchAvatar());
    }

    IEnumerator FetchAvatar()
    {
        yield return GetDataRequest("https://emergeholoapi.herokuapp.com/photo");
        byte[] bytes = File.ReadAllBytes(photoPath);
        yield return GenerateAvatarFunc(bytes);
    }

    IEnumerator GetDataRequest(string api)
    {   
        using (UnityWebRequest req = UnityWebRequest.Get(api))
        {
            req.downloadHandler = new DownloadHandlerFile(photoPath);
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.ConnectionError || req.result == UnityWebRequest.Result.ProtocolError)
                Debug.LogError($"{req.error}");
        }
    }

    protected IEnumerator GenerateAvatarFunc(byte[] photoBytes)
    {
        var avatarObject = GameObject.Find(AVATAR_OBJECT_NAME);
        Destroy(avatarObject);
        yield return StartCoroutine(GenerateAndDisplayHead(photoBytes, selectedPipelineType));
        AnimateAvatar();
    }

    private void AnimateAvatar()
    {
        Transform skeleton = avatarObject.gameObject.transform.Find("skeleton");
        Animator animator = skeleton.gameObject.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.avatar = avatar;
        animator.applyRootMotion = true;
        AvatarController control = skeleton.gameObject.AddComponent<AvatarController>();
    }

    protected IEnumerator Await(params AsyncRequest[] requests)
    {
        foreach (var r in requests)
            while (!r.IsDone)
            {
                // yield null to wait until next frame (to avoid blocking the main thread)
                yield return null;

                // This function will throw on any error. Such primitive error handling only provided as
                // an example, the production app probably should be more clever about it.
                if (r.IsError)
                {
                    Debug.LogError(r.ErrorMessage);
                    progressText.text = r.ErrorMessage;
                    throw new Exception(r.ErrorMessage);
                }

                // Each requests may or may not contain "subrequests" - the asynchronous subtasks needed to
                // complete the request. The progress for the requests can be tracked overall, as well as for
                // every subtask. The code below shows how to recursively iterate over current subtasks
                // to display progress for them.
                var progress = new List<string>();
                AsyncRequest request = r;
                while (request != null)
                {
                    progress.Add(string.Format("{0}: {1}%", request.State, request.ProgressPercent.ToString("0.0")));
                    request = request.CurrentSubrequest;
                }
                progressText.text = string.Join("\n", progress.ToArray());
            }
    }

    IEnumerator Initialize()
    {
        fullbodyAvatarProvider = AvatarSdkMgr.GetFullbodyAvatarProvider();
        avatarProvider = fullbodyAvatarProvider; 
        yield return Await(avatarProvider.InitializeAsync());
        yield return CheckIfFullbodyPipelineAvailable();
    }

    protected IEnumerator GenerateAndDisplayHead(byte[] photoBytes, PipelineType pipeline)
    {
        if (avatarObject != null)
            DestroyImmediate(avatarObject);

        // Create computation parameters.
        // By default GLTF format is used for fullbody avatars.
        FullbodyAvatarComputationParameters computationParameters = new FullbodyAvatarComputationParameters();
        // Request "generated" haircut to be computed.
        computationParameters.haircuts.names.Add(generatedHaircutName);
        computationParameters.outfits.names.Add(outfitName);

        // Generate avatar from the photo and get its code in the Result of request
        var initializeRequest = fullbodyAvatarProvider.InitializeFullbodyAvatarAsync(photoBytes, computationParameters);
        yield return Await(initializeRequest);
        currentAvatarCode = initializeRequest.Result;

        // StartCoroutine(SampleUtils.DisplayPhotoPreview(currentAvatarCode, photoPreview));

        // Wait avatar to be calculated
        var calculateRequest = fullbodyAvatarProvider.StartAndAwaitAvatarCalculationAsync(currentAvatarCode);
        yield return Await(calculateRequest);

        // Download all avatar data from the cloud and store on the local drive
        var gettingAvatarModelRequest = fullbodyAvatarProvider.RetrieveAllAvatarDataFromCloudAsync(currentAvatarCode);
        yield return Await(gettingAvatarModelRequest);

        // var retrievingOutfitRequest = fullbodyAvatarProvider.RetrieveOutfitModelFromCloudAsync(currentAvatarCode, outfitName);
        // yield return Await(retrievingOutfitRequest);

        // FullbodyAvatarLoader is used to display fullbody avatars on the scene.
        FullbodyAvatarLoader avatarLoader = new FullbodyAvatarLoader(AvatarSdkMgr.GetFullbodyAvatarProvider());
        yield return avatarLoader.LoadAvatarAsync(currentAvatarCode);
        avatarLoader.AvatarGameObject.SetActive(false);
        avatarLoader.AvatarGameObject.transform.position = new Vector3(0, -1.5f, 2.68f);
        avatarLoader.AvatarGameObject.transform.eulerAngles = new Vector3(0, 180, 0);
        yield return avatarLoader.LoadOutfitAsync(outfitName);

        // Show "generated" haircut
        var showOutfitRequest = avatarLoader.ShowOutfitAsync(outfitName);
        yield return Await(showOutfitRequest);

        var showHaircutRequest = avatarLoader.ShowHaircutAsync(generatedHaircutName);
        yield return Await(showHaircutRequest);

        avatarLoader.AvatarGameObject.SetActive(true);
        avatarObject = avatarLoader.AvatarGameObject;
        avatarObject.AddComponent<MoveByMouse>();
        progressText.gameObject.SetActive(false);
    }

    private IEnumerator CheckIfFullbodyPipelineAvailable()
    {
        // Fullbody avatars are available on the Pro plan. Need to verify it.
        var pipelineAvailabilityRequest = avatarProvider.IsPipelineSupportedAsync(selectedPipelineType);
        yield return Await(pipelineAvailabilityRequest);
        if (pipelineAvailabilityRequest.IsError)
            yield break;

        if (pipelineAvailabilityRequest.Result == true)
        {
            progressText.text = string.Empty;
        }
        else
        {
            string errorMsg = "You can't generate fullbody avatars.\nThis option is available on the PRO plan.";
            progressText.text = errorMsg;
            progressText.color = Color.red;
            Debug.LogError(errorMsg);
        }
    }
}
