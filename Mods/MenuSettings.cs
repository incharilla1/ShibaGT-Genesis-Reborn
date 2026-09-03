using GorillaNetworking;
using Photon.Pun;
using ShibaGTGenesisReborn.Classes;
using ShibaGTGenesisReborn.Libs;
using ShibaGTGenesisReborn.Menu;
using System.Collections.Generic;
using UnityEngine;

namespace ShibaGTGenesisReborn.Mods
{
    public partial class mods
    {
        [Setting] public static int OutlineIndex;
        public static Color[] outlines =
        {
            Color.blue,
            Color.green,
            Color.red,
            Color.yellow,
            Color.cyan,
            Color.magenta,
            Color.white,
            Color.black,
            new Color(0.06f, 0.06f, 0.06f),
            new Color(1f, 0.5f, 0f),
            new Color(1f, 0.4f, 0.7f),
            new Color(0.5f, 0f, 1f),
            new Color(0.6f, 0.3f, 0f),
            new Color(0.6f, 1f, 0f),
            new Color(0.2f, 1f, 0.5f),
            new Color(1f, 0.2f, 0.2f),
            new Color(0.3f, 0.8f, 1f),
        };

        public static readonly string[] outnames =
        {
            "Blue",
            "Green",
            "Red",
            "Yellow",
            "Cyan",
            "Magenta",
            "White",
            "Black",
            "Dark Grey",
            "Orange",
            "Pink",
            "Purple",
            "Brown",
            "Lime",
            "Mint",
            "Coral",
            "Sky",
        };

        public struct ThemeInfo
        {
            public Color bg;
            public Color btnOff;
            public Color btnOn;
            public Color textOff;
            public Color textOn;
            public Color outline;
            public bool rainbow;
            public bool rig;
        }

        [Setting] public static int ThemeIndex;

        public static readonly string[] ThemeNames =
        {
            "Genesis",
            "Blue",
            "Green",
            "Red",
            "Yellow",
            "Cyan",
            "Magenta",
            "White",
            "Black",
            "Dark Grey",
            "Orange",
            "Pink",
            "Purple",
            "Brown",
            "Lime",
            "Mint",
            "Coral",
            "Sky",
            "Rainbow",
            "Rig Match"
        };

        public static readonly ThemeInfo[] Themes =
        {
            new ThemeInfo { bg = Color.black, btnOff = new Color(0.06f, 0.06f, 0.06f), textOff = Color.white, textOn = Color.magenta, outline = Color.blue },
            new ThemeInfo { bg = new Color(0f, 0f, 0.65f), btnOff = new Color(0f, 0f, 0.4f), textOff = Color.white, textOn = Color.yellow, outline = Color.blue },
            new ThemeInfo { bg = new Color(0f, 0.6f, 0f), btnOff = new Color(0f, 0.38f, 0f), textOff = Color.white, textOn = Color.yellow, outline = Color.green },
            new ThemeInfo { bg = new Color(0.65f, 0f, 0f), btnOff = new Color(0.42f, 0f, 0f), textOff = Color.white, textOn = Color.yellow, outline = Color.red },
            new ThemeInfo { bg = new Color(0.7f, 0.64f, 0.01f), btnOff = new Color(0.48f, 0.44f, 0.01f), textOff = Color.white, textOn = Color.cyan, outline = Color.yellow },
            new ThemeInfo { bg = new Color(0f, 0.65f, 0.65f), btnOff = new Color(0f, 0.42f, 0.42f), textOff = Color.white, textOn = Color.yellow, outline = Color.cyan },
            new ThemeInfo { bg = new Color(0.65f, 0f, 0.65f), btnOff = new Color(0.42f, 0.42f, 0.42f), textOff = Color.white, textOn = Color.yellow, outline = Color.magenta },
            new ThemeInfo { bg = new Color(0.82f, 0.82f, 0.85f), btnOff = new Color(0.65f, 0.65f, 0.68f), textOff = Color.black, textOn = new Color(0f, 0.45f, 1f), outline = Color.white },
            new ThemeInfo { bg = new Color(0.04f, 0.04f, 0.04f), btnOff = new Color(0.09f, 0.09f, 0.09f), textOff = Color.white, textOn = Color.yellow, outline = new Color(0.35f, 0.35f, 0.35f) },
            new ThemeInfo { bg = new Color(0.12f, 0.12f, 0.12f), btnOff = new Color(0.06f, 0.06f, 0.06f), textOff = Color.white, textOn = Color.cyan, outline = new Color(0.45f, 0.45f, 0.45f) },
            new ThemeInfo { bg = new Color(0.7f, 0.35f, 0f), btnOff = new Color(0.46f, 0.23f, 0f), textOff = Color.white, textOn = Color.yellow, outline = new Color(1f, 0.5f, 0f) },
            new ThemeInfo { bg = new Color(0.7f, 0.28f, 0.49f), btnOff = new Color(0.46f, 0.18f, 0.32f), textOff = Color.white, textOn = Color.yellow, outline = new Color(1f, 0.4f, 0.7f) },
            new ThemeInfo { bg = new Color(0.35f, 0f, 0.7f), btnOff = new Color(0.23f, 0f, 0.46f), textOff = Color.white, textOn = Color.cyan, outline = new Color(0.5f, 0f, 1f) },
            new ThemeInfo { bg = new Color(0.42f, 0.21f, 0f), btnOff = new Color(0.28f, 0.14f, 0f), textOff = Color.white, textOn = Color.yellow, outline = new Color(0.6f, 0.3f, 0f) },
            new ThemeInfo { bg = new Color(0.42f, 0.7f, 0f), btnOff = new Color(0.28f, 0.46f, 0f), textOff = Color.white, textOn = Color.yellow, outline = new Color(0.6f, 1f, 0f) },
            new ThemeInfo { bg = new Color(0.14f, 0.7f, 0.35f), btnOff = new Color(0.09f, 0.46f, 0.23f), textOff = Color.white, textOn = Color.yellow, outline = new Color(0.2f, 1f, 0.5f) },
            new ThemeInfo { bg = new Color(0.7f, 0.14f, 0.14f), btnOff = new Color(0.46f, 0.09f, 0.09f), textOff = Color.white, textOn = Color.yellow, outline = new Color(1f, 0.2f, 0.2f) },
            new ThemeInfo { bg = new Color(0.21f, 0.56f, 0.7f), btnOff = new Color(0.14f, 0.37f, 0.46f), textOff = Color.white, textOn = Color.yellow, outline = new Color(0.3f, 0.8f, 1f) },
            new ThemeInfo { bg = Color.black, btnOff = new Color(0.06f, 0.06f, 0.06f), textOff = Color.white, textOn = Color.yellow, outline = Color.magenta, rainbow = true },
            new ThemeInfo { bg = Color.black, btnOff = new Color(0.06f, 0.06f, 0.06f), textOff = Color.white, textOn = Color.yellow, outline = Color.blue, rig = true }
        };

        public static void ApplyTheme()
        {
            if (ThemeIndex < 0 || ThemeIndex >= Themes.Length) ThemeIndex = 0;
            ThemeInfo t = Themes[ThemeIndex];
            Settings.backgroundColor = new ExtGradient { colors = Main.GetSolidGradient(t.bg), isRainbow = t.rainbow, copyRigColors = t.rig };
            Settings.buttonColors = new ExtGradient[]
            {
                new ExtGradient { colors = Main.GetSolidGradient(t.btnOff), isRainbow = t.rainbow, copyRigColors = t.rig },
                new ExtGradient { colors = Main.GetSolidGradient(t.btnOff), isRainbow = t.rainbow, copyRigColors = t.rig }
            };
            Settings.textColors = new Color[] { t.textOff, t.textOn };
            if (Main.menu != null) Main.RecreateMenu();
        }

        public static void SwitchPagePos()
        {
            Main.Change("PPos", ref Settings.pageButtonIndex, Settings.pageButtonNames, () => Main.RecreateMenu(), "Page Buttons: ");
            Main.sideLayout = (Settings.pageButtonIndex == 1);
        }

        private static float notifcooldown;
        public static void AntiReport()
        {
            foreach (GorillaPlayerScoreboardLine boardline in GorillaScoreboardTotalUpdater.allScoreboardLines)
            {
                if (boardline.linePlayer == NetworkSystem.Instance.LocalPlayer && boardline.reportButton != null)
                {
                    Transform transform = boardline.reportButton.gameObject.transform;
                    foreach (VRRig vrrig in VRRigCache.ActiveRigs)
                    {
                        if (vrrig != null && vrrig != GorillaTagger.Instance.offlineVRRig)
                        {
                            if ((Vector3.Distance(vrrig.rightHandTransform.position, transform.position) < 0.4f || Vector3.Distance(vrrig.leftHandTransform.position, transform.position) < 0.4f) && Time.time > notifcooldown + 0.5f)
                            {
                                notifcooldown = Time.time;
                                Disconnect();
                                return;
                            }
                        }
                    }
                }
            }
        }

        private static void Disconnect()
        {
            PhotonNetwork.SendAllOutgoingCommands();
            NetworkSystem.Instance.ReturnToSinglePlayer();
            NotificationLib.SendNotification(NotificationLib.NotificationType.Alert, "Anti Report disconnected you");
        }

        private static readonly List<ButtonInfo> panicSavedMods = new List<ButtonInfo>();

        public static void EnablePanic()
        {
            panicSavedMods.Clear();
            for (int i = 0; i < Buttons.buttons.Length; i++)
            {
                if (i == 0 || i == 1 || i == 10 || i == 11 || i == 13 || i == 14 || i == 15 || i == 16 || i == 17 || i == 18 || i >= 19)
                    continue;

                foreach (ButtonInfo btn in Buttons.buttons[i])
                {
                    if (btn != null && btn.enabled && btn.buttonText != "Panic Button")
                    {
                        panicSavedMods.Add(btn);
                        btn.enabled = false;
                        btn.disableMethod?.Invoke();
                    }
                }
            }

            SlideControl(0.00425f);
            AirSwimDisable();
            JesusMonkeDisable();
            ZiplineSpeed(10f);
            ResetStickyHands();
            ReSlip();
            FixHead();
            NormalArms();
        }

        public static void DisablePanic()
        {
            foreach (ButtonInfo btn in panicSavedMods)
            {
                if (btn != null)
                {
                    btn.enabled = true;
                    btn.enableMethod?.Invoke();
                    btn.method?.Invoke();
                }
            }
            panicSavedMods.Clear();
        }
    }
}