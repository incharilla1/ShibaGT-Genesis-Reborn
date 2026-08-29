using BepInEx;
using GorillaLocomotion;
using GorillaNetworking;
using Photon.Realtime;
using ShibaGTGenesisReborn.Classes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ShibaGTGenesisReborn.Libs
{
    public static class ModsLib
    {
        #region Path Utilities
        private static string genesisDirectory;

        public static string GenesisDirectory
        {
            get
            {
                if (string.IsNullOrEmpty(genesisDirectory))
                {
                    string rootDir = Paths.GameRootPath ?? Directory.GetParent(Application.dataPath).FullName;
                    if (!rootDir.Contains("Gorilla Tag"))
                        return "uh oh!";

                    genesisDirectory = Path.Combine(rootDir, "Genesis");
                    if (!Directory.Exists(genesisDirectory))
                        Directory.CreateDirectory(genesisDirectory);

                    string soundboardDir = Path.Combine(genesisDirectory, "soundboard");
                    if (!Directory.Exists(soundboardDir))
                        Directory.CreateDirectory(soundboardDir);

                    string boomboxDir = Path.Combine(genesisDirectory, "boombox");
                    if (!Directory.Exists(boomboxDir))
                        Directory.CreateDirectory(boomboxDir);

                    string presetsDir = Path.Combine(genesisDirectory, "Presets");
                    if (!Directory.Exists(presetsDir))
                        Directory.CreateDirectory(presetsDir);
                }

                return genesisDirectory;
            }
        }
        #endregion
        #region Ender Pearl Utilities
        private static Texture2D enderPearlTexture;
        private static Material enderPearlMaterial;

        private static Texture2D metaTexture;
        private static Material metaMaterial;
        private static Texture2D steamTexture;
        private static Material steamMaterial;
        private static Texture2D folderTexture;
        private static Texture2D settingsTexture;
        private static Texture2D homeTexture;
        private static Texture2D heartTexture;

        public static Texture2D LoadTextureResource(string fileName)
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                string[] resourceNames = assembly.GetManifestResourceNames();
                string resource = resourceNames.FirstOrDefault(name => name.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrEmpty(resource))
                {
                    using Stream stream = assembly.GetManifestResourceStream(resource);
                    if (stream != null)
                    {
                        byte[] buffer = new byte[stream.Length];
                        stream.Read(buffer, 0, buffer.Length);
                        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                        if (texture.LoadImage(buffer))
                        {
                            return texture;
                        }
                    }
                }
            }
            catch { }

            string[] candidatePaths = new[]
            {
                Path.Combine(GenesisDirectory, fileName),
                Path.Combine(GenesisDirectory, "Resources", fileName),
                Path.Combine(Paths.PluginPath, "Genesis", fileName),
                Path.Combine(Paths.PluginPath, "Resources", fileName),
                Path.Combine(Paths.PluginPath, fileName),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Genesis", fileName),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", fileName),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BepInEx", "plugins", "Genesis", fileName),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BepInEx", "plugins", "Resources", fileName),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BepInEx", "plugins", fileName),
                "Resources/" + fileName,
                "Resources\\" + fileName,
                fileName
            };

            foreach (string path in candidatePaths)
            {
                if (File.Exists(path))
                {
                    try
                    {
                        byte[] fileBytes = File.ReadAllBytes(path);
                        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                        if (texture.LoadImage(fileBytes))
                        {
                            return texture;
                        }
                    }
                    catch { }
                }
            }

            return null;
        }

        public static Texture2D GetPearlTexture()
        {
            if (enderPearlTexture != null)
            {
                return enderPearlTexture;
            }

            enderPearlTexture = LoadTextureResource("pearl.png");
            if (enderPearlTexture != null)
            {
                return enderPearlTexture;
            }

            const int size = 64;
            enderPearlTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size * 0.45f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    if (distance > radius)
                    {
                        enderPearlTexture.SetPixel(x, y, Color.clear);
                    }
                    else
                    {
                        float normalizedDistance = distance / radius;
                        Color pixelColor = normalizedDistance < 0.25f
                            ? new Color(0.04f, 0.2f, 0.16f, 1f)
                            : (normalizedDistance < 0.7f ? new Color(0.12f, 0.95f, 0.76f, 1f) : new Color(0.02f, 0.4f, 0.3f, 1f));
                        enderPearlTexture.SetPixel(x, y, pixelColor);
                    }
                }
            }
            enderPearlTexture.Apply();
            return enderPearlTexture;
        }

        public static Material GetEnderPearlMaterial()
        {
            if (enderPearlMaterial == null)
            {
                Shader spriteShader = Shader.Find("Sprites/Default") ?? Shader.Find("GUI/Text Shader") ?? Shader.Find("Unlit/Transparent");
                enderPearlMaterial = new Material(spriteShader)
                {
                    mainTexture = GetPearlTexture()
                };
            }

            return enderPearlMaterial;
        }

        public static Texture2D GetMetaTexture()
        {
            if (metaTexture != null)
            {
                return metaTexture;
            }

            metaTexture = LoadTextureResource("meta.png");
            return metaTexture;
        }

        public static Material GetMetaMaterial()
        {
            if (metaMaterial == null)
            {
                Shader spriteShader = Shader.Find("Sprites/Default") ?? Shader.Find("GUI/Text Shader") ?? Shader.Find("Unlit/Transparent");
                metaMaterial = new Material(spriteShader)
                {
                    mainTexture = GetMetaTexture()
                };
            }

            return metaMaterial;
        }

        public static Texture2D GetSteamTexture()
        {
            if (steamTexture != null)
            {
                return steamTexture;
            }

            steamTexture = LoadTextureResource("steam.png");
            return steamTexture;
        }

        public static Material GetSteamMaterial()
        {
            if (steamMaterial == null)
            {
                Shader spriteShader = Shader.Find("Sprites/Default") ?? Shader.Find("GUI/Text Shader") ?? Shader.Find("Unlit/Transparent");
                steamMaterial = new Material(spriteShader)
                {
                    mainTexture = GetSteamTexture()
                };
            }

            return steamMaterial;
        }

        public static Texture2D GetFolderTexture()
        {
            if (folderTexture != null)
            {
                return folderTexture;
            }

            folderTexture = LoadTextureResource("folder.png");
            return folderTexture;
        }

        public static Texture2D GetSettingsTexture()
        {
            if (settingsTexture != null)
            {
                return settingsTexture;
            }

            settingsTexture = LoadTextureResource("settings.png");
            return settingsTexture;
        }

        public static Texture2D GetHomeTexture()
        {
            if (homeTexture != null)
            {
                return homeTexture;
            }

            homeTexture = LoadTextureResource("home.png");
            return homeTexture;
        }

        public static Texture2D GetHeartTexture()
        {
            if (heartTexture != null)
            {
                return heartTexture;
            }

            heartTexture = LoadTextureResource("heart.png");
            return heartTexture;
        }

        public static GameObject CreatePearlVisual(string objectName, Vector3 initialPosition, float scale = 0.14f)
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Quad);
            UnityEngine.Object.Destroy(visual.GetComponent<Collider>());
            visual.name = objectName;
            visual.transform.localScale = new Vector3(scale, scale, scale);
            visual.transform.position = initialPosition;

            Renderer renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = GetEnderPearlMaterial();
            }

            return visual;
        }

        public static Vector3 GetHandThrowVelocity(bool isLeftHand)
        {
            if (GunLib.IsXRDeviceActive())
            {
                var tracker = isLeftHand ? GTPlayer.Instance.LeftHand.velocityTracker : GTPlayer.Instance.RightHand.velocityTracker;
                Transform handTransform = isLeftHand ? GorillaTagger.Instance.leftHandTransform : GorillaTagger.Instance.rightHandTransform;

                Vector3 trackedVelocity = tracker != null ? tracker.GetAverageVelocity(true, 0.05f) : Vector3.zero;

                if (trackedVelocity.sqrMagnitude > 0.08f)
                {
                    return trackedVelocity * 1.15f;
                }

                return handTransform != null ? -handTransform.up * 3f : Vector3.down * 3f;
            }

            Camera mainCamera = Camera.main != null ? Camera.main : GorillaTagger.Instance.mainCamera.GetComponent<Camera>();
            return mainCamera != null ? (mainCamera.transform.forward * 20f + mainCamera.transform.up * 2f) : (Vector3.forward * 20f);
        }
        #endregion
        #region Zipline Utilities
        private static Material canyonsZiplineMaterial;

        public static Material GetCanyonsZiplineMaterial()
        {
            if (canyonsZiplineMaterial == null)
            {
                var existingZipline = UnityEngine.Object.FindObjectsByType<GorillaLocomotion.Gameplay.GorillaZipline>(FindObjectsSortMode.None);
                if (existingZipline != null && existingZipline.Length > 0)
                {
                    var renderer = existingZipline[0].GetComponentInChildren<Renderer>();
                    if (renderer != null && renderer.sharedMaterial != null)
                    {
                        canyonsZiplineMaterial = new Material(renderer.sharedMaterial);
                    }
                }

                if (canyonsZiplineMaterial == null)
                {
                    Shader uberShader = Shader.Find("GorillaTag/UberShader") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
                    canyonsZiplineMaterial = new Material(uberShader)
                    {
                        color = new Color(0.14f, 0.14f, 0.15f, 1f)
                    };
                }
            }

            return canyonsZiplineMaterial;
        }

        public static Vector3 CalculateClosestPointOnSegment(Vector3 segmentStart, Vector3 segmentEnd, Vector3 point, out float normalizedDistance)
        {
            Vector3 segmentVector = segmentEnd - segmentStart;
            float segmentLengthSquared = segmentVector.sqrMagnitude;

            if (segmentLengthSquared < 0.0001f)
            {
                normalizedDistance = 0f;
                return segmentStart;
            }

            normalizedDistance = Mathf.Clamp01(Vector3.Dot(point - segmentStart, segmentVector) / segmentLengthSquared);
            return segmentStart + segmentVector * normalizedDistance;
        }

        public static void CreateZiplineVisual(
            Vector3 startPoint,
            Vector3 endPoint,
            ref GameObject cableObject,
            ref LineRenderer lineRenderer,
            ref GameObject startAnchor,
            ref GameObject endAnchor)
        {
            if (cableObject == null)
            {
                cableObject = new GameObject("CustomZiplineCable");
                lineRenderer = cableObject.AddComponent<LineRenderer>();
                lineRenderer.startWidth = 0.026f;
                lineRenderer.endWidth = 0.026f;
                lineRenderer.positionCount = 2;
                lineRenderer.useWorldSpace = true;
                lineRenderer.material = GetCanyonsZiplineMaterial();
                lineRenderer.startColor = new Color(0.16f, 0.16f, 0.17f, 1f);
                lineRenderer.endColor = new Color(0.16f, 0.16f, 0.17f, 1f);
                lineRenderer.numCapVertices = 6;
                lineRenderer.numCornerVertices = 6;
            }

            lineRenderer.SetPosition(0, startPoint);
            lineRenderer.SetPosition(1, endPoint);

            if (startAnchor == null)
            {
                startAnchor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                UnityEngine.Object.Destroy(startAnchor.GetComponent<Collider>());
                startAnchor.name = "ZiplineStartAnchor";
                startAnchor.transform.localScale = new Vector3(0.1f, 0.04f, 0.1f);
                Renderer renderer = startAnchor.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.shader = Shader.Find("GorillaTag/UberShader") ?? Shader.Find("GUI/Text Shader");
                    renderer.material.color = new Color(0.22f, 0.22f, 0.24f, 1f);
                }
            }
            startAnchor.transform.position = startPoint;
            startAnchor.transform.rotation = Quaternion.LookRotation((endPoint - startPoint).normalized) * Quaternion.Euler(90f, 0f, 0f);

            if (endAnchor == null)
            {
                endAnchor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                UnityEngine.Object.Destroy(endAnchor.GetComponent<Collider>());
                endAnchor.name = "ZiplineEndAnchor";
                endAnchor.transform.localScale = new Vector3(0.1f, 0.04f, 0.1f);
                Renderer renderer = endAnchor.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.shader = Shader.Find("GorillaTag/UberShader") ?? Shader.Find("GUI/Text Shader");
                    renderer.material.color = new Color(0.22f, 0.22f, 0.24f, 1f);
                }
            }
            endAnchor.transform.position = endPoint;
            endAnchor.transform.rotation = Quaternion.LookRotation((endPoint - startPoint).normalized) * Quaternion.Euler(90f, 0f, 0f);
        }

        public static void DestroyZiplineVisual(
            ref GameObject cableObject,
            ref LineRenderer lineRenderer,
            ref GameObject startAnchor,
            ref GameObject endAnchor)
        {
            if (cableObject != null)
            {
                UnityEngine.Object.Destroy(cableObject);
                cableObject = null;
                lineRenderer = null;
            }

            if (startAnchor != null)
            {
                UnityEngine.Object.Destroy(startAnchor);
                startAnchor = null;
            }

            if (endAnchor != null)
            {
                UnityEngine.Object.Destroy(endAnchor);
                endAnchor = null;
            }
        }
        #endregion

        #region Custom Item & Asset Utilities
        public static string TryFindLocalFile(params string[] candidatePaths)
        {
            foreach (string candidate in candidatePaths)
            {
                if (!string.IsNullOrEmpty(candidate) && File.Exists(candidate) && new FileInfo(candidate).Length > 50)
                    return candidate;
            }
            return null;
        }

        public static string FindLocalAsset(string fileName, params string[] extraCandidates)
        {
            if (string.IsNullOrEmpty(fileName)) return null;

            List<string> paths = new List<string>
            {
                Path.Combine(GenesisDirectory, fileName),
                Path.Combine(GenesisDirectory, "files", fileName),
                Path.Combine(Paths.PluginPath ?? string.Empty, "files", fileName),
                Path.Combine(Paths.PluginPath ?? string.Empty, fileName),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mods", "Custom", "files", fileName),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "files", fileName),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName)
            };

            if (extraCandidates != null)
                paths.AddRange(extraCandidates);

            return TryFindLocalFile(paths.ToArray());
        }

        public static void IgnoreCollisionRecursive(Collider col, Transform target)
        {
            if (!col || !target) return;
            foreach (Collider c in target.GetComponentsInChildren<Collider>(true))
                Physics.IgnoreCollision(col, c, true);
        }

        public static Material CreateItemMaterial(Texture2D texture = null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("GorillaTag/UberShader") ?? Shader.Find("Sprites/Default"));
            Texture2D tex = texture ?? Texture2D.whiteTexture;
            mat.mainTexture = tex;
            mat.SetTexture("_BaseMap", tex);
            mat.color = Color.white;
            return mat;
        }

        public static Mesh ParseObj(string s)
        {
            if (string.IsNullOrEmpty(s) || s.StartsWith("<") || s.StartsWith("404")) return null;

            List<Vector3> v = new List<Vector3>();
            List<Vector2> u = new List<Vector2>();
            List<Vector3> n = new List<Vector3>();
            List<int> t = new List<int>();
            List<Vector3> nv = new List<Vector3>();
            List<Vector2> nu = new List<Vector2>();
            List<Vector3> nn = new List<Vector3>();

            using (StringReader r = new StringReader(s))
            {
                string l;
                while ((l = r.ReadLine()) != null)
                {
                    if (l.Length < 2 || l[0] == '#') continue;
                    string[] p = l.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (p.Length < 2) continue;

                    if (p[0] == "v" && p.Length >= 4)
                        v.Add(new Vector3(-float.Parse(p[1], CultureInfo.InvariantCulture), float.Parse(p[2], CultureInfo.InvariantCulture), float.Parse(p[3], CultureInfo.InvariantCulture)));
                    else if (p[0] == "vt" && p.Length >= 3)
                        u.Add(new Vector2(float.Parse(p[1], CultureInfo.InvariantCulture), float.Parse(p[2], CultureInfo.InvariantCulture)));
                    else if (p[0] == "vn" && p.Length >= 4)
                        n.Add(new Vector3(-float.Parse(p[1], CultureInfo.InvariantCulture), float.Parse(p[2], CultureInfo.InvariantCulture), float.Parse(p[3], CultureInfo.InvariantCulture)));
                    else if (p[0] == "f" && p.Length >= 4)
                    {
                        for (int i = 3; i >= 1; i--) FixObjVertex(p[i], v, u, n, nv, nu, nn, t);
                        if (p.Length == 5)
                        {
                            FixObjVertex(p[4], v, u, n, nv, nu, nn, t);
                            FixObjVertex(p[3], v, u, n, nv, nu, nn, t);
                            FixObjVertex(p[1], v, u, n, nv, nu, nn, t);
                        }
                    }
                }
            }

            if (nv.Count == 0) return null;

            Mesh m = new Mesh();
            if (nv.Count > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.vertices = nv.ToArray();
            m.uv = nu.ToArray();
            m.normals = nn.ToArray();
            m.triangles = t.ToArray();
            m.RecalculateBounds();
            m.RecalculateNormals();
            return m;
        }

        private static void FixObjVertex(string s, List<Vector3> v, List<Vector2> u, List<Vector3> n, List<Vector3> nv, List<Vector2> nu, List<Vector3> nn, List<int> t)
        {
            string[] c = s.Split('/');
            int vIdx = int.Parse(c[0], CultureInfo.InvariantCulture) - 1;
            if (vIdx >= 0 && vIdx < v.Count) nv.Add(v[vIdx]); else nv.Add(Vector3.zero);
            if (c.Length > 1 && !string.IsNullOrEmpty(c[1]))
            {
                int uIdx = int.Parse(c[1], CultureInfo.InvariantCulture) - 1;
                if (uIdx >= 0 && uIdx < u.Count) nu.Add(u[uIdx]); else nu.Add(Vector2.zero);
            }
            else nu.Add(Vector2.zero);

            if (c.Length > 2 && !string.IsNullOrEmpty(c[2]))
            {
                int nIdx = int.Parse(c[2], CultureInfo.InvariantCulture) - 1;
                if (nIdx >= 0 && nIdx < n.Count) nn.Add(n[nIdx]); else nn.Add(Vector3.up);
            }
            else nn.Add(Vector3.up);

            t.Add(nv.Count - 1);
        }
        #endregion

        #region Platform & Cosmetic Utilities
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
        #endregion
    }
}
