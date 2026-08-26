using BepInEx;
using Photon.Pun;
using ShibaGTGenesisReborn.Classes;
using ShibaGTGenesisReborn.Libs;
using System;
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
            StreamerMode.EnsureInitialized();
            Preferences.Load();
        }

        private void Update()
        {
            if (Lockdown) return;
            
            try
            {
                bool toOpen = ControllerInputPoller.instance != null && ((!rightHanded && ControllerInputPoller.instance.leftControllerSecondaryButton) || (rightHanded && ControllerInputPoller.instance.rightControllerPrimaryButton));
                bool keyboardOpen = UnityInput.Current.GetKey(keyboardButton);
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
                        if (reference == null)
                        {
                            CreateReference(rightHanded);
                        }
                        RecenterMenu(rightHanded, keyboardOpen);
                    }
                }
                else
                {
                    if ((toOpen || keyboardOpen))
                    {
                        RecenterMenu(rightHanded, keyboardOpen);
                    }
                    else
                    {
                        if (shoulderCamera != null)
                        {
                            shoulderCamera.transform.Find("CM vcam1").gameObject.SetActive(true);
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
                    fpsObject.text = "FPS: " + Mathf.Ceil(1f / Time.unscaledDeltaTime).ToString();
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

        public static int GetTotalButtonCount()
        {
            int count = 0;
            for (int i = 0; i < buttons.Length; i++)
            {
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
            Renderer r = gameObject.GetComponent<Renderer>();
            r.material.color = color1;
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
            menu.transform.localScale = new Vector3(0.1f, 0.3f, 0.3825f);

            menuBackground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            UnityEngine.Object.Destroy(menuBackground.GetComponent<Rigidbody>());
            UnityEngine.Object.Destroy(menuBackground.GetComponent<BoxCollider>());
            menuBackground.transform.parent = menu.transform;
            menuBackground.transform.rotation = Quaternion.identity;
            menuBackground.transform.localScale = menuSize;
            menuBackground.GetComponent<Renderer>().material.color = backgroundColor.colors[0].color;
            menuBackground.transform.position = new Vector3(0.05f, 0f, 0f);
            menuBackground.GetComponent<Renderer>().material.color = Color.black;
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
            text.text = PluginInfo.Name;
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
                fpsObject.text = "FPS: " + Mathf.Ceil(1f / Time.unscaledDeltaTime).ToString();
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

            var But1 = GameObject.CreatePrimitive(PrimitiveType.Cube);

            But1.GetComponent<BoxCollider>().isTrigger = true;

            But1.transform.parent = menu.transform;
            But1.transform.rotation = Quaternion.identity;
            But1.transform.localScale = new Vector3(0.05f, 0.1f, 0.08f);
            But1.transform.localPosition = new Vector3(0.56f, -0.45f, -0.57f);

            But1.GetComponent<Renderer>().material.color = new Color(0.06f, 0.06f, 0.06f);
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
                var But = GameObject.CreatePrimitive(PrimitiveType.Cube);

                But.GetComponent<BoxCollider>().isTrigger = true;

                But.transform.parent = menu.transform;
                But.transform.rotation = Quaternion.identity;
                But.transform.localScale = new Vector3(0.05f, 0.1f, 0.08f);

                But.transform.localPosition = new Vector3(0.56f, -0.29f, -0.57f);

                But.GetComponent<Renderer>().material.color = new Color(0.06f, 0.06f, 0.06f);
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
                var folderBtn = GameObject.CreatePrimitive(PrimitiveType.Cube);

                folderBtn.GetComponent<BoxCollider>().isTrigger = true;

                folderBtn.transform.parent = menu.transform;
                folderBtn.transform.rotation = Quaternion.identity;
                folderBtn.transform.localScale = new Vector3(0.05f, 0.1f, 0.08f);
                folderBtn.transform.localPosition = new Vector3(0.56f, -0.13f, -0.57f);

                folderBtn.GetComponent<Renderer>().material.color = new Color(0.06f, 0.06f, 0.06f);
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

            if (disconnectButton)
            {
                GameObject disconnectbutton = GameObject.CreatePrimitive(PrimitiveType.Cube);
                UnityEngine.Object.Destroy(disconnectbutton.GetComponent<Rigidbody>());
                disconnectbutton.GetComponent<BoxCollider>().isTrigger = true;
                disconnectbutton.transform.parent = menu.transform;
                disconnectbutton.transform.rotation = Quaternion.identity;
                disconnectbutton.transform.localScale = new Vector3(0.09f, 0.4f, 0.09f);
                disconnectbutton.transform.localPosition = new Vector3(0.56f, 0f, 0.57f);
                disconnectbutton.GetComponent<Renderer>().material.color = Color.black;
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

            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            UnityEngine.Object.Destroy(gameObject.GetComponent<Rigidbody>());
            gameObject.GetComponent<BoxCollider>().isTrigger = true;
            gameObject.transform.parent = menu.transform;
            gameObject.transform.rotation = Quaternion.identity;
            gameObject.transform.localScale = sideLayout ? new Vector3(0.045f, 0.25f, 0.8936298f) : new Vector3(0.06f, 0.25f, 0.06f);
            gameObject.transform.localPosition = sideLayout ? new Vector3(0.56f, 0.657f, 0.0063f) : new Vector3(0.56f, -0.37f, 0.555f);
            gameObject.GetComponent<Renderer>().material.color = Color.black;
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
            component.localPosition = sideLayout ? new Vector3(0.064f, 0.195f, 0f) : new Vector3(0.064f, -0.115f, 0.215f);
            component.rotation = Quaternion.Euler(new Vector3(180f, 90f, 90f));

            gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            UnityEngine.Object.Destroy(gameObject.GetComponent<Rigidbody>());
            gameObject.GetComponent<BoxCollider>().isTrigger = true;
            gameObject.transform.parent = menu.transform;
            gameObject.transform.rotation = Quaternion.identity;
            gameObject.transform.localScale = sideLayout ? new Vector3(0.045f, 0.25f, 0.8936298f) : new Vector3(0.06f, 0.25f, 0.06f);
            gameObject.transform.localPosition = sideLayout ? new Vector3(0.56f, -0.657f, 0.0063f) : new Vector3(0.56f, 0.37f, 0.555f);
            gameObject.GetComponent<Renderer>().material.color = Color.black;
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
            component.localPosition = sideLayout ? new Vector3(0.064f, -0.195f, 0f) : new Vector3(0.064f, 0.115f, 0.215f);
            component.rotation = Quaternion.Euler(new Vector3(180f, 90f, 90f));

            ButtonInfo[] activeButtons;

            if (buttonsType < buttons.Length)
            {
                activeButtons = buttons[buttonsType].Skip(pageNumber * buttonsPerPage).Take(buttonsPerPage).ToArray();
            }
            else
            {
                activeButtons = new ButtonInfo[0];
            }

            for (int i = 0; i < activeButtons.Length; i++)
            {
                CreateButton(i * 0.095f, activeButtons[i]);
            }
        }

        public static void CreateButton(float offset, ButtonInfo method)
        {
            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            UnityEngine.Object.Destroy(gameObject.GetComponent<Rigidbody>());
            gameObject.GetComponent<BoxCollider>().isTrigger = true;
            gameObject.transform.parent = menu.transform;
            gameObject.transform.rotation = Quaternion.identity;
            gameObject.transform.localScale = new Vector3(0.05f, 0.6f, 0.08f);
            gameObject.transform.localPosition = new Vector3(0.56f, 0.1f, 0.25f - offset);
            Classes.Button btn = gameObject.AddComponent<Classes.Button>();
            btn.relatedText = method.buttonText;
            btn.buttonInfo = method;
            gameObject.GetComponent<Renderer>().material.color = new Color(0.06f, 0.06f, 0.06f);
            if (showOutline)
            {
                OutlineObj(gameObject, outlineColor, outlineColor, false, 3);
            }

            GameObject gameObject1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            UnityEngine.Object.Destroy(gameObject1.GetComponent<Rigidbody>());
            gameObject1.GetComponent<BoxCollider>().isTrigger = true;
            gameObject1.transform.parent = menu.transform;
            gameObject1.transform.rotation = Quaternion.identity;
            gameObject1.transform.localScale = new Vector3(0.05f, 0.1f, 0.085f);
            gameObject1.transform.localPosition = new Vector3(0.56f, -0.35f, 0.25f - offset);
            Classes.Button favBtn = gameObject1.AddComponent<Classes.Button>();
            favBtn.relatedText = "fav_" + method.buttonText;
            favBtn.buttonInfo = method;
            gameObject1.GetComponent<Renderer>().material.color = new Color(0.06f, 0.06f, 0.06f);
            if (showOutline)
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
            if (method.enabled)
            {
                text.color = textColors[1];
            }
            else
            {
                text.color = textColors[0];
            }
            text.alignment = TextAnchor.MiddleCenter;
            text.fontStyle = FontStyle.Bold;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 0;
            RectTransform component = text.GetComponent<RectTransform>();
            component.localPosition = Vector3.zero;
            component.sizeDelta = new Vector2(.2f, .03f);
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

        public static void RecreateMenu()
        {
            if (menu != null)
            {
                UnityEngine.Object.Destroy(menu);
                menu = null;

                CreateMenu();
                RecenterMenu(rightHanded, UnityInput.Current.GetKey(keyboardButton));
            }
        }

        public static void RecenterMenu(bool isRightHanded, bool isKeyboardCondition)
        {
            if (!isKeyboardCondition)
            {
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
            MenuAudio.Initialize();
        }

        public static void Toggle(string buttonText, ButtonInfo target = null)
        {
            if (buttonText.StartsWith("fav_"))
            {
                ToggleFavorite(buttonText.Substring(4), target);
                return;
            }

            int lastPage = 0;
            if (buttonsType < buttons.Length)
            {
                lastPage = ((buttons[buttonsType].Length + buttonsPerPage - 1) / buttonsPerPage) - 1;
                if (lastPage < 0) lastPage = 0;
            }
            else
            {
                lastPage = 0;
            }

            if (buttonText == "PreviousPage")
            {
                pageNumber--;
                if (pageNumber < 0)
                {
                    pageNumber = lastPage;
                }
            }
            else if (buttonText == "NextPage")
            {
                pageNumber++;
                if (pageNumber > lastPage)
                {
                    pageNumber = 0;
                }
            }
            else if (buttonText.Equals("Disconnect", StringComparison.OrdinalIgnoreCase))
            {
                PhotonNetwork.Disconnect();
                NotificationLib.SendNotification(NotificationLib.NotificationType.Info, "Disconnected from network");
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

        public static ButtonInfo GetIndex(string buttonText)
        {
            if (string.IsNullOrEmpty(buttonText)) return null;

            for (int i = 0; i < Buttons.buttons.Length; i++)
            {
                ButtonInfo[] list = Buttons.buttons[i];
                if (list == null) continue;

                for (int j = 0; j < list.Length; j++)
                {
                    ButtonInfo btn = list[j];
                    if (btn != null && (btn.buttonText == buttonText || btn.overlapText == buttonText))
                        return btn;
                }
            }

            return null;
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
            }

            sideEffect?.Invoke();
        }

        public static void UpdateBoardText()
        {
            CacheObjects();

            if (cocHeading != null && motdBody != null && cocBody != null && motdHeading != null)
            {
                if (cocHeading.TryGetComponent<TMP_Text>(out var cocHeadText))
                {
                    cocHeadText.text = "<color=blue>ShibaGT Genesis Reborn</color>".ToUpper();
                    cocHeadText.fontSize = 75f;
                }

                if (cocBody.TryGetComponent<TMP_Text>(out var cocBodyText))
                {
                    cocBodyText.richText = true;
                    cocBodyText.text = $"\nWelcome To ShibaGT Genesis Reborn!\nThis is a Remake of the Longest Lasting Paid Mod Menu Shiba GT Genesis!\nWe currently have {GetTotalButtonCount()} total mods right now".ToUpper();
                }

                if (motdHeading.TryGetComponent<TMP_Text>(out var motdHeadText))
                    motdHeadText.text = "<color=blue>ShibaGT Genesis Reborn</color>".ToUpper();

                if (motdBody.TryGetComponent<TMP_Text>(out var motdBodyText))
                    motdBodyText.text = "Credits to ShibaGT/TAI for making the original menu!\nThis is just a remake!\n<color=red>We Are Not Responsible For Any Bans Using This Mod Menu!</color>".ToUpper();
            }
        }

        private static void CacheObjects()
        {
            if (cocHeading == null)
            {
                cocHeading = GameObject.Find("Environment Objects/LocalObjects_Prefab/TreeRoom/CodeOfConductHeadingText");
            }
            if (cocBody == null)
            {
                cocBody = GameObject.Find("Environment Objects/LocalObjects_Prefab/TreeRoom/COCBodyText_TitleData");
            }
            if (motdHeading == null)
            {
                motdHeading = GameObject.Find("motdHeadingText");
            }
            if (motdBody == null)
            {
                motdBody = GameObject.Find("motdBodyText");
            }
            if (shoulderCamera == null)
            {
                shoulderCamera = GameObject.Find("Shoulder Camera");
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

        public static GameObject menu;
        public static GameObject menuBackground;
        public static GameObject reference;
        public static GameObject canvasObject;

        public static SphereCollider buttonCollider;
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