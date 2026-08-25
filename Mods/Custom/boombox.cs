using GorillaLocomotion;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BepInEx;
using Photon.Realtime;
using ShibaGTGenesisReborn.Classes;
using ShibaGTGenesisReborn.Libs;
using ShibaGTGenesisReborn.Menu;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

namespace ShibaGTGenesisReborn.Mods.Custom
{
    internal class BoomboxManager : MonoBehaviour
    {
        public static float Volume = 0.4f;
        public static float MaxDistance = 15f;
        public static float SpatialBlend3D = 1.0f;
        public static float SpatialBlendShoulder = 0.0f;
        public static float PitchAndSpeed = 1.0f;
        public static bool UseVisualizer = true;
        public static float VisualizerIntensity = 0.3f;
        public static float BaseScale = 1.25f;
        public static float BodySide = 0.0f;
        public static float BodyHeight = -0.1f;
        public static float BodyDepth = -0.15f;
        public static float BodyRoll = -15f;
        public static Vector3 BackOffset => new Vector3(BodySide, BodyHeight, BodyDepth);

        public static GameObject Obj;
        public static AudioSource Aud;
        public static Texture2D CT;
        public static Mesh CM;
        public static BoomboxManager Me;
        public static bool Done = false;
        public static bool Down = false;
        public static bool Held = false;
        public static bool OnBack = false;
        public static Transform Hand;
        private static bool isRightHand = true;
        private static Vector3 OffP;
        private static Quaternion OffR;
        private static float ignoreTimer = 0f;
        private static float[] samples = new float[256];

        static string Dir => ModsLib.GenesisDirectory;
        static string P_Obj => Path.Combine(Dir, "boombox.obj");
        static string P_Tex => Path.Combine(Dir, "boombox.png");
        public static string P_Aud => Path.Combine(Dir, "boombox_audio.wav");
        public static string BoomboxDirectory => Path.Combine(Dir, "boombox");

        public static void EnsureDirectory()
        {
            if (!Directory.Exists(BoomboxDirectory))
                Directory.CreateDirectory(BoomboxDirectory);
        }

        public static void Initialize()
        {
            EnsureDirectory();
            RefreshSounds(false);
        }

        public static void AdjustVolume(float delta) { Volume = Mathf.Clamp(Volume + delta, 0f, 1f); if (Aud) Aud.volume = Volume; }
        public static void AdjustPitchSpeed(float delta) { PitchAndSpeed = Mathf.Clamp(PitchAndSpeed + delta, 0.5f, 2.0f); if (Aud) Aud.pitch = PitchAndSpeed; }
        public static void ToggleVisualizer() => UseVisualizer = !UseVisualizer;

        public static void BoomboxLoop(string modelUrl, string texUrl)
        {
            if (!Me)
            {
                GameObject g = new GameObject("BoomboxProcessor");
                Me = g.AddComponent<BoomboxManager>();
                DontDestroyOnLoad(g);
            }
            if (!Done && !Down && CM == null) { Down = true; Me.StartCoroutine(DoResources(modelUrl, texUrl)); }
            else if (!Done && CM != null) Spawn();
            if (Done && Obj)
            {
                HandleInteraction();
                HandleVisualizer();
            }
        }

        static void HandleVisualizer()
        {
            if (!UseVisualizer || !Aud || !Aud.isPlaying || !Obj)
            {
                if (Obj)
                    Obj.transform.localScale = Vector3.one * BaseScale;
                return;
            }

            Aud.GetOutputData(samples, 0);
            float sum = 0f;
            for (int i = 0; i < samples.Length; i++)
                sum += samples[i] * samples[i];
            float rms = Mathf.Sqrt(sum / samples.Length);
            float scale = BaseScale + (rms * VisualizerIntensity);
            Obj.transform.localScale = Vector3.one * scale;
        }

        public static void Kill()
        {
            if (Obj != null && NetworkingLibrary.Instance != null)
                Obj.UnregisterFromNetwork();
            
            if (Me) Me.StopAllCoroutines();
            if (Obj) Destroy(Obj);
            Obj = null; Aud = null; CM = null; CT = null;
            Done = false; Down = false; Held = false; OnBack = false;
        }

        static void HandleInteraction()
        {
            var player = GorillaLocomotion.GTPlayer.Instance;
            if (!player) return;

            bool rGrip = InputHandler.Instance.RightGrip.IsPressed;
            bool lGrip = InputHandler.Instance.LeftGrip.IsPressed;

            if (Time.time > ignoreTimer)
            {
                ignoreTimer = Time.time + 1.0f;
                if (Obj.TryGetComponent(out Collider c))
                {
                    ModsLib.IgnoreCollisionRecursive(c, GorillaTagger.Instance.transform);
                    if (GorillaTagger.Instance.offlineVRRig != null)
                        ModsLib.IgnoreCollisionRecursive(c, GorillaTagger.Instance.offlineVRRig.transform);
                }
            }

            if (Held)
            {
                if (!Hand) { Held = false; return; }
                Obj.transform.position = Hand.TransformPoint(OffP);
                Obj.transform.rotation = Hand.rotation * OffR;
                
                if (NetworkingLibrary.Instance != null && NetworkingLibrary.Instance.NetworkEnabled)
                    Obj.UpdateNetworkPosition();

                bool grip = isRightHand ? rGrip : lGrip;
                bool trig = isRightHand ? InputHandler.Instance.RightTrigger.IsPressed : InputHandler.Instance.LeftTrigger.IsPressed;
                if (trig)
                    if (Aud.clip != null && !Aud.isPlaying) 
                    {
                        Aud.Play();
                        if (NetworkingLibrary.Instance != null && NetworkingLibrary.Instance.NetworkEnabled)
                            Obj.SyncBoomboxAudio();
                    }

                if (!grip)
                {
                    Held = false;
                    Transform body = GorillaTagger.Instance.offlineVRRig.transform;
                    if (Vector3.Distance(Obj.transform.position, body.position) < 0.2f)
                    {
                        OnBack = true;
                        Obj.transform.SetParent(body);
                        Obj.transform.localPosition = BackOffset;
                        Obj.transform.localRotation = Quaternion.Euler(0, 0, BodyRoll);
                        if (Obj.TryGetComponent(out Rigidbody rb)) rb.isKinematic = true;
                        Aud.spatialBlend = SpatialBlendShoulder;
                    }
                    else
                    {
                        OnBack = false;
                        if (Obj.TryGetComponent(out Rigidbody rel))
                        {
                            rel.isKinematic = false;
                            rel.linearVelocity = isRightHand ? player.GetHandVelocityTracker(false).GetAverageVelocity(true, 0.05f) : player.GetHandVelocityTracker(true).GetAverageVelocity(true, 0.05f);
                        }
                    }
                }
            }
            else if (!OnBack)
            {
                if (Obj.TryGetComponent(out Rigidbody rb)) rb.isKinematic = false;
            }

            if (!Held)
            {
                if (rGrip && Vector3.Distance(player.RightHand.controllerTransform.position, Obj.transform.position) < 0.15f) Grab(player.RightHand.controllerTransform, true);
                else if (lGrip && Vector3.Distance(player.LeftHand.controllerTransform.position, Obj.transform.position) < 0.15f) Grab(player.LeftHand.controllerTransform, false);
            }
        }

        static void Grab(Transform h, bool right)
        {
            Held = true; OnBack = false; Hand = h; isRightHand = right;
            Obj.transform.SetParent(null);
            if (Obj.TryGetComponent(out Rigidbody rb)) rb.isKinematic = true;
            Aud.spatialBlend = SpatialBlend3D;
            OffP = Hand.InverseTransformPoint(Obj.transform.position);
            OffR = Quaternion.Inverse(Hand.rotation) * Obj.transform.rotation;
        }

        static IEnumerator DoResources(string u, string t)
        {
            EnsureDirectory();

            string localTex = ModsLib.FindLocalAsset("boomboxmesh.png", P_Tex, Path.Combine(Dir, "boombox.png"));
            if (!string.IsNullOrEmpty(localTex))
            {
                CT = new Texture2D(2, 2);
                CT.LoadImage(File.ReadAllBytes(localTex));
            }
            else if (!string.IsNullOrEmpty(t))
            {
                using UnityWebRequest tr = UnityWebRequestTexture.GetTexture(t);
                yield return tr.SendWebRequest();
                if (tr.result == UnityWebRequest.Result.Success)
                {
                    CT = DownloadHandlerTexture.GetContent(tr);
                    File.WriteAllBytes(P_Tex, tr.downloadHandler.data);
                }
            }

            string localObj = ModsLib.FindLocalAsset("boombox.obj", P_Obj);
            string objData = "";
            if (!string.IsNullOrEmpty(localObj))
            {
                objData = File.ReadAllText(localObj);
                if (!File.Exists(P_Obj)) File.WriteAllText(P_Obj, objData);
            }
            else if (!string.IsNullOrEmpty(u))
            {
                using UnityWebRequest r = UnityWebRequest.Get(u);
                yield return r.SendWebRequest();
                if (r.result == UnityWebRequest.Result.Success && !r.downloadHandler.text.StartsWith("<") && !r.downloadHandler.text.StartsWith("404"))
                {
                    objData = r.downloadHandler.text;
                    File.WriteAllText(P_Obj, objData);
                }
            }

            if (!string.IsNullOrEmpty(objData))
            {
                try
                {
                    CM = ModsLib.ParseObj(objData);
                    Spawn();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Boombox parse error: {ex}");
                    Down = false;
                }
            }
            else
            {
                Down = false;
            }
        }

        static void Spawn()
        {
            if (Obj) return;
            var player = GorillaLocomotion.GTPlayer.Instance;
            if (player == null || player.RightHand.controllerTransform == null) return;

            Obj = new GameObject("BoomboxItem");
            Obj.transform.position = player.RightHand.controllerTransform.position;
            Obj.layer = 8;
            Obj.AddComponent<MeshFilter>().mesh = CM;
            var mr = Obj.AddComponent<MeshRenderer>();
            mr.material = ModsLib.CreateItemMaterial(CT);
            BoxCollider col = Obj.AddComponent<BoxCollider>();
            var rb = Obj.AddComponent<Rigidbody>();
            rb.mass = 1f; rb.linearDamping = 0.5f; rb.angularDamping = 0.5f;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            Aud = Obj.AddComponent<AudioSource>();
            Aud.spatialBlend = SpatialBlend3D;
            Aud.loop = true;
            Aud.maxDistance = MaxDistance;
            Aud.volume = Volume;
            Aud.pitch = PitchAndSpeed;
            Obj.transform.localScale = Vector3.one * BaseScale;
            ModsLib.IgnoreCollisionRecursive(col, player.transform);
            if (GorillaTagger.Instance.offlineVRRig != null) ModsLib.IgnoreCollisionRecursive(col, GorillaTagger.Instance.offlineVRRig.transform);
            Done = true;
            
            if (NetworkingLibrary.Instance != null && NetworkingLibrary.Instance.NetworkEnabled)
                Obj.RegisterForNetwork();
        }

        public static void PlayAudioFile(string path)
        {
            if (!File.Exists(path))
            {
                NotificationLib.SendNotification(NotificationLib.NotificationType.Error, "Boombox audio not found");
                return;
            }

            if (!Me)
            {
                GameObject g = new GameObject("BoomboxProcessor");
                Me = g.AddComponent<BoomboxManager>();
                DontDestroyOnLoad(g);
            }

            Me.StartCoroutine(SetAudio(path));
            NotificationLib.SendNotification(NotificationLib.NotificationType.Info, $"Boombox: {Path.GetFileNameWithoutExtension(path)}");
        }

        public static void OpenFolder()
        {
            EnsureDirectory();
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = BoomboxDirectory,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"Open boombox folder error: {ex}");
            }
        }

        public static void RefreshSounds(bool notify = true)
        {
            EnsureDirectory();
            string[] extensions = { "*.mp3", "*.wav", "*.ogg" };
            List<string> audioFiles = new List<string>();

            foreach (string ext in extensions)
            {
                try
                {
                    audioFiles.AddRange(Directory.GetFiles(BoomboxDirectory, ext, SearchOption.TopDirectoryOnly));
                }
                catch { }
            }

            audioFiles = audioFiles.OrderBy(f => Path.GetFileName(f)).ToList();
            List<ButtonInfo> btnList = new List<ButtonInfo>
            {
                new ButtonInfo { buttonText = "Back", method = () => SettingsMods.fun(), isTogglable = false, toolTip = "Return to Fun mods" },
                new ButtonInfo { buttonText = "Refresh Audios", method = () => RefreshSounds(true), isTogglable = false, toolTip = "Rescan boombox folder" },
                new ButtonInfo { buttonText = "Open Folder", method = () => OpenFolder(), isTogglable = false, toolTip = "Open boombox folder in Explorer" },
                new ButtonInfo { buttonText = "Volume +", method = () => AdjustVolume(0.1f), isTogglable = false, toolTip = "Increase volume" },
                new ButtonInfo { buttonText = "Volume -", method = () => AdjustVolume(-0.1f), isTogglable = false, toolTip = "Decrease volume" },
                new ButtonInfo { buttonText = "Speed +", method = () => AdjustPitchSpeed(0.1f), isTogglable = false, toolTip = "Increase speed" },
                new ButtonInfo { buttonText = "Speed -", method = () => AdjustPitchSpeed(-0.1f), isTogglable = false, toolTip = "Decrease speed" }
            };

            foreach (string file in audioFiles)
            {
                string name = Path.GetFileNameWithoutExtension(file);
                string path = file;
                btnList.Add(new ButtonInfo
                {
                    buttonText = name,
                    toolTip = $"Play {name} on boombox",
                    isTogglable = false,
                    method = () => PlayAudioFile(path)
                });
            }

            if (Buttons.buttons.Length > 13)
            {
                Buttons.buttons[13] = btnList.ToArray();
            }

            if (notify)
            {
                NotificationLib.SendNotification(NotificationLib.NotificationType.Info, $"Boombox: {audioFiles.Count} audio(s) found");
                if (Main.buttonsType == 13)
                    Main.RecreateMenu();
            }
        }

        static IEnumerator SetAudio(string p)
        {
            string url = p.Contains("://") ? p : "file://" + p;
            using (UnityWebRequest u = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.UNKNOWN))
            {
                yield return u.SendWebRequest();
                if (u.result == UnityWebRequest.Result.Success) 
                { 
                    if (Aud.clip != null) AudioClip.Destroy(Aud.clip); 
                    Aud.clip = DownloadHandlerAudioClip.GetContent(u); 
                    Aud.Play();
    
                    if (NetworkingLibrary.Instance != null && NetworkingLibrary.Instance.NetworkEnabled)
                    {
                        NetworkingLibrary.Instance.SendEvent("audioclip", ReceiverGroup.Others, 
                            NetworkingLibrary.Instance.FindObjectId(Obj), p);
                        Obj.SyncBoomboxAudio();
                    }
                }
            }
        }
    }
}