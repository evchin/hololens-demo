using ItSeez3D.AvatarSdk.Core;
using ItSeez3D.AvatarSdkSamples.Core;
using Newtonsoft.Json;
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
    public Shader mrtkShader;
    FullbodyAvatarLoader avatarLoader;
    public AnimationManager facialAnimationManager;
    public AnimationManager bodyAnimationManager;
    string photoPath;
    public TextMeshPro progressText;
    protected IAvatarProvider avatarProvider = null;
    private IFullbodyAvatarProvider fullbodyAvatarProvider = null;
    private readonly string generatedHaircutName = "generated";
    private readonly string generatedOutfitName = "outfit_1";
    PipelineType selectedPipelineType = PipelineType.FULLBODY;
    protected string currentAvatarCode = string.Empty;
    protected readonly string AVATAR_OBJECT_NAME = "Avatar";
    Info info;

    protected void OnEnable()
    {
        if (!AvatarSdkMgr.IsInitialized)
        {
            AvatarSdkMgr.Init(sdkType: sdkType);
        }
    }

    private void Start() 
    {
        photoPath = $"{Application.persistentDataPath}/Files/photo.jpg";
        Debug.LogFormat("Current Skin Weights: {0}", QualitySettings.skinWeights);
        QualitySettings.skinWeights = SkinWeights.FourBones;
        Debug.LogFormat("New Skin Weights: {0}", QualitySettings.skinWeights); 
        StartCoroutine(Initialize());
        RenderingPipelineTraits.SetUpTheLighting();
        StartCoroutine(FetchAvatar());
    }

    IEnumerator FetchAvatar()
    {
        yield return GetPhoto("https://emergeholoapi.herokuapp.com/photo");
        yield return GetInfo("https://emergeholoapi.herokuapp.com/info");
        byte[] bytes = File.ReadAllBytes(photoPath);
        yield return GenerateAvatarFunc(bytes);
    }

    IEnumerator GetPhoto(string api)
    {   
        using (UnityWebRequest req = UnityWebRequest.Get(api))
        {
            req.downloadHandler = new DownloadHandlerFile(photoPath);
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.ConnectionError || req.result == UnityWebRequest.Result.ProtocolError)
                Debug.LogError($"{req.error}");
        }
    }

    IEnumerator GetInfo(string api)
    {   
        using (UnityWebRequest req = UnityWebRequest.Get(api))
        {
            yield return req.SendWebRequest();
            info = JsonConvert.DeserializeObject<Info>(req.downloadHandler.text);
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
        facialAnimationManager.CreateAnimator(avatarLoader.GetBodyMeshObject());
        bodyAnimationManager.CreateHumanoidAnimator(avatarObject, avatarLoader.GetBodyMeshObject().GetComponentInChildren<SkinnedMeshRenderer>());
        AvatarController a = avatarObject.AddComponent<AvatarController>();
        a.mrtkShader = mrtkShader;
        // bodyAnimationManager.PlayCurrentAnimation();
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
        computationParameters.outfits.names.Add(generatedOutfitName);
        computationParameters.bodyShape.gender.Value = info.gender;
        if (info.height > 0) computationParameters.bodyShape.height.Value = info.height;
        if (info.weight > 0) computationParameters.bodyShape.weight.Value = info.weight;

        // Generate avatar from the photo and get its code in the Result of request
        var initializeRequest = fullbodyAvatarProvider.InitializeFullbodyAvatarAsync(photoBytes, computationParameters);
        yield return Await(initializeRequest);
        currentAvatarCode = initializeRequest.Result;

        // Wait avatar to be calculated
        var calculateRequest = fullbodyAvatarProvider.StartAndAwaitAvatarCalculationAsync(currentAvatarCode);
        yield return Await(calculateRequest);

        // Download all avatar data from the cloud and store on the local drive
        var gettingAvatarModelRequest = fullbodyAvatarProvider.RetrieveAllAvatarDataFromCloudAsync(currentAvatarCode);
        yield return Await(gettingAvatarModelRequest);

        // var retrievingOutfitRequest = fullbodyAvatarProvider.RetrieveOutfitModelFromCloudAsync(currentAvatarCode, outfitName);
        // yield return Await(retrievingOutfitRequest);

        // FullbodyAvatarLoader is used to display fullbody avatars on the scene.
        avatarLoader = new FullbodyAvatarLoader(AvatarSdkMgr.GetFullbodyAvatarProvider());
        yield return avatarLoader.LoadAvatarAsync(currentAvatarCode);
        avatarLoader.AvatarGameObject.SetActive(false);
        avatarLoader.AvatarGameObject.transform.position = new Vector3(0, -1.4f, 2.68f);
        avatarLoader.AvatarGameObject.transform.eulerAngles = new Vector3(0, 180, 0);
        yield return avatarLoader.LoadOutfitAsync(generatedOutfitName);

        // Show "generated" haircut
        var showOutfitRequest = avatarLoader.ShowOutfitAsync(generatedOutfitName);
        yield return Await(showOutfitRequest);

        var showHaircutRequest = avatarLoader.ShowHaircutAsync(generatedHaircutName);
        yield return Await(showHaircutRequest);

        Transform outfit = avatarLoader.AvatarGameObject.transform.Find(generatedOutfitName);
        Transform haircut = avatarLoader.AvatarGameObject.transform.Find("haircut_generated");
        Transform mesh = avatarLoader.AvatarGameObject.transform.Find("mesh");
        SkinnedMeshRenderer hair_renderer = haircut.GetComponent<SkinnedMeshRenderer>();

        outfit.GetComponent<SkinnedMeshRenderer>().quality = SkinQuality.Bone4;
        mesh.GetComponent<SkinnedMeshRenderer>().quality = SkinQuality.Bone4;
        hair_renderer.quality = SkinQuality.Bone4;
        hair_renderer.sharedMaterial.shader = mrtkShader;
        hair_renderer.sharedMaterial.SetFloat("_Mode", 2);

        avatarObject = avatarLoader.AvatarGameObject;
        avatarObject.AddComponent<MoveByMouse>();

        // Transform hair = avatarObject.transform.Find("haircut_generated");
        // hair.GetComponent<SkinnedMeshRenderer>().sharedMaterial.shader = mrtkShader;
        
        progressText.gameObject.SetActive(false);
        avatarLoader.AvatarGameObject.SetActive(true);

        // HaircutAppearanceController hairController = hair.GetComponent<HaircutAppearanceController>();
        // hairController.haircutMeshRenderer.sharedMaterial.shader = mrtkShader;
        // hairController.haircutMeshRenderer.sharedMaterial.SetFloat("_Mode", 2);
    }
}

public struct Info
{
    // public AvatarAgeGroup ageGroup;
    public AvatarGender gender;
    public float height;
    public float weight;
}