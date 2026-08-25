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
using UnityEngine.XR;

namespace ShibaGTGenesisReborn.Mods.Custom
{
    internal class GroshHolder : MonoBehaviour
    {
        public static GroshHolder Me;
        public static GameObject Obj;
        public static AudioSource Aud;
        public static Mesh CM;
        public static Texture2D CT;
        public static bool Down;
        public static bool Done;
        public static bool Held = true;
        public static Transform Hand;
        public static Vector3 OffP;
        public static Quaternion OffR;

        private static bool isRightHand = true;
        private static float ignoreTimer = 0f;

        static string Dir => ModsLib.GenesisDirectory;
        static string P_Obj => Path.Combine(Dir, "grosh.obj");
        static string P_Tex => Path.Combine(Dir, "grosh.png");

        public static void GroshLoop(string u, string t)
        {
            if (!Me)
            {
                GameObject g = new GameObject("GroshHoldable");
                Me = g.AddComponent<GroshHolder>();
                DontDestroyOnLoad(g);
            }

            if (!Done && !Down && CM == null)
            {
                Down = true;
                Me.StartCoroutine(Do(u, t));
            }
            else if (!Done && CM != null) Spawn();

            if (Done && Obj)
            {
                var player = GorillaLocomotion.GTPlayer.Instance;
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

                if (Time.time > ignoreTimer)
                {
                    ignoreTimer = Time.time + 1.0f;
                    if (Obj.TryGetComponent(out Collider myCol))
                    {
                        ModsLib.IgnoreCollisionRecursive(myCol, player.transform);
                        if (GorillaTagger.Instance.offlineVRRig != null)
                            ModsLib.IgnoreCollisionRecursive(myCol, GorillaTagger.Instance.offlineVRRig.transform);
                        if (player.bodyCollider) Physics.IgnoreCollision(myCol, player.bodyCollider, true);
                        if (player.headCollider) Physics.IgnoreCollision(myCol, player.headCollider, true);
                    }
                }

                if (Held)
                {
                    if (Hand == null) { Held = false; return; }

                    Obj.transform.position = Hand.TransformPoint(OffP);
                    Obj.transform.rotation = Hand.rotation * OffR;
                    
                    if (NetworkingLibrary.Instance != null && NetworkingLibrary.Instance.NetworkEnabled)
                        Obj.UpdateNetworkPosition();

                    if (Obj.TryGetComponent(out Rigidbody rb)) rb.isKinematic = true;

                    bool currentTrig = isRightHand ? rTrig : lTrig;
                    bool currentGrip = isRightHand ? rGrip : lGrip;

                    float trigFactor = currentTrig ? 1f : 0f;
                    float sq = 0.6f * (1f - (trigFactor * 0.4f));
                    Obj.transform.localScale = new Vector3(sq, 0.3f, sq);

                    if (currentTrig && !Aud.isPlaying) Aud.Play();

                    if (!currentGrip)
                    {
                        Held = false;
                        if (Obj.TryGetComponent(out Rigidbody releaseRb))
                        {
                            releaseRb.isKinematic = false;
                            Vector3 throwVel = isRightHand
                                ? player.GetHandVelocityTracker(false).GetAverageVelocity(true, 0.05f)
                                : player.GetHandVelocityTracker(true).GetAverageVelocity(true, 0.05f);

                            releaseRb.linearVelocity = throwVel;
                            releaseRb.angularVelocity = UnityEngine.Random.insideUnitSphere * 5f;
                        }
                    }
                }
                else
                {
                    if (Obj.TryGetComponent(out Rigidbody rb)) rb.isKinematic = false;
                    Obj.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);

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
            }
        }

        public static void Kill()
        {
            if (Obj != null && NetworkingLibrary.Instance != null)
                Obj.UnregisterFromNetwork();
            
            if (Obj) Destroy(Obj);
            Done = false; Down = false; Held = true;
            OffP = Vector3.zero; OffR = Quaternion.identity;
        }

        static void Spawn()
        {
            if (Obj) return;
            Obj = new GameObject("GroshItem");
            Obj.layer = 8;

            MeshFilter mf = Obj.AddComponent<MeshFilter>();
            MeshRenderer mr = Obj.AddComponent<MeshRenderer>();
            mf.mesh = CM;
            mr.material = ModsLib.CreateItemMaterial(CT);

            BoxCollider col = Obj.AddComponent<BoxCollider>();
            col.isTrigger = false;

            Rigidbody rb = Obj.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.mass = 1f;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            Aud = Obj.AddComponent<AudioSource>();
            Aud.spatialBlend = 1f;
            Aud.volume = 0.4f;

            Obj.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

            if (GorillaLocomotion.GTPlayer.Instance)
                ModsLib.IgnoreCollisionRecursive(col, GorillaLocomotion.GTPlayer.Instance.transform);

            Done = true;
            
            if (NetworkingLibrary.Instance != null && NetworkingLibrary.Instance.NetworkEnabled)
                Obj.RegisterForNetwork();
        }

        static IEnumerator Do(string u, string t)
        {
            string localTex = ModsLib.FindLocalAsset("iidktexture.png", P_Tex, Path.Combine(Dir, "grosh.png"), Path.Combine(Paths.PluginPath ?? string.Empty, "files", "grosh.png"));
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

            string localObj = ModsLib.FindLocalAsset("Grosh.Holdable.obj", P_Obj, Path.Combine(Dir, "grosh.obj"));
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
                    Debug.LogError($"Grosh parse error: {ex}");
                    Down = false;
                }
            }
            else
            {
                Down = false;
            }
        }
    }
}