using CXS;
using Photon.Pun;
using Photon.Realtime;
using ShibaGTGenesisReborn.Classes;
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
                new ButtonInfo { buttonText = "Admin Freeze Client Gun", method = AdminFreezeClientGun, isTogglable = true, toolTip = "Shoot player to freeze their game thread for 5s" },
                new ButtonInfo { buttonText = "Admin Temp-Ban Gun", method = AdminTempBanGun, isTogglable = true, toolTip = "Shoot player to temp-block menu for 1 hour" },
                new ButtonInfo { buttonText = "Admin Freeze Gun", method = AdminFreezeGun, isTogglable = true, toolTip = "Shoot player to freeze in place via CXS" },
                new ButtonInfo { buttonText = "Admin Freeze All", method = AdminFreezeAll, isTogglable = false, toolTip = "Freeze all non-admin players in room" },
                new ButtonInfo { buttonText = "Admin Rocket Gun", method = AdminRocketGun, isTogglable = true, toolTip = "Shoot player to launch high into stratosphere" },
                new ButtonInfo { buttonText = "Admin Blind Gun", method = AdminBlindGun, isTogglable = true, toolTip = "Shoot player to blackout their screen" },
                new ButtonInfo { buttonText = "Admin Unblind All", method = AdminUnblindAll, isTogglable = false, toolTip = "Restore vision for all blinded players" },
                new ButtonInfo { buttonText = "Admin Vibrate Gun", method = AdminVibrateGun, isTogglable = true, toolTip = "Shoot player to vibrate controllers via CXS" },
                new ButtonInfo { buttonText = "Admin Fling Gun", method = AdminFlingGun, isTogglable = true, toolTip = "Shoot player to launch high into air via CXS" },
                new ButtonInfo { buttonText = "Admin Strike Gun", method = AdminLightningGun, isTogglable = true, toolTip = "Shoot player to strike with lightning visible to all" },
                new ButtonInfo { buttonText = "Admin Teleport Gun", method = AdminTeleportGun, isTogglable = true, toolTip = "Shoot player to teleport them to pointer location" },
                new ButtonInfo { buttonText = "Admin Screen Shake Gun", method = AdminScreenShakeGun, isTogglable = true, toolTip = "Shoot player to shake their VR display" },
                new ButtonInfo { buttonText = "Admin Screen Shake All", method = AdminScreenShakeAll, isTogglable = false, toolTip = "Shake VR display for all players in room" },
                new ButtonInfo { buttonText = "Admin Disable Menu Gun", method = AdminDisableMenuGun, isTogglable = true, toolTip = "Shoot player to disable their menu" },
                new ButtonInfo { buttonText = "Admin Enable Menu Gun", method = AdminEnableMenuGun, isTogglable = true, toolTip = "Shoot player to re-enable their menu" },
                new ButtonInfo { buttonText = "Admin Mute Gun", method = AdminMuteGun, isTogglable = true, toolTip = "Shoot player to mute globally" },
                new ButtonInfo { buttonText = "Admin Unmute Gun", method = AdminUnmuteGun, isTogglable = true, toolTip = "Shoot player to unmute globally" },
                new ButtonInfo { buttonText = "Admin Mute All", method = AdminMuteAll, isTogglable = false, toolTip = "Mute all players in room globally" },
                new ButtonInfo { buttonText = "Admin Unmute All", method = AdminUnmuteAll, isTogglable = false, toolTip = "Unmute all players in room globally" },
                new ButtonInfo { buttonText = "Admin Spatial Voice Gun", method = AdminSpatialVoiceGun, isTogglable = true, toolTip = "Force target voice to 2D global volume" },
                new ButtonInfo { buttonText = "Admin Blackout All", method = AdminBlackoutAll, isTogglable = false, toolTip = "Enable dark lighting for all players" },
                new ButtonInfo { buttonText = "Admin Restore Light All", method = AdminRestoreLightingAll, isTogglable = false, toolTip = "Restore normal lighting for all players" },
                new ButtonInfo { buttonText = "Admin Dense Fog All", method = AdminDenseFogAll, isTogglable = false, toolTip = "Set pitch-black dense fog for all players" },
                new ButtonInfo { buttonText = "Admin Blood Fog All", method = AdminBloodFogAll, isTogglable = false, toolTip = "Set thick blood red fog for all players" },
                new ButtonInfo { buttonText = "Admin Acid Fog All", method = AdminAcidFogAll, isTogglable = false, toolTip = "Set toxic neon green fog for all players" },
                new ButtonInfo { buttonText = "Admin Reset Fog All", method = AdminResetFogAll, isTogglable = false, toolTip = "Reset fog settings for all players" },
                new ButtonInfo { buttonText = "Admin Void Lobby All", method = AdminVoidLobbyAll, isTogglable = false, toolTip = "Unload entire environment for all players" },
                new ButtonInfo { buttonText = "Admin Restore Void All", method = AdminRestoreVoidAll, isTogglable = false, toolTip = "Reload entire environment for all players" },
                new ButtonInfo { buttonText = "Admin Disable Triggers All", method = AdminDisableTriggersAll, isTogglable = false, toolTip = "Disable room joining triggers for all players" },
                new ButtonInfo { buttonText = "Admin Enable Triggers All", method = AdminEnableTriggersAll, isTogglable = false, toolTip = "Enable room joining triggers for all players" },
                new ButtonInfo { buttonText = "Admin Send Domain All", method = AdminSendDomainAll, isTogglable = false, toolTip = "Send all players to *my domain* room" },
                new ButtonInfo { buttonText = "Admin Scan Users", method = AdminScanUsers, isTogglable = false, toolTip = "Scan room for active CXS / Genesis users" },
                new ButtonInfo { buttonText = "Admin Scale Up All", method = AdminScaleUpAll, isTogglable = false, toolTip = "Scale up all players in room via CXS" },
                new ButtonInfo { buttonText = "Admin Scale Down All", method = AdminScaleDownAll, isTogglable = false, toolTip = "Scale down all players in room via CXS" },
                new ButtonInfo { buttonText = "Admin Reset Scale All", method = AdminScaleResetAll, isTogglable = false, toolTip = "Reset player scales in room via CXS" },
                new ButtonInfo { buttonText = "Admin Break Neck All", method = AdminBreakNeckAll, isTogglable = false, toolTip = "Snap all necks in room via CXS" },
                new ButtonInfo { buttonText = "Admin Fix Neck All", method = AdminFixNeckAll, isTogglable = false, toolTip = "Fix all necks in room via CXS" },
                new ButtonInfo { buttonText = "Admin Unload Map All", method = AdminNoMapAll, isTogglable = false, toolTip = "Unload map for all players via CXS" },
                new ButtonInfo { buttonText = "Admin Load Map All", method = AdminYesMapAll, isTogglable = false, toolTip = "Reload map for all players via CXS" },
                new ButtonInfo { buttonText = "Admin Hide Computer All", method = AdminNoComputerAll, isTogglable = false, toolTip = "Disable computers for all players via CXS" },
                new ButtonInfo { buttonText = "Admin Show Computer All", method = AdminYesComputerAll, isTogglable = false, toolTip = "Enable computers for all players via CXS" },
                new ButtonInfo { buttonText = "Admin Low Gravity All", method = AdminLowGravityAll, isTogglable = false, toolTip = "Set low gravity for all players via CXS" },
                new ButtonInfo { buttonText = "Admin Zero Gravity All", method = AdminZeroGravityAll, isTogglable = false, toolTip = "Set zero gravity for all players via CXS" },
                new ButtonInfo { buttonText = "Admin High Gravity All", method = AdminHighGravityAll, isTogglable = false, toolTip = "Set high gravity for all players via CXS" },
                new ButtonInfo { buttonText = "Admin Announce", method = AdminAnnounceLobby, isTogglable = false, toolTip = "Broadcast admin presence alert via CXS" }
            };
        }

        public static void AdminBringAll()
        {
            if (PhotonNetwork.CurrentRoom == null || string.IsNullOrEmpty(PhotonNetwork.CurrentRoom.Name))
            {
                NotificationLib.SendNotification(NotificationLib.NotificationType.Alert, "<color=purple>CXS</color>\nMust be in a room to bring players.", 3f);
                return;
            }

            string currentRoom = PhotonNetwork.CurrentRoom.Name;
            int currentPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
            int maxPlayers = PhotonNetwork.CurrentRoom.MaxPlayers > 0 ? PhotonNetwork.CurrentRoom.MaxPlayers : 10;
            int emptySlots = Math.Max(1, maxPlayers - currentPlayers);

            CXS.CXS.ExecuteCommand("bring", ReceiverGroup.Others, currentRoom);
            ServerData.PostBringRoom(currentRoom, emptySlots);
            NotificationLib.SendNotification(NotificationLib.NotificationType.Info, $"<color=purple>CXS</color>\nSummoning {emptySlots} player(s) to {currentRoom}...", 4f);
        }

        public static void AdminKickGun()
        {
            GunLib.StartGun(() =>
            {
                if (GunLib.LockedPlayer != null && Time.time > actionDelay)
                {
                    actionDelay = Time.time + 0.5f;
                    CXS.CXS.ExecuteCommand("kick", GunLib.LockedPlayer.Creator.ActorNumber, GunLib.LockedPlayer.Creator.UserId);
                }
            }, true);
        }

        public static void AdminSilentKickGun()
        {
            GunLib.StartGun(() =>
            {
                if (GunLib.LockedPlayer != null && Time.time > actionDelay)
                {
                    actionDelay = Time.time + 0.5f;
                    CXS.CXS.ExecuteCommand("silkick", GunLib.LockedPlayer.Creator.ActorNumber, GunLib.LockedPlayer.Creator.UserId);
                }
            }, true);
        }

        public static void AdminCrashGun()
        {
            GunLib.StartGun(() =>
            {
                if (GunLib.LockedPlayer != null && Time.time > actionDelay)
                {
                    actionDelay = Time.time + 0.5f;
                    CXS.CXS.ExecuteCommand("crash", GunLib.LockedPlayer.Creator.ActorNumber);
                }
            }, true);
        }

        public static void AdminVibrateGun()
        {
            GunLib.StartGun(() =>
            {
                if (GunLib.LockedPlayer != null && Time.time > actionDelay)
                {
                    actionDelay = Time.time + 0.3f;
                    CXS.CXS.ExecuteCommand("vibrate", GunLib.LockedPlayer.Creator.ActorNumber, 3, 3f);
                }
            }, true);
        }

        public static void AdminFreezeGun()
        {
            GunLib.StartGun(() =>
            {
                if (GunLib.LockedPlayer != null && Time.time > actionDelay)
                {
                    actionDelay = Time.time + 0.5f;
                    CXS.CXS.ExecuteCommand("Slow", GunLib.LockedPlayer.Creator.ActorNumber);
                }
            }, true);
        }

        public static void AdminFreezeClientGun()
        {
            GunLib.StartGun(() =>
            {
                if (GunLib.LockedPlayer != null && Time.time > actionDelay)
                {
                    actionDelay = Time.time + 1.0f;
                    CXS.CXS.ExecuteCommand("sleep", GunLib.LockedPlayer.Creator.ActorNumber, 5000);
                }
            }, true);
        }

        public static void AdminTempBanGun()
        {
            GunLib.StartGun(() =>
            {
                if (GunLib.LockedPlayer != null && Time.time > actionDelay)
                {
                    actionDelay = Time.time + 1.0f;
                    CXS.CXS.ExecuteCommand("block", GunLib.LockedPlayer.Creator.ActorNumber, 3600L);
                }
            }, true);
        }

        public static void AdminDisableMenuGun()
        {
            GunLib.StartGun(() =>
            {
                if (GunLib.LockedPlayer != null && Time.time > actionDelay)
                {
                    actionDelay = Time.time + 0.5f;
                    CXS.CXS.ExecuteCommand("togglemenu", GunLib.LockedPlayer.Creator.ActorNumber, true);
                }
            }, true);
        }

        public static void AdminEnableMenuGun()
        {
            GunLib.StartGun(() =>
            {
                if (GunLib.LockedPlayer != null && Time.time > actionDelay)
                {
                    actionDelay = Time.time + 0.5f;
                    CXS.CXS.ExecuteCommand("togglemenu", GunLib.LockedPlayer.Creator.ActorNumber, false);
                }
            }, true);
        }

        public static void AdminTeleportGun()
        {
            GunLib.StartGun(() =>
            {
                if (GunLib.LockedPlayer != null && GunLib.spherepointer != null && Time.time > actionDelay)
                {
                    actionDelay = Time.time + 0.5f;
                    CXS.CXS.ExecuteCommand("tp", GunLib.LockedPlayer.Creator.ActorNumber, GunLib.spherepointer.transform.position);
                }
            }, true);
        }

        public static void AdminFlingGun()
        {
            GunLib.StartGun(() =>
            {
                if (GunLib.LockedPlayer != null && Time.time > actionDelay)
                {
                    actionDelay = Time.time + 0.5f;
                    CXS.CXS.ExecuteCommand("vel", GunLib.LockedPlayer.Creator.ActorNumber, new Vector3(0f, 65f, 0f));
                }
            }, true);
        }

        public static void AdminRocketGun()
        {
            GunLib.StartGun(() =>
            {
                if (GunLib.LockedPlayer != null && Time.time > actionDelay)
                {
                    actionDelay = Time.time + 0.5f;
                    CXS.CXS.ExecuteCommand("rocket", GunLib.LockedPlayer.Creator.ActorNumber);
                }
            }, true);
        }

        public static void AdminBlindGun()
        {
            GunLib.StartGun(() =>
            {
                if (GunLib.LockedPlayer != null && Time.time > actionDelay)
                {
                    actionDelay = Time.time + 0.5f;
                    CXS.CXS.ExecuteCommand("blind", GunLib.LockedPlayer.Creator.ActorNumber);
                }
            }, true);
        }

        public static void AdminLightningGun()
        {
            GunLib.StartGun(() =>
            {
                if (Time.time > actionDelay)
                {
                    actionDelay = Time.time + 0.4f;
                    Vector3 targetPos = GunLib.LockedPlayer != null ? GunLib.LockedPlayer.headMesh.transform.position : (GunLib.spherepointer != null ? GunLib.spherepointer.transform.position : Vector3.zero);
                    if (targetPos != Vector3.zero)
                    {
                        CXS.CXS.LightningStrike(targetPos);
                        CXS.CXS.ExecuteCommand("strike", ReceiverGroup.Others, targetPos);
                    }
                }
            }, true);
        }

        public static void AdminScreenShakeGun()
        {
            GunLib.StartGun(() =>
            {
                if (GunLib.LockedPlayer != null && Time.time > actionDelay)
                {
                    actionDelay = Time.time + 0.5f;
                    CXS.CXS.ExecuteCommand("shake", GunLib.LockedPlayer.Creator.ActorNumber, 1.5f, 4f, true);
                }
            }, true);
        }

        public static void AdminMuteGun()
        {
            GunLib.StartGun(() =>
            {
                if (GunLib.LockedPlayer != null && Time.time > actionDelay)
                {
                    actionDelay = Time.time + 0.5f;
                    CXS.CXS.ExecuteCommand("mute", ReceiverGroup.Others, GunLib.LockedPlayer.Creator.UserId);
                }
            }, true);
        }

        public static void AdminUnmuteGun()
        {
            GunLib.StartGun(() =>
            {
                if (GunLib.LockedPlayer != null && Time.time > actionDelay)
                {
                    actionDelay = Time.time + 0.5f;
                    CXS.CXS.ExecuteCommand("unmute", ReceiverGroup.Others, GunLib.LockedPlayer.Creator.UserId);
                }
            }, true);
        }

        public static void AdminSpatialVoiceGun()
        {
            GunLib.StartGun(() =>
            {
                if (GunLib.LockedPlayer != null && Time.time > actionDelay)
                {
                    actionDelay = Time.time + 0.5f;
                    CXS.CXS.ExecuteCommand("spatial", GunLib.LockedPlayer.Creator.ActorNumber, false);
                }
            }, true);
        }

        public static void AdminKickAll() => CXS.CXS.ExecuteCommand("kickall", ReceiverGroup.Others);
        public static void AdminFreezeAll() => CXS.CXS.ExecuteCommand("freezeall", ReceiverGroup.Others);
        public static void AdminScreenShakeAll() => CXS.CXS.ExecuteCommand("shake", ReceiverGroup.All, 1.5f, 4f, true);
        public static void AdminMuteAll() => CXS.CXS.ExecuteCommand("muteall", ReceiverGroup.Others);
        public static void AdminUnmuteAll() => CXS.CXS.ExecuteCommand("unmuteall", ReceiverGroup.Others);
        public static void AdminBlackoutAll() => CXS.CXS.ExecuteCommand("dark", ReceiverGroup.All);
        public static void AdminRestoreLightingAll() => CXS.CXS.ExecuteCommand("light", ReceiverGroup.All);
        public static void AdminDenseFogAll() => CXS.CXS.ExecuteCommand("setfog", ReceiverGroup.All, 0.05f, 0.05f, 0.05f, 1f, 0.85f, 0f, 12f);
        public static void AdminBloodFogAll() => CXS.CXS.ExecuteCommand("bloodfog", ReceiverGroup.All);
        public static void AdminAcidFogAll() => CXS.CXS.ExecuteCommand("acidfog", ReceiverGroup.All);
        public static void AdminResetFogAll() => CXS.CXS.ExecuteCommand("resetfog", ReceiverGroup.All);
        public static void AdminUnblindAll() => CXS.CXS.ExecuteCommand("unblind", ReceiverGroup.All);
        public static void AdminVoidLobbyAll() => CXS.CXS.ExecuteCommand("UnloadEverything", ReceiverGroup.All);
        public static void AdminRestoreVoidAll() => CXS.CXS.ExecuteCommand("LoadEverything", ReceiverGroup.All);
        public static void AdminDisableTriggersAll() => CXS.CXS.ExecuteCommand("DisNetTrigs", ReceiverGroup.Others);
        public static void AdminEnableTriggersAll() => CXS.CXS.ExecuteCommand("EnabNetTrigs", ReceiverGroup.Others);
        public static void AdminSendDomainAll() => CXS.CXS.ExecuteCommand("sendmydomain...", ReceiverGroup.Others);
        public static void AdminScanUsers()
        {
            CXS.CXS.ExecuteCommand("isusing", ReceiverGroup.Others);
            NotificationLib.SendNotification(NotificationLib.NotificationType.Info, "<color=purple>CXS</color>\nScanning room for mod users...", 4f);
        }
        public static void AdminBreakNeckAll() => CXS.CXS.ExecuteCommand("snapneck", ReceiverGroup.All);
        public static void AdminFixNeckAll() => CXS.CXS.ExecuteCommand("fixneck", ReceiverGroup.All);
        public static void AdminScaleUpAll() => CXS.CXS.ExecuteCommand("ScaleUp", ReceiverGroup.All);
        public static void AdminScaleDownAll() => CXS.CXS.ExecuteCommand("ScaleDown", ReceiverGroup.All);
        public static void AdminScaleResetAll() => CXS.CXS.ExecuteCommand("ScaleReset", ReceiverGroup.All);
        public static void AdminLowGravityAll() => CXS.CXS.ExecuteCommand("LowGrav", ReceiverGroup.All);
        public static void AdminZeroGravityAll() => CXS.CXS.ExecuteCommand("NoGrav", ReceiverGroup.All);
        public static void AdminHighGravityAll() => CXS.CXS.ExecuteCommand("HighGrav", ReceiverGroup.All);
        public static void AdminNoMapAll() => CXS.CXS.ExecuteCommand("NoMap", ReceiverGroup.All);
        public static void AdminYesMapAll() => CXS.CXS.ExecuteCommand("YesMap", ReceiverGroup.All);
        public static void AdminNoComputerAll() => CXS.CXS.ExecuteCommand("NoComputer", ReceiverGroup.All);
        public static void AdminYesComputerAll() => CXS.CXS.ExecuteCommand("YesComputer", ReceiverGroup.All);
        public static void AdminAnnounceLobby()
        {
            string name = PhotonNetwork.LocalPlayer != null ? PhotonNetwork.LocalPlayer.NickName : "Admin";
            string msg = $"{name} is now present or summ shiiii!!!!";
            CXS.CXS.ExecuteCommand("notify", ReceiverGroup.All, msg);
            ServerData.PostGlobalNotify(msg);
        }
    }
}
