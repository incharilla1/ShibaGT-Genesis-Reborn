using BepInEx;
using GorillaLocomotion;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using ShibaGTGenesisReborn.Libs;
using UnityEngine;
using UnityEngine.Networking;

namespace ShibaGTGenesisReborn.Mods.Custom
{
    internal class MaxwellHolder : MonoBehaviour
    {
        public static MaxwellHolder Me;
        public static GameObject Obj;
        public static AudioSource Aud;
        public static AudioClip MeowClip;
        public static Mesh CM;
        public static Texture2D CT;
        public static bool Down;
        public static bool Done;
        public static bool Held = true;
        public static Transform Hand;
        public static Vector3 OffP;
        public static Quaternion OffR;

        public const string DefaultModelUrl = "https://raw.githubusercontent.com/incharilla1/assets/refs/heads/main/maxwell.obj";
        public const string DefaultTextureUrl = "https://raw.githubusercontent.com/incharilla1/assets/refs/heads/main/Maxwell.png";
        public const string DefaultAudioUrl = "https://github.com/incharilla1/assets/raw/refs/heads/main/meow.mp3";

        private static bool isRightHand = true;
        private static float ignoreTimer = 0f;
        private const float DefaultScale = 0.5f;

        static string GenesisDirectory => ModsLib.GenesisDirectory;
        static string CachedObjPath => Path.Combine(GenesisDirectory, "maxwell.obj");
        static string CachedTexturePath => Path.Combine(GenesisDirectory, "maxwell.png");
        static string CachedAudioPath => Path.Combine(GenesisDirectory, "meow.mp3");

        public static void DownloadAssets(
            string modelUrl = DefaultModelUrl,
            string textureUrl = DefaultTextureUrl,
            string audioUrl = DefaultAudioUrl)
        {
            EnsureHostExists();

            if (!Done && !Down && (CM == null || CT == null || MeowClip == null))
            {
                Down = true;
                Me.StartCoroutine(DownloadAndInitializeAssets(modelUrl, textureUrl, audioUrl));
            }
        }

        public static void CatLoop(
            string modelUrl = DefaultModelUrl,
            string textureUrl = DefaultTextureUrl,
            string audioUrl = DefaultAudioUrl)
        {
            EnsureHostExists();

            if (!Done && !Down && CM == null)
            {
                Down = true;
                Me.StartCoroutine(DownloadAndInitializeAssets(modelUrl, textureUrl, audioUrl));
            }
            else if (!Done && CM != null)
            {
                Spawn();
            }

            if (Done && Obj)
            {
                GTPlayer player = GTPlayer.Instance;
                if (!player) return;

                if (!Hand)
                {
                    Hand = player.RightHand.controllerTransform;
                    isRightHand = true;
                }

                bool isRightGripPressed = InputHandler.Instance.RightGrip.IsPressed;
                bool isRightTriggerPressed = InputHandler.Instance.RightTrigger.IsPressed;
                bool isLeftGripPressed = InputHandler.Instance.LeftGrip.IsPressed;
                bool isLeftTriggerPressed = InputHandler.Instance.LeftTrigger.IsPressed;

                if (Time.time > ignoreTimer)
                {
                    ignoreTimer = Time.time + 1.0f;
                    if (Obj.TryGetComponent(out Collider catCollider))
                    {
                        IgnoreCollisionRecursive(catCollider, player.transform);
                        if (GorillaTagger.Instance.offlineVRRig != null)
                            IgnoreCollisionRecursive(catCollider, GorillaTagger.Instance.offlineVRRig.transform);
                        if (player.bodyCollider) Physics.IgnoreCollision(catCollider, player.bodyCollider, true);
                        if (player.headCollider) Physics.IgnoreCollision(catCollider, player.headCollider, true);
                    }
                }

                if (Held)
                {
                    if (Hand == null)
                    {
                        Held = false;
                        return;
                    }

                    Obj.transform.position = Hand.TransformPoint(OffP);
                    Obj.transform.rotation = Hand.rotation * OffR;

                    if (NetworkingLibrary.Instance != null && NetworkingLibrary.Instance.NetworkEnabled)
                        Obj.UpdateNetworkPosition();

                    if (Obj.TryGetComponent(out Rigidbody rb))
                        rb.isKinematic = true;

                    bool currentTrigger = isRightHand ? isRightTriggerPressed : isLeftTriggerPressed;
                    bool currentGrip = isRightHand ? isRightGripPressed : isLeftGripPressed;

                    float triggerFactor = currentTrigger ? 1f : 0f;
                    float squishScale = DefaultScale * (1f - (triggerFactor * 0.35f));
                    Obj.transform.localScale = new Vector3(squishScale, DefaultScale, squishScale);

                    if (!currentGrip)
                    {
                        Held = false;
                        if (Obj.TryGetComponent(out Rigidbody releaseRb))
                        {
                            releaseRb.isKinematic = false;
                            Vector3 throwVelocity = isRightHand
                                ? player.GetHandVelocityTracker(false).GetAverageVelocity(true, 0.05f)
                                : player.GetHandVelocityTracker(true).GetAverageVelocity(true, 0.05f);

                            releaseRb.linearVelocity = throwVelocity;
                            releaseRb.angularVelocity = UnityEngine.Random.insideUnitSphere * 5f;
                        }
                        PlayMeowSound();
                    }
                }
                else
                {
                    if (Obj.TryGetComponent(out Rigidbody rb))
                        rb.isKinematic = false;

                    Obj.transform.localScale = new Vector3(DefaultScale, DefaultScale, DefaultScale);

                    float rightHandDistance = Vector3.Distance(player.RightHand.controllerTransform.position, Obj.transform.position);
                    float leftHandDistance = Vector3.Distance(player.LeftHand.controllerTransform.position, Obj.transform.position);

                    if (isRightGripPressed && rightHandDistance < 0.25f)
                    {
                        Held = true;
                        Hand = player.RightHand.controllerTransform;
                        isRightHand = true;
                        OffP = Hand.InverseTransformPoint(Obj.transform.position);
                        OffR = Quaternion.Inverse(Hand.rotation) * Obj.transform.rotation;
                        PlayMeowSound();
                    }
                    else if (isLeftGripPressed && leftHandDistance < 0.25f)
                    {
                        Held = true;
                        Hand = player.LeftHand.controllerTransform;
                        isRightHand = false;
                        OffP = Hand.InverseTransformPoint(Obj.transform.position);
                        OffR = Quaternion.Inverse(Hand.rotation) * Obj.transform.rotation;
                        PlayMeowSound();
                    }
                }
            }
        }

        private static void EnsureHostExists()
        {
            if (!Me)
            {
                GameObject holderObject = new GameObject("MaxwellHolder");
                Me = holderObject.AddComponent<MaxwellHolder>();
                DontDestroyOnLoad(holderObject);
            }
        }

        private static void PlayMeowSound()
        {
            if (MeowClip == null) return;

            if (Aud != null)
            {
                Aud.PlayOneShot(MeowClip);
            }
        }

        static void IgnoreCollisionRecursive(Collider myCol, Transform target)
        {
            if (!myCol || !target) return;
            foreach (Collider targetCollider in target.GetComponentsInChildren<Collider>(true))
                Physics.IgnoreCollision(myCol, targetCollider, true);
        }

        public static void Kill()
        {
            if (Obj != null && NetworkingLibrary.Instance != null)
                Obj.UnregisterFromNetwork();

            if (Obj) Destroy(Obj);
            Done = false;
            Down = false;
            Held = true;
            OffP = Vector3.zero;
            OffR = Quaternion.identity;
        }

        static void Spawn()
        {
            if (Obj) return;
            Obj = new GameObject("Maxwell");
            Obj.layer = 8;

            MeshFilter meshFilter = Obj.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = Obj.AddComponent<MeshRenderer>();
            meshFilter.mesh = CM;

            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("GorillaTag/UberShader"));
            if (!CT) CT = Texture2D.whiteTexture;

            material.mainTexture = CT;
            material.SetTexture("_MainTex", CT);
            material.SetTexture("_BaseMap", CT);
            material.color = Color.white;
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", 0f);
            material.SetFloat("_Glossiness", 0f);
            material.SetFloat("_Metallic", 0f);
            meshRenderer.material = material;

            MeshCollider meshCollider = Obj.AddComponent<MeshCollider>();
            meshCollider.convex = true;
            meshCollider.sharedMesh = CM;

            Rigidbody rb = Obj.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.mass = 1f;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            Aud = Obj.AddComponent<AudioSource>();
            Aud.spatialBlend = 1f;
            Aud.volume = 0.6f;
            if (MeowClip != null)
                Aud.clip = MeowClip;

            Obj.transform.localScale = new Vector3(DefaultScale, DefaultScale, DefaultScale);

            if (GTPlayer.Instance)
                ModsLib.IgnoreCollisionRecursive(meshCollider, GTPlayer.Instance.transform);

            Done = true;

            PlayMeowSound();

            if (NetworkingLibrary.Instance != null && NetworkingLibrary.Instance.NetworkEnabled)
                Obj.RegisterForNetwork();
        }

        static IEnumerator DownloadAndInitializeAssets(string modelUrl, string textureUrl, string audioUrl)
        {
            string localAudio = ModsLib.FindLocalAsset("meow.mp3", CachedAudioPath);
            if (!string.IsNullOrEmpty(localAudio))
            {
                using UnityWebRequest audioRequest = UnityWebRequestMultimedia.GetAudioClip("file://" + Path.GetFullPath(localAudio), AudioType.MPEG);
                yield return audioRequest.SendWebRequest();
                if (audioRequest.result == UnityWebRequest.Result.Success)
                {
                    MeowClip = DownloadHandlerAudioClip.GetContent(audioRequest);
                    if (Aud != null && Aud.clip == null)
                        Aud.clip = MeowClip;
                }
            }
            else if (!string.IsNullOrEmpty(audioUrl))
            {
                using UnityWebRequest audioRequest = UnityWebRequestMultimedia.GetAudioClip(audioUrl, AudioType.MPEG);
                yield return audioRequest.SendWebRequest();
                if (audioRequest.result == UnityWebRequest.Result.Success)
                {
                    MeowClip = DownloadHandlerAudioClip.GetContent(audioRequest);
                    File.WriteAllBytes(CachedAudioPath, audioRequest.downloadHandler.data);
                    if (Aud != null && Aud.clip == null)
                        Aud.clip = MeowClip;
                }
            }

            string localTexture = ModsLib.FindLocalAsset("Maxwell.png", CachedTexturePath, Path.Combine(GenesisDirectory, "maxwell.png"), Path.Combine(Paths.PluginPath ?? string.Empty, "files", "maxwell.png"));
            if (!string.IsNullOrEmpty(localTexture))
            {
                CT = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                CT.LoadImage(File.ReadAllBytes(localTexture));
                CT.wrapMode = TextureWrapMode.Clamp;
                CT.filterMode = FilterMode.Bilinear;
            }
            else if (!string.IsNullOrEmpty(textureUrl))
            {
                UnityWebRequest textureRequest = UnityWebRequestTexture.GetTexture(textureUrl);
                yield return textureRequest.SendWebRequest();
                if (textureRequest.result == UnityWebRequest.Result.Success)
                {
                    CT = DownloadHandlerTexture.GetContent(textureRequest);
                    CT.wrapMode = TextureWrapMode.Clamp;
                    CT.filterMode = FilterMode.Bilinear;
                    File.WriteAllBytes(CachedTexturePath, textureRequest.downloadHandler.data);
                }
            }

            string localObj = ModsLib.FindLocalAsset("maxwell.obj", CachedObjPath, Path.Combine(Paths.PluginPath ?? string.Empty, "files", "maxwell.obj"));
            string objData = string.Empty;
            if (!string.IsNullOrEmpty(localObj))
            {
                objData = File.ReadAllText(localObj);
                if (!File.Exists(CachedObjPath))
                {
                    File.WriteAllText(CachedObjPath, objData);
                }
            }
            else if (!string.IsNullOrEmpty(modelUrl))
            {
                UnityWebRequest modelRequest = UnityWebRequest.Get(modelUrl);
                yield return modelRequest.SendWebRequest();
                if (modelRequest.result == UnityWebRequest.Result.Success)
                {
                    objData = modelRequest.downloadHandler.text;
                    File.WriteAllText(CachedObjPath, objData);
                }
            }

            if (!string.IsNullOrEmpty(objData) && !objData.StartsWith("<"))
            {
                CM = ModsLib.ParseObj(objData);
                Spawn();
            }
            else
            {
                Down = false;
            }
        }
    }
}
