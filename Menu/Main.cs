using BepInEx;
using GorillaNetworking;
using Photon.Pun;
using ShibaGTGenesisReborn.Classes;
using ShibaGTGenesisReborn.Libs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static ShibaGTGenesisReborn.Menu.Buttons;
using static ShibaGTGenesisReborn.Settings;

namespace ShibaGTGenesisReborn.Menu
{
    public class Main : MonoBehaviour
    {
        public static Main Instance { get; private set; }

        public static bool Loaded;

        public static bool Lockdown;

        private void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            MenuAudio.Initialize();
            Mods.Custom.BoomboxManager.Initialize();
            Mods.Custom.SoundboardManager.Initialize();
            Mods.Custom.SpotifyManager.Initialize();
            Preferences.EnsureDirectory();
            StreamerMode.EnsureInitialized();
            Mods.PlayerOptionsManager.Initialize();
            Preferences.Load();
        }

        private void Update()
        {
            if (Lockdown || CXS.ServerData.IsLocalBlacklisted())
            {
                if (menu != null)
                {
                    Destroy(menu);
                    menu = null;
                }
                if (reference != null)
                {
                    Destroy(reference);
                    reference = null;
                }
                DestroyDualReferences();
                barkMenuOpen = false;
                return;
            }
            
            try
            {
                bool toOpen = ControllerInputPoller.instance != null && ((!rightHanded && ControllerInputPoller.instance.leftControllerSecondaryButton) || (rightHanded && ControllerInputPoller.instance.rightControllerPrimaryButton));
                bool keyboardOpen = UnityInput.Current != null && UnityInput.Current.GetKey(keyboardButton);

                if (barkMenu && !isPCMenu && GorillaTagger.Instance?.bodyCollider != null)
                {
                    CheckBarkMenu();
                    toOpen = barkMenuOpen;
                }
                else if (!barkMenu)
                {
                    barkMenuOpen = false;
                }

                if (keyboardOpen) isPCMenu = true;
                else if (toOpen) isPCMenu = false;

                try
                {
                    bool rightMouse = Mouse.current != null && Mouse.current.rightButton.isPressed;
                    bool leftMouse = Mouse.current != null && Mouse.current.leftButton.isPressed;
                    bool rightGrab = ControllerInputPoller.instance != null && ControllerInputPoller.instance.rightGrab;
                    if (InputHandler.Instance != null)
                    {
                        InputHandler.Instance.RightGrip.IsPressed = rightMouse ? leftMouse : rightGrab;
                    }
                }
                catch { }

                if (menu == null)
                {
                    if (toOpen || keyboardOpen)
                    {
                        CreateMenu();
                        if (barkMenu && !isPCMenu)
                        {
                            CreateDualReferences();
                        }
                        else if (reference == null && !isPCMenu)
                        {
                            CreateReference(rightHanded);
                        }
                        RecenterMenu(rightHanded, isPCMenu);
                    }
                }
                else
                {
                    if (isSearching)
                    {
                        RecenterMenu(rightHanded, isPCMenu);
                        HandlePCTyping();
                        HandlePageInputs();
                    }
                    else if (toOpen || keyboardOpen)
                    {
                        RecenterMenu(rightHanded, isPCMenu);
                        HandlePageInputs();
                    }
                    else
                    {
                        if (shoulderCamera != null)
                        {
                            shoulderCamera.transform.Find("CM vcam1")?.gameObject.SetActive(true);
                        }

                        Rigidbody comp = menu.AddComponent(typeof(Rigidbody)) as Rigidbody;
                        if (rightHanded)
                        {
                            comp.linearVelocity = GorillaLocomotion.GTPlayer.Instance.RightHand.velocityTracker.GetAverageVelocity(true, 0);
                        }
                        else
                        {
                            comp.linearVelocity = GorillaLocomotion.GTPlayer.Instance.LeftHand.velocityTracker.GetAverageVelocity(true, 0);
                        }

                        Destroy(menu);
                        menu = null;

                        Destroy(reference);
                        reference = null;
                        DestroyDualReferences();
                        isPCMenu = false;
                    }
                }
            }
            catch (Exception exc)
            {
                NotificationLib.SendNotification(NotificationLib.NotificationType.Error, string.Format("{0} // Error initializing at {1}: {2}", PluginInfo.Name, exc.StackTrace, exc.Message));
            }

            try
            {
                if (fpsObject != null)
                {
                    fpsObject.text = isSearching ? "" : "FPS: " + Mathf.Ceil(1f / Time.unscaledDeltaTime).ToString();
                }

                if (!Loaded)
                {
                    Load();
                    Loaded = true;
                }

                for (int i = 0; i < buttons.Length; i++)
                {
                    ButtonInfo[] category = buttons[i];
                    if (category == null) continue;
                    for (int j = 0; j < category.Length; j++)
                    {
                        ButtonInfo button = category[j];
                        if (button != null && button.enabled && button.method != null)
                        {
                            try
                            {
                                button.method.Invoke();
                            }
                            catch (Exception exc)
                            {
                                NotificationLib.SendNotification(NotificationLib.NotificationType.Error, string.Format("{0} // Error with mod {1} at {2}: {3}", PluginInfo.Name, button.buttonText, exc.StackTrace, exc.Message));
                            }
                        }
                    }
                }

                Mods.PlayerOptionsManager.Update();
                KeybindManager.Update();
                if (Time.frameCount % 240 == 0) UpdateBoardText();
            }
            catch (Exception exc)
            {
                NotificationLib.SendNotification(NotificationLib.NotificationType.Error, string.Format("{0} // Error with executing mods at {1}: {2}", PluginInfo.Name, exc.StackTrace, exc.Message));
            }
        }

        private void OnDestroy()
        {
            if (menu != null)
            {
                Destroy(menu);
                menu = null;
            }

            if (reference != null)
            {
                Destroy(reference);
                reference = null;
            }

            DestroyDualReferences();
            barkMenuOpen = false;

            if (canvasObject != null)
            {
                Destroy(canvasObject);
                canvasObject = null;
            }

            CleanupResources();
            Instance = null;
        }

        [Setting] public static bool sideLayout;
        public static Color outlineColor = Color.blue;
        [Setting] public static bool showOutline;
        public static System.Collections.Generic.List<ButtonInfo> favoriteButtons = new System.Collections.Generic.List<ButtonInfo>();

        public static readonly Vector3 defaultMenuScale = new Vector3(0.1f, 0.3f, 0.3825f);
        public static float menuOpenTime;
        public static bool isMenuAnimating;

        public static int GetTotalButtonCount()
        {
            int count = 0;
            for (int i = 0; i < buttons.Length; i++)
            {
                if (i == 11 || i == 12 || i == 13 || i == 14 || i == 21 || i >= 19) continue;
                ButtonInfo[] category = buttons[i];
                if (category == null) continue;
                for (int j = 0; j < category.Length; j++)
                {
                    if (category[j] != null)
                        count++;
                }
            }
            return count;
        }

        private static readonly Dictionary<string, Mesh> roundedMeshCache = new Dictionary<string, Mesh>();
        public static Mesh GetRoundedBoxMesh(float width, float height, float cornerRadius) // vibecoded this shit
        {
            string key = $"{width:F3}_{height:F3}_{cornerRadius:F4}";
            if (roundedMeshCache.TryGetValue(key, out Mesh cached) && cached != null)
            {
                return cached;
            }

            Mesh mesh = new Mesh();
            mesh.name = $"RoundedBox_{key}";

            int n = 12;
            int p = 4 * n;

            float maxRadius = Mathf.Min(width, height) * 0.49f;
            float r = Mathf.Min(cornerRadius, maxRadius);
            float ry = Mathf.Clamp(r / width, 0.005f, 0.49f);
            float rz = Mathf.Clamp(r / height, 0.005f, 0.49f);

            Vector2[] perim = new Vector2[p];
            float[] cornerAngles = { 0f, Mathf.PI * 0.5f, Mathf.PI, Mathf.PI * 1.5f };
            Vector2[] centers =
            {
                new Vector2(0.5f - ry, 0.5f - rz),
                new Vector2(-0.5f + ry, 0.5f - rz),
                new Vector2(-0.5f + ry, -0.5f + rz),
                new Vector2(0.5f - ry, -0.5f + rz)
            };

            int idx = 0;
            for (int c = 0; c < 4; c++)
            {
                float baseAngle = cornerAngles[c];
                Vector2 center = centers[c];
                for (int s = 0; s < n; s++)
                {
                    float angle = baseAngle + (s / (float)n) * (Mathf.PI * 0.5f);
                    perim[idx++] = new Vector2(center.x + Mathf.Cos(angle) * ry, center.y + Mathf.Sin(angle) * rz);
                }
            }

            Vector3[] perimNormals = new Vector3[p];
            for (int i = 0; i < p; i++)
            {
                int prev = (i - 1 + p) % p;
                int next = (i + 1) % p;
                Vector2 dir = perim[next] - perim[prev];
                Vector2 norm2D = new Vector2(dir.y, -dir.x).normalized;
                perimNormals[i] = new Vector3(0f, norm2D.x, norm2D.y);
            }

            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> triangles = new List<int>();

            int frontCenterIdx = vertices.Count;
            vertices.Add(new Vector3(0.5f, 0f, 0f));
            normals.Add(new Vector3(1f, 0f, 0f));
            uvs.Add(new Vector2(0.5f, 0.5f));

            int frontStartIdx = vertices.Count;
            for (int i = 0; i < p; i++)
            {
                vertices.Add(new Vector3(0.5f, perim[i].x, perim[i].y));
                normals.Add(new Vector3(1f, 0f, 0f));
                uvs.Add(new Vector2(perim[i].x + 0.5f, perim[i].y + 0.5f));
            }

            for (int i = 0; i < p; i++)
            {
                int next = (i + 1) % p;
                triangles.Add(frontCenterIdx);
                triangles.Add(frontStartIdx + i);
                triangles.Add(frontStartIdx + next);
            }

            int backCenterIdx = vertices.Count;
            vertices.Add(new Vector3(-0.5f, 0f, 0f));
            normals.Add(new Vector3(-1f, 0f, 0f));
            uvs.Add(new Vector2(0.5f, 0.5f));

            int backStartIdx = vertices.Count;
            for (int i = 0; i < p; i++)
            {
                vertices.Add(new Vector3(-0.5f, perim[i].x, perim[i].y));
                normals.Add(new Vector3(-1f, 0f, 0f));
                uvs.Add(new Vector2(perim[i].x + 0.5f, perim[i].y + 0.5f));
            }

            for (int i = 0; i < p; i++)
            {
                int next = (i + 1) % p;
                triangles.Add(backCenterIdx);
                triangles.Add(backStartIdx + next);
                triangles.Add(backStartIdx + i);
            }

            for (int i = 0; i < p; i++)
            {
                int next = (i + 1) % p;
                Vector2 p0 = perim[i];
                Vector2 p1 = perim[next];
                Vector3 n0 = perimNormals[i];
                Vector3 n1 = perimNormals[next];

                int v0 = vertices.Count;
                int v1 = v0 + 1;
                int v2 = v0 + 2;
                int v3 = v0 + 3;
                vertices.Add(new Vector3(0.5f, p0.x, p0.y));
                vertices.Add(new Vector3(-0.5f, p0.x, p0.y));
                vertices.Add(new Vector3(-0.5f, p1.x, p1.y));
                vertices.Add(new Vector3(0.5f, p1.x, p1.y));

                normals.Add(n0);
                normals.Add(n0);
                normals.Add(n1);
                normals.Add(n1);

                float u0 = i / (float)p;
                float u1 = (i + 1) / (float)p;
                uvs.Add(new Vector2(u0, 1f));
                uvs.Add(new Vector2(u0, 0f));
                uvs.Add(new Vector2(u1, 0f));
                uvs.Add(new Vector2(u1, 1f));

                triangles.Add(v0);
                triangles.Add(v1);
                triangles.Add(v2);

                triangles.Add(v0);
                triangles.Add(v2);
                triangles.Add(v3);
            }

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();

            roundedMeshCache[key] = mesh;
            return mesh;
        }

        public static void ApplyRoundedMesh(GameObject obj, float width, float height, float cornerRadius)
        {
            if (obj == null) return;
            MeshFilter mf = obj.GetComponent<MeshFilter>();
            if (mf != null)
            {
                mf.sharedMesh = GetRoundedBoxMesh(width, height, cornerRadius);
            }
        }

        public static void OutlineObj(GameObject toOut, Color color1, Color color2, bool parentself = false, float thickness = 1)
        {
            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            UnityEngine.Object.Destroy(gameObject.GetComponent<BoxCollider>());
            UnityEngine.Object.Destroy(gameObject.GetComponent<Rigidbody>());
            gameObject.transform.parent = menu.transform;
            if (parentself)
                gameObject.transform.parent = toOut.transform.parent;

            gameObject.transform.rotation = Quaternion.identity;
            gameObject.transform.localPosition = toOut.transform.localPosition;
            gameObject.transform.localScale = toOut.transform.localScale + new Vector3(-0.01f / thickness, 0.01f * thickness, 0.0075f * thickness);
            if (Settings.roundedMenu && toOut != null)
            {
                MeshFilter outMf = toOut.GetComponent<MeshFilter>();
                if (outMf != null && outMf.sharedMesh != null)
                {
                    gameObject.GetComponent<MeshFilter>().sharedMesh = outMf.sharedMesh;
                }
            }
            Renderer r = gameObject.GetComponent<Renderer>();
            r.material.color = color1;
            if (buttonColors[1].isRainbow || buttonColors[1].copyRigColors)
            {
                ColorChanger cc = gameObject.AddComponent<ColorChanger>();
                cc.colorInfo = buttonColors[1];
                cc.Start();
            }
        }

        public static void ApplyOpenAnimation()
        {
            if (menu == null) return;

            if (!isMenuAnimating || openAnimIndex <= 0)
            {
                menu.transform.localScale = defaultMenuScale;
                return;
            }

            float elapsed = Time.unscaledTime - menuOpenTime;
            float duration = 0.16f;
            if (elapsed >= duration)
            {
                isMenuAnimating = false;
                menu.transform.localScale = defaultMenuScale;
                return;
            }

            float t = Mathf.Clamp01(elapsed / duration);
            Vector3 posOffset = Vector3.zero;
            Quaternion rotOffset = Quaternion.identity;
            float scaleMult = 1f;

            switch (openAnimIndex)
            {
                case 1:
                {
                    scaleMult = t < 0.7f ? Mathf.Lerp(0.85f, 1.08f, t / 0.7f) : Mathf.Lerp(1.08f, 1f, (t - 0.7f) / 0.3f);
                    break;
                }
                case 2:
                {
                    scaleMult = Mathf.Lerp(0.85f, 1f, 1f - Mathf.Pow(1f - t, 3f));
                    break;
                }
                case 3:
                {
                    float ease = 1f - Mathf.Pow(1f - t, 3f);
                    posOffset = -menu.transform.forward * ((1f - ease) * 0.12f);
                    break;
                }
                case 4:
                {
                    float ease = 1f - Mathf.Pow(1f - t, 3f);
                    posOffset = menu.transform.up * ((1f - ease) * 0.12f);
                    break;
                }
                case 5:
                {
                    float ease = 1f - Mathf.Pow(1f - t, 3f);
                    rotOffset = Quaternion.Euler((1f - ease) * 60f, 0f, 0f);
                    break;
                }
                case 6:
                {
                    float s = Mathf.Sin(-13f * (t + 1f) * Mathf.PI * 0.5f) * Mathf.Pow(2f, -10f * t) + 1f;
                    scaleMult = Mathf.Clamp(s, 0.85f, 1.12f);
                    break;
                }
            }

            menu.transform.position += posOffset;
            menu.transform.rotation *= rotOffset;
            menu.transform.localScale = defaultMenuScale * scaleMult;
        }

        public static void OpenGenesisFolder()
        {
            try
            {
                string path = ModsLib.GenesisDirectory;
                if (string.IsNullOrEmpty(path) || path == "uh oh!")
                {
                    NotificationLib.SendNotification(NotificationLib.NotificationType.Error, "Genesis path invalid");
                    return;
                }

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
                NotificationLib.SendNotification(NotificationLib.NotificationType.Info, "Opened Genesis folder");
            }
            catch (Exception ex)
            {
                NotificationLib.SendNotification(NotificationLib.NotificationType.Error, $"Failed to open Genesis folder: {ex.Message}");
            }
        }

        public static void CreateMenu()
        {
            if (Lockdown) return;
            
            menu = GameObject.CreatePrimitive(PrimitiveType.Cube);
            UnityEngine.Object.Destroy(menu.GetComponent<Rigidbody>());
            UnityEngine.Object.Destroy(menu.GetComponent<BoxCollider>());
            UnityEngine.Object.Destroy(menu.GetComponent<Renderer>());
            menuOpenTime = Time.unscaledTime;
            isMenuAnimating = (openAnimIndex > 0);
            menu.transform.localScale = defaultMenuScale;

            menuBackground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            UnityEngine.Object.Destroy(menuBackground.GetComponent<Rigidbody>());
            UnityEngine.Object.Destroy(menuBackground.GetComponent<BoxCollider>());
            menuBackground.transform.parent = menu.transform;
            menuBackground.transform.rotation = Quaternion.identity;
            menuBackground.transform.localScale = menuSize;
            menuBackground.GetComponent<Renderer>().material.color = backgroundColor.colors[0].color;
            menuBackground.transform.position = new Vector3(0.05f, 0f, 0f);
            ColorChanger bgChanger = menuBackground.AddComponent<ColorChanger>();
            bgChanger.colorInfo = backgroundColor;
            bgChanger.Start();
            if (Settings.roundedMenu) ApplyRoundedMesh(menuBackground, menuSize.y, menuSize.z, 0.025f);
            if (showOutline) OutlineObj(menuBackground, outlineColor, outlineColor, false, 3);

            canvasObject = new GameObject();
            canvasObject.transform.parent = menu.transform;
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            CanvasScaler canvasScaler = canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasScaler.dynamicPixelsPerUnit = 1000f;

            int lastPage = 0;
            if (buttonsType < buttons.Length)
            {
                lastPage = ((buttons[buttonsType].Length + buttonsPerPage - 1) / buttonsPerPage) - 1;
                if (lastPage < 0) lastPage = 0;
            }

            if (pageNumber > lastPage || pageNumber < 0)
            {
                pageNumber = 0;
            }

            Text text = new GameObject
            {
                transform =
                {
                    parent = canvasObject.transform
                }
            }.AddComponent<Text>();
            text.font = currentFont;
            text.text = isSearching ? "" : PluginInfo.Name;
            text.fontSize = 1;
            text.color = textColors[0];
            text.supportRichText = true;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 0;
            RectTransform component = text.GetComponent<RectTransform>();
            component.localPosition = Vector3.zero;
            component.sizeDelta = new Vector2(0.28f, 0.05f);
            component.position = new Vector3(0.06f, 0f, 0.165f);
            component.rotation = Quaternion.Euler(new Vector3(180f, 90f, 90f));

            if (fpsCounter)
            {
                fpsObject = new GameObject
                {
                    transform =
                    {
                        parent = canvasObject.transform
                    }
                }.AddComponent<Text>();
                fpsObject.font = currentFont;
                fpsObject.text = isSearching ? "" : "FPS: " + Mathf.Ceil(1f / Time.unscaledDeltaTime).ToString();
                fpsObject.color = textColors[0];
                fpsObject.fontSize = 1;
                fpsObject.supportRichText = true;
                fpsObject.fontStyle = FontStyle.Bold;
                fpsObject.alignment = TextAnchor.MiddleCenter;
                fpsObject.horizontalOverflow = UnityEngine.HorizontalWrapMode.Overflow;
                fpsObject.resizeTextForBestFit = true;
                fpsObject.resizeTextMinSize = 0;
                RectTransform component2 = fpsObject.GetComponent<RectTransform>();
                component2.localPosition = Vector3.zero;
                component2.sizeDelta = new Vector2(0.28f, 0.02f);
                component2.position = new Vector3(0.06f, 0f, 0.135f);
                component2.rotation = Quaternion.Euler(new Vector3(180f, 90f, 90f));
            }

            GameObject But1 = GameObject.CreatePrimitive(PrimitiveType.Cube);

            But1.GetComponent<BoxCollider>().isTrigger = true;

            But1.transform.parent = menu.transform;
            But1.transform.rotation = Quaternion.identity;
            But1.transform.localScale = new Vector3(0.05f, 0.1f, 0.08f);
            But1.transform.localPosition = new Vector3(0.56f, -0.45f, -0.57f);

            But1.GetComponent<Renderer>().material.color = buttonColors[0].colors[0].color;
            ColorChanger ccHome = But1.AddComponent<ColorChanger>();
            ccHome.colorInfo = buttonColors[0];
            ccHome.Start();
            if (Settings.roundedMenu) ApplyRoundedMesh(But1, 0.1f, 0.08f, 0.012f);
            if (showOutline)
            {
                OutlineObj(But1, outlineColor, outlineColor, false);
            }

            But1.AddComponent<Classes.Button>().relatedText = "home";

            RawImage homeImg = new GameObject
            {
                transform =
                {
                    parent = canvasObject.transform
                }
            }.AddComponent<RawImage>();

            homeImg.texture = ModsLib.GetHomeTexture();
            homeImg.color = textColors[0];

            RectTransform recct1 = homeImg.GetComponent<RectTransform>();

            recct1.localPosition = new Vector3(0.064f, -0.135f, -0.218f);
            recct1.sizeDelta = new Vector2(0.024f, 0.024f);
            recct1.rotation = Quaternion.Euler(new Vector3(180f, 90f, 90f));

            if (SettingsButton)
            {
                GameObject But = GameObject.CreatePrimitive(PrimitiveType.Cube);

                But.GetComponent<BoxCollider>().isTrigger = true;

                But.transform.parent = menu.transform;
                But.transform.rotation = Quaternion.identity;
                But.transform.localScale = new Vector3(0.05f, 0.1f, 0.08f);

                But.transform.localPosition = new Vector3(0.56f, -0.29f, -0.57f);

                But.GetComponent<Renderer>().material.color = buttonColors[0].colors[0].color;
                ColorChanger ccSettings = But.AddComponent<ColorChanger>();
                ccSettings.colorInfo = buttonColors[0];
                ccSettings.Start();
                if (Settings.roundedMenu) ApplyRoundedMesh(But, 0.1f, 0.08f, 0.012f);
                if (showOutline)
                {
                    OutlineObj(But, outlineColor, outlineColor, false);
                }

                But.AddComponent<Classes.Button>().relatedText = "Settings";

                RawImage settingsImg = new GameObject
                {
                    transform =
                    {
                        parent = canvasObject.transform
                    }
                }.AddComponent<RawImage>();

                settingsImg.texture = ModsLib.GetSettingsTexture();
                settingsImg.color = textColors[0];

                RectTransform recct = settingsImg.GetComponent<RectTransform>();

                recct.localPosition = new Vector3(0.064f, -0.087f, -0.218f);
                recct.sizeDelta = new Vector2(0.024f, 0.024f);
                recct.rotation = Quaternion.Euler(new Vector3(180f, 90f, 90f));
            }

            if (FolderButton)
            {
                GameObject folderBtn = GameObject.CreatePrimitive(PrimitiveType.Cube);

                folderBtn.GetComponent<BoxCollider>().isTrigger = true;

                folderBtn.transform.parent = menu.transform;
                folderBtn.transform.rotation = Quaternion.identity;
                folderBtn.transform.localScale = new Vector3(0.05f, 0.1f, 0.08f);
                folderBtn.transform.localPosition = new Vector3(0.56f, -0.13f, -0.57f);

                folderBtn.GetComponent<Renderer>().material.color = buttonColors[0].colors[0].color;
                ColorChanger ccFolder = folderBtn.AddComponent<ColorChanger>();
                ccFolder.colorInfo = buttonColors[0];
                ccFolder.Start();
                if (Settings.roundedMenu) ApplyRoundedMesh(folderBtn, 0.1f, 0.08f, 0.012f);
                if (showOutline)
                {
                    OutlineObj(folderBtn, outlineColor, outlineColor, false);
                }

                folderBtn.AddComponent<Classes.Button>().relatedText = "GenesisFolder";

                RawImage folderImg = new GameObject
                {
                    transform =
                    {
                        parent = canvasObject.transform
                    }
                }.AddComponent<RawImage>();

                folderImg.texture = ModsLib.GetFolderTexture();
                folderImg.color = textColors[0];

                RectTransform folderRect = folderImg.GetComponent<RectTransform>();

                folderRect.localPosition = new Vector3(0.064f, -0.039f, -0.218f);
                folderRect.sizeDelta = new Vector2(0.024f, 0.024f);
                folderRect.rotation = Quaternion.Euler(new Vector3(180f, 90f, 90f));
            }

            if (SearchButton)
            {
                GameObject searchBtn = GameObject.CreatePrimitive(PrimitiveType.Cube);
                searchBtn.GetComponent<BoxCollider>().isTrigger = true;
                searchBtn.transform.parent = menu.transform;
                searchBtn.transform.rotation = Quaternion.identity;
                searchBtn.transform.localScale = new Vector3(0.05f, 0.1f, 0.08f);
                searchBtn.transform.localPosition = new Vector3(0.56f, 0.03f, -0.57f);

                searchBtn.GetComponent<Renderer>().material.color = isSearching ? buttonColors[1].colors[0].color : buttonColors[0].colors[0].color;
                ColorChanger ccSearch = searchBtn.AddComponent<ColorChanger>();
                ccSearch.colorInfo = isSearching ? buttonColors[1] : buttonColors[0];
                ccSearch.Start();
                if (Settings.roundedMenu) ApplyRoundedMesh(searchBtn, 0.1f, 0.08f, 0.012f);
                if (showOutline)
                {
                    OutlineObj(searchBtn, outlineColor, outlineColor, false);
                }

                searchBtn.AddComponent<Classes.Button>().relatedText = "Search";

                RawImage searchImg = new GameObject
                {
                    transform =
                    {
                        parent = canvasObject.transform
                    }
                }.AddComponent<RawImage>();

                searchImg.texture = ModsLib.GetSearchTexture();
                searchImg.color = isSearching ? textColors[1] : textColors[0];

                RectTransform searchRect = searchImg.GetComponent<RectTransform>();
                searchRect.localPosition = new Vector3(0.064f, 0.009f, -0.218f);
                searchRect.sizeDelta = new Vector2(0.024f, 0.024f);
                searchRect.rotation = Quaternion.Euler(new Vector3(180f, 90f, 90f));
            }

            if (disconnectButton)
            {
                GameObject disconnectbutton = GameObject.CreatePrimitive(PrimitiveType.Cube);
                UnityEngine.Object.Destroy(disconnectbutton.GetComponent<Rigidbody>());
                disconnectbutton.GetComponent<BoxCollider>().isTrigger = true;
                disconnectbutton.transform.parent = menu.transform;
                disconnectbutton.transform.rotation = Quaternion.identity;
                disconnectbutton.transform.localScale = new Vector3(0.09f, 0.4f, 0.09f);
                disconnectbutton.transform.localPosition = new Vector3(0.56f, 0f, 0.57f);
                disconnectbutton.GetComponent<Renderer>().material.color = buttonColors[0].colors[0].color;
                ColorChanger ccDisc = disconnectbutton.AddComponent<ColorChanger>();
                ccDisc.colorInfo = buttonColors[0];
                ccDisc.Start();
                if (Settings.roundedMenu) ApplyRoundedMesh(disconnectbutton, 0.4f, 0.09f, 0.014f);
                if (showOutline)
                {
                    OutlineObj(disconnectbutton, outlineColor, outlineColor, false, 3);
                }
                disconnectbutton.AddComponent<Classes.Button>().relatedText = "Disconnect";

                Text discontext = new GameObject
                {
                    transform =
                    {
                        parent = canvasObject.transform
                    }
                }.AddComponent<Text>();
                discontext.text = "Leave";
                discontext.font = currentFont;
                discontext.fontSize = 1;
                discontext.color = textColors[0];
                discontext.alignment = TextAnchor.MiddleCenter;
                discontext.resizeTextForBestFit = true;
                discontext.resizeTextMinSize = 0;

                RectTransform rectt = discontext.GetComponent<RectTransform>();
                rectt.localPosition = Vector3.zero;
                rectt.sizeDelta = new Vector2(0.2f, 0.03f);
                rectt.localPosition = new Vector3(0.064f, 0f, 0.22f);
                rectt.rotation = Quaternion.Euler(new Vector3(180f, 90f, 90f));
            }

            if (isSearching && showSearchKeyboard)
            {
                RenderVirtualKeyboard();
                return;
            }

            if (isSearching)
            {
                RenderSearchResultsHeader();
            }

            if (Settings.pageButtonIndex < 2)
            {
                bool isSide = Settings.pageButtonIndex == 1;
                GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                UnityEngine.Object.Destroy(gameObject.GetComponent<Rigidbody>());
                gameObject.GetComponent<BoxCollider>().isTrigger = true;
                gameObject.transform.parent = menu.transform;
                gameObject.transform.rotation = Quaternion.identity;
                gameObject.transform.localScale = isSide ? new Vector3(0.045f, 0.25f, 0.8936298f) : new Vector3(0.06f, 0.25f, 0.06f);
                gameObject.transform.localPosition = isSide ? new Vector3(0.56f, 0.657f, 0.0063f) : new Vector3(0.56f, -0.37f, 0.555f);
                gameObject.GetComponent<Renderer>().material.color = buttonColors[0].colors[0].color;
                ColorChanger ccNext = gameObject.AddComponent<ColorChanger>();
                ccNext.colorInfo = buttonColors[0];
                ccNext.Start();
                if (Settings.roundedMenu) ApplyRoundedMesh(gameObject, isSide ? 0.25f : 0.25f, isSide ? 0.8936f : 0.06f, isSide ? 0.015f : 0.012f);
                gameObject.AddComponent<Classes.Button>().relatedText = "NextPage";
                if (showOutline)
                {
                    OutlineObj(gameObject, outlineColor, outlineColor, false, 3);
                }

                text = new GameObject
                {
                    transform =
                    {
                        parent = canvasObject.transform
                    }
                }.AddComponent<Text>();
                text.font = currentFont;
                text.text = "";
                text.fontSize = 1;
                text.color = textColors[0];
                text.alignment = TextAnchor.MiddleCenter;
                text.resizeTextForBestFit = true;
                text.resizeTextMinSize = 0;
                component = text.GetComponent<RectTransform>();
                component.localPosition = Vector3.zero;
                component.sizeDelta = new Vector2(0.2f, 0.03f);
                component.localPosition = isSide ? new Vector3(0.064f, 0.195f, 0f) : new Vector3(0.064f, -0.115f, 0.215f);
                component.rotation = Quaternion.Euler(new Vector3(180f, 90f, 90f));

                gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                UnityEngine.Object.Destroy(gameObject.GetComponent<Rigidbody>());
                gameObject.GetComponent<BoxCollider>().isTrigger = true;
                gameObject.transform.parent = menu.transform;
                gameObject.transform.rotation = Quaternion.identity;
                gameObject.transform.localScale = isSide ? new Vector3(0.045f, 0.25f, 0.8936298f) : new Vector3(0.06f, 0.25f, 0.06f);
                gameObject.transform.localPosition = isSide ? new Vector3(0.56f, -0.657f, 0.0063f) : new Vector3(0.56f, 0.37f, 0.555f);
                gameObject.GetComponent<Renderer>().material.color = buttonColors[0].colors[0].color;
                ColorChanger ccPrev = gameObject.AddComponent<ColorChanger>();
                ccPrev.colorInfo = buttonColors[0];
                ccPrev.Start();
                if (Settings.roundedMenu) ApplyRoundedMesh(gameObject, isSide ? 0.25f : 0.25f, isSide ? 0.8936f : 0.06f, isSide ? 0.015f : 0.012f);
                if (showOutline)
                {
                    OutlineObj(gameObject, outlineColor, outlineColor, false, 3);
                }
                gameObject.AddComponent<Classes.Button>().relatedText = "PreviousPage";

                text = new GameObject
                {
                    transform =
                    {
                        parent = canvasObject.transform
                    }
                }.AddComponent<Text>();
                text.font = currentFont;
                text.text = "";
                text.fontSize = 1;
                text.color = textColors[0];
                text.alignment = TextAnchor.MiddleCenter;
                text.resizeTextForBestFit = true;
                text.resizeTextMinSize = 0;
                component = text.GetComponent<RectTransform>();
                component.localPosition = Vector3.zero;
                component.sizeDelta = new Vector2(0.2f, 0.03f);
                component.localPosition = isSide ? new Vector3(0.064f, -0.195f, 0f) : new Vector3(0.064f, 0.115f, 0.215f);
                component.rotation = Quaternion.Euler(new Vector3(180f, 90f, 90f));
            }

            ButtonInfo[] activeButtons;

            if (buttonsType < buttons.Length)
            {
                activeButtons = buttons[buttonsType].Skip(pageNumber * buttonsPerPage).Take(buttonsPerPage).ToArray();
            }
            else
            {
                activeButtons = new ButtonInfo[0];
            }

            float startOffset = isSearching ? 0.055f : 0f;
            for (int i = 0; i < activeButtons.Length; i++)
            {
                CreateButton(i * 0.095f + startOffset, activeButtons[i]);
            }
        }

        public static void CreateButton(float offset, ButtonInfo method)
        {
            Vector3 btnScale = new Vector3(0.05f, 0.6f, 0.08f);
            Vector3 favScale = new Vector3(0.05f, 0.1f, 0.085f);
            float textWidth = 0.20f;
            float textHeight = 0.03f;

            switch (buttonStyleIndex)
            {
                case 1:
                    btnScale = new Vector3(0.035f, 0.62f, 0.065f);
                    favScale = new Vector3(0.035f, 0.085f, 0.065f);
                    textWidth = 0.20f;
                    textHeight = 0.026f;
                    break;
                case 2:
                    btnScale = new Vector3(0.085f, 0.58f, 0.082f);
                    favScale = new Vector3(0.085f, 0.095f, 0.082f);
                    textWidth = 0.19f;
                    textHeight = 0.030f;
                    break;
                case 3:
                    btnScale = new Vector3(0.05f, 0.6f, 0.078f);
                    favScale = new Vector3(0.05f, 0.095f, 0.078f);
                    textWidth = 0.19f;
                    textHeight = 0.028f;
                    break;
                case 4:
                    btnScale = new Vector3(0.045f, 0.52f, 0.072f);
                    favScale = new Vector3(0.045f, 0.08f, 0.072f);
                    textWidth = 0.18f;
                    textHeight = 0.026f;
                    break;
            }

            int maxFontSize = 40;
            switch (textSizeIndex)
            {
                case 1: maxFontSize = 24; break;
                case 2: maxFontSize = 32; break;
                case 3: maxFontSize = 44; break;
                default: maxFontSize = 40; break;
            }

            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            UnityEngine.Object.Destroy(gameObject.GetComponent<Rigidbody>());
            gameObject.GetComponent<BoxCollider>().isTrigger = true;
            gameObject.transform.parent = menu.transform;
            gameObject.transform.rotation = Quaternion.identity;
            gameObject.transform.localScale = btnScale;
            gameObject.transform.localPosition = new Vector3(0.56f, 0.1f, 0.25f - offset);
            Classes.Button btn = gameObject.AddComponent<Classes.Button>();
            btn.relatedText = method.buttonText;
            btn.buttonInfo = method;
            gameObject.GetComponent<Renderer>().material.color = buttonColors[0].colors[0].color;
            if (Settings.roundedMenu) ApplyRoundedMesh(gameObject, btnScale.y, btnScale.z, 0.014f);
            if (showOutline || buttonStyleIndex == 3)
            {
                OutlineObj(gameObject, outlineColor, outlineColor, false, 3);
            }

            GameObject gameObject1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            UnityEngine.Object.Destroy(gameObject1.GetComponent<Rigidbody>());
            gameObject1.GetComponent<BoxCollider>().isTrigger = true;
            gameObject1.transform.parent = menu.transform;
            gameObject1.transform.rotation = Quaternion.identity;
            gameObject1.transform.localScale = favScale;
            gameObject1.transform.localPosition = new Vector3(0.56f, -0.35f, 0.25f - offset);
            Classes.Button favBtn = gameObject1.AddComponent<Classes.Button>();
            favBtn.relatedText = "fav_" + method.buttonText;
            favBtn.buttonInfo = method;
            gameObject1.GetComponent<Renderer>().material.color = buttonColors[0].colors[0].color;
            ColorChanger ccFav = gameObject1.AddComponent<ColorChanger>();
            ccFav.colorInfo = buttonColors[0];
            ccFav.Start();
            if (Settings.roundedMenu) ApplyRoundedMesh(gameObject1, favScale.y, favScale.z, 0.012f);
            if (showOutline || buttonStyleIndex == 3)
            {
                OutlineObj(gameObject1, outlineColor, outlineColor, false, 3);
            }

            ColorChanger colorChanger = gameObject.AddComponent<ColorChanger>();
            if (method.enabled)
            {
                colorChanger.colorInfo = buttonColors[1];
            }
            else
            {
                colorChanger.colorInfo = buttonColors[0];
            }
            colorChanger.Start();

            Text text = new GameObject
            {
                transform =
                {
                    parent = canvasObject.transform
                }
            }.AddComponent<Text>();
            text.font = currentFont;
            text.text = method.buttonText;
            if (method.overlapText != null)
            {
                text.text = method.overlapText;
            }
            text.supportRichText = true;
            text.fontSize = 1;
            text.color = method.enabled ? textColors[1] : textColors[0];
            text.alignment = TextAnchor.MiddleCenter;
            text.fontStyle = FontStyle.Bold;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 0;
            text.resizeTextMaxSize = maxFontSize;
            RectTransform component = text.GetComponent<RectTransform>();
            component.localPosition = Vector3.zero;
            component.sizeDelta = new Vector2(textWidth, textHeight);
            component.localPosition = new Vector3(.064f, 0.03f, 0.095625f - offset * 0.3825f);
            component.rotation = Quaternion.Euler(new Vector3(180f, 90f, 90f));

            RawImage heartImg = new GameObject
            {
                transform =
                {
                    parent = canvasObject.transform
                }
            }.AddComponent<RawImage>();
            heartImg.texture = ModsLib.GetHeartTexture();
            heartImg.color = method.isFavorite ? Color.yellow : Color.white;
            RectTransform component1 = heartImg.GetComponent<RectTransform>();
            component1.localPosition = Vector3.zero;
            component1.sizeDelta = new Vector2(0.022f, 0.022f);
            component1.localPosition = new Vector3(.064f, -0.105f, 0.095625f - offset * 0.3825f);
            component1.rotation = Quaternion.Euler(new Vector3(180f, 90f, 90f));
        }

        public static void NextPage()
        {
            int lastPage = 0;
            if (buttonsType < buttons.Length)
            {
                lastPage = ((buttons[buttonsType].Length + buttonsPerPage - 1) / buttonsPerPage) - 1;
                if (lastPage < 0) lastPage = 0;
            }
            pageNumber++;
            if (pageNumber > lastPage)
            {
                pageNumber = 0;
            }
            RecreateMenu();
        }

        public static void PreviousPage()
        {
            int lastPage = 0;
            if (buttonsType < buttons.Length)
            {
                lastPage = ((buttons[buttonsType].Length + buttonsPerPage - 1) / buttonsPerPage) - 1;
                if (lastPage < 0) lastPage = 0;
            }
            pageNumber--;
            if (pageNumber < 0)
            {
                pageNumber = lastPage;
            }
            RecreateMenu();
        }

        private static void HandlePageInputs()
        {
            if (Settings.pageButtonIndex < 2 || InputHandler.Instance == null) return;

            if (Settings.pageButtonIndex == 2)
            {
                if (InputHandler.Instance.RightGrip.WasPressed || (isPCMenu && UnityInput.Current != null && UnityInput.Current.GetKeyDown(KeyCode.E)))
                {
                    MenuAudio.PlayClickSound();
                    NextPage();
                }
                else if (InputHandler.Instance.LeftGrip.WasPressed || (isPCMenu && UnityInput.Current != null && UnityInput.Current.GetKeyDown(KeyCode.Q)))
                {
                    MenuAudio.PlayClickSound();
                    PreviousPage();
                }
            }
            else if (Settings.pageButtonIndex == 3)
            {
                if (InputHandler.Instance.RightTrigger.WasPressed || (isPCMenu && UnityInput.Current != null && UnityInput.Current.GetKeyDown(KeyCode.RightArrow)))
                {
                    MenuAudio.PlayClickSound();
                    NextPage();
                }
                else if (InputHandler.Instance.LeftTrigger.WasPressed || (isPCMenu && UnityInput.Current != null && UnityInput.Current.GetKeyDown(KeyCode.LeftArrow)))
                {
                    MenuAudio.PlayClickSound();
                    PreviousPage();
                }
            }
        }

        public static void RecreateMenu()
        {
            if (menu != null)
            {
                UnityEngine.Object.Destroy(menu);
                menu = null;

                CreateMenu();
                isMenuAnimating = false;
                if (menu != null) menu.transform.localScale = defaultMenuScale;
                RecenterMenu(rightHanded, isPCMenu || (UnityInput.Current != null && UnityInput.Current.GetKey(keyboardButton)));
            }
        }

        public static void RecenterMenu(bool isRightHanded, bool isKeyboardCondition)
        {
            if (!isKeyboardCondition)
            {
                if (isSearching)
                {
                    menu.transform.position = pinnedMenuPosition;
                    menu.transform.rotation = pinnedMenuRotation;
                    ApplyOpenAnimation();
                    return;
                }

                if (barkMenu && barkMenuOpen)
                {
                    Transform head = GorillaTagger.Instance.headCollider.transform;
                    Vector3 forward = head.forward;
                    forward.y = 0f;
                    forward.Normalize();

                    Vector3 pos = head.position + forward * 0.55f + Vector3.down * 0.15f;
                    menu.transform.position = pos;
                    menu.transform.LookAt(head.position);
                    menu.transform.rotation = Quaternion.Euler(0f, menu.transform.eulerAngles.y, 0f) * Quaternion.Euler(-90f, 0f, -90f);
                    ApplyOpenAnimation();
                    return;
                }

                if (!isRightHanded)
                {
                    menu.transform.position = GorillaTagger.Instance.leftHandTransform.position;
                    menu.transform.rotation = GorillaTagger.Instance.leftHandTransform.rotation;
                }
                else
                {
                    menu.transform.position = GorillaTagger.Instance.rightHandTransform.position;
                    Vector3 rotation = GorillaTagger.Instance.rightHandTransform.rotation.eulerAngles;
                    rotation += new Vector3(0f, 0f, 180f);
                    menu.transform.rotation = Quaternion.Euler(rotation);
                }
                ApplyOpenAnimation();
            }
            else
            {
                try
                {
                    if (cachedTPC == null)
                    {
                        GameObject shoulderCam = GameObject.Find("Player Objects/Third Person Camera/Shoulder Camera") ?? GameObject.Find("Shoulder Camera");
                        if (shoulderCam != null)
                        {
                            cachedTPC = shoulderCam.GetComponent<Camera>();
                            shoulderCamera = shoulderCam;
                        }
                        if (cachedTPC == null)
                        {
                            foreach (Camera cam in Camera.allCameras)
                            {
                                if (cam != null && (cam.name.Contains("Shoulder") || cam.name.Contains("Third Person")))
                                {
                                    cachedTPC = cam;
                                    shoulderCamera = cam.gameObject;
                                    break;
                                }
                            }
                        }
                    }
                    TPC = cachedTPC;
                }
                catch { }

                if (shoulderCamera != null)
                {
                    Transform vcam = shoulderCamera.transform.Find("CM vcam1");
                    if (vcam != null)
                    {
                        vcam.gameObject.SetActive(false);
                    }
                }

                if (TPC != null)
                {
                    TPC.transform.position = new Vector3(-999f, -999f, -999f);
                    TPC.transform.rotation = Quaternion.identity;
                    menu.transform.parent = TPC.transform;
                    menu.transform.position = (TPC.transform.position + (Vector3.Scale(TPC.transform.forward, new Vector3(0.5f, 0.5f, 0.5f)))) + (Vector3.Scale(TPC.transform.up, new Vector3(-0.02f, -0.02f, -0.02f)));
                    Vector3 rot = TPC.transform.rotation.eulerAngles;
                    rot = new Vector3(rot.x - 90, rot.y + 90, rot.z);
                    menu.transform.rotation = Quaternion.Euler(rot);
                    ApplyOpenAnimation();

                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;

                    bool isClick = false;
                    try
                    {
                        if (Mouse.current != null)
                        {
                            isClick = Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.leftButton.isPressed;
                        }
                    }
                    catch { }

                    if (!isClick)
                    {
                        try
                        {
                            isClick = UnityInput.Current.GetMouseButtonDown(0) || UnityInput.Current.GetMouseButton(0) || Input.GetMouseButtonDown(0) || Input.GetMouseButton(0);
                        }
                        catch { }
                    }

                    if (isClick && menu != null)
                    {
                        Vector2 mousePos = Vector2.zero;
                        try { mousePos = Input.mousePosition; } catch { }
                        if (mousePos == Vector2.zero)
                        {
                            try { mousePos = (Vector2)UnityInput.Current.mousePosition; } catch { }
                        }
                        if (mousePos == Vector2.zero && Mouse.current != null)
                        {
                            try { mousePos = Mouse.current.position.ReadValue(); } catch { }
                        }

                        Classes.Button clickedButton = null;

                        Classes.Button[] allButtons = menu.GetComponentsInChildren<Classes.Button>();
                        if (allButtons != null && allButtons.Length > 0)
                        {
                            foreach (Classes.Button btn in allButtons)
                            {
                                if (btn == null) continue;
                                Renderer rend = btn.GetComponent<Renderer>();
                                if (rend == null) continue;

                                Vector3 c = rend.bounds.center;
                                Vector3 e = rend.bounds.extents;
                                Vector3[] corners = new Vector3[]
                                {
                                    TPC.WorldToScreenPoint(new Vector3(c.x - e.x, c.y - e.y, c.z - e.z)),
                                    TPC.WorldToScreenPoint(new Vector3(c.x + e.x, c.y - e.y, c.z - e.z)),
                                    TPC.WorldToScreenPoint(new Vector3(c.x - e.x, c.y + e.y, c.z - e.z)),
                                    TPC.WorldToScreenPoint(new Vector3(c.x + e.x, c.y + e.y, c.z - e.z)),
                                    TPC.WorldToScreenPoint(new Vector3(c.x - e.x, c.y - e.y, c.z + e.z)),
                                    TPC.WorldToScreenPoint(new Vector3(c.x + e.x, c.y - e.y, c.z + e.z)),
                                    TPC.WorldToScreenPoint(new Vector3(c.x - e.x, c.y + e.y, c.z + e.z)),
                                    TPC.WorldToScreenPoint(new Vector3(c.x + e.x, c.y + e.y, c.z + e.z)),
                                };

                                float minX = float.MaxValue, minY = float.MaxValue;
                                float maxX = float.MinValue, maxY = float.MinValue;
                                for (int k = 0; k < corners.Length; k++)
                                {
                                    if (corners[k].z > 0)
                                    {
                                        if (corners[k].x < minX) minX = corners[k].x;
                                        if (corners[k].x > maxX) maxX = corners[k].x;
                                        if (corners[k].y < minY) minY = corners[k].y;
                                        if (corners[k].y > maxY) maxY = corners[k].y;
                                    }
                                }

                                if (mousePos.x >= minX - 4f && mousePos.x <= maxX + 4f && mousePos.y >= minY - 4f && mousePos.y <= maxY + 4f)
                                {
                                    clickedButton = btn;
                                    break;
                                }
                            }
                        }

                        if (clickedButton == null)
                        {
                            Ray ray = TPC.ScreenPointToRay(mousePos);
                            RaycastHit[] hits = Physics.RaycastAll(ray, 100f, ~0, QueryTriggerInteraction.Collide);
                            if (hits != null && hits.Length > 0)
                            {
                                Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                                foreach (RaycastHit hit in hits)
                                {
                                    Classes.Button collide = hit.transform.GetComponent<Classes.Button>() ?? hit.transform.GetComponentInParent<Classes.Button>();
                                    if (collide != null)
                                    {
                                        clickedButton = collide;
                                        break;
                                    }
                                }
                            }
                        }

                        if (clickedButton == null && Screen.width > 0 && Screen.height > 0)
                        {
                            float vx = mousePos.x / Screen.width;
                            float vy = mousePos.y / Screen.height;
                            Ray vpRay = TPC.ViewportPointToRay(new Vector3(vx, vy, 0));
                            RaycastHit[] vpHits = Physics.RaycastAll(vpRay, 100f, ~0, QueryTriggerInteraction.Collide);
                            if (vpHits != null && vpHits.Length > 0)
                            {
                                Array.Sort(vpHits, (a, b) => a.distance.CompareTo(b.distance));
                                foreach (RaycastHit hit in vpHits)
                                {
                                    Classes.Button collide = hit.transform.GetComponent<Classes.Button>() ?? hit.transform.GetComponentInParent<Classes.Button>();
                                    if (collide != null)
                                    {
                                        clickedButton = collide;
                                        break;
                                    }
                                }
                            }
                        }

                        if (clickedButton != null)
                        {
                            clickedButton.Click();
                        }
                    }
                    else if (reference != null && (Mouse.current == null || !Mouse.current.leftButton.isPressed))
                    {
                        reference.transform.position = new Vector3(999f, -999f, -999f);
                    }
                }
            }
        }

        public static void CreateReference(bool isRightHanded)
        {
            reference = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            if (isRightHanded)
            {
                reference.transform.parent = GorillaTagger.Instance.leftHandTransform;
            }
            else
            {
                reference.transform.parent = GorillaTagger.Instance.rightHandTransform;
            }
            reference.GetComponent<Renderer>().material.color = backgroundColor.colors[0].color;
            reference.transform.localPosition = new Vector3(0f, -0.1f, 0f);
            reference.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            buttonCollider = reference.GetComponent<SphereCollider>();

            ColorChanger colorChanger = reference.AddComponent<ColorChanger>();
            colorChanger.colorInfo = backgroundColor;
            colorChanger.Start();
        }

        private static float lastChestTap;
        private static int chestTaps;
        private static bool chestContact;

        public static void CheckBarkMenu()
        {
            if (!barkMenuOpen)
            {
                Vector3 lHand = GorillaTagger.Instance.leftHandTransform.position;
                Vector3 rHand = GorillaTagger.Instance.rightHandTransform.position;
                Collider body = GorillaTagger.Instance.bodyCollider;

                bool touching = Vector3.Distance(body.ClosestPoint(lHand), lHand) < 0.14f ||
                                Vector3.Distance(body.ClosestPoint(rHand), rHand) < 0.14f;

                if (touching && !chestContact)
                {
                    if (Time.time - lastChestTap > 0.6f) chestTaps = 0;
                    lastChestTap = Time.time;
                    chestTaps++;

                    if (chestTaps >= 3)
                    {
                        chestTaps = 0;
                        barkMenuOpen = true;
                    }
                }
                chestContact = touching;
            }
            else
            {
                bool buttonPress = ControllerInputPoller.instance != null && 
                    ((!rightHanded && ControllerInputPoller.instance.leftControllerSecondaryButton) || 
                     (rightHanded && ControllerInputPoller.instance.rightControllerPrimaryButton));

                if (buttonPress)
                {
                    barkMenuOpen = false;
                }
            }
        }

        public static void ToggleFavorite(string buttonText, ButtonInfo target = null)
        {
            if (target == null)
            {
                target = GetIndex(buttonText);
            }

            if (target != null)
            {
                target.isFavorite = !target.isFavorite;

                if (target.isFavorite)
                {
                    if (!favoriteButtons.Contains(target))
                    {
                        favoriteButtons.Add(target);
                    }
                }
                else
                {
                    if (favoriteButtons.Contains(target))
                    {
                        favoriteButtons.Remove(target);
                    }
                }

                UpdateFavoritesCategory();
                RecreateMenu();
            }
        }

        public static void UpdateFavoritesCategory()
        {
            if (favoriteButtons.Count > 0)
            {
                if (buttons.Length > 11)
                {
                    buttons[11] = favoriteButtons.ToArray();
                }
            }
            else
            {
                if (buttons.Length > 11)
                {
                    buttons[11] = new ButtonInfo[0];
                }
            }
        }

        public static void Load()
        {
            UpdateFavoritesCategory();
            KeybindManager.RefreshKeybindMenu();
            MenuAudio.Initialize();
        }

        public static void Toggle(string buttonText, ButtonInfo target = null)
        {
            if (buttonText.StartsWith("fav_"))
            {
                ToggleFavorite(buttonText.Substring(4), target);
                return;
            }

            if (buttonText == "Search")
            {
                ToggleSearchMode();
                return;
            }

            if (buttonText.StartsWith("vkey_"))
            {
                string key = buttonText.Substring(5);
                if (searchQuery.Length < 24)
                {
                    searchQuery += key;
                    SettingsMods.UpdateSearchResults();
                    RecreateMenu();
                }
                return;
            }

            if (buttonText == "Search_Backspace")
            {
                if (searchQuery.Length > 0)
                {
                    searchQuery = searchQuery.Substring(0, searchQuery.Length - 1);
                    SettingsMods.UpdateSearchResults();
                    RecreateMenu();
                }
                return;
            }

            if (buttonText == "Search_Clear")
            {
                searchQuery = "";
                SettingsMods.UpdateSearchResults();
                RecreateMenu();
                return;
            }

            if (buttonText == "Search_Space")
            {
                if (searchQuery.Length < 24 && searchQuery.Length > 0 && !searchQuery.EndsWith(" "))
                {
                    searchQuery += " ";
                    SettingsMods.UpdateSearchResults();
                    RecreateMenu();
                }
                return;
            }

            if (buttonText == "Search_ShowResults")
            {
                showSearchKeyboard = false;
                buttonsType = 24;
                pageNumber = 0;
                SettingsMods.UpdateSearchResults();
                RecreateMenu();
                return;
            }

            if (buttonText == "Search_ShowKeyboard")
            {
                showSearchKeyboard = true;
                RecreateMenu();
                return;
            }

            if (buttonText == "PreviousPage")
            {
                PreviousPage();
                return;
            }
            else if (buttonText == "NextPage")
            {
                NextPage();
                return;
            }
            else if (buttonText.Equals("Disconnect", StringComparison.OrdinalIgnoreCase))
            {
                PhotonNetwork.Disconnect();
                NotificationLib.SendNotification(NotificationLib.NotificationType.Info, "Disconnected");
            }
            else if (buttonText == "Settings")
            {
                SettingsMods.MenuSettings();
            }
            else if (buttonText == "GenesisFolder")
            {
                OpenGenesisFolder();
            }
            else if (buttonText == "home")
            {
                if (isSearching)
                {
                    ToggleSearchMode();
                    return;
                }
                buttonsType = 0;
                pageNumber = 0;
                RecreateMenu();
                return;
            }
            else
            {
                if (target == null)
                {
                    target = GetIndex(buttonText);
                }

                if (target != null)
                {
                    if (CXS.ServerData.IsModDisabled(target.buttonText) && !CXS.ServerData.IsLocalAdmin())
                    {
                        NotificationLib.SendNotification(NotificationLib.NotificationType.Alert, $"{target.buttonText} is remotely disabled.", 3f);
                        return;
                    }

                    string displayName = string.IsNullOrEmpty(target.toolTip) ? target.buttonText : target.toolTip;

                    if (!string.IsNullOrEmpty(displayName))
                    {
                        if (target.isTogglable)
                        {
                            target.enabled = !target.enabled;
                            if (target.enabled)
                            {
                                if (target.enableMethod != null)
                                {
                                    try
                                    {
                                        target.enableMethod.Invoke();
                                        NotificationLib.SendNotification(NotificationLib.NotificationType.Enabled, displayName);
                                    }
                                    catch { }
                                }
                            }
                            else
                            {
                                if (target.disableMethod != null)
                                {
                                    try
                                    {
                                        target.disableMethod.Invoke();
                                        NotificationLib.SendNotification(NotificationLib.NotificationType.Disabled, displayName);
                                    }
                                    catch { }
                                }
                            }
                        }
                        else
                        {
                            if (target.method != null)
                            {
                                try
                                {
                                    target.method.Invoke();
                                    NotificationLib.SendNotification(NotificationLib.NotificationType.Info, displayName);
                                }
                                catch { }
                            }
                        }
                    }
                    else
                    {
                        if (target.isTogglable)
                        {
                            target.enabled = !target.enabled;
                            if (target.enabled)
                            {
                                if (target.enableMethod != null)
                                {
                                    try { target.enableMethod.Invoke(); } catch { }
                                }
                            }
                            else
                            {
                                if (target.disableMethod != null)
                                {
                                    try { target.disableMethod.Invoke(); } catch { }
                                }
                            }
                        }
                        else
                        {
                            if (target.method != null)
                            {
                                try { target.method.Invoke(); } catch { }
                            }
                        }
                    }
                }
                else
                {
                    NotificationLib.SendNotification(NotificationLib.NotificationType.Error, buttonText + " does not exist");
                }
            }

            if (buttonsType == 10)
            {
                SettingsMods.UpdateEnabledMods();
            }
            RecreateMenu();
        }

        public static GradientColorKey[] GetSolidGradient(Color color)
        {
            return new GradientColorKey[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) };
        }

        private static readonly Dictionary<string, ButtonInfo> buttonLookup = buildbl();

        private static Dictionary<string, ButtonInfo> buildbl()
        {
            Dictionary<string, ButtonInfo> lookup = new Dictionary<string, ButtonInfo>(GetTotalButtonCount(), StringComparer.Ordinal);

            for (int i = 0; i < buttons.Length; i++)
            {
                ButtonInfo[] cat = buttons[i];
                if (cat == null) continue;

                for (int j = 0; j < cat.Length; j++)
                {
                    ButtonInfo b = cat[j];
                    if (b == null) continue;

                    if (!string.IsNullOrEmpty(b.buttonText))
                        lookup[b.buttonText] = b;

                    if (!string.IsNullOrEmpty(b.overlapText))
                        lookup[b.overlapText] = b;
                }
            }

            return lookup;
        }

        public static ButtonInfo GetIndex(string buttonText)
        {
            if (string.IsNullOrEmpty(buttonText)) return null;
            buttonLookup.TryGetValue(buttonText, out ButtonInfo button);
            return button;
        }

        public static void Change(string buttonText, ref int index, string[] names, Action sideEffect = null, string prefix = null)
        {
            if (names == null || names.Length == 0) return;
            index = (index + 1) % names.Length;

            ButtonInfo btn = GetIndex(buttonText);
            if (btn != null)
            {
                if (prefix == null)
                {
                    int colon = btn.overlapText != null ? btn.overlapText.IndexOf(':') : -1;
                    prefix = colon >= 0 ? btn.overlapText.Substring(0, colon + 2) : $"{buttonText}: ";
                }
                btn.overlapText = prefix + names[index];
                buttonLookup[btn.overlapText] = btn;
            }

            sideEffect?.Invoke();
        }

        public static void UpdateBoardText()
        {
            if (cocHeading == null)
                cocHeading = GameObject.Find("Environment Objects/LocalObjects_Prefab/TreeRoom/CodeOfConductHeadingText") ?? GameObject.Find("CodeOfConductHeadingText");

            if (cocBody == null)
                cocBody = GameObject.Find("Environment Objects/LocalObjects_Prefab/TreeRoom/COCBodyText_TitleData") ?? GameObject.Find("COCBodyText_TitleData");

            if (motdHeading == null)
                motdHeading = GameObject.Find("Environment Objects/LocalObjects_Prefab/TreeRoom/motdHeadingText") ?? GameObject.Find("motdHeadingText");

            if (motdBody == null)
                motdBody = GameObject.Find("Environment Objects/LocalObjects_Prefab/TreeRoom/motdBodyText") ?? GameObject.Find("motdBodyText");

            if (shoulderCamera == null)
                shoulderCamera = GameObject.Find("Shoulder Camera");

            if (cocHeading != null && cocHeading.TryGetComponent<TMP_Text>(out var cocHeadText))
            {
                cocHeadText.text = "<color=blue>ShibaGT Genesis Reborn</color>".ToUpper();
                cocHeadText.fontSize = 75f;
            }

            if (cocBody != null)
            {
                if (cocBody.TryGetComponent<PlayFabTitleDataTextDisplay>(out var cocDisplay))
                    cocDisplay.enabled = false;

                if (cocBody.TryGetComponent<TMP_Text>(out var cocBodyText))
                {
                    cocBodyText.richText = true;
                    cocBodyText.text = $"\nWelcome To ShibaGT Genesis Reborn!\nThis is a Remake of the Longest Lasting Paid Mod Menu Shiba GT Genesis!\nWe currently have {GetTotalButtonCount()} total mods right now".ToUpper();
                }
            }

            if (motdHeading != null && motdHeading.TryGetComponent<TMP_Text>(out var motdHeadText))
            {
                motdHeadText.text = "<color=blue>ShibaGT Genesis Reborn</color>".ToUpper();
            }

            if (motdBody != null)
            {
                if (motdBody.TryGetComponent<PlayFabTitleDataTextDisplay>(out var motdDisplay))
                    motdDisplay.enabled = false;

                if (motdBody.TryGetComponent<TMP_Text>(out var motdBodyText))
                {
                    motdBodyText.text = "Credits to ShibaGT/TAI for making the original menu!\nThis is just a remake!\n<color=red>We Are Not Responsible For Any Bans Using This Mod Menu!</color>".ToUpper();
                }
            }
        }

        public static void CleanupResources()
        {
            if (favoriteButtons != null)
            {
                favoriteButtons.Clear();
            }

            cocHeading = null;
            cocBody = null;
            motdHeading = null;
            motdBody = null;
            shoulderCamera = null;
            cachedTPC = null;
        }

        private static readonly string[] KeyboardRow0 = new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" };
        private static readonly string[] KeyboardRow1 = new string[] { "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P" };
        private static readonly string[] KeyboardRow2 = new string[] { "A", "S", "D", "F", "G", "H", "J", "K", "L" };
        private static readonly string[] KeyboardRow3 = new string[] { "Z", "X", "C", "V", "B", "N", "M" };

        private static void CreateKey(float posX, float posY, float posZ, float width, float height, string label, string relatedText)
        {
            GameObject keyObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            UnityEngine.Object.Destroy(keyObj.GetComponent<Rigidbody>());
            keyObj.GetComponent<BoxCollider>().isTrigger = true;
            keyObj.transform.parent = menu.transform;
            keyObj.transform.rotation = Quaternion.identity;
            keyObj.transform.localScale = new Vector3(0.05f, width, height);
            keyObj.transform.localPosition = new Vector3(posX, posY, posZ);

            keyObj.GetComponent<Renderer>().material.color = buttonColors[0].colors[0].color;
            ColorChanger cc = keyObj.AddComponent<ColorChanger>();
            cc.colorInfo = buttonColors[0];
            cc.Start();
            if (Settings.roundedMenu) ApplyRoundedMesh(keyObj, width, height, 0.008f);
            if (showOutline) OutlineObj(keyObj, outlineColor, outlineColor, false, 2);

            Classes.Button btn = keyObj.AddComponent<Classes.Button>();
            btn.relatedText = relatedText;

            Text keyText = new GameObject
            {
                transform = { parent = canvasObject.transform }
            }.AddComponent<Text>();
            keyText.font = currentFont;
            keyText.text = label;
            keyText.fontSize = 1;
            keyText.color = textColors[0];
            keyText.alignment = TextAnchor.MiddleCenter;
            keyText.fontStyle = FontStyle.Bold;
            keyText.resizeTextForBestFit = true;
            keyText.resizeTextMinSize = 0;

            RectTransform rect = keyText.GetComponent<RectTransform>();
            rect.localPosition = Vector3.zero;
            rect.sizeDelta = new Vector2(width * 0.3f, height * 0.3825f);
            rect.localPosition = new Vector3(0.064f, posY * 0.3f, posZ * 0.3825f);
            rect.rotation = Quaternion.Euler(new Vector3(180f, 90f, 90f));
        }

        private static void RenderVirtualKeyboard()
        {
            GameObject searchBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
            UnityEngine.Object.Destroy(searchBox.GetComponent<Rigidbody>());
            searchBox.GetComponent<BoxCollider>().isTrigger = true;
            searchBox.transform.parent = menu.transform;
            searchBox.transform.rotation = Quaternion.identity;
            searchBox.transform.localScale = new Vector3(0.05f, 0.88f, 0.09f);
            searchBox.transform.localPosition = new Vector3(0.56f, 0f, 0.33f);
            searchBox.GetComponent<Renderer>().material.color = buttonColors[0].colors[0].color;
            ColorChanger ccBox = searchBox.AddComponent<ColorChanger>();
            ccBox.colorInfo = buttonColors[0];
            ccBox.Start();
            if (Settings.roundedMenu) ApplyRoundedMesh(searchBox, 0.88f, 0.09f, 0.012f);
            if (showOutline) OutlineObj(searchBox, outlineColor, outlineColor, false, 2);

            Text searchBoxText = new GameObject
            {
                transform = { parent = canvasObject.transform }
            }.AddComponent<Text>();
            searchBoxText.font = currentFont;
            string displayText = string.IsNullOrEmpty(searchQuery) ? "<color=grey>Type here...</color>" : searchQuery + "_";
            searchBoxText.text = $"<color=yellow>Search:</color> {displayText}";
            searchBoxText.supportRichText = true;
            searchBoxText.fontSize = 1;
            searchBoxText.color = textColors[0];
            searchBoxText.alignment = TextAnchor.MiddleCenter;
            searchBoxText.fontStyle = FontStyle.Bold;
            searchBoxText.resizeTextForBestFit = true;
            searchBoxText.resizeTextMinSize = 0;

            RectTransform rectBox = searchBoxText.GetComponent<RectTransform>();
            rectBox.localPosition = Vector3.zero;
            rectBox.sizeDelta = new Vector2(0.26f, 0.035f);
            rectBox.localPosition = new Vector3(0.064f, 0f, 0.33f * 0.3825f);
            rectBox.rotation = Quaternion.Euler(new Vector3(180f, 90f, 90f));

            float z0 = 0.20f;
            for (int i = 0; i < KeyboardRow0.Length; i++)
            {
                float y = 0.405f - i * 0.09f;
                CreateKey(0.56f, y, z0, 0.08f, 0.075f, KeyboardRow0[i], "vkey_" + KeyboardRow0[i]);
            }

            float z1 = 0.105f;
            for (int i = 0; i < KeyboardRow1.Length; i++)
            {
                float y = 0.405f - i * 0.09f;
                CreateKey(0.56f, y, z1, 0.08f, 0.075f, KeyboardRow1[i], "vkey_" + KeyboardRow1[i]);
            }

            float z2 = 0.01f;
            for (int i = 0; i < KeyboardRow2.Length; i++)
            {
                float y = 0.36f - i * 0.09f;
                CreateKey(0.56f, y, z2, 0.082f, 0.075f, KeyboardRow2[i], "vkey_" + KeyboardRow2[i]);
            }

            float z3 = -0.085f;
            for (int i = 0; i < KeyboardRow3.Length; i++)
            {
                float y = 0.36f - i * 0.09f;
                CreateKey(0.56f, y, z3, 0.082f, 0.075f, KeyboardRow3[i], "vkey_" + KeyboardRow3[i]);
            }
            CreateKey(0.56f, -0.315f, z3, 0.17f, 0.075f, "<-", "Search_Backspace");

            float z4 = -0.18f;
            CreateKey(0.56f, 0.27f, z4, 0.28f, 0.075f, "Space", "Search_Space");
            CreateKey(0.56f, -0.01f, z4, 0.22f, 0.075f, "Clear", "Search_Clear");
            int matchCount = Buttons.buttons.Length > 24 && Buttons.buttons[24] != null ? Buttons.buttons[24].Length : 0;
            CreateKey(0.56f, -0.28f, z4, 0.28f, 0.075f, $"Results ({matchCount})", "Search_ShowResults");
        }

        private static void RenderSearchResultsHeader()
        {
            int matchCount = Buttons.buttons.Length > 24 && Buttons.buttons[24] != null ? Buttons.buttons[24].Length : 0;
            CreateKey(0.56f, 0f, 0.38f, 0.88f, 0.065f, $"Query: \"{searchQuery}\" ({matchCount}) - Tap to Edit", "Search_ShowKeyboard");
        }

        public static void CreateDualReferences()
        {
            DestroyDualReferences();

            if (reference != null)
            {
                UnityEngine.Object.Destroy(reference);
                reference = null;
                buttonCollider = null;
            }

            if (GorillaTagger.Instance == null) return;

            leftReference = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leftReference.transform.parent = GorillaTagger.Instance.leftHandTransform;
            leftReference.GetComponent<Renderer>().material.color = backgroundColor.colors[0].color;
            leftReference.transform.localPosition = new Vector3(0f, -0.1f, 0f);
            leftReference.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            leftButtonCollider = leftReference.GetComponent<SphereCollider>();

            ColorChanger ccL = leftReference.AddComponent<ColorChanger>();
            ccL.colorInfo = backgroundColor;
            ccL.Start();

            rightReference = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rightReference.transform.parent = GorillaTagger.Instance.rightHandTransform;
            rightReference.GetComponent<Renderer>().material.color = backgroundColor.colors[0].color;
            rightReference.transform.localPosition = new Vector3(0f, -0.1f, 0f);
            rightReference.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            rightButtonCollider = rightReference.GetComponent<SphereCollider>();

            ColorChanger ccR = rightReference.AddComponent<ColorChanger>();
            ccR.colorInfo = backgroundColor;
            ccR.Start();
        }

        public static void DestroyDualReferences()
        {
            if (leftReference != null)
            {
                UnityEngine.Object.Destroy(leftReference);
                leftReference = null;
                leftButtonCollider = null;
            }
            if (rightReference != null)
            {
                UnityEngine.Object.Destroy(rightReference);
                rightReference = null;
                rightButtonCollider = null;
            }
        }

        public static void ToggleSearchMode()
        {
            isSearching = !isSearching;
            if (isSearching)
            {
                showSearchKeyboard = true;
                buttonsType = 24;
                pageNumber = 0;
                SettingsMods.UpdateSearchResults();

                if (menu != null && !isPCMenu)
                {
                    pinnedMenuPosition = menu.transform.position;
                    Vector3 headPos = GorillaTagger.Instance != null && GorillaTagger.Instance.headCollider != null
                        ? GorillaTagger.Instance.headCollider.transform.position
                        : (Camera.main != null ? Camera.main.transform.position : menu.transform.position + Vector3.back);

                    Vector3 toHead = headPos - menu.transform.position;
                    toHead.y = 0f;
                    float yaw = toHead.sqrMagnitude > 0.001f ? Mathf.Atan2(toHead.x, toHead.z) * Mathf.Rad2Deg : 0f;

                    pinnedMenuRotation = Quaternion.Euler(-90f, yaw - 90f, 0f);
                    menu.transform.position = pinnedMenuPosition;
                    menu.transform.rotation = pinnedMenuRotation;
                    menu.transform.parent = null;
                    CreateDualReferences();
                }
            }
            else
            {
                showSearchKeyboard = false;
                searchQuery = "";
                buttonsType = 0;
                pageNumber = 0;
                DestroyDualReferences();
                if (reference == null && !isPCMenu)
                {
                    CreateReference(rightHanded);
                }
            }
            RecreateMenu();
        }

        private readonly struct TypeKey
        {
            public readonly Key InputKey;
            public readonly KeyCode LegacyKey;
            public readonly char Lower;
            public readonly char Upper;

            public TypeKey(Key inputKey, KeyCode legacyKey, char lower, char upper)
            {
                InputKey = inputKey;
                LegacyKey = legacyKey;
                Lower = lower;
                Upper = upper;
            }

            public TypeKey(Key inputKey, KeyCode legacyKey, char c) : this(inputKey, legacyKey, c, c) { }
        }

        private static readonly TypeKey[] PCKeys = new TypeKey[]
        {
            new TypeKey(Key.A, KeyCode.A, 'a', 'A'),
            new TypeKey(Key.B, KeyCode.B, 'b', 'B'),
            new TypeKey(Key.C, KeyCode.C, 'c', 'C'),
            new TypeKey(Key.D, KeyCode.D, 'd', 'D'),
            new TypeKey(Key.E, KeyCode.E, 'e', 'E'),
            new TypeKey(Key.F, KeyCode.F, 'f', 'F'),
            new TypeKey(Key.G, KeyCode.G, 'g', 'G'),
            new TypeKey(Key.H, KeyCode.H, 'h', 'H'),
            new TypeKey(Key.I, KeyCode.I, 'i', 'I'),
            new TypeKey(Key.J, KeyCode.J, 'j', 'J'),
            new TypeKey(Key.K, KeyCode.K, 'k', 'K'),
            new TypeKey(Key.L, KeyCode.L, 'l', 'L'),
            new TypeKey(Key.M, KeyCode.M, 'm', 'M'),
            new TypeKey(Key.N, KeyCode.N, 'n', 'N'),
            new TypeKey(Key.O, KeyCode.O, 'o', 'O'),
            new TypeKey(Key.P, KeyCode.P, 'p', 'P'),
            new TypeKey(Key.Q, KeyCode.Q, 'q', 'Q'),
            new TypeKey(Key.R, KeyCode.R, 'r', 'R'),
            new TypeKey(Key.S, KeyCode.S, 's', 'S'),
            new TypeKey(Key.T, KeyCode.T, 't', 'T'),
            new TypeKey(Key.U, KeyCode.U, 'u', 'U'),
            new TypeKey(Key.V, KeyCode.V, 'v', 'V'),
            new TypeKey(Key.W, KeyCode.W, 'w', 'W'),
            new TypeKey(Key.X, KeyCode.X, 'x', 'X'),
            new TypeKey(Key.Y, KeyCode.Y, 'y', 'Y'),
            new TypeKey(Key.Z, KeyCode.Z, 'z', 'Z'),
            new TypeKey(Key.Digit0, KeyCode.Alpha0, '0', ')'),
            new TypeKey(Key.Digit1, KeyCode.Alpha1, '1', '!'),
            new TypeKey(Key.Digit2, KeyCode.Alpha2, '2', '@'),
            new TypeKey(Key.Digit3, KeyCode.Alpha3, '3', '#'),
            new TypeKey(Key.Digit4, KeyCode.Alpha4, '4', '$'),
            new TypeKey(Key.Digit5, KeyCode.Alpha5, '5', '%'),
            new TypeKey(Key.Digit6, KeyCode.Alpha6, '6', '^'),
            new TypeKey(Key.Digit7, KeyCode.Alpha7, '7', '&'),
            new TypeKey(Key.Digit8, KeyCode.Alpha8, '8', '*'),
            new TypeKey(Key.Digit9, KeyCode.Alpha9, '9', '('),
            new TypeKey(Key.Numpad0, KeyCode.Keypad0, '0'),
            new TypeKey(Key.Numpad1, KeyCode.Keypad1, '1'),
            new TypeKey(Key.Numpad2, KeyCode.Keypad2, '2'),
            new TypeKey(Key.Numpad3, KeyCode.Keypad3, '3'),
            new TypeKey(Key.Numpad4, KeyCode.Keypad4, '4'),
            new TypeKey(Key.Numpad5, KeyCode.Keypad5, '5'),
            new TypeKey(Key.Numpad6, KeyCode.Keypad6, '6'),
            new TypeKey(Key.Numpad7, KeyCode.Keypad7, '7'),
            new TypeKey(Key.Numpad8, KeyCode.Keypad8, '8'),
            new TypeKey(Key.Numpad9, KeyCode.Keypad9, '9'),
            new TypeKey(Key.Minus, KeyCode.Minus, '-', '_'),
            new TypeKey(Key.NumpadMinus, KeyCode.KeypadMinus, '-'),
            new TypeKey(Key.Period, KeyCode.Period, '.', '>'),
            new TypeKey(Key.NumpadPeriod, KeyCode.KeypadPeriod, '.'),
            new TypeKey(Key.Slash, KeyCode.Slash, '/', '?'),
            new TypeKey(Key.NumpadDivide, KeyCode.KeypadDivide, '/')
        };

        private static void HandlePCTyping()
        {
            try
            {
                Keyboard kb = Keyboard.current;
                bool isShift = (kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed))
                    || (UnityInput.Current != null && (UnityInput.Current.GetKey(KeyCode.LeftShift) || UnityInput.Current.GetKey(KeyCode.RightShift)));

                if ((kb != null && kb.escapeKey.wasPressedThisFrame) || (UnityInput.Current != null && UnityInput.Current.GetKeyDown(KeyCode.Escape)))
                {
                    ToggleSearchMode();
                    return;
                }

                if ((kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)) || (UnityInput.Current != null && (UnityInput.Current.GetKeyDown(KeyCode.Return) || UnityInput.Current.GetKeyDown(KeyCode.KeypadEnter))))
                {
                    showSearchKeyboard = false;
                    buttonsType = 24;
                    pageNumber = 0;
                    SettingsMods.UpdateSearchResults();
                    RecreateMenu();
                    return;
                }

                bool changed = false;
                if ((kb != null && kb.backspaceKey.wasPressedThisFrame) || (UnityInput.Current != null && UnityInput.Current.GetKeyDown(KeyCode.Backspace)))
                {
                    if (searchQuery.Length > 0)
                    {
                        searchQuery = searchQuery.Substring(0, searchQuery.Length - 1);
                        changed = true;
                    }
                }
                else if ((kb != null && kb.spaceKey.wasPressedThisFrame) || (UnityInput.Current != null && UnityInput.Current.GetKeyDown(KeyCode.Space)))
                {
                    if (searchQuery.Length < 24 && searchQuery.Length > 0 && !searchQuery.EndsWith(" "))
                    {
                        searchQuery += ' ';
                        changed = true;
                    }
                }
                else
                {
                    for (int i = 0; i < PCKeys.Length; i++)
                    {
                        TypeKey k = PCKeys[i];
                        if ((kb != null && kb[k.InputKey].wasPressedThisFrame) || (UnityInput.Current != null && UnityInput.Current.GetKeyDown(k.LegacyKey)))
                        {
                            if (searchQuery.Length < 24)
                            {
                                searchQuery += isShift ? k.Upper : k.Lower;
                                changed = true;
                                break;
                            }
                        }
                    }
                }

                if (changed)
                {
                    SettingsMods.UpdateSearchResults();
                    RecreateMenu();
                }
            }
            catch { }
        }

        public static bool RequireMasterClient(string action)
        {
            bool isMaster = (PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient) ||
                            (NetworkSystem.Instance != null &&
                             NetworkSystem.Instance.InRoom &&
                             NetworkSystem.Instance.IsMasterClient);

            if (isMaster) return true;

            NotificationLib.SendNotification(
                NotificationLib.NotificationType.Alert,
                $"{action} needs master");
            return false;
        }

        public static bool isPCMenu = false;
        public static GameObject menu;
        public static GameObject menuBackground;
        public static GameObject reference;
        public static GameObject leftReference;
        public static GameObject rightReference;
        public static GameObject canvasObject;

        public static SphereCollider buttonCollider;
        public static SphereCollider leftButtonCollider;
        public static SphereCollider rightButtonCollider;
        public static Camera TPC;
        public static Text fpsObject;

        private static GameObject cocHeading;
        private static GameObject cocBody;
        private static GameObject motdHeading;
        private static GameObject motdBody;
        private static GameObject shoulderCamera;
        private static Camera cachedTPC;

        public static int pageNumber = 0;
        public static int buttonsType = 0;
    }
}
