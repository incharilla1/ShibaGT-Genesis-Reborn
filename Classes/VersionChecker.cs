using System;
using System.Collections;
using System.Text.RegularExpressions;
using ShibaGTGenesisReborn.Libs;
using ShibaGTGenesisReborn.Menu;
using UnityEngine;
using UnityEngine.Networking;

namespace ShibaGTGenesisReborn.Classes
{
    public static class VersionChecker
    {
        private const string VersionUrl = "https://raw.githubusercontent.com/incharilla1/ShibaGT-Genesis-Reborn/main/PluginInfo.cs";
        public static string LatestVersion { get; private set; }
        public static bool? IsUpToDate { get; private set; }

        public static void CheckVersion(bool notifyIfLatest = true)
        {
            if (Plugin.Instance != null)
                Plugin.Instance.StartCoroutine(CheckVersionCoroutine(notifyIfLatest));
            else if (Main.Instance != null)
                Main.Instance.StartCoroutine(CheckVersionCoroutine(notifyIfLatest));
        }

        private static IEnumerator CheckVersionCoroutine(bool notifyIfLatest)
        {
            using UnityWebRequest request = UnityWebRequest.Get(VersionUrl);
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                if (notifyIfLatest)
                    NotificationLib.SendNotification(NotificationLib.NotificationType.Error, "Failed to check version");
                yield break;
            }

            string text = request.downloadHandler.text;
            Match match = Regex.Match(text, @"Version\s*=\s*""([^""]+)""");
            if (!match.Success)
            {
                if (notifyIfLatest)
                    NotificationLib.SendNotification(NotificationLib.NotificationType.Error, "Invalid version format");
                yield break;
            }

            LatestVersion = match.Groups[1].Value.Trim();
            bool isCurrent = string.Equals(LatestVersion, PluginInfo.Version, StringComparison.OrdinalIgnoreCase);

            if (Version.TryParse(LatestVersion, out Version remoteVer) && Version.TryParse(PluginInfo.Version, out Version localVer))
                isCurrent = localVer >= remoteVer;

            IsUpToDate = isCurrent;

            if (isCurrent)
            {
                if (notifyIfLatest)
                    NotificationLib.SendNotification(NotificationLib.NotificationType.Info, $"You are on the latest version (v{PluginInfo.Version})");
            }
            else
            {
                NotificationLib.SendNotification(NotificationLib.NotificationType.Alert, $"Update available! v{PluginInfo.Version} -> v{LatestVersion}");
            }
        }
    }
}
