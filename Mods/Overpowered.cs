using ExitGames.Client.Photon;
using GorillaNetworking;
using GorillaTag;
using GorillaTag.CosmeticSystem;
using GorillaTagScripts;
using Liv.Lck.Tablet;
using Photon.Pun;
using Photon.Realtime;
using ShibaGTGenesisReborn.Classes;
using ShibaGTGenesisReborn.Libs;
using ShibaGTGenesisReborn.Menu;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

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

        public static void TargetSpam()
        {
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
                player.ClearStealthMode();
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
            int changed = 0;
            foreach (GRDoorWrapper door in GameObject.FindObjectsByType<GRDoorWrapper>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                try
                {
                    door.ToggleDoor(open);
                    changed++;
                }
                catch { }
            }

            GRElevator.ElevatorState elevatorState = open
                ? GRElevator.ElevatorState.DoorOpen
                : GRElevator.ElevatorState.DoorClosed;

            foreach (GRElevator elevator in GameObject.FindObjectsByType<GRElevator>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (elevator == null) continue;

                try
                {
                    elevator.UpdateLocalState(elevatorState);
                    changed++;
                }
                catch { }
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
            RPCProt(true);

            NetworkingLibrary.SendRigPosition(GorillaTagger.Instance.myVRRig.GetView, GorillaTagger.Instance.headCollider.transform.position);
            
            int[] loudIds = { 66, 67, 8, 12, 24, 32 };
            for (int i = 0; i < 6; i++)
            {
                int sound = loudIds[i % loudIds.Length];
                GorillaTagger.Instance.myVRRig.SendRPC("RPC_PlayHandTap", RpcTarget.Others, sound, (i % 2 == 0), 0.1f);
            }
        }

        public static void BlindingFlashGun()
        {
            GunLib.StartGun(() =>
            {
                if (GunLib.LockedPlayer != null)
                    TriggerBlindness(GunLib.LockedPlayer.headMesh.transform.position);
            }, true);
        }

        public static void BlindingFlashAll()
        {
            foreach (VRRig rig in VRRigCache.ActiveRigs)
            {
                if (!rig.isLocal)
                    TriggerBlindness(rig.headMesh.transform.position);
            }
        }

        private static void TriggerBlindness(Vector3 targetHead)
        {
            if (!NetworkSystem.Instance.InRoom) return;
            RPCProt(true);
            VRRig.LocalRig.rightHandTransform.position = targetHead;
            GorillaTagger.Instance.myVRRig.SendRPC("RPC_PlaySplashEffect", RpcTarget.All, targetHead, Quaternion.identity, 1f, 0.5f, true, true);
            GorillaTagger.Instance.myVRRig.SendRPC("RPC_PlayGeodeEffect", RpcTarget.All, targetHead);
        }
    }
}
