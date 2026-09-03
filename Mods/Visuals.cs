using System;
using System.Collections.Generic;
using System.Linq;
using GorillaLocomotion;
using GorillaNetworking;
using HarmonyLib;
using Photon.Pun;
using Photon.Realtime;
using ShibaGTGenesisReborn.Classes;
using ShibaGTGenesisReborn.Libs;
using ShibaGTGenesisReborn.Menu;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ShibaGTGenesisReborn.Mods
{
    public partial class mods
    {
        public static void Tracers()
        {
            foreach (VRRig rig in VRRigCache.ActiveRigs)
            {
                if (!rig.isLocal)
                {
                    GameObject g = new GameObject("Line");
                    LineRenderer l = g.AddComponent<LineRenderer>();
                    l.startWidth = 0.01f;
                    l.endWidth = 0.01f;
                    l.positionCount = 2;
                    l.useWorldSpace = true;
                    l.SetPosition(0, GorillaLocomotion.GTPlayer.Instance.RightHand.controllerTransform.position);
                    l.SetPosition(1, rig.transform.position);
                    l.material.shader = Shader.Find("GUI/Text Shader");
                    l.startColor = rig.playerColor;
                    l.endColor = rig.playerColor;
                    Object.Destroy(g, Time.deltaTime * 2f);
                }
            }
        }

        public static void BeaconESP()
        {
            foreach (VRRig rig in VRRigCache.ActiveRigs)
            {
                if (rig == null || rig.isLocal)
                    continue;

                GameObject obj = new GameObject("BeaconESP");
                LineRenderer line = obj.AddComponent<LineRenderer>();
                line.startWidth = 0.1f;
                line.endWidth = 0.1f;
                line.positionCount = 2;
                line.useWorldSpace = true;
                line.SetPosition(0, rig.transform.position);
                line.SetPosition(1, rig.transform.position + Vector3.up * 500f);
                line.material.shader = Shader.Find("GUI/Text Shader");
                line.startColor = rig.playerColor;
                line.endColor = rig.playerColor;
                Object.Destroy(obj, Time.deltaTime * 2f);
            }
        }

        public static void FullBodyESP()
        {
            foreach (VRRig vrrig in VRRigCache.ActiveRigs)
            {
                if (!vrrig.isLocal)
                {
                    if (vrrig.mainSkin.material.name.Contains("fected") || vrrig.mainSkin.material.name.Contains("It"))
                    {
                        vrrig.mainSkin.material.shader = Shader.Find("GUI/Text Shader");
                        vrrig.mainSkin.material.color = new Color32(255, 0, 0, 100);
                    }
                    else
                    {
                        vrrig.mainSkin.material.shader = Shader.Find("GUI/Text Shader");
                        vrrig.mainSkin.material.color = new Color32(0, 255, 0, 100);
                    }
                }
            }
        }

        public static void CasualFullBodyESP()
        {
            foreach (VRRig vrrig in VRRigCache.ActiveRigs)
            {
                if (!vrrig.isLocal)
                {
                    vrrig.mainSkin.material.shader = Shader.Find("GUI/Text Shader");
                    vrrig.mainSkin.material.color = vrrig.playerColor;
                }
            }
        }

        public static void DisableFullBodyESP()
        {
            foreach (VRRig vrrig in VRRigCache.ActiveRigs)
            {
                if (vrrig != VRRig.LocalRig && vrrig.mainSkin.material.shader == Shader.Find("GUI/Text Shader"))
                {
                    vrrig.mainSkin.material.shader = Shader.Find("GorillaTag/UberShader");
                }
            }
        }

        public static void RGB(bool strobe = false)
        {
            if (!NetworkSystem.Instance.InRoom) return;
            Color c = strobe ? new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value) : Color.HSVToRGB(Mathf.Repeat(Time.time * 0.2f, 1f), 1f, 1f);
            GorillaTagger.Instance.myVRRig.SendRPC("RPC_InitializeNoobMaterial", RpcTarget.All, c.r, c.g, c.b);
        }

        private static Color GetESPColor(VRRig rig, bool infection)
        {
            if (!infection) return rig.playerColor;
            bool isTagged = rig.mainSkin != null && rig.mainSkin.material != null &&
                (rig.mainSkin.material.name.Contains("fected") || rig.mainSkin.material.name.Contains("It"));
            return isTagged ? Color.red : Color.green;
        }

        public static void SkeletonESP(bool infection = false)
        {
            foreach (VRRig rig in VRRigCache.ActiveRigs)
            {
                if (rig == null || rig.isLocal)
                    continue;

                Color col = GetESPColor(rig, infection);
                Vector3 head = rig.headConstraint != null ? rig.headConstraint.position : rig.transform.position + Vector3.up * 0.5f;
                Vector3 spine = rig.transform.position + Vector3.up * 0.1f;
                Vector3 leftHand = rig.leftHandTransform != null ? rig.leftHandTransform.position : spine;
                Vector3 rightHand = rig.rightHandTransform != null ? rig.rightHandTransform.position : spine;
                Vector3 basePos = rig.transform.position - Vector3.up * 0.2f;

                DrawLine(head, spine, col);
                DrawLine(spine, leftHand, col);
                DrawLine(spine, rightHand, col);
                DrawLine(spine, basePos, col);
            }
        }

        public static void InfectionSkeletonESP() => SkeletonESP(true);

        public static bool filledESP;

        public static void BoxESP(bool infection = false)
        {
            foreach (VRRig rig in VRRigCache.ActiveRigs)
            {
                if (rig == null || rig.isLocal)
                    continue;

                Color col = GetESPColor(rig, infection);
                Vector3 center = rig.transform.position;
                Vector3 extents = new Vector3(0.35f, 0.45f, 0.35f);

                if (filledESP)
                {
                    GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Object.Destroy(cube.GetComponent<Collider>());
                    cube.name = "3DBoxESP_Filled";
                    cube.transform.position = center;
                    cube.transform.localScale = extents * 2f;

                    Renderer rend = cube.GetComponent<Renderer>();
                    rend.material.shader = Shader.Find("GUI/Text Shader");
                    rend.material.color = new Color(col.r, col.g, col.b, 0.35f);
                    Object.Destroy(cube, Time.deltaTime * 2f);
                }

                Vector3 c0 = center + new Vector3(-extents.x, -extents.y, -extents.z);
                Vector3 c1 = center + new Vector3(extents.x, -extents.y, -extents.z);
                Vector3 c2 = center + new Vector3(extents.x, -extents.y, extents.z);
                Vector3 c3 = center + new Vector3(-extents.x, -extents.y, extents.z);

                Vector3 c4 = center + new Vector3(-extents.x, extents.y, -extents.z);
                Vector3 c5 = center + new Vector3(extents.x, extents.y, -extents.z);
                Vector3 c6 = center + new Vector3(extents.x, extents.y, extents.z);
                Vector3 c7 = center + new Vector3(-extents.x, extents.y, extents.z);

                DrawLine(c0, c1, col);
                DrawLine(c1, c2, col);
                DrawLine(c2, c3, col);
                DrawLine(c3, c0, col);

                DrawLine(c4, c5, col);
                DrawLine(c5, c6, col);
                DrawLine(c6, c7, col);
                DrawLine(c7, c4, col);

                DrawLine(c0, c4, col);
                DrawLine(c1, c5, col);
                DrawLine(c2, c6, col);
                DrawLine(c3, c7, col);
            }
        }

        public static void InfectionBoxESP() => BoxESP(true);

        public static void TwoDBoxESP(bool infection = false)
        {
            Camera cam = Camera.main != null ? Camera.main : GorillaTagger.Instance.mainCamera.GetComponent<Camera>();
            foreach (VRRig rig in VRRigCache.ActiveRigs)
            {
                if (rig == null || rig.isLocal)
                    continue;

                Color col = GetESPColor(rig, infection);
                Vector3 center = rig.transform.position;
                Vector3 extents = new Vector3(0.325f, 0.425f, 0f);

                if (filledESP)
                {
                    GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    Object.Destroy(quad.GetComponent<Collider>());
                    quad.name = "2DBoxESP";
                    quad.transform.position = center;
                    quad.transform.localScale = new Vector3(0.65f, 0.85f, 1f);
                    if (cam != null)
                        quad.transform.rotation = cam.transform.rotation;

                    Renderer rend = quad.GetComponent<Renderer>();
                    rend.material.shader = Shader.Find("GUI/Text Shader");
                    rend.material.color = new Color(col.r, col.g, col.b, 0.45f);
                    Object.Destroy(quad, Time.deltaTime * 2f);
                }

                if (cam != null)
                {
                    Vector3 right = cam.transform.right * extents.x;
                    Vector3 up = cam.transform.up * extents.y;

                    Vector3 tl = center - right + up;
                    Vector3 tr = center + right + up;
                    Vector3 br = center + right - up;
                    Vector3 bl = center - right - up;

                    DrawLine(tl, tr, col);
                    DrawLine(tr, br, col);
                    DrawLine(br, bl, col);
                    DrawLine(bl, tl, col);
                }
            }
        }

        public static void InfectionTwoDBoxESP() => TwoDBoxESP(true);

        private static readonly List<GameObject> wireframePool = new List<GameObject>();
        private static int wireframePoolIndex = 0;

        private static GameObject GetPooledWireSphere()
        {
            while (wireframePoolIndex < wireframePool.Count)
            {
                GameObject obj = wireframePool[wireframePoolIndex++];
                if (obj != null)
                {
                    obj.SetActive(true);
                    return obj;
                }
            }

            GameObject sphere = new GameObject("PooledWireSphere");
            sphere.hideFlags = HideFlags.HideAndDontSave;
            Shader shader = Shader.Find("GUI/Text Shader");

            CreateCircleRenderer(sphere, "XY", Vector3.forward, shader);
            CreateCircleRenderer(sphere, "XZ", Vector3.up, shader);
            CreateCircleRenderer(sphere, "YZ", Vector3.right, shader);

            wireframePool.Add(sphere);
            wireframePoolIndex = wireframePool.Count;
            return sphere;
        }

        private static void CreateCircleRenderer(GameObject parent, string name, Vector3 normal, Shader shader)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent.transform, false);
            LineRenderer lr = child.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.startWidth = 0.012f;
            lr.endWidth = 0.012f;
            lr.material = new Material(shader);

            int segments = 12;
            lr.positionCount = segments;
            Quaternion rot = Quaternion.FromToRotation(Vector3.forward, normal);
            for (int i = 0; i < segments; i++)
            {
                float rad = (i / (float)segments) * Mathf.PI * 2f;
                Vector3 p = rot * new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);
                lr.SetPosition(i, p);
            }
        }

        private static void SetWireSphere(GameObject sphere, Vector3 position, float radius, Color color)
        {
            if (sphere == null) return;
            sphere.transform.position = position;
            sphere.transform.localScale = Vector3.one * radius;
            LineRenderer[] renderers = sphere.GetComponentsInChildren<LineRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].startColor = color;
                renderers[i].endColor = color;
            }
        }

        private static void ResetWireframePool()
        {
            for (int i = wireframePoolIndex; i < wireframePool.Count; i++)
            {
                if (wireframePool[i] != null && wireframePool[i].activeSelf)
                    wireframePool[i].SetActive(false);
            }
            wireframePoolIndex = 0;
        }

        public static void WireframeHitboxESP(bool infection = false)
        {
            float mult = hitboxExpander ? hitboxExpanderMultiplier : 1f;
            wireframePoolIndex = 0;

            foreach (VRRig rig in VRRigCache.ActiveRigs)
            {
                if (rig == null || rig.isLocal) continue;

                Color col = GetESPColor(rig, infection);
                Vector3 head = rig.headConstraint != null ? rig.headConstraint.position : rig.transform.position + Vector3.up * 0.45f;
                Vector3 body = rig.transform.position;
                Vector3 leftHand = rig.leftHandTransform != null ? rig.leftHandTransform.position : body;
                Vector3 rightHand = rig.rightHandTransform != null ? rig.rightHandTransform.position : body;

                SetWireSphere(GetPooledWireSphere(), head, 0.22f * mult, col);
                SetWireSphere(GetPooledWireSphere(), body, 0.32f * mult, col);
                SetWireSphere(GetPooledWireSphere(), leftHand, 0.12f * mult, col);
                SetWireSphere(GetPooledWireSphere(), rightHand, 0.12f * mult, col);
            }

            ResetWireframePool();
        }

        public static void InfectionWireframeHitboxESP() => WireframeHitboxESP(true);

        public static void DisableWireframeHitboxESP()
        {
            for (int i = 0; i < wireframePool.Count; i++)
            {
                if (wireframePool[i] != null)
                    Object.Destroy(wireframePool[i]);
            }
            wireframePool.Clear();
            wireframePoolIndex = 0;
        }

        public static void NameAndDistanceTags()
        {
            Camera cam = Camera.main != null ? Camera.main : GorillaTagger.Instance.mainCamera.GetComponent<Camera>();
            foreach (VRRig rig in VRRigCache.ActiveRigs)
            {
                if (rig == null || rig.isLocal) continue;
                
                Player player = RigManager.GetPlayerFromVRRig(rig);
                NetPlayer netPlayer = RigManager.GetNetPlayerFromVRRig(rig);
                string name = player != null ? player.NickName : (!string.IsNullOrEmpty(rig.playerNameVisible) ? rig.playerNameVisible : "Player");
                float dist = Vector3.Distance(GorillaTagger.Instance.bodyCollider.transform.position, rig.transform.position);
                int fps = rig.fps;

                GameObject tagObj = new GameObject("NameTagESP");
                Vector3 headPos = (rig.headConstraint != null ? rig.headConstraint.position : rig.transform.position) + Vector3.up * 0.35f;
                tagObj.transform.position = headPos;
                if (cam != null)
                    tagObj.transform.LookAt(tagObj.transform.position + cam.transform.rotation * Vector3.forward, cam.transform.rotation * Vector3.up);

                TextMesh tm = tagObj.AddComponent<TextMesh>();
                tm.text = $"{name} [{dist:F1}m] [{fps} FPS]";
                tm.fontSize = 42;
                tm.characterSize = 0.02f;
                tm.alignment = TextAlignment.Center;
                tm.anchor = TextAnchor.MiddleCenter;
                tm.color = rig.playerColor;

                bool isSteam = ModsLib.IsSteamUser(netPlayer);
                Material platformMat = isSteam ? ModsLib.GetSteamMaterial() : ModsLib.GetMetaMaterial();

                if (platformMat != null)
                {
                    GameObject platformQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    Object.Destroy(platformQuad.GetComponent<Collider>());
                    platformQuad.name = isSteam ? "SteamPlatformQuad" : "MetaPlatformQuad";
                    platformQuad.transform.SetParent(tagObj.transform, false);
                    platformQuad.transform.localPosition = new Vector3(0f, 0.12f, 0f);
                    platformQuad.transform.localRotation = Quaternion.identity;
                    platformQuad.transform.localScale = new Vector3(0.24f, 0.24f, 1f);

                    Renderer quadRenderer = platformQuad.GetComponent<Renderer>();
                    if (quadRenderer != null)
                    {
                        quadRenderer.sharedMaterial = platformMat;
                    }
                }

                Object.Destroy(tagObj, Time.deltaTime * 2f);
            }
        }

        private static void DrawLine(Vector3 start, Vector3 end, Color col)
        {
            GameObject obj = new GameObject("ESPLine");
            LineRenderer lr = obj.AddComponent<LineRenderer>();
            lr.startWidth = 0.012f;
            lr.endWidth = 0.012f;
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            lr.SetPosition(0, start);
            lr.SetPosition(1, end);
            lr.material.shader = Shader.Find("GUI/Text Shader");
            lr.startColor = col;
            lr.endColor = col;
            Object.Destroy(obj, Time.deltaTime * 2f);
        }

        private struct MaterialState
        {
            public Shader OriginalShader;
            public Color OriginalColor;
            public bool HasColor;
            public Color OriginalBaseColor;
            public bool HasBaseColor;
        }

        [Setting] public static int cursedIndex = 4;
        public static readonly string[] cursedNames = { "Void", "Glitch", "Blood", "Acid", "Off" };
        private static readonly Dictionary<Material, MaterialState> originalMaterialStates = new Dictionary<Material, MaterialState>();
        private static bool originalFog;
        private static Color originalFogColor;
        private static Color originalAmbientLight;
        private static bool savedLighting;

        public static void CursedGTAG()
        {
            Main.Change("cursedgtag", ref cursedIndex, cursedNames, () =>
            {
                if (!savedLighting)
                {
                    originalFog = RenderSettings.fog;
                    originalFogColor = RenderSettings.fogColor;
                    originalAmbientLight = RenderSettings.ambientLight;
                    savedLighting = true;
                }

                if (cursedIndex == 4)
                {
                    FixShaders();
                    if (BetterDayNightManager.instance != null)
                    {
                        BetterDayNightManager.instance.UnsetTimeIndexOverrideFunction();
                        BetterDayNightManager.instance.ClearTimeOfDay(true);
                        BetterDayNightManager.instance.UpdateTimeOfDay(true);
                    }
                    return;
                }

                ApplyCursedShaders(cursedIndex);
            }, "Cursed: ");
        }

        private static void ApplyCursedShaders(int mode)
        {
            Shader textShader = Shader.Find("GUI/Text Shader") ?? Shader.Find("Sprites/Default");
            Shader spritesShader = Shader.Find("Sprites/Default") ?? Shader.Find("GUI/Text Shader");
            Shader unlitShader = Shader.Find("UI/Default") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard");

            Renderer[] allRenderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);

            for (int i = 0; i < allRenderers.Length; i++)
            {
                Renderer renderer = allRenderers[i];
                if (renderer == null)
                    continue;

                Material[] materials = renderer.sharedMaterials;
                if (materials == null)
                    continue;

                for (int m = 0; m < materials.Length; m++)
                {
                    Material material = materials[m];
                    if (material == null)
                        continue;

                    if (!originalMaterialStates.ContainsKey(material))
                    {
                        originalMaterialStates[material] = new MaterialState
                        {
                            OriginalShader = material.shader,
                            OriginalColor = material.HasProperty("_Color") ? material.color : Color.white,
                            HasColor = material.HasProperty("_Color"),
                            OriginalBaseColor = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : Color.white,
                            HasBaseColor = material.HasProperty("_BaseColor")
                        };
                    }

                    switch (mode)
                    {
                        case 0:
                            material.shader = textShader;
                            if (material.HasProperty("_Color"))
                                material.color = new Color(0f, 0f, 0f, 0.9f);
                            break;
                        case 1:
                            material.shader = (i % 2 == 0) ? textShader : spritesShader;
                            if (material.HasProperty("_Color"))
                                material.color = (i % 3 == 0) ? Color.magenta : ((i % 2 == 0) ? Color.cyan : Color.yellow);
                            break;
                        case 2:
                            material.shader = textShader;
                            if (material.HasProperty("_Color"))
                                material.color = new Color(0.8f, 0f, 0f, 0.85f);
                            break;
                        case 3:
                            material.shader = unlitShader;
                            if (material.HasProperty("_Color"))
                                material.color = Color.HSVToRGB((i * 0.05f) % 1f, 1f, 1f);
                            break;
                    }
                }
            }

            if (BetterDayNightManager.instance != null)
            {
                switch (mode)
                {
                    case 0:
                        BetterDayNightManager.instance.SetTimeIndexOverrideFunction(_ => 3);
                        RenderSettings.fog = true;
                        RenderSettings.fogColor = Color.black;
                        RenderSettings.ambientLight = Color.black;
                        break;
                    case 1:
                        BetterDayNightManager.instance.SetTimeIndexOverrideFunction(_ => 0);
                        RenderSettings.fog = true;
                        RenderSettings.fogColor = Color.magenta;
                        RenderSettings.ambientLight = Color.cyan;
                        break;
                    case 2:
                        BetterDayNightManager.instance.SetTimeIndexOverrideFunction(_ => 2);
                        RenderSettings.fog = true;
                        RenderSettings.fogColor = new Color(0.5f, 0f, 0f);
                        RenderSettings.ambientLight = Color.red;
                        break;
                    case 3:
                        BetterDayNightManager.instance.SetTimeIndexOverrideFunction(_ => 1);
                        RenderSettings.fog = true;
                        RenderSettings.fogColor = Color.green;
                        RenderSettings.ambientLight = Color.yellow;
                        break;
                }
                BetterDayNightManager.instance.UpdateTimeOfDay(true);
            }
        }

        private static void FixShaders()
        {
            foreach (KeyValuePair<Material, MaterialState> pair in originalMaterialStates)
            {
                Material material = pair.Key;
                MaterialState state = pair.Value;

                if (material == null)
                    continue;

                if (state.OriginalShader != null)
                {
                    material.shader = state.OriginalShader;
                }

                if (state.HasColor && material.HasProperty("_Color"))
                {
                    material.color = state.OriginalColor;
                }

                if (state.HasBaseColor && material.HasProperty("_BaseColor"))
                {
                    material.SetColor("_BaseColor", state.OriginalBaseColor);
                }
            }

            originalMaterialStates.Clear();

            if (savedLighting)
            {
                RenderSettings.fog = originalFog;
                RenderSettings.fogColor = originalFogColor;
                RenderSettings.ambientLight = originalAmbientLight;
                savedLighting = false;
            }
        }

        [Setting] public static int timeOfDayIndex;
        public static readonly string[] timeOfDayNames = { "Morning", "Day", "Evening", "Night", "Default" };

        public static void TimeSwitcher()
        {
            Main.Change("Time Switcher", ref timeOfDayIndex, timeOfDayNames, () =>
            {
                if (BetterDayNightManager.instance == null) return;
                switch (timeOfDayIndex)
                {
                    case 0: BetterDayNightManager.instance.SetTimeOfDay(1, true); break;
                    case 1: BetterDayNightManager.instance.SetTimeOfDay(3, true); break;
                    case 2: BetterDayNightManager.instance.SetTimeOfDay(7, true); break;
                    case 3: BetterDayNightManager.instance.SetTimeOfDay(0, true); break;
                    case 4: BetterDayNightManager.instance.ClearTimeOfDay(true); break;
                }
                BetterDayNightManager.instance.UpdateTimeOfDay(true);
            });
        }

        public static void WeatherSwitcher() => TimeSwitcher();

        [Setting] public static int weatherIndex;
        public static readonly string[] weatherNames = { "Rain", "Clear", "Default" };

        public static void CycleWeather()
        {
            Main.Change("Weather Switcher", ref weatherIndex, weatherNames, () =>
            {
                if (BetterDayNightManager.instance == null) return;
                switch (weatherIndex)
                {
                    case 0: BetterDayNightManager.instance.SetFixedWeather(BetterDayNightManager.WeatherType.Raining, true); break;
                    case 1: BetterDayNightManager.instance.SetFixedWeather(BetterDayNightManager.WeatherType.None, true); break;
                    case 2: BetterDayNightManager.instance.ClearFixedWeather(true); break;
                }
            });
        }

        private static List<CosmeticsController.CosmeticItem> savedUnlockedCosmetics = new List<CosmeticsController.CosmeticItem>();
        private static List<CosmeticsController.CosmeticItem> savedUnlockedHats = new List<CosmeticsController.CosmeticItem>();
        private static List<CosmeticsController.CosmeticItem> savedUnlockedFaces = new List<CosmeticsController.CosmeticItem>();
        private static List<CosmeticsController.CosmeticItem> savedUnlockedBadges = new List<CosmeticsController.CosmeticItem>();
        private static List<CosmeticsController.CosmeticItem> savedUnlockedPaws = new List<CosmeticsController.CosmeticItem>();
        private static List<CosmeticsController.CosmeticItem> savedUnlockedChests = new List<CosmeticsController.CosmeticItem>();
        private static List<CosmeticsController.CosmeticItem> savedUnlockedFurs = new List<CosmeticsController.CosmeticItem>();
        private static List<CosmeticsController.CosmeticItem> savedUnlockedShirts = new List<CosmeticsController.CosmeticItem>();
        private static List<CosmeticsController.CosmeticItem> savedUnlockedPants = new List<CosmeticsController.CosmeticItem>();
        private static List<CosmeticsController.CosmeticItem> savedUnlockedBacks = new List<CosmeticsController.CosmeticItem>();
        private static List<CosmeticsController.CosmeticItem> savedUnlockedArms = new List<CosmeticsController.CosmeticItem>();
        private static List<CosmeticsController.CosmeticItem> savedUnlockedTagFX = new List<CosmeticsController.CosmeticItem>();
        private static List<CosmeticsController.CosmeticItem> savedUnlockedThrowables = new List<CosmeticsController.CosmeticItem>();
        private static HashSet<string> savedPlayerOwnedCosmetics = new HashSet<string>();
        private static HashSet<string> savedOfflinePlayerOwnedCosmetics = new HashSet<string>();

        public static bool cosmetXEnabled;
        public static bool disableQuitbox = true;

        public static void EnableCosmetX()
        {
            cosmetXEnabled = true;
            CosmeticsController controller = CosmeticsController.instance;
            if (controller == null) return;

            if (controller.unlockedCosmetics != null && controller.unlockedCosmetics.Count > 0)
            {
                savedUnlockedCosmetics = new List<CosmeticsController.CosmeticItem>(controller.unlockedCosmetics);
                savedUnlockedHats = new List<CosmeticsController.CosmeticItem>(controller.unlockedHats);
                savedUnlockedFaces = new List<CosmeticsController.CosmeticItem>(controller.unlockedFaces);
                savedUnlockedBadges = new List<CosmeticsController.CosmeticItem>(controller.unlockedBadges);
                savedUnlockedPaws = new List<CosmeticsController.CosmeticItem>(controller.unlockedPaws);
                savedUnlockedChests = new List<CosmeticsController.CosmeticItem>(controller.unlockedChests);
                savedUnlockedFurs = new List<CosmeticsController.CosmeticItem>(controller.unlockedFurs);
                savedUnlockedShirts = new List<CosmeticsController.CosmeticItem>(controller.unlockedShirts);
                savedUnlockedPants = new List<CosmeticsController.CosmeticItem>(controller.unlockedPants);
                savedUnlockedBacks = new List<CosmeticsController.CosmeticItem>(controller.unlockedBacks);
                savedUnlockedArms = new List<CosmeticsController.CosmeticItem>(controller.unlockedArms);
                savedUnlockedTagFX = new List<CosmeticsController.CosmeticItem>(controller.unlockedTagFX);
                savedUnlockedThrowables = new List<CosmeticsController.CosmeticItem>(controller.unlockedThrowables);
            }

            if (VRRig.LocalRig?._playerOwnedCosmetics != null)
                savedPlayerOwnedCosmetics = new HashSet<string>(VRRig.LocalRig._playerOwnedCosmetics);
            if (GorillaTagger.Instance?.offlineVRRig?._playerOwnedCosmetics != null)
                savedOfflinePlayerOwnedCosmetics = new HashSet<string>(GorillaTagger.Instance.offlineVRRig._playerOwnedCosmetics);

            controller.unlockedCosmetics.Clear();
            controller.unlockedHats.Clear();
            controller.unlockedFaces.Clear();
            controller.unlockedBadges.Clear();
            controller.unlockedPaws.Clear();
            controller.unlockedChests.Clear();
            controller.unlockedFurs.Clear();
            controller.unlockedShirts.Clear();
            controller.unlockedPants.Clear();
            controller.unlockedBacks.Clear();
            controller.unlockedArms.Clear();
            controller.unlockedTagFX.Clear();
            controller.unlockedThrowables.Clear();

            IEnumerable<CosmeticsController.CosmeticItem> allItems = controller.allCosmetics ?? (IEnumerable<CosmeticsController.CosmeticItem>)controller.allCosmeticsDict.Values;
            if (allItems != null)
            {
                foreach (CosmeticsController.CosmeticItem item in allItems)
                {
                    if (item.isNullItem || string.IsNullOrEmpty(item.itemName)) continue;

                    if (!controller.unlockedCosmetics.Contains(item))
                        controller.unlockedCosmetics.Add(item);

                    switch (item.itemCategory)
                    {
                        case CosmeticsController.CosmeticCategory.Hat:
                            if (!controller.unlockedHats.Contains(item)) controller.unlockedHats.Add(item);
                            break;
                        case CosmeticsController.CosmeticCategory.Face:
                            if (!controller.unlockedFaces.Contains(item)) controller.unlockedFaces.Add(item);
                            break;
                        case CosmeticsController.CosmeticCategory.Badge:
                            if (!controller.unlockedBadges.Contains(item)) controller.unlockedBadges.Add(item);
                            break;
                        case CosmeticsController.CosmeticCategory.Paw:
                            if (item.isThrowable)
                            {
                                if (!controller.unlockedThrowables.Contains(item)) controller.unlockedThrowables.Add(item);
                            }
                            else
                            {
                                if (!controller.unlockedPaws.Contains(item)) controller.unlockedPaws.Add(item);
                            }
                            break;
                        case CosmeticsController.CosmeticCategory.Chest:
                            if (!controller.unlockedChests.Contains(item)) controller.unlockedChests.Add(item);
                            break;
                        case CosmeticsController.CosmeticCategory.Fur:
                            if (!controller.unlockedFurs.Contains(item)) controller.unlockedFurs.Add(item);
                            break;
                        case CosmeticsController.CosmeticCategory.Shirt:
                            if (!controller.unlockedShirts.Contains(item)) controller.unlockedShirts.Add(item);
                            break;
                        case CosmeticsController.CosmeticCategory.Pants:
                            if (!controller.unlockedPants.Contains(item)) controller.unlockedPants.Add(item);
                            break;
                        case CosmeticsController.CosmeticCategory.Back:
                            if (!controller.unlockedBacks.Contains(item)) controller.unlockedBacks.Add(item);
                            break;
                        case CosmeticsController.CosmeticCategory.Arms:
                            if (!controller.unlockedArms.Contains(item)) controller.unlockedArms.Add(item);
                            break;
                        case CosmeticsController.CosmeticCategory.TagEffect:
                            if (!controller.unlockedTagFX.Contains(item)) controller.unlockedTagFX.Add(item);
                            break;
                    }

                    if (VRRig.LocalRig != null)
                        VRRig.LocalRig.AddCosmetic(item.itemName);
                    if (GorillaTagger.Instance?.offlineVRRig != null && GorillaTagger.Instance.offlineVRRig != VRRig.LocalRig)
                        GorillaTagger.Instance.offlineVRRig.AddCosmetic(item.itemName);
                }
            }

            controller.concatStringCosmeticsAllowed = string.Concat(controller.unlockedCosmetics.Select(x => x.itemName));
            controller.UpdateWardrobeModelsAndButtons();
            controller.OnCosmeticsUpdated?.Invoke();
            VRRig.LocalRig?.RefreshCosmetics();
            GorillaTagger.Instance?.offlineVRRig?.RefreshCosmetics();
            ModsLib.SyncCosmeticsToNetwork();
        }

        public static void DisableCosmetX()
        {
            cosmetXEnabled = false;
            CosmeticsController controller = CosmeticsController.instance;
            if (controller == null) return;

            controller.unlockedCosmetics.Clear();
            controller.unlockedCosmetics.AddRange(savedUnlockedCosmetics);
            controller.unlockedHats.Clear();
            controller.unlockedHats.AddRange(savedUnlockedHats);
            controller.unlockedFaces.Clear();
            controller.unlockedFaces.AddRange(savedUnlockedFaces);
            controller.unlockedBadges.Clear();
            controller.unlockedBadges.AddRange(savedUnlockedBadges);
            controller.unlockedPaws.Clear();
            controller.unlockedPaws.AddRange(savedUnlockedPaws);
            controller.unlockedChests.Clear();
            controller.unlockedChests.AddRange(savedUnlockedChests);
            controller.unlockedFurs.Clear();
            controller.unlockedFurs.AddRange(savedUnlockedFurs);
            controller.unlockedShirts.Clear();
            controller.unlockedShirts.AddRange(savedUnlockedShirts);
            controller.unlockedPants.Clear();
            controller.unlockedPants.AddRange(savedUnlockedPants);
            controller.unlockedBacks.Clear();
            controller.unlockedBacks.AddRange(savedUnlockedBacks);
            controller.unlockedArms.Clear();
            controller.unlockedArms.AddRange(savedUnlockedArms);
            controller.unlockedTagFX.Clear();
            controller.unlockedTagFX.AddRange(savedUnlockedTagFX);
            controller.unlockedThrowables.Clear();
            controller.unlockedThrowables.AddRange(savedUnlockedThrowables);

            if (VRRig.LocalRig?._playerOwnedCosmetics != null)
            {
                VRRig.LocalRig._playerOwnedCosmetics.Clear();
                VRRig.LocalRig._playerOwnedCosmetics.UnionWith(savedPlayerOwnedCosmetics);
            }

            if (GorillaTagger.Instance?.offlineVRRig?._playerOwnedCosmetics != null)
            {
                GorillaTagger.Instance.offlineVRRig._playerOwnedCosmetics.Clear();
                GorillaTagger.Instance.offlineVRRig._playerOwnedCosmetics.UnionWith(savedOfflinePlayerOwnedCosmetics);
            }

            if (controller.cosmeticsPages != null)
            {
                for (int i = 0; i < controller.cosmeticsPages.Length; i++)
                    controller.cosmeticsPages[i] = 0;
            }

            if (savedUnlockedCosmetics.Count == 0)
            {
                try
                {
                    controller.GetCosmeticsPlayFabCatalogData();
                    GorillaTagger.Instance?.offlineVRRig?.GetCosmeticsPlayFabCatalogData();
                }
                catch { }
            }

            controller.concatStringCosmeticsAllowed = string.Concat(controller.unlockedCosmetics.Select(x => x.itemName));

            try
            {
                controller.currentWornSet?.LoadFromPlayerPreferences(controller);
                controller.UpdateWornCosmetics(true);
            }
            catch { }

            controller.UpdateWardrobeModelsAndButtons();
            controller.OnCosmeticsUpdated?.Invoke();
            controller.OnOutfitsUpdated?.Invoke();
            VRRig.LocalRig?.RefreshCosmetics();
            GorillaTagger.Instance?.offlineVRRig?.RefreshCosmetics();
            ModsLib.SyncCosmeticsToNetwork();
        }

        private static GameObject debugOverlayObj;
        private static UnityEngine.UI.Text debugOverlayText;
        private static GameObject debugVrObj;
        private static TextMesh debugVrText;
        private static float debugUpdateTimer;
        private static GameObject cachedShoulder;
        private static GameObject cachedVCam;
        private static Camera cachedTpc;

        private static GameObject roomOverlayObj;
        private static UnityEngine.UI.Text roomOverlayText;
        private static GameObject roomVrObj;
        private static TextMesh roomVrText;
        private static float roomUpdateTimer;

        public static void DebugInfo()
        {
            if (debugOverlayObj == null)
            {
                debugOverlayObj = new GameObject("DebugInfo");
                Canvas canvas = debugOverlayObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 999;

                GameObject textObj = new GameObject("Text");
                textObj.transform.SetParent(debugOverlayObj.transform, false);
                debugOverlayText = textObj.AddComponent<UnityEngine.UI.Text>();
                debugOverlayText.font = Settings.currentFont ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                debugOverlayText.fontSize = 12;
                debugOverlayText.color = Color.white;
                debugOverlayText.alignment = TextAnchor.UpperRight;
                debugOverlayText.horizontalOverflow = HorizontalWrapMode.Overflow;
                debugOverlayText.verticalOverflow = VerticalWrapMode.Overflow;

                UnityEngine.UI.Outline outline = textObj.AddComponent<UnityEngine.UI.Outline>();
                outline.effectColor = Color.black;
                outline.effectDistance = new Vector2(1, -1);

                RectTransform rect = textObj.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.one;
                rect.anchorMax = Vector2.one;
                rect.pivot = Vector2.one;
                rect.anchoredPosition = new Vector2(-15, -15);
                rect.sizeDelta = new Vector2(500, 350);
            }

            if (debugVrObj == null)
            {
                Camera cam = Camera.main ?? GorillaTagger.Instance?.mainCamera?.GetComponent<Camera>();
                if (cam != null)
                {
                    debugVrObj = new GameObject("VR_DebugInfo");
                    debugVrObj.transform.SetParent(cam.transform, false);
                    debugVrObj.transform.localPosition = new Vector3(0.24f, 0.16f, 0.55f);
                    debugVrObj.transform.localRotation = Quaternion.identity;

                    debugVrText = debugVrObj.AddComponent<TextMesh>();
                    debugVrText.font = Settings.currentFont ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                    debugVrText.fontSize = 22;
                    debugVrText.characterSize = 0.003f;
                    debugVrText.alignment = TextAlignment.Right;
                    debugVrText.anchor = TextAnchor.UpperRight;
                    debugVrText.color = Color.white;
                }
            }

            if (Time.unscaledTime < debugUpdateTimer) return;
            debugUpdateTimer = Time.unscaledTime + 0.1f;

            Vector3 pos = GTPlayer.Instance != null ? GTPlayer.Instance.transform.position : (VRRig.LocalRig != null ? VRRig.LocalRig.transform.position : Vector3.zero);
            Vector3 vel = GTPlayer.Instance != null ? GTPlayer.Instance.currentVelocity : Vector3.zero;
            Vector3 vrCam = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            float yaw = Camera.main != null ? Camera.main.transform.eulerAngles.y : 0f;

            Vector3 lHand = GorillaTagger.Instance?.leftHandTransform != null ? GorillaTagger.Instance.leftHandTransform.position : Vector3.zero;
            Vector3 rHand = GorillaTagger.Instance?.rightHandTransform != null ? GorillaTagger.Instance.rightHandTransform.position : Vector3.zero;

            if (cachedTpc == null)
                cachedTpc = Main.TPC ?? GameObject.Find("Shoulder Camera")?.GetComponent<Camera>();
            Vector3 tpcPos = cachedTpc != null ? cachedTpc.transform.position : Vector3.zero;

            if (cachedShoulder == null)
                cachedShoulder = GameObject.Find("Player Objects/Third Person Camera/Shoulder Camera") ?? GameObject.Find("Shoulder Camera");
            Vector3 shoulderPos = cachedShoulder != null ? cachedShoulder.transform.position : Vector3.zero;

            if (cachedVCam == null && cachedShoulder != null)
                cachedVCam = cachedShoulder.transform.Find("CM vcam1")?.gameObject ?? GameObject.Find("CM vcam1");
            Vector3 vcamPos = cachedVCam != null ? cachedVCam.transform.position : Vector3.zero;
            bool vcamActive = cachedVCam != null && cachedVCam.activeInHierarchy;

            int fps = Mathf.CeilToInt(1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f));
            long ram = GC.GetTotalMemory(false) / (1024 * 1024);
            bool inRoom = PhotonNetwork.InRoom;
            string room = inRoom ? $"{PhotonNetwork.CurrentRoom.Name} ({PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers})" : "Offline";
            string ping = inRoom ? $"{PhotonNetwork.GetPing()} ms" : "N/A";
            string zone = !string.IsNullOrEmpty(lastmap) ? lastmap : (GorillaComputer.instance?.currentQueue ?? "forest");
            bool isGrounded = GTPlayer.Instance != null && (GTPlayer.Instance.IsHandTouching(true) || GTPlayer.Instance.IsHandTouching(false));
            bool isInfected = VRRig.LocalRig != null && VRRig.LocalRig.mainSkin != null && VRRig.LocalRig.mainSkin.material != null && VRRig.LocalRig.mainSkin.material.name.Contains("fected");

            string text =
                $"FPS: {fps} ({(Time.unscaledDeltaTime * 1000f):F1} ms)\n" +
                $"RAM: {ram} MB\n" +
                $"Pos: ({pos.x:F2}, {pos.y:F2}, {pos.z:F2})\n" +
                $"Vel: {vel.magnitude:F2} m/s\n" +
                $"Yaw: {yaw:F1}°\n" +
                $"L-Hand: ({lHand.x:F2}, {lHand.y:F2}, {lHand.z:F2})\n" +
                $"R-Hand: ({rHand.x:F2}, {rHand.y:F2}, {rHand.z:F2})\n" +
                $"Grounded: {isGrounded}\n" +
                $"Infected: {isInfected}\n" +
                $"VR Cam: ({vrCam.x:F2}, {vrCam.y:F2}, {vrCam.z:F2})\n" +
                $"TPC: ({tpcPos.x:F2}, {tpcPos.y:F2}, {tpcPos.z:F2})\n" +
                $"Shoulder: ({shoulderPos.x:F2}, {shoulderPos.y:F2}, {shoulderPos.z:F2})\n" +
                $"VCam1: ({vcamPos.x:F2}, {vcamPos.y:F2}, {vcamPos.z:F2}) [{(vcamActive ? "Active" : "Disabled")}]\n" +
                $"Room: {room}\n" +
                $"Ping: {ping}\n" +
                $"Zone: {zone}";

            if (debugOverlayText != null) debugOverlayText.text = text;
            if (debugVrText != null) debugVrText.text = text;
        }

        public static void DisableDebugInfo()
        {
            if (debugOverlayObj != null)
            {
                Object.Destroy(debugOverlayObj);
                debugOverlayObj = null;
                debugOverlayText = null;
            }
            if (debugVrObj != null)
            {
                Object.Destroy(debugVrObj);
                debugVrObj = null;
                debugVrText = null;
            }
            cachedShoulder = null;
            cachedVCam = null;
            cachedTpc = null;
        }

        public static void RoomInfo()
        {
            if (roomOverlayObj == null)
            {
                roomOverlayObj = new GameObject("RoomInfo");
                Canvas canvas = roomOverlayObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 999;

                GameObject textObj = new GameObject("Text");
                textObj.transform.SetParent(roomOverlayObj.transform, false);
                roomOverlayText = textObj.AddComponent<UnityEngine.UI.Text>();
                roomOverlayText.font = Settings.currentFont ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                roomOverlayText.fontSize = 13;
                roomOverlayText.color = Color.white;
                roomOverlayText.alignment = TextAnchor.UpperLeft;
                roomOverlayText.horizontalOverflow = HorizontalWrapMode.Overflow;
                roomOverlayText.verticalOverflow = VerticalWrapMode.Overflow;

                UnityEngine.UI.Outline outline = textObj.AddComponent<UnityEngine.UI.Outline>();
                outline.effectColor = Color.black;
                outline.effectDistance = new Vector2(1, -1);

                RectTransform rect = textObj.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(15, -15);
                rect.sizeDelta = new Vector2(500, 300);
            }

            if (roomVrObj == null)
            {
                Camera cam = Camera.main ?? GorillaTagger.Instance?.mainCamera?.GetComponent<Camera>();
                if (cam != null)
                {
                    roomVrObj = new GameObject("VR_RoomInfo");
                    roomVrObj.transform.SetParent(cam.transform, false);
                    roomVrObj.transform.localPosition = new Vector3(-0.24f, 0.16f, 0.55f);
                    roomVrObj.transform.localRotation = Quaternion.identity;

                    roomVrText = roomVrObj.AddComponent<TextMesh>();
                    roomVrText.font = Settings.currentFont ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                    roomVrText.fontSize = 22;
                    roomVrText.characterSize = 0.003f;
                    roomVrText.alignment = TextAlignment.Left;
                    roomVrText.anchor = TextAnchor.UpperLeft;
                    roomVrText.color = Color.white;
                }
            }

            if (Time.unscaledTime < roomUpdateTimer) return;
            roomUpdateTimer = Time.unscaledTime + 0.1f;

            bool inRoom = PhotonNetwork.InRoom;
            string roomName = inRoom ? PhotonNetwork.CurrentRoom.Name : "Offline";
            bool isPrivate = inRoom && !PhotonNetwork.CurrentRoom.IsVisible;
            string gameMode = GorillaComputer.instance?.currentGameMode?.Value ?? "None";
            string queue = GorillaComputer.instance?.currentQueue ?? "default";
            int playerCount = inRoom ? PhotonNetwork.CurrentRoom.PlayerCount : 0;
            int maxPlayers = inRoom ? PhotonNetwork.CurrentRoom.MaxPlayers : 10;
            string hostName = inRoom && PhotonNetwork.MasterClient != null ? PhotonNetwork.MasterClient.NickName : "None";
            int hostActor = inRoom && PhotonNetwork.MasterClient != null ? PhotonNetwork.MasterClient.ActorNumber : 0;
            string region = inRoom ? PhotonNetwork.CloudRegion : "N/A";
            string ping = inRoom ? $"{PhotonNetwork.GetPing()} ms" : "N/A";
            string zone = !string.IsNullOrEmpty(lastmap) ? lastmap : (GorillaComputer.instance?.currentQueue ?? "forest");

            int infectedCount = 0;
            int survivorCount = 0;
            foreach (VRRig rig in VRRigCache.ActiveRigs)
            {
                if (rig == null || rig.isLocal) continue;
                if (rig.mainSkin != null && rig.mainSkin.material != null && rig.mainSkin.material.name.Contains("fected"))
                    infectedCount++;
                else
                    survivorCount++;
            }

            string text =
                $"Room: {roomName}\n" +
                $"Status: {(inRoom ? (isPrivate ? "Private" : "Public") : "Offline")}\n" +
                $"Mode: {gameMode}\n" +
                $"Queue: {queue}\n" +
                $"Players: {playerCount}/{maxPlayers} (Infected: {infectedCount}, Survivors: {survivorCount})\n" +
                $"Host: {hostName} [Actor #{hostActor}]\n" +
                $"Region: {region}\n" +
                $"Ping: {ping}\n" +
                $"Zone: {zone}";

            if (roomOverlayText != null) roomOverlayText.text = text;
            if (roomVrText != null) roomVrText.text = text;
        }

        public static void DisableRoomInfo()
        {
            if (roomOverlayObj != null)
            {
                Object.Destroy(roomOverlayObj);
                roomOverlayObj = null;
                roomOverlayText = null;
            }
            if (roomVrObj != null)
            {
                Object.Destroy(roomVrObj);
                roomVrObj = null;
                roomVrText = null;
            }
        }
    }
}
