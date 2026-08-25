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
                if (!rig.isOfflineVRRig)
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
                    Object.Destroy(l, Time.deltaTime);
                }
            }
        }

        public static void BeaconESP()
        {
            foreach (VRRig rig in VRRigCache.ActiveRigs)
            {
                if (rig == null || rig.isOfflineVRRig)
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
                Object.Destroy(obj, Time.deltaTime);
            }
        }

        public static void FullBodyESP()
        {
            foreach (VRRig vrrig in VRRigCache.ActiveRigs)
            {
                if (!vrrig.isOfflineVRRig)
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
                if (!vrrig.isOfflineVRRig)
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
                if (rig == null || rig.isOfflineVRRig)
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
                if (rig == null || rig.isOfflineVRRig)
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
                    Object.Destroy(cube, Time.deltaTime);
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
                if (rig == null || rig.isOfflineVRRig)
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
                    Object.Destroy(quad, Time.deltaTime);
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

        public static string GetPlayerPlatform(NetPlayer player)
        {
            if (player == null || NetworkSystem.Instance == null) return string.Empty;
            return NetworkSystem.Instance.GetPlayerPlatform(player) ?? string.Empty;
        }

        public static bool IsSteamUser(NetPlayer player)
        {
            return GetPlayerPlatform(player).IndexOf("steam", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static bool IsSteamUser(VRRig rig)
        {
            return IsSteamUser(rig?.creator);
        }

        public static void NameAndDistanceTags()
        {
            Camera cam = Camera.main != null ? Camera.main : GorillaTagger.Instance.mainCamera.GetComponent<Camera>();
            foreach (VRRig rig in VRRigCache.ActiveRigs)
            {
                if (rig == null || rig.isOfflineVRRig) continue;
                
                NetPlayer netPlayer = rig.creator;
                Player player = null;
                if (netPlayer != null)
                    player = PhotonNetwork.CurrentRoom?.GetPlayer(netPlayer.ActorNumber);

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

                bool isSteam = IsSteamUser(netPlayer);
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

                Object.Destroy(tagObj, Time.deltaTime);
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
            Object.Destroy(obj, Time.deltaTime);
        }

        private struct MaterialState
        {
            public Shader OriginalShader;
            public Color OriginalColor;
            public bool HasColor;
            public Color OriginalBaseColor;
            public bool HasBaseColor;
        }

        [Setting] private static int cursedIndex = 4;
        private static readonly string[] cursedNames = { "Void", "Glitch", "Blood", "Acid", "Off" };
        private static readonly Dictionary<Material, MaterialState> originalMaterialStates = new Dictionary<Material, MaterialState>();
        private static bool originalFog;
        private static Color originalFogColor;
        private static Color originalAmbientLight;
        private static bool savedLighting;

        public static void CursedGTAG()
        {
            cursedIndex = (cursedIndex + 1) % cursedNames.Length;

            ButtonInfo cursedBtn = Main.GetIndex("cursedgtag");
            if (cursedBtn != null)
            {
                cursedBtn.overlapText = "Cursed: " + cursedNames[cursedIndex];
            }

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

        [Setting] private static int timeOfDayIndex;
        private static readonly string[] timeOfDayNames = { "Morning", "Day", "Evening", "Night", "Default" };

        public static void TimeSwitcher()
        {
            timeOfDayIndex = (timeOfDayIndex + 1) % timeOfDayNames.Length;

            ButtonInfo timeBtn = Main.GetIndex("Time Switcher") ?? Main.GetIndex("Weather Switcher");
            if (timeBtn != null)
            {
                timeBtn.overlapText = "Time: " + timeOfDayNames[timeOfDayIndex];
            }

            if (BetterDayNightManager.instance == null)
            {
                return;
            }

            switch (timeOfDayIndex)
            {
                case 0:
                    BetterDayNightManager.instance.SetTimeOfDay(1, true);
                    break;
                case 1:
                    BetterDayNightManager.instance.SetTimeOfDay(3, true);
                    break;
                case 2:
                    BetterDayNightManager.instance.SetTimeOfDay(7, true);
                    break;
                case 3:
                    BetterDayNightManager.instance.SetTimeOfDay(0, true);
                    break;
                case 4:
                    BetterDayNightManager.instance.ClearTimeOfDay(true);
                    break;
            }

            BetterDayNightManager.instance.UpdateTimeOfDay(true);
        }

        public static void WeatherSwitcher() => TimeSwitcher();

        [Setting] private static int weatherIndex;
        private static readonly string[] weatherNames = { "Rain", "Clear", "Default" };

        public static void CycleWeather()
        {
            weatherIndex = (weatherIndex + 1) % weatherNames.Length;
            Main.GetIndex("Weather Switcher").overlapText = "Weather: " + weatherNames[weatherIndex];

            if (BetterDayNightManager.instance == null)
                return;

            switch (weatherIndex)
            {
                case 0:
                    BetterDayNightManager.instance.SetFixedWeather(BetterDayNightManager.WeatherType.Raining, true);
                    break;
                case 1:
                    BetterDayNightManager.instance.SetFixedWeather(BetterDayNightManager.WeatherType.None, true);
                    break;
                case 2:
                    BetterDayNightManager.instance.ClearFixedWeather(true);
                    break;
            }
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

        public static void EnableCosmetX()
        {
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
            SyncCosmeticsToNetwork();
        }

        public static void DisableCosmetX()
        {
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
            SyncCosmeticsToNetwork();
        }

        public static string GetLocalCosmeticString()
        {
            if (VRRig.LocalRig == null || VRRig.LocalRig.cosmeticSet == null || VRRig.LocalRig.cosmeticSet.items == null) return string.Empty;
            List<string> items = new List<string>();
            for (int i = 0; i < VRRig.LocalRig.cosmeticSet.items.Length; i++)
            {
                var item = VRRig.LocalRig.cosmeticSet.items[i];
                items.Add((!item.isNullItem && !string.IsNullOrEmpty(item.itemName)) ? item.itemName : "null");
            }
            return string.Join(",", items);
        }

        public static void SyncCosmeticsToNetwork()
        {
            if (VRRig.LocalRig == null || NetworkingLibrary.Instance == null || !NetworkingLibrary.Instance.NetworkEnabled) return;
            string cosmeticString = GetLocalCosmeticString();
            if (!string.IsNullOrEmpty(cosmeticString))
                NetworkingLibrary.Instance.SendCosmeticUpdate(cosmeticString);
        }
    }
}
