using ShibaGTGenesisReborn.Classes;
using ShibaGTGenesisReborn.Libs;
using ShibaGTGenesisReborn.Menu;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace ShibaGTGenesisReborn.Mods.Custom
{
    [ComImport]
    [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    internal class MMDeviceEnumerator
    {
    }

    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMMDeviceEnumerator
    {
        int EnumAudioEndpoints(int dataFlow, int stateMask, out IntPtr devices);
        int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice endpoint);
        int GetDevice(string id, out IMMDevice device);
        int RegisterEndpointNotificationCallback(IntPtr client);
        int UnregisterEndpointNotificationCallback(IntPtr client);
    }

    [Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMMDevice
    {
        int Activate(ref Guid id, int clsCtx, IntPtr activationParams, [MarshalAs(UnmanagedType.IUnknown)] out object interfacePointer);
        int OpenPropertyStore(int stgmAccess, out IntPtr properties);
        int GetId(out string id);
        int GetState(out int state);
    }

    [Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioSessionManager2
    {
        int GetAudioSessionControl(IntPtr sessionId, int flags, out IntPtr control);
        int GetSimpleAudioVolume(IntPtr sessionId, int flags, out IntPtr volume);
        int GetSessionEnumerator(out IAudioSessionEnumerator sessionEnum);
        int RegisterSessionNotification(IntPtr notifications);
        int UnregisterSessionNotification(IntPtr notifications);
        int RegisterDuckNotification(string sessionId, IntPtr duckNotification);
        int UnregisterDuckNotification(IntPtr duckNotification);
    }

    [Guid("E2F5E06E-4D50-4E08-B0E4-6F14B8214A05"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioSessionEnumerator
    {
        int GetCount(out int sessionCount);
        int GetSession(int sessionIndex, out IAudioSessionControl session);
    }

    [Guid("F4B1A599-7266-4319-A8CA-E70ACB11E8CD"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioSessionControl
    {
        int GetState(out int state);
        int GetDisplayName(out string displayName);
        int SetDisplayName(string displayName, ref Guid eventContext);
        int GetIconPath(out string iconPath);
        int SetIconPath(string iconPath, ref Guid eventContext);
        int GetGroupingParam(out Guid groupingParam);
        int SetGroupingParam(ref Guid groupingParam, ref Guid eventContext);
        int RegisterAudioSessionNotification(IntPtr client);
        int UnregisterAudioSessionNotification(IntPtr client);
    }

    [Guid("BFB7FF88-7239-4FC9-8FA2-07C950BE9C6D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioSessionControl2 : IAudioSessionControl
    {
        new int GetState(out int state);
        new int GetDisplayName(out string displayName);
        new int SetDisplayName(string displayName, ref Guid eventContext);
        new int GetIconPath(out string iconPath);
        new int SetIconPath(string iconPath, ref Guid eventContext);
        new int GetGroupingParam(out Guid groupingParam);
        new int SetGroupingParam(ref Guid groupingParam, ref Guid eventContext);
        new int RegisterAudioSessionNotification(IntPtr client);
        new int UnregisterAudioSessionNotification(IntPtr client);

        int GetSessionIdentifier(out string sessionIdentifier);
        int GetSessionInstanceIdentifier(out string sessionInstanceIdentifier);
        int GetProcessId(out uint processId);
        int IsSystemSoundsSession();
        int SetDuckingPreference(bool optOut);
    }

    [Guid("87CE5498-68D6-44E5-9215-6DA47EF883D8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ISimpleAudioVolume
    {
        int SetMasterVolume(float level, ref Guid eventContext);
        int GetMasterVolume(out float level);
        int SetMute(bool isMuted, ref Guid eventContext);
        int GetMute(out bool isMuted);
    }

    // thanks [DELETED USER] from reddit ;)
    public class SpotifyManager : MonoBehaviour
    {
        private const uint WM_APPCOMMAND = 0x0319;
        private const int APPCOMMAND_MEDIA_NEXTTRACK = 11;
        private const int APPCOMMAND_MEDIA_PREVIOUSTRACK = 12;
        private const int APPCOMMAND_MEDIA_STOP = 13;
        private const int APPCOMMAND_MEDIA_PLAY_PAUSE = 14;

        private const int SW_SHOWNORMAL = 1;
        private const int SW_MINIMIZE = 6;
        private const int SW_RESTORE = 9;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        public static SpotifyManager Instance { get; private set; }
        public static bool ShowHUD = false;
        public static bool AutoNotifyTrack = true;

        private static GameObject hudOverlayObj;
        private static UnityEngine.UI.Text hudOverlayText;
        private static GameObject hudVrObj;
        private static UnityEngine.UI.Text hudVrText;
        private static float hudUpdateTimer;

        private static string currentTrackDisplay = "Not Running";
        private static string lastKnownTrack = "";

        public static void Initialize()
        {
            if (Instance == null)
            {
                GameObject go = new GameObject("SpotifyManager");
                Instance = go.AddComponent<SpotifyManager>();
                DontDestroyOnLoad(go);
            }
        }

        public static IntPtr GetSpotifyWindow()
        {
            IntPtr foundHwnd = IntPtr.Zero;
            Process[] procs = Process.GetProcessesByName("Spotify");
            if (procs == null || procs.Length == 0)
                return IntPtr.Zero;

            HashSet<uint> pids = new HashSet<uint>();
            for (int i = 0; i < procs.Length; i++)
                pids.Add((uint)procs[i].Id);

            EnumWindows((hWnd, lParam) =>
            {
                uint pid;
                GetWindowThreadProcessId(hWnd, out pid);
                if (pids.Contains(pid))
                {
                    int length = GetWindowTextLength(hWnd);
                    if (length > 0)
                    {
                        StringBuilder sb = new StringBuilder(length + 1);
                        GetWindowText(hWnd, sb, sb.Capacity);
                        string title = sb.ToString();
                        if (!string.IsNullOrEmpty(title))
                        {
                            foundHwnd = hWnd;
                            return false;
                        }
                    }
                }
                return true;
            }, IntPtr.Zero);

            return foundHwnd;
        }

        private static ISimpleAudioVolume GetSpotifyVolumeControl()
        {
            try
            {
                Process[] procs = Process.GetProcessesByName("Spotify");
                if (procs == null || procs.Length == 0)
                    return null;

                HashSet<uint> pids = new HashSet<uint>();
                for (int i = 0; i < procs.Length; i++)
                    pids.Add((uint)procs[i].Id);

                IMMDeviceEnumerator enumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();
                enumerator.GetDefaultAudioEndpoint(0, 1, out IMMDevice device);
                if (device == null) return null;

                Guid IID_IAudioSessionManager2 = typeof(IAudioSessionManager2).GUID;
                device.Activate(ref IID_IAudioSessionManager2, 0, IntPtr.Zero, out object sessionManagerObj);
                IAudioSessionManager2 mgr = sessionManagerObj as IAudioSessionManager2;
                if (mgr == null) return null;

                mgr.GetSessionEnumerator(out IAudioSessionEnumerator sessionEnum);
                if (sessionEnum == null) return null;

                sessionEnum.GetCount(out int count);
                for (int i = 0; i < count; i++)
                {
                    sessionEnum.GetSession(i, out IAudioSessionControl control);
                    if (control is IAudioSessionControl2 control2)
                    {
                        control2.GetProcessId(out uint pid);
                        if (pids.Contains(pid))
                        {
                            return control as ISimpleAudioVolume;
                        }
                    }
                }
            }
            catch
            {
            }

            return null;
        }

        public static bool SendSpotifyCommand(int command)
        {
            IntPtr hwnd = GetSpotifyWindow();
            if (hwnd == IntPtr.Zero)
            {
                NotificationLib.SendNotification(NotificationLib.NotificationType.Alert, "<color=yellow>Spotify</color>\nSpotify is not running", 3f);
                return false;
            }

            SendMessage(hwnd, WM_APPCOMMAND, IntPtr.Zero, (IntPtr)(command << 16));
            return true;
        }

        public static string GetCurrentTrackTitle()
        {
            IntPtr hwnd = GetSpotifyWindow();
            if (hwnd == IntPtr.Zero)
                return "Not Running";

            int length = GetWindowTextLength(hwnd);
            if (length <= 0)
                return "Not Running";

            StringBuilder sb = new StringBuilder(length + 1);
            GetWindowText(hwnd, sb, sb.Capacity);
            string title = sb.ToString();

            if (title.Equals("Spotify", StringComparison.OrdinalIgnoreCase) ||
                title.Equals("Spotify Free", StringComparison.OrdinalIgnoreCase) ||
                title.Equals("Spotify Premium", StringComparison.OrdinalIgnoreCase))
            {
                return "Paused";
            }

            return title;
        }

        public static void TogglePlay()
        {
            if (SendSpotifyCommand(APPCOMMAND_MEDIA_PLAY_PAUSE))
                NotificationLib.SendNotification(NotificationLib.NotificationType.Info, "<color=green>Spotify</color>\nPlay / Pause", 2f);
        }

        public static void NextTrack()
        {
            if (SendSpotifyCommand(APPCOMMAND_MEDIA_NEXTTRACK))
                NotificationLib.SendNotification(NotificationLib.NotificationType.Info, "<color=green>Spotify</color>\nNext Track", 2f);
        }

        public static void PreviousTrack()
        {
            if (SendSpotifyCommand(APPCOMMAND_MEDIA_PREVIOUSTRACK))
                NotificationLib.SendNotification(NotificationLib.NotificationType.Info, "<color=green>Spotify</color>\nPrevious Track", 2f);
        }

        public static void RestartTrack()
        {
            if (SendSpotifyCommand(APPCOMMAND_MEDIA_PREVIOUSTRACK))
            {
                Thread.Sleep(50);
                SendSpotifyCommand(APPCOMMAND_MEDIA_PREVIOUSTRACK);
                NotificationLib.SendNotification(NotificationLib.NotificationType.Info, "<color=green>Spotify</color>\nRestart Song", 2f);
            }
        }

        public static void StopPlayback()
        {
            if (SendSpotifyCommand(APPCOMMAND_MEDIA_STOP))
                NotificationLib.SendNotification(NotificationLib.NotificationType.Info, "<color=green>Spotify</color>\nStopped", 2f);
        }

        public static void Mute()
        {
            ISimpleAudioVolume vol = GetSpotifyVolumeControl();
            if (vol == null)
            {
                NotificationLib.SendNotification(NotificationLib.NotificationType.Alert, "<color=yellow>Spotify</color>\nSpotify audio session not found", 3f);
                return;
            }

            vol.GetMute(out bool isMuted);
            bool target = !isMuted;
            Guid guid = Guid.Empty;
            vol.SetMute(target, ref guid);
            NotificationLib.SendNotification(NotificationLib.NotificationType.Info, target ? "<color=red>Spotify Muted</color>" : "<color=green>Spotify Unmuted</color>", 1.5f);
        }

        public static void ShowTrackNotification()
        {
            string track = GetCurrentTrackTitle();
            NotificationLib.SendNotification(NotificationLib.NotificationType.Info, $"<color=green>Spotify</color>\n{track}", 4f);
        }

        public static void CopyCurrentTrack()
        {
            string track = GetCurrentTrackTitle();
            if (track == "Not Running" || track == "Paused")
            {
                NotificationLib.SendNotification(NotificationLib.NotificationType.Alert, "<color=yellow>Spotify</color>\nNo song currently playing", 2f);
                return;
            }

            GUIUtility.systemCopyBuffer = track;
            NotificationLib.SendNotification(NotificationLib.NotificationType.Info, $"<color=green>Spotify</color>\nCopied: {track}", 3f);
        }

        public static void LaunchSpotify()
        {
            try
            {
                Process.Start(new ProcessStartInfo("spotify:") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                NotificationLib.SendNotification(NotificationLib.NotificationType.Error, "Launch failed: " + ex.Message, 3f);
            }
        }

        public static void FocusSpotify()
        {
            IntPtr hwnd = GetSpotifyWindow();
            if (hwnd == IntPtr.Zero)
            {
                LaunchSpotify();
                return;
            }

            ShowWindow(hwnd, SW_RESTORE);
            SetForegroundWindow(hwnd);
            NotificationLib.SendNotification(NotificationLib.NotificationType.Info, "<color=green>Spotify</color>\nBrought to front", 2f);
        }

        public static void MinimizeSpotify()
        {
            IntPtr hwnd = GetSpotifyWindow();
            if (hwnd == IntPtr.Zero)
            {
                NotificationLib.SendNotification(NotificationLib.NotificationType.Alert, "<color=yellow>Spotify</color>\nSpotify is not running", 3f);
                return;
            }

            ShowWindow(hwnd, SW_MINIMIZE);
            NotificationLib.SendNotification(NotificationLib.NotificationType.Info, "<color=green>Spotify</color>\nMinimized", 2f);
        }

        public static void CloseSpotify()
        {
            Process[] procs = Process.GetProcessesByName("Spotify");
            if (procs == null || procs.Length == 0)
            {
                NotificationLib.SendNotification(NotificationLib.NotificationType.Alert, "<color=yellow>Spotify</color>\nSpotify is not running", 3f);
                return;
            }

            for (int i = 0; i < procs.Length; i++)
            {
                try { procs[i].Kill(); } catch { }
            }

            NotificationLib.SendNotification(NotificationLib.NotificationType.Disabled, "<color=yellow>Spotify</color>\nClosed Spotify", 2f);
        }

        public static void EnableHUD()
        {
            ShowHUD = true;
            InitializeHUD();
        }

        public static void DisableHUD()
        {
            ShowHUD = false;
            DestroyHUD();
        }

        private static void InitializeHUD()
        {
            InitializeScreenHUD();
            if (!Settings.disableVRViewHUD)
                InitializeVRHUD();
        }

        private static void InitializeScreenHUD()
        {
            if (hudOverlayObj != null) return;

            hudOverlayObj = new GameObject("Spotify_HUD_Screen");
            Canvas canvas = hudOverlayObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 998;

            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(hudOverlayObj.transform, false);
            Image bg = bgObj.AddComponent<Image>();
            bg.color = new Color(0.07f, 0.07f, 0.09f, 0.88f);
            RectTransform bgRect = bg.rectTransform;
            bgRect.anchorMin = new Vector2(0, 1);
            bgRect.anchorMax = new Vector2(0, 1);
            bgRect.pivot = new Vector2(0, 1);
            bgRect.anchoredPosition = new Vector2(16, -16);
            bgRect.sizeDelta = new Vector2(270, 56);

            GameObject barObj = new GameObject("AccentBar");
            barObj.transform.SetParent(bgObj.transform, false);
            Image bar = barObj.AddComponent<Image>();
            bar.color = new Color(0.11f, 0.84f, 0.38f, 1f);
            RectTransform barRect = bar.rectTransform;
            barRect.anchorMin = new Vector2(0, 0);
            barRect.anchorMax = new Vector2(0, 1);
            barRect.pivot = new Vector2(0, 0.5f);
            barRect.anchoredPosition = new Vector2(0, 0);
            barRect.sizeDelta = new Vector2(3.5f, 0);

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(bgObj.transform, false);
            hudOverlayText = textObj.AddComponent<UnityEngine.UI.Text>();
            hudOverlayText.font = Settings.currentFont ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            hudOverlayText.fontSize = 12;
            hudOverlayText.supportRichText = true;
            hudOverlayText.alignment = TextAnchor.MiddleLeft;
            hudOverlayText.horizontalOverflow = HorizontalWrapMode.Wrap;
            hudOverlayText.verticalOverflow = VerticalWrapMode.Truncate;

            UnityEngine.UI.Outline outline = textObj.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(0.8f, -0.8f);

            RectTransform textRect = hudOverlayText.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12, 4);
            textRect.offsetMax = new Vector2(-8, -4);
        }

        private static void InitializeVRHUD()
        {
            if (hudVrObj != null) return;

            Camera cam = Camera.main ?? GorillaTagger.Instance?.mainCamera?.GetComponent<Camera>();
            if (cam == null) return;

            hudVrObj = new GameObject("VR_Spotify_HUD");
            hudVrObj.transform.SetParent(cam.transform, false);
            hudVrObj.transform.localPosition = new Vector3(-0.30f, 0.18f, 0.60f);
            hudVrObj.transform.localRotation = Quaternion.Euler(0f, 6f, 0f);
            hudVrObj.transform.localScale = new Vector3(0.0009f, 0.0009f, 0.0009f);

            Canvas canvas = hudVrObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = cam;

            CanvasScaler scaler = hudVrObj.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 25f;

            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(hudVrObj.transform, false);
            Image bg = bgObj.AddComponent<Image>();
            bg.color = new Color(0.07f, 0.07f, 0.09f, 0.88f);
            RectTransform bgRect = bg.rectTransform;
            bgRect.sizeDelta = new Vector2(280, 60);

            GameObject barObj = new GameObject("AccentBar");
            barObj.transform.SetParent(bgObj.transform, false);
            Image bar = barObj.AddComponent<Image>();
            bar.color = new Color(0.11f, 0.84f, 0.38f, 1f);
            RectTransform barRect = bar.rectTransform;
            barRect.anchorMin = new Vector2(0, 0);
            barRect.anchorMax = new Vector2(0, 1);
            barRect.pivot = new Vector2(0, 0.5f);
            barRect.anchoredPosition = new Vector2(0, 0);
            barRect.sizeDelta = new Vector2(4f, 0);

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(bgObj.transform, false);
            hudVrText = textObj.AddComponent<UnityEngine.UI.Text>();
            hudVrText.font = Settings.currentFont ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            hudVrText.fontSize = 13;
            hudVrText.supportRichText = true;
            hudVrText.alignment = TextAnchor.MiddleLeft;

            UnityEngine.UI.Outline outline = textObj.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(1f, -1f);

            RectTransform textRect = hudVrText.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(14, 4);
            textRect.offsetMax = new Vector2(-8, -4);
        }

        private static void DestroyHUD()
        {
            if (hudOverlayObj != null)
            {
                Destroy(hudOverlayObj);
                hudOverlayObj = null;
                hudOverlayText = null;
            }

            if (hudVrObj != null)
            {
                Destroy(hudVrObj);
                hudVrObj = null;
                hudVrText = null;
            }
        }

        private static string FormatDisplayString(string rawTrack)
        {
            if (rawTrack == "Not Running")
                return "<color=#1DB954><b>SPOTIFY</b></color> <color=#666666>|</color> <color=#888888>OFFLINE</color>\n<size=11><color=#BBBBBB>Spotify is not running</color></size>";

            if (rawTrack == "Paused")
            {
                string prev = string.IsNullOrEmpty(lastKnownTrack) ? "No recent track" : lastKnownTrack;
                return $"<color=#1DB954><b>SPOTIFY</b></color> <color=#666666>|</color> <color=#EAA838>PAUSED</color>\n<size=11><color=#D0D0D0>{prev}</color></size>";
            }

            int dash = rawTrack.IndexOf(" - ");
            if (dash > 0)
            {
                string artist = rawTrack.Substring(0, dash).Trim();
                string title = rawTrack.Substring(dash + 3).Trim();
                return $"<color=#1DB954><b>SPOTIFY</b></color> <color=#666666>|</color> <color=#4FE284>PLAYING</color>\n<size=12><b><color=#FFFFFF>{title}</color></b></size>\n<size=10><color=#A6A6A6>{artist}</color></size>";
            }

            return $"<color=#1DB954><b>SPOTIFY</b></color> <color=#666666>|</color> <color=#4FE284>PLAYING</color>\n<size=12><b><color=#FFFFFF>{rawTrack}</color></b></size>";
        }

        private void Update()
        {
            if (Time.unscaledTime >= hudUpdateTimer)
            {
                hudUpdateTimer = Time.unscaledTime + 0.5f;
                string track = GetCurrentTrackTitle();

                if (track != currentTrackDisplay)
                {
                    currentTrackDisplay = track;
                    if (track != "Not Running" && track != "Paused")
                        lastKnownTrack = track;

                    if (AutoNotifyTrack && track != "Not Running" && track != "Paused")
                        NotificationLib.SendNotification(NotificationLib.NotificationType.Info, $"<color=green>Spotify</color>\n{track}", 4f);
                }
            }

            if (!ShowHUD)
            {
                if (hudOverlayObj != null || hudVrObj != null)
                    DestroyHUD();
                return;
            }

            if (Settings.disableVRViewHUD)
            {
                if (hudVrObj != null)
                {
                    Destroy(hudVrObj);
                    hudVrObj = null;
                    hudVrText = null;
                }
            }
            else if (hudVrObj == null)
            {
                InitializeVRHUD();
            }

            if (hudOverlayObj == null)
                InitializeScreenHUD();

            string formatted = FormatDisplayString(currentTrackDisplay);
            if (hudOverlayText != null)
                hudOverlayText.text = formatted;
            if (hudVrText != null)
                hudVrText.text = formatted;
        }
    }
}
