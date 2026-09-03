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
using UnityEngine;
using UnityEngine.Playables;

namespace ShibaGTGenesisReborn.Mods
{
    public partial class mods
    {
        [Setting] public static int lagindex;
        public static int[] lagthings =
        {
            300,
            1250,
            3000
        };

        public static readonly string[] lagnames =
        {
            "Weak",
            "Strong",
            "Spike",
        };

        public static float[] lagcooldowns =
        {
            0.37f,
            2.8f,
            7.9f
        };

        public static float tagTimer;
        public static float CDown;

        public static void LagGun()
        {
            GunLib.StartGun(() =>
            {
                if (GunLib.LockedPlayer != null)
                {
                    if (Time.time > CDown)
                    {
                        RPCProt();
                        for (int i = 0; i < lagthings[lagindex]; i++)
                        {
                            SendOPRaiseEvent202(GunLib.LockedPlayer);
                        }
                        CDown = Time.time + lagcooldowns[lagindex];
                    }
                }
            }, true);
        }

        public static void LagAll()
        {
            if (Time.time > CDown)
            {
                RPCProt();
                for (int i = 0; i < lagthings[lagindex]; i++)
                {
                    SendOPRaiseEvent202();
                }
                CDown = Time.time + lagcooldowns[lagindex];
            }
        }

        public static void SendOPRaiseEvent202(VRRig p = null)
        {
            RaiseEventOptions o;
            if (p != null)
                o = new RaiseEventOptions { TargetActors = new int[] { p.Creator.ActorNumber }, CachingOption = EventCaching.DoNotCache };
            else
                o = new RaiseEventOptions { Receivers = ReceiverGroup.Others, CachingOption = EventCaching.DoNotCache };

            PhotonNetwork.NetworkingClient.OpRaiseEvent(202, new object[]
             {
                -2147483647,
                76,
                float.NaN,
             }, o, new SendOptions { DeliveryMode = DeliveryMode.Unreliable, Reliability = false, Encrypt = true });
        }

        public static void DestroyGun()
        {
            GunLib.StartGun(() =>
             {
                 if (GunLib.LockedPlayer != null)
                 {
                     if (!Main.RequireMasterClient("Destroy Gun")) return;
                     RPCProt();
                     PhotonNetwork.OpRemoveCompleteCacheOfPlayer(RigManager.GetPlayerFromVRRig(GunLib.LockedPlayer).ActorNumber);
                 }
             }, true);
        }

        public static void DestroyAll()
        {
            if (!Main.RequireMasterClient("Destroy All")) return;
            RPCProt();
            foreach (Player player in PhotonNetwork.PlayerListOthers)
            {
                PhotonNetwork.OpRemoveCompleteCacheOfPlayer(player.ActorNumber);
            }
        }

        public static void TargetSpam()
        {
            if (!Main.RequireMasterClient("Target Spam")) return;
            foreach (HitTargetNetworkState target in GameObject.FindObjectsByType<HitTargetNetworkState>(FindObjectsSortMode.None))
            {
                if (target == null) continue;
                Vector3 pos = target.transform.position;
                target.TargetHit(pos, pos);
            }
        }

        public static void BecomeGuardian()
        {
            if (!Main.RequireMasterClient("Become Guardian")) return;

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
            if (!Main.RequireMasterClient("Eject Guardians")) return;

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

            int allShieldEffects = (int)(GRPlayer.GRPlayerShieldFlags.Light |
                                         GRPlayer.GRPlayerShieldFlags.Stealth |
                                         GRPlayer.GRPlayerShieldFlags.Heal);

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

        public static void ForceStartCurrentGame()
        {
            if (!Main.RequireMasterClient("Force Start Game")) return;
            GorillaGameManager manager = GorillaGameManager.instance;
            if (manager == null) return;

            manager.StartPlaying();
        }

        public static void ResetCurrentGame()
        {
            if (!Main.RequireMasterClient("Reset Current Game")) return;
            GorillaGameManager manager = GorillaGameManager.instance;
            if (manager == null) return;

            manager.ResetGame();
        }

        public static void FreezeAllPlayers()
        {
            if (!Main.RequireMasterClient("Freeze All Players")) return;
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

    }
}
