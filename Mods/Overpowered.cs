using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using ShibaGTGenesisReborn.Classes;
using ShibaGTGenesisReborn.Libs;
using UnityEngine;

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
            0.35f,
            2.7f,
            7.6f
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
                o = new RaiseEventOptions { TargetActors = new int[] { p.Creator.ActorNumber } };
            else
                o = new RaiseEventOptions { Receivers = ReceiverGroup.Others };

            PhotonNetwork.NetworkingClient.OpRaiseEvent(202, new object[]
            {
                "ello"
            }, o, SendOptions.SendUnreliable);
        }

        public static void DestroyGun()
        {
           GunLib.StartGun(() =>
            {
                if (GunLib.LockedPlayer != null)
                {
                    PhotonNetwork.OpRemoveCompleteCacheOfPlayer(RigManager.GetPlayerFromVRRig(GunLib.LockedPlayer).ActorNumber);
                }
            }, true);
        }

        public static void DestroyAll()
        {
            foreach (Player player in PhotonNetwork.PlayerListOthers)
                PhotonNetwork.OpRemoveCompleteCacheOfPlayer(player.ActorNumber);
        }
    }
}