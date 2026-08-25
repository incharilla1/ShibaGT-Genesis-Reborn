using GorillaLocomotion;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using BepInEx;
using ShibaGTGenesisReborn.Libs;
using UnityEngine;
using UnityEngine.Networking;

namespace ShibaGTGenesisReborn.Mods.Custom
{
    internal class Vape : MonoBehaviour
    {
        public static Vape Me;
        public static GameObject Obj;
        public static Mesh CM;
        public static Texture2D CT;
        public static bool Down;
        public static bool Done;
        public static bool Held = false;
        public static Transform Hand;
        public static Vector3 OffP;
        public static Quaternion OffR;
        private static ParticleSystem p;
        private static ParticleSystem exhalePS;
        private static Transform mouth;
        private static Transform head;
        private static bool isRightHand = true;
        private static float ignoreTimer = 0f;
        private static float inhaleAmount = 0f;
        private static float maxInhale = 5f;
        private static bool wasInhaling = false;
        public static bool isExhaling = false;
        private static float exhaleTimer = 0f;
        private static float exhaleDuration = 2.0f;
        public static bool showTweakBar = false;
        private static float tweakLevel = 0f;
        private static float maxTweak = 10f;
        private static GameObject tweakBar;
        private static float originalFovMain = -1f;
        private static float originalFovCurrent = -1f;

        static string Dir => ModsLib.GenesisDirectory;
        static string P_Obj => Path.Combine(Dir, "vape.obj");
        static string P_Tex => Path.Combine(Dir, "vape.png");

        public static void InitVape(string u, string t)
        {
            if (!Me)
            {
                GameObject g = new GameObject("Vaper");
                Me = g.AddComponent<Vape>();
                DontDestroyOnLoad(g);
            }
            if (Done && Obj != null)
            {
                Obj.SetActive(true);
                if (Obj.TryGetComponent(out Rigidbody rb))
                {
                    rb.isKinematic = false;
                    rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                }
                return;
            }
            if (!Done && !Down && CM == null)
            {
                Down = true;
                Me.StartCoroutine(Load(u, t));
            }
            else if (!Done && CM != null)
                Spawn();

            if (!Done || Obj == null) return;
            var player = GTPlayer.Instance;
            if (!player) return;
            if (Time.time > ignoreTimer)
            {
                ignoreTimer = Time.time + 1.0f;
                if (Obj.TryGetComponent(out Collider myCol))
                {
                    IgnoreCollisionRecursive(myCol, player.transform);
                    if (GorillaTagger.Instance.offlineVRRig != null)
                        IgnoreCollisionRecursive(myCol, GorillaTagger.Instance.offlineVRRig.transform);
                    if (player.bodyCollider) Physics.IgnoreCollision(myCol, player.bodyCollider, true);
                    if (player.headCollider) Physics.IgnoreCollision(myCol, player.headCollider, true);
                }
            }
        }

        static void IgnoreCollisionRecursive(Collider myCol, Transform target)
        {
            if (!myCol || !target) return;
            foreach (Collider c in target.GetComponentsInChildren<Collider>(true))
                Physics.IgnoreCollision(myCol, c, true);
        }

        public static void Kill()
        {
            if (!Done || Obj == null) return;
            
            if (Obj != null && NetworkingLibrary.Instance != null)
                Obj.UnregisterFromNetwork();
            
            if (p != null && p.isPlaying) p.Stop();
            if (exhalePS != null && exhalePS.isPlaying) exhalePS.Stop();
            isExhaling = false;
            exhaleTimer = 0f;
            wasInhaling = false;
            inhaleAmount = 0f;
            tweakLevel = 0f;
            Held = false;
            if (Obj.TryGetComponent(out Rigidbody rb)) rb.isKinematic = true;
            if (tweakBar != null) tweakBar.SetActive(false);
            Obj.SetActive(false);
        }

        void Update()
        {
            if (!Done || Obj == null) return;

            if (mouth == null)
            {
                GameObject mObj = GameObject.Find("Player Objects/Local VRRig/Local Gorilla Player/rig/head/MouthPosition");
                if (mObj) mouth = mObj.transform;
            }
            if (head == null)
            {
                GameObject hObj = GameObject.Find("Player Objects/Local VRRig/Local Gorilla Player/rig/head");
                if (hObj) head = hObj.transform;
            }

            if (isExhaling && exhalePS != null && mouth != null)
            {
                exhalePS.transform.position = mouth.position;
                if (head != null) exhalePS.transform.rotation = Quaternion.LookRotation(head.forward);
                exhaleTimer -= Time.deltaTime;
                if (exhaleTimer <= 0f)
                {
                    isExhaling = false;
                    inhaleAmount = 0f;
                    exhalePS.Stop();
                    
                    if (NetworkingLibrary.Instance != null && NetworkingLibrary.Instance.NetworkEnabled)
                        Obj.SyncVapeSmoke(false);
                }
            }

            var player = GTPlayer.Instance;
            if (!player) return;

            if (!Hand)
            {
                Hand = player.RightHand.controllerTransform;
                isRightHand = true;
            }

            bool rGrip = InputHandler.Instance.RightGrip.IsPressed;
            bool rTrig = InputHandler.Instance.RightTrigger.IsPressed;
            bool lGrip = InputHandler.Instance.LeftGrip.IsPressed;
            bool lTrig = InputHandler.Instance.LeftTrigger.IsPressed;

            if (Held && Hand != null)
            {
                Obj.transform.position = Hand.TransformPoint(OffP);
                Obj.transform.rotation = Hand.rotation * OffR;
                
                if (NetworkingLibrary.Instance != null && NetworkingLibrary.Instance.NetworkEnabled)
                    Obj.UpdateNetworkPosition();
                
                if (Obj.TryGetComponent(out Rigidbody rb)) rb.isKinematic = true;
                bool trigger = isRightHand ? rTrig : lTrig;
                bool grip = isRightHand ? rGrip : lGrip;
                bool nearMouth = mouth != null && Vector3.Distance(Obj.transform.position, mouth.position) < 0.60f;
                bool inhaling = trigger && nearMouth && Held;

                if (inhaling)
                {
                    inhaleAmount = Mathf.Min(inhaleAmount + Time.deltaTime, maxInhale);
                    if (inhaleAmount > 1.2f)
                    {
                        tweakLevel = Mathf.Min(tweakLevel + (Time.deltaTime * 2f), maxTweak);
                    }
                    wasInhaling = true;
                }

                if (wasInhaling && !nearMouth && !trigger && inhaleAmount > 0.05f && !isExhaling)
                {
                    TriggerExhale();
                    wasInhaling = false;
                }

                if (p != null)
                {
                    if (inhaling && !p.isPlaying) p.Play();
                    else if (!inhaling && p.isPlaying) p.Stop();
                }

                if (!grip)
                {
                    Held = false;
                    if (Obj.TryGetComponent(out Rigidbody releaseRb))
                    {
                        releaseRb.isKinematic = false;
                        releaseRb.linearVelocity = isRightHand
                            ? player.GetHandVelocityTracker(false).GetAverageVelocity(true, 0.05f)
                            : player.GetHandVelocityTracker(true).GetAverageVelocity(true, 0.05f);
                        releaseRb.angularVelocity = UnityEngine.Random.insideUnitSphere * 5f;
                    }
                }
            }
            else
            {
                if (Obj.TryGetComponent(out Rigidbody rb))
                    rb.isKinematic = false;
                if (rGrip && Vector3.Distance(player.RightHand.controllerTransform.position, Obj.transform.position) < 0.15f)
                {
                    Held = true;
                    Hand = player.RightHand.controllerTransform;
                    isRightHand = true;
                    OffP = Hand.InverseTransformPoint(Obj.transform.position);
                    OffR = Quaternion.Inverse(Hand.rotation) * Obj.transform.rotation;
                }
                else if (lGrip && Vector3.Distance(player.LeftHand.controllerTransform.position, Obj.transform.position) < 0.15f)
                {
                    Held = true;
                    Hand = player.LeftHand.controllerTransform;
                    isRightHand = false;
                    OffP = Hand.InverseTransformPoint(Obj.transform.position);
                    OffR = Quaternion.Inverse(Hand.rotation) * Obj.transform.rotation;
                }
            }

            HandleTweaking();
            HandleUIBar(player);
        }

        void HandleTweaking()
        {
            if (tweakLevel > 0)
            {
                tweakLevel = Mathf.Max(0, tweakLevel - (Time.deltaTime * 0.5f));
                float intensity = tweakLevel / maxTweak;

                ApplyTrippyEffect(Camera.main, ref originalFovMain, intensity);
                if (Camera.current != null)
                {
                    ApplyTrippyEffect(Camera.current, ref originalFovCurrent, intensity);
                }
            }
            else
            {
                ResetCamera(Camera.main, ref originalFovMain);
                if (Camera.current != null) ResetCamera(Camera.current, ref originalFovCurrent);
            }
        }

        void ApplyTrippyEffect(Camera cam, ref float origFov, float intensity)
        {
            if (cam == null) return;
            if (origFov < 0) origFov = cam.fieldOfView;

            cam.fieldOfView = origFov + (Mathf.Sin(Time.time * 10f) * 15f * intensity);
            cam.transform.Rotate(
                UnityEngine.Random.Range(-1f, 1f) * intensity * 5f,
                UnityEngine.Random.Range(-1f, 1f) * intensity * 5f,
                Mathf.Sin(Time.time * 5f) * intensity * 5f
            );
        }

        void ResetCamera(Camera cam, ref float origFov)
        {
            if (cam == null || origFov < 0) return;
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, origFov, Time.deltaTime * 5f);
            if (Mathf.Abs(cam.fieldOfView - origFov) < 0.1f)
            {
                cam.fieldOfView = origFov;
                origFov = -1f;
            }
        }

        void HandleUIBar(GTPlayer player)
        {
            if (showTweakBar && tweakLevel > 0.1f)
            {
                if (tweakBar == null)
                {
                    tweakBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Destroy(tweakBar.GetComponent<BoxCollider>());
                    tweakBar.transform.localScale = new Vector3(0.1f, 0.02f, 0.01f);
                    tweakBar.GetComponent<Renderer>().material.shader = Shader.Find("GUI/Text Shader");
                    tweakBar.GetComponent<Renderer>().material.color = Color.green;
                }

                tweakBar.SetActive(true);
                Transform rHand = player.RightHand.controllerTransform;
                tweakBar.transform.position = rHand.position + rHand.up * 0.15f;
                tweakBar.transform.rotation = rHand.rotation;

                float scaleX = (tweakLevel / maxTweak) * 0.15f;
                tweakBar.transform.localScale = new Vector3(scaleX, 0.02f, 0.01f);
                tweakBar.GetComponent<Renderer>().material.color = Color.Lerp(Color.green, Color.red, tweakLevel / maxTweak);
            }
            else if (tweakBar != null)
            {
                tweakBar.SetActive(false);
            }
        }

        public static void TriggerExhale()
        {
            if (exhalePS == null || mouth == null) return;
            isExhaling = true;
            float fillRatio = Mathf.Clamp01(inhaleAmount / maxInhale);

            exhaleDuration = Mathf.Lerp(0.5f, 4.5f, fillRatio);
            exhaleTimer = exhaleDuration;
            exhalePS.transform.position = mouth.position;
            if (head != null) exhalePS.transform.rotation = Quaternion.LookRotation(head.forward);
            var main = exhalePS.main;
            main.startLifetime = Mathf.Lerp(0.8f, 4.5f, fillRatio);
            main.startSpeed = Mathf.Lerp(0.2f, 1.5f, fillRatio);
            main.startSize = Mathf.Lerp(0.05f, 0.45f, fillRatio);
            var emission = exhalePS.emission;
            emission.rateOverTime = Mathf.Lerp(10f, 150f, fillRatio);

            exhalePS.Play();
            
            if (NetworkingLibrary.Instance != null && NetworkingLibrary.Instance.NetworkEnabled)
                Obj.SyncVapeSmoke(true);
        }

        static IEnumerator Load(string u, string t)
        {
            while (GTPlayer.Instance == null || GTPlayer.Instance.RightHand.controllerTransform == null)
                yield return null;

            if (!Directory.Exists(Dir)) Directory.CreateDirectory(Dir);

            string localTex = ModsLib.FindLocalAsset("JUUL_BOI_Color.png", P_Tex, Path.Combine(Dir, "vape.png"), Path.Combine(Paths.PluginPath ?? string.Empty, "files", "vape.png"));
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

            string localObj = ModsLib.FindLocalAsset("juul.obj", P_Obj, Path.Combine(Dir, "vape.obj"), Path.Combine(Paths.PluginPath ?? string.Empty, "files", "vape.obj"));
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
                    Debug.LogError($"Vape parse error: {ex}");
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
            var player = GTPlayer.Instance;
            if (player == null || player.RightHand.controllerTransform == null) return;
            Transform rightHand = player.RightHand.controllerTransform;
            Obj = new GameObject("VapeObject");
            Obj.layer = 8;
            Obj.transform.position = rightHand.position;
            Obj.transform.rotation = rightHand.rotation;
            Obj.transform.localScale = Vector3.one * 2f;
            MeshFilter mf = Obj.AddComponent<MeshFilter>();
            MeshRenderer mr = Obj.AddComponent<MeshRenderer>();
            mf.mesh = CM;
            mr.material = ModsLib.CreateItemMaterial(CT);
            BoxCollider col = Obj.AddComponent<BoxCollider>();
            Rigidbody rb = Obj.AddComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.mass = 0.15f;
            rb.linearDamping = 0.2f;
            rb.angularDamping = 0.2f;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            ModsLib.IgnoreCollisionRecursive(col, player.transform);
            Texture2D smokeT = new Texture2D(64, 64);
            for (int sy = 0; sy < 64; sy++)
                for (int sx = 0; sx < 64; sx++)
                {
                    float d = Vector2.Distance(new Vector2(sx, sy), new Vector2(32, 32));
                    float a = Mathf.Pow(Mathf.Clamp01(1f - (d / 32f)), 3f);
                    smokeT.SetPixel(sx, sy, new Color(1f, 1f, 1f, a));
                }
            smokeT.Apply();
            Material smokeMat = new Material(Shader.Find("Sprites/Default"));
            smokeMat.mainTexture = smokeT;
            GameObject tipObj = new GameObject("S");
            tipObj.transform.SetParent(Obj.transform, false);
            tipObj.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            tipObj.transform.localRotation = Quaternion.Euler(-90, 0, 0);
            p = tipObj.AddComponent<ParticleSystem>();
            var tMain = p.main;
            tMain.startLifetime = 2.779358f;
            tMain.startSpeed = 0.738578f;
            tMain.startSize = 0.1610092f;
            tMain.startColor = new Color(1f, 1f, 1f, 0.02362385f);
            tMain.simulationSpace = ParticleSystemSimulationSpace.World;
            tMain.maxParticles = 2000;
            var tEmission = p.emission;
            tEmission.rateOverTime = 0f;
            var tShape = p.shape;
            tShape.shapeType = ParticleSystemShapeType.Cone;
            tShape.angle = 15f;
            tShape.radius = 0.02f;
            var tSize = p.sizeOverLifetime;
            tSize.enabled = true;
            AnimationCurve tipCurve = new AnimationCurve();
            tipCurve.AddKey(0f, 1f);
            tipCurve.AddKey(1f, 8f);
            tSize.size = new ParticleSystem.MinMaxCurve(1f, tipCurve);
            p.GetComponent<ParticleSystemRenderer>().material = smokeMat;
            p.Stop();
            GameObject exhaleObj = new GameObject("ExhaleSmoke");
            DontDestroyOnLoad(exhaleObj);
            exhalePS = exhaleObj.AddComponent<ParticleSystem>();
            var eMain = exhalePS.main;
            eMain.startLifetime = 2.779358f;
            eMain.startSpeed = 0.738578f;
            eMain.startSize = 0.1610092f;
            eMain.startColor = new Color(1f, 1f, 1f, 0.02362385f);
            eMain.simulationSpace = ParticleSystemSimulationSpace.World;
            eMain.maxParticles = 2000;
            var eEmission = exhalePS.emission;
            eEmission.rateOverTime = 0f;
            var eShape = exhalePS.shape;
            eShape.shapeType = ParticleSystemShapeType.Cone;
            eShape.angle = 15f;
            eShape.radius = 0.02f;
            var eSize = exhalePS.sizeOverLifetime;
            eSize.enabled = true;
            AnimationCurve exhaleCurve = new AnimationCurve();
            exhaleCurve.AddKey(0f, 1f);
            exhaleCurve.AddKey(1f, 8f);
            eSize.size = new ParticleSystem.MinMaxCurve(1f, exhaleCurve);
            exhalePS.GetComponent<ParticleSystemRenderer>().material = smokeMat;
            exhalePS.Stop();
            Done = true;
            Hand = rightHand;
            isRightHand = true;
            OffP = Hand.InverseTransformPoint(Obj.transform.position);
            OffR = Quaternion.Inverse(Hand.rotation) * Obj.transform.rotation;
            if (Obj.TryGetComponent(out Rigidbody dropRb))
            {
                dropRb.isKinematic = false;
                dropRb.linearVelocity = Vector3.zero;
                dropRb.angularVelocity = Vector3.zero;
            }
            Held = false;
            
            if (NetworkingLibrary.Instance != null && NetworkingLibrary.Instance.NetworkEnabled)
                Obj.RegisterForNetwork();
        }
    }
}