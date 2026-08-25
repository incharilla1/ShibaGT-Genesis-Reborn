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
    internal class FatSealSpammer : MonoBehaviour
    {
        public static FatSealSpammer Me;
        public static Mesh CM;
        public static Texture2D CT;
        public static bool Down;
        public static bool Done;
        public static List<GameObject> Seals = new List<GameObject>();
        public static PhysicsMaterial BouncyMat;

        static float lastSpawn = 0f;
        const float SpawnInterval = 0.08f;
        const float BaseScale = 2f;

        static string Dir => ModsLib.GenesisDirectory;
        static string P_Obj => Path.Combine(Dir, "fatseal.obj");
        static string P_Tex => Path.Combine(Dir, "fatseal.png");

        public static void SealLoop(string u, string t)
        {
            if (!Me)
            {
                GameObject g = new GameObject("FatSealSpammer");
                Me = g.AddComponent<FatSealSpammer>();
                DontDestroyOnLoad(g);
            }

            if (!Done && !Down && CM == null)
            {
                Down = true;
                Me.StartCoroutine(Load(u, t));
            }

            if (!Done) return;

            if (XRSettings.isDeviceActive)
            {
                bool vrPressed = InputHandler.Instance != null
                    && InputHandler.Instance.RightTrigger.IsPressed;
                if (vrPressed && Time.time - lastSpawn >= SpawnInterval)
                {
                    lastSpawn = Time.time;
                    SpawnSealVR();
                }
            }
            else
            {
                bool pcPressed = UnityInput.Current.GetMouseButton(0);
                if (pcPressed && Time.time - lastSpawn >= SpawnInterval)
                {
                    lastSpawn = Time.time;
                    SpawnSealPC();
                }
            }
        }

        static void SpawnSealVR()
        {
            var player = GTPlayer.Instance;
            if (!player) return;
            Transform hand = player.RightHand.controllerTransform;
            if (!hand) return;

            Vector3 origin = hand.position + hand.forward * 0.15f;
            Vector3 velocity = hand.forward * 6f + UnityEngine.Random.insideUnitSphere * 0.5f;
            BuildSeal(origin, hand.rotation, velocity);
        }

        static void SpawnSealPC()
        {
            var player = GTPlayer.Instance;
            if (!player) return;
            Camera cam = Camera.main;
            if (!cam) return;

            Ray ray = cam.ScreenPointToRay(UnityInput.Current.mousePosition);
            Vector3 target;
            if (Physics.Raycast(ray, out RaycastHit hit, 500f)) target = hit.point;
            else target = ray.origin + ray.direction * 30f;

            Vector3 origin = cam.transform.position + cam.transform.forward * 0.5f + cam.transform.right * 0.3f;
            Vector3 dir = (target - origin).normalized;
            Vector3 velocity = dir * 18f + UnityEngine.Random.insideUnitSphere * 0.3f;
            Quaternion rot = Quaternion.LookRotation(dir);
            BuildSeal(origin, rot, velocity);
        }

        static void BuildSeal(Vector3 position, Quaternion rotation, Vector3 velocity)
        {
            var player = GTPlayer.Instance;
            if (!player) return;

            GameObject seal = new GameObject("FatSeal");
            seal.layer = 8;
            seal.transform.position = position;
            seal.transform.rotation = rotation;
            seal.transform.localScale = new Vector3(BaseScale, BaseScale, BaseScale);

            seal.AddComponent<MeshFilter>().mesh = CM;
            MeshRenderer mr = seal.AddComponent<MeshRenderer>();
            mr.material = ModsLib.CreateItemMaterial(CT);

            MeshCollider col = seal.AddComponent<MeshCollider>();
            col.convex = true;
            col.sharedMesh = CM;

            if (BouncyMat == null)
            {
                BouncyMat = new PhysicsMaterial("FatSealBouncy")
                {
                    bounciness = 1f,
                    dynamicFriction = 0f,
                    staticFriction = 0f,
                    bounceCombine = PhysicsMaterialCombine.Maximum,
                    frictionCombine = PhysicsMaterialCombine.Minimum,
                };
            }
            col.material = BouncyMat;

            Rigidbody rb = seal.AddComponent<Rigidbody>();
            rb.mass = 1.5f;
            rb.linearDamping = 0f;
            rb.angularDamping = 0.05f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.linearVelocity = velocity;
            rb.angularVelocity = UnityEngine.Random.insideUnitSphere * 8f;

            ModsLib.IgnoreCollisionRecursive(col, player.transform);
            if (GorillaTagger.Instance && GorillaTagger.Instance.offlineVRRig != null)
                ModsLib.IgnoreCollisionRecursive(col, GorillaTagger.Instance.offlineVRRig.transform);
            if (player.bodyCollider) Physics.IgnoreCollision(col, player.bodyCollider, true);
            if (player.headCollider) Physics.IgnoreCollision(col, player.headCollider, true);

            Seals.Add(seal);
            
            if (NetworkingLibrary.Instance != null && NetworkingLibrary.Instance.NetworkEnabled)
                seal.RegisterForNetwork();
        }

        public static void Kill()
        {
            for (int i = 0; i < Seals.Count; i++)
            {
                if (Seals[i] != null && NetworkingLibrary.Instance != null)
                    Seals[i].UnregisterFromNetwork();
                if (Seals[i]) Destroy(Seals[i]);
            }
            Seals.Clear();
        }

        void FixedUpdate()
        {
            for (int i = 0; i < Seals.Count; i++)
            {
                GameObject s = Seals[i];
                if (!s) continue;
                if (!s.TryGetComponent(out Rigidbody rb)) continue;

                if (rb.linearVelocity.sqrMagnitude < 0.5f)
                {
                    Vector3 kick = new Vector3(
                        UnityEngine.Random.Range(-2f, 2f),
                        UnityEngine.Random.Range(5f, 8f),
                        UnityEngine.Random.Range(-2f, 2f));
                    rb.AddForce(kick, ForceMode.VelocityChange);
                    
                    if (NetworkingLibrary.Instance != null && NetworkingLibrary.Instance.NetworkEnabled)
                        s.UpdateNetworkPosition();
                }
            }
        }

        static IEnumerator Load(string u, string t)
        {
            string localTex = ModsLib.FindLocalAsset("fatseal.jpeg", P_Tex, Path.Combine(Dir, "fatseal.png"), Path.Combine(Paths.PluginPath ?? string.Empty, "files", "fatseal.png"));
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

            string localObj = ModsLib.FindLocalAsset("fatseal.obj", P_Obj);
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
                    Done = true;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Seal parse error: {ex}");
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