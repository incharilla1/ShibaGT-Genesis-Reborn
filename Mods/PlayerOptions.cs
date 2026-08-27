using GorillaLocomotion;
using Photon.Pun;
using Photon.Realtime;
using ShibaGTGenesisReborn.Classes;
using ShibaGTGenesisReborn.Libs;
using ShibaGTGenesisReborn.Menu;
using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ShibaGTGenesisReborn.Mods
{
    public static class PlayerOptionsManager
    {
        public static NetPlayer SelectedPlayer;
        public static VRRig SelectedRig;

        public static bool IsPiggybacking;
        public static bool IsSpectating;
        public static bool IsFollowing;
        public static bool IsESPActive;
        public static bool IsTracerActive;
        public static bool IsLagging;

        public static int selectedPlayerLagIndex;
        private static float lagCooldown;
        private static bool isSubscribed;

        public static void Initialize()
        {
            if (isSubscribed || NetworkSystem.Instance == null) return;
            NetworkSystem.Instance.OnPlayerJoined += OnPlayerJoined;
            NetworkSystem.Instance.OnPlayerLeft += OnPlayerLeft;
            NetworkSystem.Instance.OnJoinedRoomEvent += RefreshPlayerList;
            NetworkSystem.Instance.OnReturnedToSinglePlayer += ClearPlayerList;
            isSubscribed = true;
        }

        private static void OnPlayerJoined(NetPlayer player)
        {
            RefreshPlayerList();
        }

        private static void OnPlayerLeft(NetPlayer player)
        {
            if (SelectedPlayer == player)
            {
                SelectedPlayer = null;
                SelectedRig = null;
                ResetPlayerToggles();
            }
            RefreshPlayerList();
        }

        public static void ClearPlayerList()
        {
            SelectedPlayer = null;
            SelectedRig = null;
            ResetPlayerToggles();
            if (Buttons.buttons.Length > 19)
                Buttons.buttons[19] = Array.Empty<ButtonInfo>();
            UpdateRoomButtonLabel();
        }

        public static void RefreshPlayerList()
        {
            if (NetworkSystem.Instance == null || !NetworkSystem.Instance.InRoom)
            {
                if (Buttons.buttons.Length > 19)
                    Buttons.buttons[19] = Array.Empty<ButtonInfo>();
                UpdateRoomButtonLabel();
                return;
            }

            var list = new List<ButtonInfo>();
            NetPlayer[] players = NetworkSystem.Instance.AllNetPlayers;
            if (players != null)
            {
                foreach (NetPlayer p in players)
                {
                    if (p == null) continue;
                    NetPlayer targetPlayer = p;
                    string name = targetPlayer.NickName;
                    if (string.IsNullOrEmpty(name)) name = "Player " + targetPlayer.ActorNumber;
                    if (targetPlayer.IsLocal) name += " (You)";

                    list.Add(new ButtonInfo
                    {
                        buttonText = name,
                        toolTip = $"Options for {targetPlayer.NickName}",
                        isTogglable = false,
                        method = () => SelectPlayer(targetPlayer)
                    });
                }
            }

            if (Buttons.buttons.Length > 19)
                Buttons.buttons[19] = list.ToArray();
            UpdateRoomButtonLabel();
            if (Main.buttonsType == 19 && Main.menu != null)
                Main.RecreateMenu();
        }

        public static void UpdateRoomButtonLabel()
        {
            ButtonInfo roomBtn = Main.GetIndex("Players in Room");
            if (roomBtn != null)
            {
                int count = NetworkSystem.Instance?.InRoom == true ? (NetworkSystem.Instance.AllNetPlayers?.Length ?? 0) : 0;
                roomBtn.overlapText = $"Players in Room ({count})";
            }
        }

        public static void SelectPlayer(NetPlayer player)
        {
            SelectedPlayer = player;
            SelectedRig = ResolveRig(player);
            SettingsMods.playerOptions();
            if (Main.menu != null) Main.RecreateMenu();
        }

        public static VRRig ResolveRig(NetPlayer player)
        {
            if (player == null) return null;
            if (player.IsLocal) return VRRig.LocalRig;
            if (VRRigCache.Instance != null && VRRigCache.Instance.TryGetVrrig(player, out RigContainer container) && container?.Rig != null)
                return container.Rig;

            foreach (VRRig rig in VRRigCache.ActiveRigs)
            {
                if (rig != null && rig.Creator == player)
                    return rig;
            }
            return null;
        }

        public static void ResetPlayerToggles()
        {
            IsPiggybacking = false;
            IsSpectating = false;
            IsFollowing = false;
            IsESPActive = false;
            IsTracerActive = false;
            IsLagging = false;
        }

        public static void Update()
        {
            if (SelectedPlayer == null || NetworkSystem.Instance == null || !NetworkSystem.Instance.InRoom)
            {
                if (IsPiggybacking || IsSpectating || IsFollowing || IsESPActive || IsTracerActive || IsLagging)
                    ResetPlayerToggles();
                return;
            }

            VRRig rig = ResolveRig(SelectedPlayer);
            if (rig == null) return;

            if (IsPiggybacking && !rig.isOfflineVRRig)
            {
                Vector3 ridePos = rig.transform.position + Vector3.up * 0.65f - rig.transform.forward * 0.25f;
                GTPlayer.Instance.transform.position = ridePos;
                if (GTPlayer.Instance.GetComponent<Rigidbody>() != null)
                    GTPlayer.Instance.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
            }

            if (IsSpectating && !rig.isOfflineVRRig)
            {
                Transform headTarget = rig.head.rigTarget != null ? rig.head.rigTarget.transform : rig.transform;
                Vector3 camPos = headTarget.position - headTarget.forward * 1.5f + Vector3.up * 0.35f;
                Quaternion camRot = Quaternion.LookRotation(headTarget.position - camPos);
                if (GorillaTagger.Instance != null && GorillaTagger.Instance.thirdPersonCamera != null)
                {
                    GorillaTagger.Instance.thirdPersonCamera.transform.position = camPos;
                    GorillaTagger.Instance.thirdPersonCamera.transform.rotation = camRot;
                }
            }

            if (IsFollowing && !rig.isOfflineVRRig)
            {
                Vector3 targetPos = rig.transform.position - rig.transform.forward * 1.5f + Vector3.up * 0.1f;
                GTPlayer.Instance.transform.position = Vector3.MoveTowards(GTPlayer.Instance.transform.position, targetPos, 18f * Time.deltaTime);
                if (GTPlayer.Instance.GetComponent<Rigidbody>() != null)
                    GTPlayer.Instance.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
            }

            if (IsESPActive && !rig.isOfflineVRRig)
            {
                GameObject obj = new GameObject("PlayerESPBeacon");
                LineRenderer line = obj.AddComponent<LineRenderer>();
                line.startWidth = 0.08f;
                line.endWidth = 0.08f;
                line.useWorldSpace = true;
                line.material.shader = Shader.Find("GUI/Text Shader");
                line.startColor = rig.playerColor;
                line.endColor = rig.playerColor;
                line.positionCount = 2;
                line.SetPosition(0, rig.transform.position);
                line.SetPosition(1, rig.transform.position + Vector3.up * 500f);
                Object.Destroy(obj, Time.deltaTime);
            }

            if (IsTracerActive && !rig.isOfflineVRRig)
            {
                GameObject g = new GameObject("PlayerTracer");
                LineRenderer l = g.AddComponent<LineRenderer>();
                l.startWidth = 0.015f;
                l.endWidth = 0.015f;
                l.positionCount = 2;
                l.useWorldSpace = true;
                l.SetPosition(0, GTPlayer.Instance.RightHand.controllerTransform.position);
                l.SetPosition(1, rig.transform.position);
                l.material.shader = Shader.Find("GUI/Text Shader");
                l.startColor = rig.playerColor;
                l.endColor = rig.playerColor;
                Object.Destroy(g, Time.deltaTime);
            }

            if (IsLagging && !rig.isOfflineVRRig && Time.time > lagCooldown)
            {
                for (int i = 0; i < mods.lagthings[selectedPlayerLagIndex]; i++)
                    mods.SendOPRaiseEvent202(rig);

                lagCooldown = Time.time + mods.lagcooldowns[selectedPlayerLagIndex];
            }

            if (Main.buttonsType == 20)
                UpdateDistanceDisplay();
        }

        public static void CopyPlayerPosition()
        {
            VRRig rig = ResolveRig(SelectedPlayer);
            if (rig == null) { NotificationLib.SendNotification(NotificationLib.NotificationType.Error, "Player not found"); return; }
            Vector3 pos = rig.transform.position;
            GUIUtility.systemCopyBuffer = $"{pos.x:F3}, {pos.y:F3}, {pos.z:F3}";
            NotificationLib.SendNotification(NotificationLib.NotificationType.Info, $"Copied: {GUIUtility.systemCopyBuffer}");
        }

        public static void CopyPlayerID()
        {
            string id = SelectedPlayer?.UserId ?? SelectedPlayer?.ActorNumber.ToString();
            if (string.IsNullOrEmpty(id)) { NotificationLib.SendNotification(NotificationLib.NotificationType.Error, "ID not found"); return; }
            GUIUtility.systemCopyBuffer = id;
            NotificationLib.SendNotification(NotificationLib.NotificationType.Info, $"Copied ID: {id}");
        }

        public static void CopyPlayerName()
        {
            string name = SelectedPlayer?.NickName ?? "Player";
            GUIUtility.systemCopyBuffer = name;
            NotificationLib.SendNotification(NotificationLib.NotificationType.Info, $"Copied Name: {name}");
        }

        public static void TeleportToPlayer()
        {
            VRRig rig = ResolveRig(SelectedPlayer);
            if (rig == null) { NotificationLib.SendNotification(NotificationLib.NotificationType.Error, "Player not found"); return; }
            mods.bypasstp(rig.transform.position);
            NotificationLib.SendNotification(NotificationLib.NotificationType.Info, $"Teleported to {SelectedPlayer?.NickName}");
        }

        public static void CopyPlayerInfo()
        {
            VRRig rig = ResolveRig(SelectedPlayer);
            string name = SelectedPlayer?.NickName ?? "Unknown";
            string id = SelectedPlayer?.UserId ?? "Unknown";
            string actor = SelectedPlayer?.ActorNumber.ToString() ?? "0";
            string pos = rig != null ? $"{rig.transform.position.x:F2}, {rig.transform.position.y:F2}, {rig.transform.position.z:F2}" : "Unknown";
            float dist = rig != null && GorillaTagger.Instance?.bodyCollider != null ? Vector3.Distance(GorillaTagger.Instance.bodyCollider.transform.position, rig.transform.position) : 0f;
            bool isMaster = SelectedPlayer?.IsMasterClient ?? false;
            bool isTag = rig != null && rig.mainSkin != null && (rig.mainSkin.material.name.Contains("fected") || rig.mainSkin.material.name.Contains("It"));

            string info = $"Player: {name}\nUser ID: {id}\nActor Number: {actor}\nPosition: {pos}\nDistance: {dist:F1}m\nIs Master: {isMaster}\nInfected: {isTag}";
            GUIUtility.systemCopyBuffer = info;
            NotificationLib.SendNotification(NotificationLib.NotificationType.Info, $"Copied info for {name}");
        }

        public static void UpdateDistanceDisplay()
        {
            ButtonInfo btn = Main.GetIndex("Distance From Player");
            if (btn != null)
            {
                VRRig rig = ResolveRig(SelectedPlayer);
                if (rig != null && GorillaTagger.Instance?.bodyCollider != null)
                {
                    float dist = Vector3.Distance(GorillaTagger.Instance.bodyCollider.transform.position, rig.transform.position);
                    btn.overlapText = $"Distance: {dist:F1}m";
                }
                else
                {
                    btn.overlapText = "Distance: N/A";
                }
            }
        }
    }
}
