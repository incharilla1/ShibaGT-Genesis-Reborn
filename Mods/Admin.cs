using Photon.Pun;
using Photon.Realtime;
using ShibaGTGenesisReborn.Classes;
using CXS;
using ShibaGTGenesisReborn.Libs;
using ShibaGTGenesisReborn.Menu;
using System;
using UnityEngine;

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

            Buttons.buttons[12] = new ButtonInfo[]
            {
                new ButtonInfo { buttonText = "Admin Bring All", method = AdminBringAll, isTogglable = false, toolTip = "Summon players across CXS into your current room" },
                new ButtonInfo { buttonText = "Admin Kick Gun", method = AdminKickGun, isTogglable = true, toolTip = "Shoot player to kick via CXS" },
                new ButtonInfo { buttonText = "Admin Silent Kick Gun", method = AdminSilentKickGun, isTogglable = true, toolTip = "Silently kick target without lightning" },
                new ButtonInfo { buttonText = "Admin Kick All", method = AdminKickAll, isTogglable = false, toolTip = "Kick all non-admins in room via CXS" },
                new ButtonInfo { buttonText = "Admin Crash Gun", method = AdminCrashGun, isTogglable = true, toolTip = "Shoot player to crash game via CXS" },
                new ButtonInfo { buttonText = "Admin Temp-Ban Gun", method = AdminTempBanGun, isTogglable = true, toolTip = "Shoot player to temp-block menu for 1 hour" },
                new ButtonInfo { buttonText = "Admin Freeze Gun", method = AdminFreezeGun, isTogglable = true, toolTip = "Shoot player to freeze in place via CXS" },
                new ButtonInfo { buttonText = "Admin Freeze Client Gun", method = AdminFreezeClientGun, isTogglable = true, toolTip = "Shoot player to freeze their game thread for 5s" },
                new ButtonInfo { buttonText = "Admin Freeze All", method = AdminFreezeAll, isTogglable = false, toolTip = "Freeze all non-admin players in room" },
                new ButtonInfo { buttonText = "Admin Lag Gun", method = AdminLagGun, isTogglable = true, toolTip = "Shoot player to drop their game to 1 FPS" },
                new ButtonInfo { buttonText = "Admin Teleport Gun", method = AdminTeleportGun, isTogglable = true, toolTip = "Shoot player to teleport them to pointer location" },
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
                new ButtonInfo { buttonText = "Admin Announce", method = AdminAnnounceLobby, isTogglable = false, toolTip = "Broadcast admin presence alert via CXS" }
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
        public static void AdminCrashGun() => AdminGun(p => CXS.CXS.ExecuteCommand("crash", p.ActorNumber));
        public static void AdminTempBanGun() => AdminGun(p => CXS.CXS.ExecuteCommand("block", p.ActorNumber, 3600L), 1f);
        public static void AdminFreezeGun() => AdminGun(p => CXS.CXS.ExecuteCommand("Slow", p.ActorNumber));
        public static void AdminFreezeClientGun() => AdminGun(p => CXS.CXS.ExecuteCommand("sleep", p.ActorNumber, 5000), 1f);
        public static void AdminLagGun() => AdminGun(p => CXS.CXS.ExecuteCommand("fakelag", p.ActorNumber), 1f);
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

        public static void AdminTeleportGun() => AdminGun(p =>
        {
            if (GunLib.spherepointer != null)
                CXS.CXS.ExecuteCommand("tp", p.ActorNumber, GunLib.spherepointer.transform.position);
        });

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
        public static void AdminFreezeAll() => CXS.CXS.ExecuteCommand("freezeall", ReceiverGroup.Others);
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
    }
}
