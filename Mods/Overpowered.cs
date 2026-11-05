using ExitGames.Client.Photon;
using GorillaNetworking;
using GorillaTagScripts;
using HarmonyLib;
using Photon.Pun;
using Photon.Realtime;
using Photon.Voice.PUN;
using Photon.Voice.Unity;
using POpusCodec.Enums;
using ShibaGTGenesisReborn.Classes;
using ShibaGTGenesisReborn.Libs;
using ShibaGTGenesisReborn.Menu;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using UnityEngine;

namespace ShibaGTGenesisReborn.Mods
{
    public partial class mods
    {
        public static void DestroyGun()
        {
            GunLib.StartGun(() =>
            {
                if (GunLib.LockedPlayer != null)
                {
                    RPCProt();
                    PhotonNetwork.OpRemoveCompleteCacheOfPlayer(RigManager.GetPlayerFromVRRig(GunLib.LockedPlayer).ActorNumber);
                }
            }, true);
        }

        public static void DestroyAll()
        {
            RPCProt();
            foreach (Player player in PhotonNetwork.PlayerListOthers)
            {
                PhotonNetwork.OpRemoveCompleteCacheOfPlayer(player.ActorNumber);
            }
        }

        private static float nextTargetSpam;

        public static void TargetSpam()
        {
            if (Time.time < nextTargetSpam) return;
            nextTargetSpam = Time.time + 1f;

            foreach (HitTargetNetworkState target in GameObject.FindObjectsByType<HitTargetNetworkState>(FindObjectsSortMode.None))
            {
                if (target == null) continue;
                Vector3 pos = target.transform.position;
                target.TargetHit(pos, pos);
            }
        }

        public static void BecomeGuardian()
        {
            int changed = 0;
            NetPlayer localPlayer = NetworkSystem.Instance.LocalPlayer;
            foreach (GorillaGuardianZoneManager zone in GorillaGuardianZoneManager.zoneManagers)
            {
                if (zone == null || !zone.IsZoneValid()) continue;
                zone.SetGuardian(localPlayer);
                changed++;
            }
        }

        public static void EjectAllGuardians()
        {
            int changed = 0;
            foreach (GorillaGuardianZoneManager zone in GorillaGuardianZoneManager.zoneManagers)
            {
                if (zone == null || zone.CurrentGuardian == null) continue;
                zone.SetGuardian(null);
                changed++;
            }
        }

        private static bool IsLocalGuardian() =>
            GorillaGameManager.instance is GorillaGuardianManager manager && manager.IsPlayerGuardian(NetworkSystem.Instance.LocalPlayer);

        private static readonly Dictionary<VRRig, float> guardianLaunchTimes = new Dictionary<VRRig, float>();

        private static void GuardianLaunch(VRRig rig, Vector3 velocity)
        {
            if (Time.time < guardianLaunchTimes.GetValueOrDefault(rig)) return;
            guardianLaunchTimes[rig] = Time.time + 0.5f;
            velocity = Vector3.ClampMagnitude(velocity, 19.9f);
            GorillaGameModes.GameMode.ActiveNetworkHandler.SendRPC("GuardianLaunchPlayer", rig.Creator, velocity);
            rig.ApplyLocalTrajectoryOverride(velocity);
        }

        public static void GuardianFlingGun()
        {
            GunLib.StartGun(() =>
            {
                if (!IsLocalGuardian()) return;
                remoteHeld = true;
                VRRig.LocalRig.transform.position = GunLib.LockedPlayer.transform.position + Vector3.down;
                GuardianLaunch(GunLib.LockedPlayer, Vector3.up * 20f);
            }, true);
        }

        private static int guardianGrabIndex;
        private static float guardianGrabTime;
        private static VRRig guardianGrabTarget;

        public static void GuardianGrabAll()
        {
            if (!IsLocalGuardian()) return;
            if (Time.time > guardianGrabTime || guardianGrabTarget == null || !VRRigCache.ActiveRigs.Contains(guardianGrabTarget))
            {
                VRRig[] others = VRRigCache.ActiveRigs.Where(rig => rig != null && !rig.isLocal && rig != VRRig.LocalRig).ToArray();
                if (others.Length == 0) return;
                guardianGrabTarget = others[++guardianGrabIndex % others.Length];
                guardianGrabTime = Time.time + 0.4f;
            }

            VRRig target = guardianGrabTarget;
            remoteHeld = true;
            VRRig.LocalRig.transform.position = target.transform.position + Vector3.down;
            if (guardianGrabTime - Time.time < 0.2f)
                GuardianLaunch(target, (GorillaTagger.Instance.bodyCollider.transform.position - target.transform.position).normalized * 20f + Vector3.up * 3f);
        }

        public static void GuardianAura()
        {
            if (!IsLocalGuardian()) return;
            Vector3 center = GorillaTagger.Instance.bodyCollider.transform.position;
            foreach (VRRig rig in VRRigCache.ActiveRigs)
            {
                if (rig == null || rig.isLocal || rig == VRRig.LocalRig || Vector3.Distance(rig.transform.position, center) > 4f) continue;
                GuardianLaunch(rig, ((rig.transform.position - center).normalized + Vector3.up) * 20f);
            }
        }

        public static void GhostReactorGodMode()
        {
            GhostReactor reactor = GhostReactor.instance;
            GRPlayer player = GRPlayer.GetLocal();
            if (reactor == null || reactor.grManager == null || player == null) return;

            if (player.State == GRPlayer.GRPlayerState.Ghost)
                player.OnPlayerRevive(reactor.grManager);

            int allShieldEffects = (int)(GRPlayer.GRPlayerShieldFlags.Light | GRPlayer.GRPlayerShieldFlags.Stealth | GRPlayer.GRPlayerShieldFlags.Heal);

            if (player.Hp < player.MaxHp || player.ShieldHp < player.MaxShieldHp || !player.InStealthMode)
                player.TryActivateShield(player.MaxShieldHp, allShieldEffects);
        }

        public static void DisableGhostReactorGodMode()
        {
            GRPlayer player = GRPlayer.GetLocal();
            if (player != null)
            {
                player.ClearStealthMode();
                player.SetShieldHp(0);
                player.shieldFlags = 0;
                player.RefreshPlayerVisuals();
            }
        }

        public static void KillAllGhostReactorEnemies()
        {
            GhostReactorManager manager = GhostReactor.instance?.grManager;
            if (manager == null) return;

            manager.InstantDeathForCurrentEnemies();
        }

        private static float grNukeCooldown;

        public static void GRNuker()
        {
            if (Time.time < grNukeCooldown || !NetworkSystem.Instance.InRoom) return;
            grNukeCooldown = Time.time + 0.25f;

            GhostReactor reactor = GhostReactor.instance ?? GameObject.FindAnyObjectByType<GhostReactor>();
            if (reactor == null) return;

            GhostReactorManager manager = reactor.grManager;
            manager.InstantDeathForCurrentEnemies();

            reactor.ClearAllRespawns();

            foreach (GREnemyBossMoon moon in GameObject.FindObjectsByType<GREnemyBossMoon>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                moon.KillAllEyes();
                moon.KillAllSummoned(false, true);
                moon.SetHP(0);
                moon.GotoDyingIdle();
            }

            foreach (GREnemyBossMoonEye eye in GameObject.FindObjectsByType<GREnemyBossMoonEye>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                eye.InstantKill();

            foreach (GREnemyPhantom phantom in GameObject.FindObjectsByType<GREnemyPhantom>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                phantom.SetHP(0);
                phantom.SetBehavior(GREnemyPhantom.Behavior.Return, true);
                if (phantom.entity != null)
                    phantom.entity.RequestState(phantom.entity.id, 0L);
            }

            foreach (GRBreakable breakable in GameObject.FindObjectsByType<GRBreakable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (breakable.BrokenLocal) continue;
                breakable.BreakLocal();
                if (breakable.gameEntity != null)
                    breakable.gameEntity.RequestState(breakable.gameEntity.id, 1L);
            }

            foreach (GRBarrierOverloadable barrier in GameObject.FindObjectsByType<GRBarrierOverloadable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                barrier.SetState(GRBarrierOverloadable.State.Destroyed);
                if (barrier.gameEntity != null)
                    barrier.gameEntity.RequestState(barrier.gameEntity.id, 1L);
            }

            foreach (GRBarrierSpectral spectral in GameObject.FindObjectsByType<GRBarrierSpectral>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                spectral.ChangeHealth(0);
                if (spectral.entity != null)
                    spectral.entity.RequestState(spectral.entity.id, 0L);
            }

            foreach (GRMetalEnergyGate gate in GameObject.FindObjectsByType<GRMetalEnergyGate>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                gate.SetState(GRMetalEnergyGate.State.Open);
                if (gate.gameEntity != null)
                    gate.gameEntity.RequestState(gate.gameEntity.id, 1L);
            }

            foreach (GRHazardTower tower in GameObject.FindObjectsByType<GRHazardTower>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                tower.nextFireTime = double.MaxValue;
                tower.enabled = false;
            }

            foreach (GRSentientCore core in GameObject.FindObjectsByType<GRSentientCore>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                core.Sleep();

            SetAllDoors(true);

            GRPlayer localPlayer = GRPlayer.GetLocal();
            if (localPlayer != null)
            {
                if (localPlayer.State == GRPlayer.GRPlayerState.Ghost && manager != null)
                    localPlayer.OnPlayerRevive(manager);

                int shieldFlags = (int)(GRPlayer.GRPlayerShieldFlags.Light | GRPlayer.GRPlayerShieldFlags.Stealth | GRPlayer.GRPlayerShieldFlags.Heal);
                if (localPlayer.Hp < localPlayer.MaxHp || localPlayer.ShieldHp < localPlayer.MaxShieldHp || !localPlayer.InStealthMode)
                    localPlayer.TryActivateShield(localPlayer.MaxShieldHp, shieldFlags);
            }

            if (manager != null)
            {
                foreach (GRPlayer player in GameObject.FindObjectsByType<GRPlayer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                {
                    if (player != null && player.State == GRPlayer.GRPlayerState.Ghost)
                        manager.RequestPlayerStateChange(player, GRPlayer.GRPlayerState.Alive);
                }
            }
        }

        public static void ForceStartCurrentGame()
        {
            GorillaGameManager manager = GorillaGameManager.instance;
            if (manager == null) return;

            manager.StartPlaying();
        }

        public static void ResetCurrentGame()
        {
            GorillaGameManager manager = GorillaGameManager.instance;
            if (manager == null) return;

            manager.ResetGame();
        }

        public static void FreezeAllPlayers()
        {
            if (!(GorillaGameManager.instance is GorillaFreezeTagManager manager)) return;

            foreach (NetPlayer player in NetworkSystem.Instance.AllNetPlayers)
            {
                if (player == NetworkSystem.Instance.LocalPlayer || manager.currentInfected.Contains(player)) continue;

                if (!manager.currentFrozen.ContainsKey(player))
                    RoomSystem.SendStatusEffectToPlayer(RoomSystem.StatusEffects.FrozenTime, player);

                manager.currentFrozen[player] = Time.time;
            }
        }

        public static void SetAllDoors(bool open)
        {
            foreach (GRDoorWrapper door in GameObject.FindObjectsByType<GRDoorWrapper>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (door.grDoor != null && door.grDoor.animation != null)
                    door.ToggleDoor(open);
            }

            foreach (GRShuttle shuttle in GameObject.FindObjectsByType<GRShuttle>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (open) shuttle.OpenDoorLocal();
                else shuttle.CloseDoorLocal();
            }

            foreach (GTDoor door in GameObject.FindObjectsByType<GTDoor>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                door.currentState = open ? GTDoor.DoorState.Closed : GTDoor.DoorState.Open;
                if (open) door.OpenDoor();
                else door.CloseDoor();
            }

            foreach (GRElevator elevator in GameObject.FindObjectsByType<GRElevator>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (elevator.upperDoor == null || elevator.lowerDoor == null || elevator.openTargetTop == null || elevator.closedTargetTop == null) continue;
                elevator.UpdateLocalState(open ? GRElevator.ElevatorState.DoorBeginOpening : GRElevator.ElevatorState.DoorBeginClosing);
            }
        }

        public static void EarRapeGun()
        {
            GunLib.StartGun(() =>
            {
                if (GunLib.LockedPlayer != null)
                    BlastEarRape(GunLib.LockedPlayer);
            }, true);
        }

        public static void EarRapeAll()
        {
            BlastEarRape(null);
        }

        private static void BlastEarRape(VRRig target = null)
        {
            if (!NetworkSystem.Instance.InRoom) return;
            RPCProt();

            NetworkingLibrary.SendRigPosition(GorillaTagger.Instance.headCollider.transform.position);
            
            int[] loudIds = { 66, 67, 8, 12, 24, 32 };
            for (int i = 0; i < 6; i++)
            {
                int sound = loudIds[i % loudIds.Length];
                GorillaTagger.Instance.myVRRig.SendRPC("RPC_PlayHandTap", RpcTarget.Others, sound, (i % 2 == 0), 0.1f);
            }
        }

        private static bool antiBanJoinPending;
        private static float antiBanCooldown;

        [HarmonyPatch(typeof(GorillaNetworkJoinTrigger), "GetFullDesiredGameModeString")]
        public class GamemodePatch
        {
            public static void Postfix(ref string __result)
            {
                if (antiBanJoinPending)
                {
                    __result = "private;DEFAULT;MODDED_Casual;;";
                    antiBanJoinPending = false;
                }
            }
        }

        public static void AntiBan()
        {
            if (!Main.GetIndex("Anti Ban [Private]").enabled) return;
            if (Time.time < antiBanCooldown) return;
            antiBanCooldown = Time.time + 3f;

            GorillaComputer computer = GorillaComputer.instance;
            PhotonNetworkController controller = PhotonNetworkController.Instance;
            if (computer == null || controller == null) return;

            string[] allowedMaps = computer.allowedMapsToJoin;
            if (allowedMaps.Length == 0) return;

            int mapIndex = Mathf.Clamp(computer.groupMapJoinIndex, 0, allowedMaps.Length - 1);
            GorillaNetworkJoinTrigger trigger = computer.GetJoinTriggerForZone(allowedMaps[mapIndex]);
            GorillaFriendCollider collider = computer.friendJoinCollider;

            int others = collider.playerIDsCurrentlyTouching.Count;
            if (collider.playerIDsCurrentlyTouching.Contains(NetworkSystem.Instance.LocalPlayer.UserId)) others--;
            if (others < 1) return;

            if (FriendshipGroupDetection.Instance.IsInParty)
            {
                if (trigger.CanPartyJoin())
                {
                    antiBanJoinPending = true;
                    controller.AttemptToJoinPublicRoom(trigger, GorillaNetworking.JoinType.ForceJoinWithParty);
                }
                return;
            }

            if (!NetworkSystem.Instance.InRoom || !NetworkSystem.Instance.SessionIsPrivate)
            {
                NotificationLib.SendNotification(NotificationLib.NotificationType.Error, "You arent in a private room");
                return;
            }

            controller.SetFriendIDList(collider.playerIDsCurrentlyTouching);
            controller.shuffler = Random.Range(0, 99).ToString().PadLeft(2, '0') + Random.Range(0, 99999999).ToString().PadLeft(8, '0');
            controller.keyStr = Random.Range(0, 99999999).ToString().PadLeft(8, '0');

            object[] followData = { controller.shuffler, controller.keyStr };
            foreach (NetPlayer player in RoomSystem.PlayersInRoom)
            {
                if (player != NetworkSystem.Instance.LocalPlayer && collider.playerIDsCurrentlyTouching.Contains(player.UserId))
                    RoomSystem.SendEvent(4, followData, new NetEventOptions { TargetActors = new[] { player.ActorNumber } }, false);
            }

            PhotonNetwork.SendAllOutgoingCommands();
            antiBanJoinPending = true;
            controller.AttemptToJoinPublicRoom(trigger, GorillaNetworking.JoinType.JoinWithNearby, null, false);
        }
    }
}
