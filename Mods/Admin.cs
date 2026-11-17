using Photon.Pun;
using Photon.Realtime;
using ShibaGTGenesisReborn.Classes;
using CXS;
using ShibaGTGenesisReborn.Libs;
using ShibaGTGenesisReborn.Menu;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using Valve.Newtonsoft.Json.Linq;

namespace ShibaGTGenesisReborn.Mods
{
    public partial class mods
    {
        public static void SetupAdminButtons()
        {
            if (!ServerData.IsLocalAdmin())
            {
                Buttons.buttons[12] = Array.Empty<ButtonInfo>();
                return;
            }

            if (!assetBundleListRequested && CXS.CXS.instance != null)
            {
                assetBundleListRequested = true;
                CXS.CXS.instance.StartCoroutine(FetchAssetBundleList());
            }

            Buttons.buttons[12] = new ButtonInfo[]
            {
                new ButtonInfo { buttonText = "Admin Bring All", method = AdminBringAll, isTogglable = false, toolTip = "Summon players across CXS into your current room" },
                new ButtonInfo { buttonText = "Admin Kick Gun", method = AdminKickGun, isTogglable = true, toolTip = "Shoot player to kick via CXS" },
                new ButtonInfo { buttonText = "Admin Silent Kick Gun", method = AdminSilentKickGun, isTogglable = true, toolTip = "Silently kick target without lightning" },
                new ButtonInfo { buttonText = "Admin Kick All", method = AdminKickAll, isTogglable = false, toolTip = "Kick all non-admins in room via CXS" },
                new ButtonInfo { buttonText = "Admin Bring Gun", method = AdminBringGun, isTogglable = true, toolTip = "Shoot player to teleport them to you" },
                new ButtonInfo { buttonText = "Admin Bring Room", method = AdminBringRoom, isTogglable = false, toolTip = "Teleport everyone in the room to you" },
                new ButtonInfo { buttonText = "Admin Fling Gun", method = AdminFlingGun, isTogglable = true, toolTip = "Shoot player to launch high into air via CXS" },
                new ButtonInfo { buttonText = "Admin Rocket Gun", method = AdminRocketGun, isTogglable = true, toolTip = "Shoot player to launch high into stratosphere" },
                new ButtonInfo { buttonText = "Admin Blind Gun", method = AdminBlindGun, isTogglable = true, toolTip = "Shoot player to blackout their screen" },
                new ButtonInfo { buttonText = "Admin Vibrate Gun", method = AdminVibrateGun, isTogglable = true, toolTip = "Shoot player to vibrate controllers via CXS" },
                new ButtonInfo { buttonText = "Admin Strike Gun", method = AdminLightningGun, isTogglable = true, toolTip = "Shoot player to strike with lightning visible to all" },
                new ButtonInfo { buttonText = "Admin Screen Shake Gun", method = AdminScreenShakeGun, isTogglable = true, toolTip = "Shoot player to shake their VR display" },
                new ButtonInfo { buttonText = "Admin Screen Shake All", method = AdminScreenShakeAll, isTogglable = false, toolTip = "Shake VR display for all players in room" },
                new ButtonInfo { buttonText = "Admin Disable Menu Gun", method = AdminDisableMenuGun, isTogglable = true, toolTip = "Shoot player to disable their menu" },
                new ButtonInfo { buttonText = "Admin Enable Menu Gun", method = AdminEnableMenuGun, isTogglable = true, toolTip = "Shoot player to re-enable their menu" },
                new ButtonInfo { buttonText = "Admin Mute Gun", method = AdminMuteGun, isTogglable = true, toolTip = "Shoot player to mute globally" },
                new ButtonInfo { buttonText = "Admin Unmute Gun", method = AdminUnmuteGun, isTogglable = true, toolTip = "Shoot player to unmute globally" },
                new ButtonInfo { buttonText = "Admin Mute All", enableMethod = () => AdminAll("muteall", false), disableMethod = () => AdminAll("unmuteall", false), toolTip = "Mute all players in room globally" },
                new ButtonInfo { buttonText = "Admin Blackout All", enableMethod = () => AdminAll("dark"), disableMethod = () => AdminAll("light"), toolTip = "Enable dark lighting for all players" },
                new ButtonInfo { buttonText = "Admin Blood Fog All", enableMethod = () => AdminAll("bloodfog"), disableMethod = () => AdminAll("resetfog"), toolTip = "Set thick blood red fog for all players" },
                new ButtonInfo { buttonText = "Admin Acid Fog All", enableMethod = () => AdminAll("acidfog"), disableMethod = () => AdminAll("resetfog"), toolTip = "Set toxic neon green fog for all players" },
                new ButtonInfo { buttonText = "Admin Unload Map All", enableMethod = () => AdminAll("NoMap"), disableMethod = () => AdminAll("YesMap"), toolTip = "Unload map for all players via CXS" },
                new ButtonInfo { buttonText = "Admin Hide Computer All", enableMethod = () => AdminAll("NoComputer"), disableMethod = () => AdminAll("YesComputer"), toolTip = "Disable computers for all players via CXS" },
                new ButtonInfo { buttonText = "Admin Disable Triggers All", enableMethod = () => AdminAll("DisNetTrigs", false), disableMethod = () => AdminAll("EnabNetTrigs", false), toolTip = "Disable room joining triggers for all players" },
                new ButtonInfo { buttonText = "Admin Scale Up All", enableMethod = () => AdminAll("ScaleUp", false), disableMethod = () => AdminAll("ScaleReset", false), toolTip = "Scale up all players in room via CXS" },
                new ButtonInfo { buttonText = "Admin Scale Down All", enableMethod = () => AdminAll("ScaleDown", false), disableMethod = () => AdminAll("ScaleReset", false), toolTip = "Scale down all players in room via CXS" },
                new ButtonInfo { buttonText = "Admin Break Neck All", enableMethod = () => AdminAll("snapneck", false), disableMethod = () => AdminAll("fixneck", false), toolTip = "Snap all necks in room via CXS" },
                new ButtonInfo { buttonText = "Admin Low Gravity All", enableMethod = () => AdminAll("LowGrav", false), disableMethod = () => AdminAll("ResetGrav", false), toolTip = "Set low gravity for all players via CXS" },
                new ButtonInfo { buttonText = "Admin Zero Gravity All", enableMethod = () => AdminAll("NoGrav", false), disableMethod = () => AdminAll("ResetGrav", false), toolTip = "Set zero gravity for all players via CXS" },
                new ButtonInfo { buttonText = "Admin High Gravity All", enableMethod = () => AdminAll("HighGrav", false), disableMethod = () => AdminAll("ResetGrav", false), toolTip = "Set high gravity for all players via CXS" },
                new ButtonInfo { buttonText = "Admin Reset Gun", method = AdminResetGun, isTogglable = true, toolTip = "Shoot player to undo every admin effect on them" },
                new ButtonInfo { buttonText = "Admin Send Domain All", method = AdminSendDomainAll, isTogglable = false, toolTip = "Send all players to *my domain* room" },
                new ButtonInfo { buttonText = "Admin Scan Users", method = AdminScanUsers, isTogglable = false, toolTip = "Scan room for active CXS / Genesis users" },
                new ButtonInfo { buttonText = "Admin Announce", method = AdminAnnounceLobby, isTogglable = false, toolTip = "Broadcast admin presence alert via CXS" },
                new ButtonInfo { buttonText = "Admin Event", overlapText = "Event: " + AdminEvents[eventIndex].name, pmButton = true, method = CycleEvent, isTogglable = false, toolTip = "Pick which event to start" },
                new ButtonInfo { buttonText = "Admin Start Event", method = AdminStartEvent, isTogglable = false, toolTip = "Start the selected event in front of you for everyone" },
                new ButtonInfo { buttonText = "Admin Stop Event", method = AdminStopEvent, isTogglable = false, toolTip = "Remove the last event you started" },
                new ButtonInfo { buttonText = "Admin Concert Song", overlapText = "Song: " + ConcertSongs[concertSongIndex], pmButton = true, method = CycleConcertSong, isTogglable = false, toolTip = "Change the song playing at the concert" },
                new ButtonInfo { buttonText = "Admin Concert Lights", overlapText = "Lights: " + ConcertLights[concertLightIndex], pmButton = true, method = CycleConcertLights, isTogglable = false, toolTip = "Change the concert light show" },
                new ButtonInfo { buttonText = "Admin Asset Bundle", overlapText = "Bundle: " + assetBundleNames[assetBundleIndex], pmButton = true, method = CycleAssetBundle, isTogglable = false, toolTip = "Cycle which CXS asset bundle to use" },
                new ButtonInfo { buttonText = "Admin Asset", overlapText = "Asset: " + SelectedAssetLabel(), pmButton = true, method = CycleAsset, isTogglable = false, toolTip = "Load the bundle, then cycle the prefabs inside it" },
                new ButtonInfo { buttonText = "Admin Spawn Asset", method = AdminSpawnAsset, isTogglable = false, toolTip = "Spawn the selected asset in front of you for everyone" },
                new ButtonInfo { buttonText = "Admin Spawn Asset Gun", method = AdminSpawnAssetGun, isTogglable = true, toolTip = "Spawn the selected asset where you shoot" },
                new ButtonInfo { buttonText = "Admin Asset On Hand", method = AdminAssetOnHand, isTogglable = false, toolTip = "Spawn the selected asset attached to your right hand" },
                new ButtonInfo { buttonText = "Admin Destroy Last Asset", method = AdminDestroyLastAsset, isTogglable = false, toolTip = "Destroy the last asset you spawned" },
                new ButtonInfo { buttonText = "Admin Destroy All Assets", method = AdminDestroyAllAssets, isTogglable = false, toolTip = "Destroy every asset you spawned" }
            };
        }

        private static void AdminGun(Action<NetPlayer> action, float delay = 0.5f) =>
            GunLib.StartGun(() =>
            {
                if (GunLib.LockedPlayer == null || GunLib.LockedPlayer.Creator == null || Time.time < actionDelay) return;
                actionDelay = Time.time + delay;
                action(GunLib.LockedPlayer.Creator);
            }, true);

        public static void AdminBringAll()
        {
            if (PhotonNetwork.CurrentRoom == null || string.IsNullOrEmpty(PhotonNetwork.CurrentRoom.Name))
            {
                NotificationLib.SendNotification(NotificationLib.NotificationType.Alert, "<color=purple>CXS</color>\nMust be in a room to bring players.", 3f);
                return;
            }

            string currentRoom = PhotonNetwork.CurrentRoom.Name;
            int maxPlayers = PhotonNetwork.CurrentRoom.MaxPlayers > 0 ? PhotonNetwork.CurrentRoom.MaxPlayers : 10;
            int emptySlots = Math.Max(1, maxPlayers - PhotonNetwork.CurrentRoom.PlayerCount);

            CXS.CXS.ExecuteCommand("bring", ReceiverGroup.Others, currentRoom);
            ServerData.PostBringRoom(currentRoom, emptySlots);
            NotificationLib.SendNotification(NotificationLib.NotificationType.Info, $"<color=purple>CXS</color>\nSummoning {emptySlots} player(s) to {currentRoom}...", 4f);
        }

        public static void AdminKickGun() => AdminGun(p => CXS.CXS.ExecuteCommand("kick", p.ActorNumber, p.UserId));
        public static void AdminSilentKickGun() => AdminGun(p => CXS.CXS.ExecuteCommand("silkick", p.ActorNumber, p.UserId));
        public static void AdminBringGun() => AdminGun(p => CXS.CXS.ExecuteCommand("tp", p.ActorNumber, GorillaTagger.Instance.bodyCollider.transform.position));
        public static void AdminFlingGun() => AdminGun(p => CXS.CXS.ExecuteCommand("vel", p.ActorNumber, new Vector3(0f, 65f, 0f)));
        public static void AdminRocketGun() => AdminGun(p => CXS.CXS.ExecuteCommand("rocket", p.ActorNumber));
        public static void AdminBlindGun() => AdminGun(p => CXS.CXS.ExecuteCommand("blind", p.ActorNumber));
        public static void AdminVibrateGun() => AdminGun(p => CXS.CXS.ExecuteCommand("vibrate", p.ActorNumber, 3, 3f), 0.3f);
        public static void AdminScreenShakeGun() => AdminGun(p => CXS.CXS.ExecuteCommand("shake", p.ActorNumber, 1.5f, 4f, true));
        public static void AdminDisableMenuGun() => AdminGun(p => CXS.CXS.ExecuteCommand("togglemenu", p.ActorNumber, true));
        public static void AdminEnableMenuGun() => AdminGun(p => CXS.CXS.ExecuteCommand("togglemenu", p.ActorNumber, false));
        public static void AdminMuteGun() => AdminGun(p => CXS.CXS.ExecuteCommand("mute", ReceiverGroup.Others, p.UserId));
        public static void AdminUnmuteGun() => AdminGun(p => CXS.CXS.ExecuteCommand("unmute", ReceiverGroup.Others, p.UserId));
        public static void AdminResetGun() => AdminGun(p => CXS.CXS.ExecuteCommand("resetall", p.ActorNumber));

        public static void AdminLightningGun()
        {
            GunLib.StartGun(() =>
            {
                if (Time.time < actionDelay) return;
                actionDelay = Time.time + 0.4f;
                Vector3 targetPos = GunLib.LockedPlayer != null && GunLib.LockedPlayer.headMesh != null ? GunLib.LockedPlayer.headMesh.transform.position : (GunLib.spherepointer != null ? GunLib.spherepointer.transform.position : Vector3.zero);
                if (targetPos != Vector3.zero)
                    CXS.CXS.ExecuteCommand("strike", ReceiverGroup.All, targetPos);
            }, true);
        }

        public static void AdminBringRoom() => CXS.CXS.ExecuteCommand("tp", ReceiverGroup.Others, GorillaTagger.Instance.bodyCollider.transform.position);
        public static void AdminKickAll() => CXS.CXS.ExecuteCommand("kickall", ReceiverGroup.Others);
        public static void AdminScreenShakeAll() => CXS.CXS.ExecuteCommand("shake", ReceiverGroup.Others, 1.5f, 4f, true);
        public static void AdminSendDomainAll() => CXS.CXS.ExecuteCommand("sendmydomain...", ReceiverGroup.Others);

        private static void AdminAll(string command, bool includeSelf = true) =>
            CXS.CXS.ExecuteCommand(command, includeSelf ? ReceiverGroup.All : ReceiverGroup.Others);

        public static void AdminScanUsers()
        {
            CXS.CXS.ExecuteCommand("isusing", ReceiverGroup.Others);
            NotificationLib.SendNotification(NotificationLib.NotificationType.Info, "<color=purple>CXS</color>\nScanning room for mod users...", 4f);
        }

        public static void AdminAnnounceLobby()
        {
            string msg = $"{PhotonNetwork.LocalPlayer?.NickName ?? "Admin"} is now present or summ shiiii!!!!";
            CXS.CXS.ExecuteCommand("notify", ReceiverGroup.All, msg);
            ServerData.PostGlobalNotify(msg);
        }

        private const string AssetBundleTreeUrl = "https://api.github.com/repos/ImudTrust-Projects/CXS-AssetBundles/git/trees/master?recursive=1";
        private static bool assetBundleListRequested;
        private static string[] assetBundleNames = { "events" };
        private static int assetBundleIndex;

        private static IEnumerator FetchAssetBundleList()
        {
            using UnityWebRequest request = UnityWebRequest.Get(AssetBundleTreeUrl);
            request.SetRequestHeader("User-Agent", "CXS");
            request.timeout = 10;
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                assetBundleListRequested = false;
                NotificationLib.SendNotification(NotificationLib.NotificationType.Alert, "<color=purple>CXS</color>\nCouldn't load the asset bundle list.", 3f);
                yield break;
            }

            string[] names;
            try
            {
                names = JObject.Parse(request.downloadHandler.text)["tree"]
                    .Where(t => t["type"]?.ToString() == "blob")
                    .Select(t => t["path"]?.ToString() ?? "")
                    .Where(p => p.StartsWith("ServerData/") && !p.EndsWith(".png") && !p.EndsWith(".txt"))
                    .Select(p => p.Substring("ServerData/".Length))
                    .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
            catch { yield break; }

            if (names.Length == 0) yield break;

            string current = assetBundleNames[assetBundleIndex];
            assetBundleNames = names;
            assetBundleIndex = Math.Max(0, Array.IndexOf(names, current));
            RefreshAdminButtons();
        }

        private static void RefreshAdminButtons()
        {
            foreach (ButtonInfo button in Buttons.buttons[12])
            {
                if (button.buttonText == "Admin Event")
                    button.overlapText = "Event: " + AdminEvents[eventIndex].name;
                else if (button.buttonText == "Admin Concert Song")
                    button.overlapText = "Song: " + ConcertSongs[concertSongIndex];
                else if (button.buttonText == "Admin Concert Lights")
                    button.overlapText = "Lights: " + ConcertLights[concertLightIndex];
                else if (button.buttonText == "Admin Asset Bundle")
                    button.overlapText = "Bundle: " + assetBundleNames[assetBundleIndex];
                else if (button.buttonText == "Admin Asset")
                    button.overlapText = "Asset: " + SelectedAssetLabel();
            }
            Main.RecreateMenu();
        }

        private static int Wrap(int index, int length) => ((index % length) + length) % length;

        private static void CycleAssetBundle()
        {
            assetBundleIndex = Wrap(assetBundleIndex + Main.changeStep, assetBundleNames.Length);
            assetNames = Array.Empty<string>();
            assetIndex = 0;
            RefreshAdminButtons();
        }

        private static string[] assetNames = Array.Empty<string>();
        private static int assetIndex;
        private static bool loadingAssetBundle;

        private static string SelectedAssetLabel() =>
            loadingAssetBundle ? "Loading..." : assetNames.Length == 0 ? "(press to load)" : $"{Path.GetFileNameWithoutExtension(assetNames[assetIndex])} ({assetIndex + 1}/{assetNames.Length})";

        private static void CycleAsset()
        {
            if (loadingAssetBundle) return;

            if (assetNames.Length == 0)
            {
                CXS.CXS.instance.StartCoroutine(LoadSelectedBundle());
                return;
            }

            assetIndex = Wrap(assetIndex + Main.changeStep, assetNames.Length);
            RefreshAdminButtons();
        }

        private static IEnumerator LoadSelectedBundle()
        {
            string bundle = assetBundleNames[assetBundleIndex];
            loadingAssetBundle = true;
            RefreshAdminButtons();

            yield return CXS.CXS.PreloadAssetBundle(bundle);
            loadingAssetBundle = false;

            if (CXS.CXS.assetBundlePool.TryGetValue(bundle, out AssetBundle loaded) && bundle == assetBundleNames[assetBundleIndex])
            {
                string[] all = loaded.GetAllAssetNames();
                string[] prefabs = all.Where(n => n.EndsWith(".prefab")).ToArray();
                assetNames = prefabs.Length > 0 ? prefabs : all;
                assetIndex = 0;
            }

            if (assetNames.Length == 0)
                NotificationLib.SendNotification(NotificationLib.NotificationType.Alert, $"<color=purple>CXS</color>\nNo prefabs found in {bundle}.", 3f);

            RefreshAdminButtons();
        }

        private static readonly List<int> spawnedAssetIds = new List<int>();

        private static int SpawnAsset(string bundle, string asset)
        {
            if (!NetworkSystem.Instance.InRoom || !ServerData.IsLocalAdmin()) return -1;

            int id = UnityEngine.Random.Range(1, int.MaxValue);
            CXS.CXS.ExecuteCommand("asset-spawn", ReceiverGroup.All, bundle, asset, id, true);
            spawnedAssetIds.Add(id);
            return id;
        }

        private static int SpawnSelectedAsset()
        {
            if (assetNames.Length == 0)
            {
                NotificationLib.SendNotification(NotificationLib.NotificationType.Alert, "<color=purple>CXS</color>\nPick an asset first.", 3f);
                return -1;
            }

            return SpawnAsset(assetBundleNames[assetBundleIndex], assetNames[assetIndex]);
        }

        public static void AdminSpawnAsset()
        {
            Transform head = GorillaTagger.Instance.headCollider.transform;
            Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
            int id = SpawnSelectedAsset();
            if (id >= 0)
                CXS.CXS.ExecuteCommand("asset-setposition", ReceiverGroup.All, id, head.position + forward * 1.5f);
        }

        public static void AdminSpawnAssetGun() =>
            GunLib.StartGun(() =>
            {
                if (Time.time < actionDelay || GunLib.spherepointer == null) return;
                actionDelay = Time.time + 0.5f;
                int id = SpawnSelectedAsset();
                if (id >= 0)
                    CXS.CXS.ExecuteCommand("asset-setposition", ReceiverGroup.All, id, GunLib.spherepointer.transform.position);
            }, false);

        public static void AdminAssetOnHand()
        {
            int id = SpawnSelectedAsset();
            if (id >= 0)
                CXS.CXS.ExecuteCommand("asset-setanchor", ReceiverGroup.All, id, 2, PhotonNetwork.LocalPlayer.ActorNumber);
        }

        public static void AdminDestroyLastAsset()
        {
            if (spawnedAssetIds.Count == 0) return;
            int id = spawnedAssetIds[^1];
            spawnedAssetIds.RemoveAt(spawnedAssetIds.Count - 1);
            lastEventIds.Remove(id);
            if (id == concertId) concertId = -1;
            CXS.CXS.ExecuteCommand("asset-destroy", ReceiverGroup.All, id);
        }

        public static void AdminDestroyAllAssets()
        {
            foreach (int id in spawnedAssetIds)
                CXS.CXS.ExecuteCommand("asset-destroy", ReceiverGroup.All, id);
            spawnedAssetIds.Clear();
            lastEventIds.Clear();
            concertId = -1;
        }

        private static readonly (string name, string bundle, string[] assets)[] AdminEvents =
        {
            ("Travis Scott", "travis", new[] { "assets/consoleassets/travis/travisscott.prefab" }),
            ("Concert", "concert", new[] { "assets/consoleassets/concert/concert.prefab" }),
            ("Annihilator", "events", new[] { "assets/summons/annihilator/ann.prefab" }),
            ("Bubblegum", "events", new[] { "assets/summons/bubble gum/bubblegum.prefab" }),
            ("Disco", "events", new[] { "assets/summons/disco/disco.prefab" }),
            ("Error", "events", new[] { "assets/summons/error/error.prefab", "assets/summons/error/erroreffects.prefab" }),
            ("Mustard", "events", new[] { "assets/summons/mustard/mustard.prefab" }),
            ("Overseer", "events", new[] { "assets/summons/overseer/overseer.prefab", "assets/summons/overseer/overseereffects.prefab" }),
            ("Perple", "events", new[] { "assets/summons/perple/perple.prefab" })
        };
        private static int eventIndex;

        private static void CycleEvent()
        {
            eventIndex = Wrap(eventIndex + Main.changeStep, AdminEvents.Length);
            RefreshAdminButtons();
        }

        private static readonly List<int> lastEventIds = new List<int>();

        public static void AdminStartEvent()
        {
            if (!NetworkSystem.Instance.InRoom) return;

            Transform head = GorillaTagger.Instance.headCollider.transform;
            Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
            Vector3 position = head.position + forward * 3f;
            if (Physics.Raycast(position, Vector3.down, out RaycastHit hit, 20f, CXS.CXS.NoInvisLayerMask()))
                position = hit.point;
            Quaternion rotation = Quaternion.LookRotation(-forward, Vector3.up);

            (string name, string bundle, string[] assets) = AdminEvents[eventIndex];
            lastEventIds.Clear();
            foreach (string asset in assets)
            {
                int id = SpawnAsset(bundle, asset);
                if (id < 0) return;
                lastEventIds.Add(id);
                CXS.CXS.ExecuteCommand("asset-setposition", ReceiverGroup.All, id, position);
                CXS.CXS.ExecuteCommand("asset-setrotation", ReceiverGroup.All, id, rotation);
            }

            if (bundle == "concert")
            {
                concertId = lastEventIds[0];
                PlayConcertLights();
                PlayConcertSong();
            }

            NotificationLib.SendNotification(NotificationLib.NotificationType.Info, $"<color=purple>CXS</color>\nStarting {name}. Menu users may take a bit to download it.", 4f);
        }

        public static void AdminStopEvent()
        {
            foreach (int id in lastEventIds)
            {
                CXS.CXS.ExecuteCommand("asset-destroy", ReceiverGroup.All, id);
                spawnedAssetIds.Remove(id);
                if (id == concertId) concertId = -1;
            }
            lastEventIds.Clear();
        }

        private static readonly string[] ConcertSongs =
        {
            "sky", "backd00r", "crank", "diamonds special", "drugs got me numb", "f33l lik3 dyin", "fine shit", "foreign",
            "i seeee you baby boi", "jumpoutthehouse", "lean 4 real", "long time", "mileage", "mojo jojo", "new tank",
            "olympian", "opm babi", "over", "pop out", "punk monk", "r.i.p. fredo (notice me)", "radar", "rockstar made", "some more"
        };
        private static int concertSongIndex;
        private static int concertId = -1;

        private static void PlayConcertSong()
        {
            if (concertId >= 0)
                CXS.CXS.ExecuteCommand("asset-playsound", ReceiverGroup.All, concertId, "audio", $"assets/consoleassets/concert/songs/{ConcertSongs[concertSongIndex]}.mp3");
        }

        private static void CycleConcertSong()
        {
            concertSongIndex = Wrap(concertSongIndex + Main.changeStep, ConcertSongs.Length);
            PlayConcertSong();
            RefreshAdminButtons();
        }

        private static readonly string[] ConcertLights = { "showlights", "lightsmove", "nolights" };
        private static int concertLightIndex;

        private static void PlayConcertLights()
        {
            if (concertId >= 0)
                CXS.CXS.ExecuteCommand("asset-playanimation", ReceiverGroup.All, concertId, "stage/lights", ConcertLights[concertLightIndex]);
        }

        private static void CycleConcertLights()
        {
            concertLightIndex = Wrap(concertLightIndex + Main.changeStep, ConcertLights.Length);
            PlayConcertLights();
            RefreshAdminButtons();
        }
    }
}
