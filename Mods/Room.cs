using GorillaNetworking;
using GorillaTag.Audio;
using Photon.Pun;
using Photon.Realtime;
using Photon.Voice.Unity;
using ShibaGTGenesisReborn.Classes;
using ShibaGTGenesisReborn.Libs;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ShibaGTGenesisReborn.Mods
{
    public partial class mods
    {
        public static string lastmap;
        private static float actionDelay;

        public static void BDisconnect()
        {
            if (InputHandler.Instance.RightSecondary.IsPressed)
            {
                NetworkSystem.Instance.ReturnToSinglePlayer();
                PhotonNetwork.Disconnect();
            }
        }

        public static void Joincodegenesis()
        {
            PhotonNetworkController.Instance.AttemptToJoinSpecificRoom("GENESIS", GorillaNetworking.JoinType.Solo);
        }

        public static void JoinRandom()
        {
            if (PhotonNetworkController.Instance.currentJoinTrigger.networkZone != null)
            {
                lastmap = PhotonNetworkController.Instance.currentJoinTrigger.networkZone;
            }
            if (!NetworkSystem.Instance.InRoom)
            {
                PhotonNetworkController.Instance.AttemptToJoinPublicRoom(GorillaComputer.instance.GetJoinTriggerForZone(lastmap), GorillaNetworking.JoinType.Solo);
            }
        }

        public static void ConnectToRegion(string region)
        {
            if (PhotonNetwork.CloudRegion != region) PhotonNetwork.ConnectToRegion(region);
            NetworkSystem.Instance.currentRegionIndex = Array.IndexOf(NetworkSystem.Instance.regionNames, region);
        }

        public static void RPCProt(bool experimental = false)
        {
            if (!NetworkSystem.Instance.InRoom) return;
            try
            {
                if (MonkeAgent.instance != null)
                {
                    MonkeAgent.instance.rpcErrorMax = int.MaxValue;
                    MonkeAgent.instance.rpcCallLimit = int.MaxValue;
                    MonkeAgent.instance.logErrorMax = int.MaxValue;
                    MonkeAgent.instance.userDecayTime = 0f;

                    MonkeAgent.instance.reportedPlayers?.Clear();
                    MonkeAgent.instance.userRPCCalls?.Clear();

                    Application.logMessageReceived -= MonkeAgent.instance.LogErrorCount;
                    GorillaSlicerSimpleManager.UnregisterSliceable(MonkeAgent.instance, GorillaSlicerSimpleManager.UpdateStep.Update);
                }

                PhotonNetwork.MaxResendsBeforeDisconnect = int.MaxValue;
                PhotonNetwork.QuickResends = int.MaxValue;

                if (experimental)
                {
                    MonkeAgent.instance.logErrorCount = 0;
                    MonkeAgent.instance.lastCheck = float.MaxValue;
                    MonkeAgent.instance.reportCheckCooldown = float.MaxValue;
                    MonkeAgent.instance.testAssault = false;
                    MonkeAgent.instance._sendReport = false;
                    MonkeAgent.instance._suspiciousPlayerId = "";
                    MonkeAgent.instance._suspiciousPlayerName = "";
                    MonkeAgent.instance._suspiciousReason = "";

                    if (PhotonNetwork.NetworkingClient != null && PhotonNetwork.NetworkingClient.LoadBalancingPeer != null)
                    {
                        PhotonNetwork.NetworkingClient.LoadBalancingPeer.DisconnectTimeout = 60000;
                        PhotonNetwork.NetworkingClient.LoadBalancingPeer.SentCountAllowance = int.MaxValue;
                    }
                }
            }
            catch { /* if it goes here its a skill issue */ }
        }

        public static void lbaction(GorillaPlayerLineButton.ButtonType type, NetPlayer player = null, bool? state = null)
        {
            if (type == GorillaPlayerLineButton.ButtonType.Mute)
            {
                Action<NetPlayer> mute = p =>
                {
                    if (p == null || string.IsNullOrEmpty(p.UserId)) return;

                    bool shouldMute = state ?? (PlayerPrefs.GetInt(p.UserId, 0) == 0);
                    int muteValue = shouldMute ? 1 : 0;
                    PlayerPrefs.SetInt(p.UserId, muteValue);
                    PlayerPrefs.Save();

                    if (VRRigCache.Instance != null && VRRigCache.Instance.TryGetVrrig(p, out RigContainer rigContainer) && rigContainer != null)
                    {
                        rigContainer.hasManualMute = true;
                        rigContainer.SetMuted(RigContainer.MuteReason.Manual, shouldMute);
                        if (rigContainer.Rig != null)
                        {
                            rigContainer.Rig.muted = shouldMute;
                            if (rigContainer.Rig.voiceAudio != null)
                                rigContainer.Rig.voiceAudio.mute = shouldMute;
                        }
                        rigContainer.RefreshVoiceChat();
                    }

                    try { GorillaScoreboardTotalUpdater.ReportMute(p, muteValue); } catch { }

                    if (GorillaScoreboardTotalUpdater.allScoreboardLines != null)
                    {
                        foreach (var line in GorillaScoreboardTotalUpdater.allScoreboardLines)
                        {
                            if (line != null && (line.linePlayer?.UserId == p.UserId || line.playerActorNumber == p.ActorNumber))
                            {
                                line.PressButton(shouldMute, GorillaPlayerLineButton.ButtonType.Mute);
                                if (line.muteButton != null)
                                {
                                    line.muteButton.isOn = shouldMute;
                                    line.muteButton.UpdateColor();
                                }
                            }
                        }
                    }
                };

                if (player != null)
                    mute(player);
                else
                    foreach (NetPlayer otherPlayer in NetworkSystem.Instance.PlayerListOthers)
                        mute(otherPlayer);
            }
            else
            {
                Action<NetPlayer> report = p =>
                {
                    if (p == null || string.IsNullOrEmpty(p.UserId)) return;

                    string targetNickName = p.NickName ?? p.DefaultName ?? "GORILLA";
                    GorillaPlayerScoreboardLine.ReportPlayer(p.UserId, GorillaPlayerLineButton.ButtonType.Cheating, targetNickName);

                    if (GorillaScoreboardTotalUpdater.hasInstance && p.ActorNumber != -1)
                    {
                        var updater = GorillaScoreboardTotalUpdater.instance;
                        if (updater.reportDict.TryGetValue(p.ActorNumber, out var existingReports))
                        {
                            existingReports.cheating = true;
                            existingReports.pressedReport = true;
                            updater.reportDict[p.ActorNumber] = existingReports;
                        }
                        else
                        {
                            updater.reportDict[p.ActorNumber] = new GorillaScoreboardTotalUpdater.PlayerReports
                            {
                                cheating = true,
                                pressedReport = true
                            };
                        }
                    }

                    if (GorillaScoreboardTotalUpdater.allScoreboardLines != null)
                    {
                        foreach (var line in GorillaScoreboardTotalUpdater.allScoreboardLines)
                        {
                            if (line != null && line.linePlayer == p && line.reportButton != null)
                            {
                                line.reportButton.isOn = true;
                                line.reportButton.UpdateColor();
                            }
                        }
                    }
                };

                if (player != null)
                    report(player);
                else
                    foreach (NetPlayer otherPlayer in NetworkSystem.Instance.PlayerListOthers)
                        report(otherPlayer);
            }
        }

        public static void MuteGun()
        {
            GunLib.StartGun(() =>
            {
                if (GunLib.LockedPlayer != null && !GunLib.LockedPlayer.isOfflineVRRig && Time.time > actionDelay)
                {
                    NetPlayer player = GunLib.LockedPlayer.Creator;
                    if (player == null)
                    {
                        var photonPlayer = RigManager.GetPlayerFromVRRig(GunLib.LockedPlayer);
                        if (photonPlayer != null)
                            player = NetworkSystem.Instance.GetPlayer(photonPlayer.ActorNumber);
                    }

                    if (player != null)
                    {
                        lbaction(GorillaPlayerLineButton.ButtonType.Mute, player, state: true);
                        actionDelay = Time.time + 0.5f;
                    }
                }
            }, true);
        }

        public static void UnmuteGun()
        {
            GunLib.StartGun(() =>
            {
                if (GunLib.LockedPlayer != null && !GunLib.LockedPlayer.isOfflineVRRig && Time.time > actionDelay)
                {
                    NetPlayer player = GunLib.LockedPlayer.Creator;
                    if (player == null)
                    {
                        var photonPlayer = RigManager.GetPlayerFromVRRig(GunLib.LockedPlayer);
                        if (photonPlayer != null)
                            player = NetworkSystem.Instance.GetPlayer(photonPlayer.ActorNumber);
                    }

                    if (player != null)
                    {
                        lbaction(GorillaPlayerLineButton.ButtonType.Mute, player, state: false);
                        actionDelay = Time.time + 0.5f;
                    }
                }
            }, true);
        }

        public static void MuteAll() => lbaction(GorillaPlayerLineButton.ButtonType.Mute, state: true);
        public static void UnmuteAll() => lbaction(GorillaPlayerLineButton.ButtonType.Mute, state: false);

        public static VRRig priorityVoiceTarget;

        public static void PriorityVoiceGun()
        {
            GunLib.StartGun(() =>
            {
                if (GunLib.LockedPlayer != null && !GunLib.LockedPlayer.isOfflineVRRig && Time.time > actionDelay)
                {
                    if (priorityVoiceTarget == GunLib.LockedPlayer)
                    {
                        ResetRigVoice(GunLib.LockedPlayer);
                        priorityVoiceTarget = null;
                        NotificationLib.SendNotification(NotificationLib.NotificationType.Info, "Priority Voice: Cleared");
                    }
                    else
                    {
                        if (priorityVoiceTarget != null)
                            ResetRigVoice(priorityVoiceTarget);

                        priorityVoiceTarget = GunLib.LockedPlayer;
                        string name = priorityVoiceTarget.Creator?.NickName ?? priorityVoiceTarget.playerNameVisible;
                        NotificationLib.SendNotification(NotificationLib.NotificationType.Enabled, "Priority Voice: " + name);
                    }
                    actionDelay = Time.time + 0.5f;
                }
            }, true);

            if (priorityVoiceTarget != null && (!priorityVoiceTarget.gameObject.activeInHierarchy || priorityVoiceTarget.isOfflineVRRig))
                priorityVoiceTarget = null;

            if (priorityVoiceTarget != null)
            {
                ApplyPriorityVoice(priorityVoiceTarget);

                VRRig[] allRigs = Object.FindObjectsByType<VRRig>(FindObjectsSortMode.None);
                if (allRigs != null)
                {
                    foreach (var rig in allRigs)
                    {
                        if (rig == null || rig.isOfflineVRRig || rig == priorityVoiceTarget) continue;
                        AudioSource src = GetRigAudioSource(rig);
                        if (src != null)
                            src.volume = 0.2f;
                    }
                }
            }
        }

        public static void PriorityVoiceDisable()
        {
            VRRig[] allRigs = Object.FindObjectsByType<VRRig>(FindObjectsSortMode.None);
            if (allRigs != null)
            {
                foreach (var rig in allRigs)
                {
                    if (rig == null || rig.isOfflineVRRig) continue;
                    ResetRigVoice(rig);
                }
            }
            priorityVoiceTarget = null;
        }

        private static AudioSource GetRigAudioSource(VRRig rig)
        {
            if (rig == null) return null;
            if (rig.voiceAudio != null) return rig.voiceAudio;
            var speaker = rig.GetComponentInChildren<Speaker>() ?? rig.GetComponentInChildren<GTSpeaker>();
            if (speaker != null)
            {
                var src = speaker.GetComponent<AudioSource>();
                if (src != null) return src;
            }
            return rig.GetComponentInChildren<AudioSource>();
        }

        private static void ApplyPriorityVoice(VRRig rig)
        {
            AudioSource src = GetRigAudioSource(rig);
            if (src != null)
            {
                src.volume = 1f;
                src.spatialBlend = 0f;
                src.minDistance = 500f;
                src.maxDistance = 1000f;
            }
        }

        private static void ResetRigVoice(VRRig rig)
        {
            AudioSource src = GetRigAudioSource(rig);
            if (src != null)
            {
                src.volume = 1f;
                src.spatialBlend = 1f;
                src.minDistance = 1f;
                src.maxDistance = 30f;
            }
        }

        private static GTRecorder GetActiveGTRecorder()
        {
            if (NetworkSystem.Instance?.LocalRecorder is GTRecorder netGt) return netGt;
            if (NetworkSystem.Instance?.LocalRecorder != null)
            {
                GTRecorder comp = NetworkSystem.Instance.LocalRecorder.GetComponent<GTRecorder>();
                if (comp != null) return comp;
            }
            if (NetworkSystem.Instance?.VoiceConnection?.PrimaryRecorder is GTRecorder voiceGt) return voiceGt;
            if (NetworkSystem.Instance?.VoiceConnection?.PrimaryRecorder != null)
            {
                GTRecorder comp = NetworkSystem.Instance.VoiceConnection.PrimaryRecorder.GetComponent<GTRecorder>();
                if (comp != null) return comp;
            }
            if (GorillaTagger.Instance?.myRecorder is GTRecorder myGt) return myGt;
            if (GorillaTagger.Instance?.myRecorder != null)
            {
                GTRecorder comp = GorillaTagger.Instance.myRecorder.GetComponent<GTRecorder>();
                if (comp != null) return comp;
            }
            return Object.FindFirstObjectByType<GTRecorder>();
        }

        public static void LoudMicrophone(float volumeMultiplier = 15f)
        {
            if (!NetworkSystem.Instance.InRoom) return;
            GTRecorder recorder = GetActiveGTRecorder();
            if (recorder == null) return;

            recorder.AllowVolumeAdjustment = true;
            recorder.VolumeAdjustment = volumeMultiplier;
            recorder.VoiceDetection = false;
            recorder.TransmitEnabled = true;
        }

        public static void ResetMicrophoneVolume()
        {
            if (!NetworkSystem.Instance.InRoom) return;
            GTRecorder recorder = GetActiveGTRecorder();
            if (recorder == null) return;

            recorder.AllowVolumeAdjustment = false;
            recorder.VolumeAdjustment = 1f;
            recorder.VoiceDetection = true;
            recorder.VoiceDetectionThreshold = 0.07f;
        }

        public static void MuteMicrophone()
        {
            if (!NetworkSystem.Instance.InRoom) return;
            GTRecorder recorder = GetActiveGTRecorder();
            if (recorder == null) return;

            recorder.AllowVolumeAdjustment = true;
            recorder.VolumeAdjustment = 0f;
            recorder.TransmitEnabled = false;
            recorder.VoiceDetectionThreshold = 1f;

            if (GorillaTagger.Instance?.offlineVRRig != null)
                GorillaTagger.Instance.offlineVRRig.shouldSendSpeakingLoudness = false;
        }

        public static void UnmuteMicrophone()
        {
            if (!NetworkSystem.Instance.InRoom) return;
            GTRecorder recorder = GetActiveGTRecorder();
            if (recorder == null) return;

            recorder.AllowVolumeAdjustment = false;
            recorder.VolumeAdjustment = 1f;
            recorder.TransmitEnabled = true;
            recorder.VoiceDetectionThreshold = 0.07f;

            if (GorillaTagger.Instance?.offlineVRRig != null)
                GorillaTagger.Instance.offlineVRRig.shouldSendSpeakingLoudness = true;
        }

        public static bool microphoneEchoForOthers;
        public static float echoDelaySeconds = 0.25f;
        public static float echoDecayFactor = 0.55f;

        public static void MicrophoneEcho(bool enableEcho = true)
        {
            if (!NetworkSystem.Instance.InRoom) return;
            microphoneEchoForOthers = enableEcho;
        }

        public static void HearSelf(bool enable = true)
        {
            if (!NetworkSystem.Instance.InRoom) return;
            GTRecorder recorder = GetActiveGTRecorder();
            if (recorder == null) return;

            recorder.DebugEchoMode = enable;
            if (enable && !recorder.TransmitEnabled) recorder.TransmitEnabled = true;
        }

        public static void SetMicrophonePitch(float pitch)
        {
            if (!NetworkSystem.Instance.InRoom) return;
            GTRecorder recorder = GetActiveGTRecorder();
            if (recorder == null) return;

            recorder.AllowPitchAdjustment = true;
            recorder.PitchAdjustment = pitch;
        }

        public static void ResetMicrophonePitch()
        {
            if (!NetworkSystem.Instance.InRoom) return;
            GTRecorder recorder = GetActiveGTRecorder();
            if (recorder == null) return;

            recorder.AllowPitchAdjustment = false;
            recorder.PitchAdjustment = 1f;
        }

        private static bool savedNoiseVoiceDetection;
        private static float savedNoiseVoiceDetectionThreshold = 0.07f;
        private static int savedNoiseVoiceDetectionDelayMs = 500;
        private static int savedNoiseBitrate = 30000;
        private static bool savedNoiseAllowVolume;
        private static float savedNoiseVolume = 1f;

        public static void NoiseCancellation()
        {
            if (!NetworkSystem.Instance.InRoom) return;
            GTRecorder recorder = GetActiveGTRecorder();
            if (recorder == null) return;

            savedNoiseVoiceDetection = recorder.VoiceDetection;
            savedNoiseVoiceDetectionThreshold = recorder.VoiceDetectionThreshold;
            savedNoiseVoiceDetectionDelayMs = recorder.VoiceDetectionDelayMs;
            savedNoiseBitrate = recorder.Bitrate;
            savedNoiseAllowVolume = recorder.AllowVolumeAdjustment;
            savedNoiseVolume = recorder.VolumeAdjustment;

            recorder.VoiceDetection = true;
            recorder.VoiceDetectionThreshold = 0.035f;
            recorder.VoiceDetectionDelayMs = 150;
            recorder.Bitrate = 64000;
            recorder.TransmitEnabled = true;
            recorder.AllowVolumeAdjustment = false;
            recorder.VolumeAdjustment = 1f;
            recorder.AllowPitchAdjustment = false;
            recorder.PitchAdjustment = 1f;
        }

        public static void DisableNoiseCancellation()
        {
            if (!NetworkSystem.Instance.InRoom) return;
            GTRecorder recorder = GetActiveGTRecorder();
            if (recorder == null) return;

            recorder.VoiceDetection = savedNoiseVoiceDetection;
            recorder.VoiceDetectionThreshold = savedNoiseVoiceDetectionThreshold;
            recorder.VoiceDetectionDelayMs = savedNoiseVoiceDetectionDelayMs;
            recorder.Bitrate = savedNoiseBitrate;
            recorder.AllowVolumeAdjustment = savedNoiseAllowVolume;
            recorder.VolumeAdjustment = savedNoiseVolume;
        }

        public static void FixMicrophone()
        {
            if (!NetworkSystem.Instance.InRoom) return;
            microphoneEchoForOthers = false;

            GTRecorder recorder = GetActiveGTRecorder();
            if (recorder != null)
            {
                recorder.SourceType = Recorder.InputSourceType.Microphone;
                recorder.AudioClip = null;
                recorder.DebugEchoMode = false;
                recorder.TransmitEnabled = true;
                recorder.VoiceDetection = true;
                recorder.VoiceDetectionThreshold = 0.07f;
                recorder.VoiceDetectionDelayMs = 500;
                recorder.RecordOnlyWhenJoined = true;
                recorder.StopRecordingWhenPaused = false;
                recorder.AllowVolumeAdjustment = false;
                recorder.VolumeAdjustment = 1f;
                recorder.AllowPitchAdjustment = false;
                recorder.PitchAdjustment = 1f;
                recorder.RestartRecording(true);
            }

            if (GorillaTagger.Instance?.offlineVRRig != null)
            {
                GorillaTagger.Instance.offlineVRRig.remoteUseReplacementVoice = false;
                GorillaTagger.Instance.offlineVRRig.localUseReplacementVoice = false;
                GorillaTagger.Instance.offlineVRRig.shouldSendSpeakingLoudness = true;
            }

            if (GorillaComputer.instance != null)
            {
                GorillaComputer.instance.voiceChatOn = "TRUE";
            }
        }

        public static void ReportGun()
        {
            GunLib.StartGun(() =>
            {
                if (GunLib.LockedPlayer != null && !GunLib.LockedPlayer.isOfflineVRRig && Time.time > actionDelay)
                {
                    NetPlayer player = GunLib.LockedPlayer.Creator;
                    if (player == null)
                    {
                        var photonPlayer = RigManager.GetPlayerFromVRRig(GunLib.LockedPlayer);
                        if (photonPlayer != null)
                            player = NetworkSystem.Instance.GetPlayer(photonPlayer.ActorNumber);
                    }

                    if (player != null)
                    {
                        lbaction(GorillaPlayerLineButton.ButtonType.Cheating, player);
                        actionDelay = Time.time + 0.3f;
                    }
                }
            }, true);
        }

        public static void ReportAll() => lbaction(GorillaPlayerLineButton.ButtonType.Cheating);

        public static void CopyPlayerIdentity()
        {
            GunLib.StartGun(() =>
            {
                if (GunLib.LockedPlayer != null && !GunLib.LockedPlayer.isOfflineVRRig)
                {
                    string targetName = GunLib.LockedPlayer.Creator != null ? GunLib.LockedPlayer.Creator.NickName : GunLib.LockedPlayer.playerNameVisible;
                    if (!string.IsNullOrEmpty(targetName))
                    {
                        NetworkSystem.Instance.SetMyNickName(targetName);
                        GorillaComputer.instance.currentName = targetName;
                        GorillaComputer.instance.savedName = targetName;
                        GorillaTagger.Instance.offlineVRRig.SetNameTagText(targetName);
                        PhotonNetwork.LocalPlayer.NickName = targetName;
                        PlayerPrefs.SetString("playerName", targetName);
                    }

                    Color targetColor = GunLib.LockedPlayer.playerColor;
                    GorillaTagger.Instance.myVRRig.SendRPC("RPC_InitializeNoobMaterial", RpcTarget.All, targetColor.r, targetColor.g, targetColor.b);
                    PlayerPrefs.SetFloat("redValue", targetColor.r);
                    PlayerPrefs.SetFloat("greenValue", targetColor.g);
                    PlayerPrefs.SetFloat("blueValue", targetColor.b);
                    PlayerPrefs.Save();
                    VRRig.LocalRig.SetColor(targetColor);
                    GorillaTagger.Instance.offlineVRRig.SetColor(targetColor);
                }
            }, true);
        }

        public static async void LobbyHop()
        {
            if (PhotonNetworkController.Instance.currentJoinTrigger?.networkZone != null)
                lastmap = PhotonNetworkController.Instance.currentJoinTrigger.networkZone;

            await NetworkSystem.Instance.ReturnToSinglePlayer();
            PhotonNetworkController.Instance.AttemptToJoinPublicRoom(GorillaComputer.instance.GetJoinTriggerForZone(lastmap ?? "forest"), GorillaNetworking.JoinType.Solo);
        }

        public static async void RejoinRoom()
        {
            if (!NetworkSystem.Instance.InRoom) return;

            string roomName = NetworkSystem.Instance.RoomName;
            RoomConfig config = NetworkSystem.Instance.CurrentRoom;

            if (string.IsNullOrEmpty(roomName)) return;

            await NetworkSystem.Instance.ReturnToSinglePlayer();

            if (config != null)
                await NetworkSystem.Instance.ConnectToRoom(roomName, config);
            else
                PhotonNetworkController.Instance.AttemptToJoinSpecificRoom(roomName, GorillaNetworking.JoinType.Solo);
        }

        public static async void CreateRoom()
        {
            if (PhotonNetworkController.Instance.currentJoinTrigger?.networkZone != null)
                lastmap = PhotonNetworkController.Instance.currentJoinTrigger.networkZone;

            if (NetworkSystem.Instance.InRoom)
                await NetworkSystem.Instance.ReturnToSinglePlayer();

            string roomName = NetworkSystem.GetRandomRoomName();
            RoomConfig config = RoomConfig.AnyPublicConfig();

            GorillaNetworkJoinTrigger joinTrigger = PhotonNetworkController.Instance.currentJoinTrigger ?? GorillaComputer.instance.GetJoinTriggerForZone(lastmap ?? "forest");
            if (joinTrigger != null)
            {
                config.MaxPlayers = joinTrigger.GetRoomSize(false);
                PhotonNetworkController.Instance.currentJoinTrigger = joinTrigger;
                ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
                {
                    { "gameMode", joinTrigger.GetFullDesiredGameModeString() },
                    { "platform", PhotonNetworkController.Instance.platformTag },
                    { "queueName", GorillaComputer.instance.currentQueue }
                };
                GorillaNetworking.ScheduledEvents.ScheduledEventMatchmaking.ApplyScheduledEventStateToHashes(props, out var searchFilter);
                config.CustomProps = props;
                config.SearchFilter = searchFilter;
            }

            await NetworkSystem.Instance.ConnectToRoom(roomName, config);
        }
    }
}
