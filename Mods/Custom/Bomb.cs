using BepInEx;
using GorillaLocomotion;
using ShibaGTGenesisReborn.Libs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;

namespace ShibaGTGenesisReborn.Mods
{
    internal class BombManager : MonoBehaviour
    {
        public static BombManager Instance;

        private const string ObjUrl = "https://raw.githubusercontent.com/incharilla1/assets/refs/heads/main/Bomb.obj";
        private const string TextureUrl = "https://raw.githubusercontent.com/incharilla1/assets/refs/heads/main/Bomb.png";
        private const string AudioUrl = "https://github.com/incharilla1/assets/raw/refs/heads/main/explode_bomb.mp3";

        private static string GenesisDir => ModsLib.GenesisDirectory;
        private static string LocalObjPath => Path.Combine(GenesisDir, "Bomb.obj");
        private static string LocalTexturePath => Path.Combine(GenesisDir, "Bomb.png");
        private static string LocalAudioPath => Path.Combine(GenesisDir, "explode_bomb.mp3");

        public static Mesh bombMesh;
        public static Texture2D bombTexture;
        public static Material bombMaterial;
        public static AudioClip bombAudioClip;

        public static bool isLoaded;
        public static bool isDownloading;

        public static GameObject activeBombObject;
        private static Vector3 activeBombPosition;
        private static float detonationTimestamp;
        private static bool isBombActive;
        private static bool wasGripPressedLastFrame;

        private const float BombScale = 0.35f;
        private const float FuseDurationSeconds = 3.0f;

        public static void BombLoop()
        {
            EnsureInitialized();

            if (!isLoaded && !isDownloading && bombMesh == null)
            {
                isDownloading = true;
                Instance.StartCoroutine(DownloadAndLoadAssets());
            }

            if (!isLoaded || bombMesh == null)
            {
                return;
            }

            bool isGripPressed = CheckRightGripPressed();

            if (isBombActive)
            {
                if (Time.time >= detonationTimestamp)
                {
                    DetonateBomb();
                }
                wasGripPressedLastFrame = isGripPressed;
                return;
            }

            if (isGripPressed && !wasGripPressedLastFrame)
            {
                SpawnBomb();
            }

            wasGripPressedLastFrame = isGripPressed;
        }

        private static void SpawnBomb()
        {
            Vector3 handPosition = GetRightHandPosition();
            Quaternion handRotation = GetRightHandRotation();

            activeBombObject = CreateBombGameObject(handPosition, handRotation);
            activeBombPosition = handPosition;
            detonationTimestamp = Time.time + FuseDurationSeconds;
            isBombActive = true;
        }

        private static GameObject CreateBombGameObject(Vector3 position, Quaternion rotation)
        {
            GameObject bomb = new GameObject("CustomBomb");
            bomb.transform.position = position;
            bomb.transform.rotation = rotation;
            bomb.transform.localScale = Vector3.one * BombScale;

            MeshFilter meshFilter = bomb.AddComponent<MeshFilter>();
            meshFilter.mesh = bombMesh;

            MeshRenderer meshRenderer = bomb.AddComponent<MeshRenderer>();
            meshRenderer.material = bombMaterial;

            if (NetworkingLibrary.Instance != null && NetworkingLibrary.Instance.NetworkEnabled)
            {
                bomb.RegisterForNetwork();
            }

            return bomb;
        }

        private static void DetonateBomb()
        {
            isBombActive = false;

            Vector3 explosionPosition = activeBombPosition;

            if (activeBombObject != null)
            {
                explosionPosition = activeBombObject.transform.position;

                if (NetworkingLibrary.Instance != null && NetworkingLibrary.Instance.NetworkEnabled)
                {
                    activeBombObject.UnregisterFromNetwork();
                }

                Destroy(activeBombObject);
                activeBombObject = null;
            }

            if (bombAudioClip != null)
            {
                GameObject explosionAudioSource = new GameObject("BombExplosionAudio");
                explosionAudioSource.transform.position = explosionPosition;

                AudioSource audioSource = explosionAudioSource.AddComponent<AudioSource>();
                audioSource.clip = bombAudioClip;
                audioSource.volume = 1.0f;
                audioSource.spatialBlend = 1.0f;
                audioSource.minDistance = 2f;
                audioSource.maxDistance = 150f;
                audioSource.rolloffMode = AudioRolloffMode.Linear;
                audioSource.Play();

                Destroy(explosionAudioSource, bombAudioClip.length + 0.5f);
            }
        }

        public static void Kill()
        {
            isBombActive = false;
            wasGripPressedLastFrame = false;

            if (activeBombObject != null)
            {
                if (NetworkingLibrary.Instance != null && NetworkingLibrary.Instance.NetworkEnabled)
                {
                    activeBombObject.UnregisterFromNetwork();
                }

                Destroy(activeBombObject);
                activeBombObject = null;
            }
        }

        private static bool CheckRightGripPressed()
        {
            if (GunLib.IsXRDeviceActive())
            {
                return InputHandler.Instance != null && InputHandler.Instance.RightGrip.IsPressed;
            }

            return (Mouse.current?.rightButton.isPressed ?? false) || UnityInput.Current.GetKey(KeyCode.E) || UnityInput.Current.GetMouseButton(1);
        }

        private static Vector3 GetRightHandPosition()
        {
            if (GTPlayer.Instance != null)
            {
                Transform rightController = GTPlayer.Instance.RightHand.controllerTransform;
                if (rightController != null)
                {
                    return rightController.position + rightController.rotation * GTPlayer.Instance.RightHand.handOffset;
                }
            }

            if (GorillaTagger.Instance != null && GorillaTagger.Instance.rightHandTransform != null)
            {
                return GorillaTagger.Instance.rightHandTransform.position;
            }

            return Vector3.zero;
        }

        private static Quaternion GetRightHandRotation()
        {
            if (GTPlayer.Instance != null)
            {
                Transform rightController = GTPlayer.Instance.RightHand.controllerTransform;
                if (rightController != null)
                {
                    return rightController.rotation * GTPlayer.Instance.RightHand.handRotOffset;
                }
            }

            if (GorillaTagger.Instance != null && GorillaTagger.Instance.rightHandTransform != null)
            {
                return GorillaTagger.Instance.rightHandTransform.rotation;
            }

            return Quaternion.identity;
        }

        private static void EnsureInitialized()
        {
            if (!Instance)
            {
                GameObject managerObject = new GameObject("BombManager");
                Instance = managerObject.AddComponent<BombManager>();
                DontDestroyOnLoad(managerObject);
            }
        }

        private static IEnumerator DownloadAndLoadAssets()
        {
            if (!Directory.Exists(GenesisDir))
            {
                Directory.CreateDirectory(GenesisDir);
            }

            string localAudio = ModsLib.FindLocalAsset("explode_bomb.mp3", LocalAudioPath);
            if (!string.IsNullOrEmpty(localAudio))
            {
                using UnityWebRequest localAudioRequest = UnityWebRequestMultimedia.GetAudioClip("file://" + Path.GetFullPath(localAudio), AudioType.MPEG);
                yield return localAudioRequest.SendWebRequest();
                if (localAudioRequest.result == UnityWebRequest.Result.Success)
                    bombAudioClip = DownloadHandlerAudioClip.GetContent(localAudioRequest);
            }
            else if (!string.IsNullOrEmpty(AudioUrl))
            {
                using UnityWebRequest audioRequest = UnityWebRequestMultimedia.GetAudioClip(AudioUrl, AudioType.MPEG);
                yield return audioRequest.SendWebRequest();
                if (audioRequest.result == UnityWebRequest.Result.Success)
                {
                    bombAudioClip = DownloadHandlerAudioClip.GetContent(audioRequest);
                    File.WriteAllBytes(LocalAudioPath, audioRequest.downloadHandler.data);
                }
            }

            string localTex = ModsLib.FindLocalAsset("Bomb.png", LocalTexturePath);
            if (!string.IsNullOrEmpty(localTex))
            {
                bombTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                bombTexture.LoadImage(File.ReadAllBytes(localTex));
            }
            else if (!string.IsNullOrEmpty(TextureUrl))
            {
                using UnityWebRequest textureRequest = UnityWebRequestTexture.GetTexture(TextureUrl);
                yield return textureRequest.SendWebRequest();
                if (textureRequest.result == UnityWebRequest.Result.Success)
                {
                    bombTexture = DownloadHandlerTexture.GetContent(textureRequest);
                    File.WriteAllBytes(LocalTexturePath, textureRequest.downloadHandler.data);
                }
            }

            string localObj = ModsLib.FindLocalAsset("Bomb.obj", LocalObjPath);
            string objContent = "";
            if (!string.IsNullOrEmpty(localObj))
            {
                objContent = File.ReadAllText(localObj);
            }
            else if (!string.IsNullOrEmpty(ObjUrl))
            {
                using UnityWebRequest objRequest = UnityWebRequest.Get(ObjUrl);
                yield return objRequest.SendWebRequest();
                if (objRequest.result == UnityWebRequest.Result.Success)
                {
                    objContent = objRequest.downloadHandler.text;
                    File.WriteAllText(LocalObjPath, objContent);
                }
            }

            if (!string.IsNullOrEmpty(objContent) && !objContent.StartsWith("<"))
            {
                bombMesh = ModsLib.ParseObj(objContent);
                bombMaterial = ModsLib.CreateItemMaterial(bombTexture);
                isLoaded = true;
            }

            isDownloading = false;
        }
    }
}
