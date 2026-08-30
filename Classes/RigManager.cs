using BepInEx;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;
using UnityEngine;

namespace ShibaGTGenesisReborn.Classes
{
    internal class RigManager : BaseUnityPlugin
    {
        public static VRRig GetClosestVRRig()
        {
            float min = float.MaxValue;
            VRRig outRig = null;
            Vector3 localPos = VRRig.LocalRig.transform.position;

            foreach (VRRig vrrig in VRRigCache.ActiveRigs)
            {
                if (vrrig == null || vrrig.isLocal || vrrig.isLocal) continue;
                float d = Vector3.Distance(localPos, vrrig.transform.position);
                if (d < min)
                {
                    min = d;
                    outRig = vrrig;
                }
            }
            return outRig;
        }

        public static VRRig GetClosestUntaggedVRRig(float maxDistance = float.MaxValue)
        {
            float min = maxDistance;
            VRRig outRig = null;
            Vector3 localPos = GorillaTagger.Instance != null ? GorillaTagger.Instance.headCollider.transform.position : (VRRig.LocalRig != null ? VRRig.LocalRig.transform.position : Vector3.zero);

            foreach (VRRig vrrig in VRRigCache.ActiveRigs)
            {
                if (vrrig == null || vrrig.isLocal || vrrig == VRRig.LocalRig) continue;
                if (vrrig.mainSkin != null && vrrig.mainSkin.material != null && vrrig.mainSkin.material.name.Contains("fected")) continue;

                Vector3 targetPos = vrrig.headConstraint != null ? vrrig.headConstraint.position : vrrig.transform.position;
                float d = Vector3.Distance(localPos, targetPos);
                if (d < min)
                {
                    min = d;
                    outRig = vrrig;
                }
            }
            return outRig;
        }

        public static Player GetPlayerFromID(string id)
        {
            if (PhotonNetwork.LocalPlayer != null && PhotonNetwork.LocalPlayer.UserId == id) return PhotonNetwork.LocalPlayer;
            foreach (Player player in PhotonNetwork.PlayerList) 
            {
                if (player != null && player.UserId == id)
                    return player;
            }
            return null;
        }

        public static VRRig GetRandomVRRig(bool includeSelf)
        {
            if (VRRigCache.ActiveRigs.Count == 0) return null;
            if (includeSelf)
                return VRRigCache.ActiveRigs[UnityEngine.Random.Range(0, VRRigCache.ActiveRigs.Count)];

            var others = VRRigCache.ActiveRigs.Where(r => r != null && !r.isLocal && r != VRRig.LocalRig).ToList();
            return others.Count > 0 ? others[UnityEngine.Random.Range(0, others.Count)] : null;
        }

        public static Player GetRandomPlayer(bool includeSelf)
        {
            if (includeSelf)
                return PhotonNetwork.PlayerList.Length > 0 ? PhotonNetwork.PlayerList[UnityEngine.Random.Range(0, PhotonNetwork.PlayerList.Length)] : null;
            return PhotonNetwork.PlayerListOthers.Length > 0 ? PhotonNetwork.PlayerListOthers[UnityEngine.Random.Range(0, PhotonNetwork.PlayerListOthers.Length)] : null;
        }

        public static Player NetPlayerToPlayer(NetPlayer p)
        {
            return p?.GetPlayerRef() ?? null;
        }

        public static NetPlayer PlayerToNetPlayer(Player p)
        {
            return NetworkSystem.Instance.GetPlayer(p.ActorNumber) ?? null;
        }

        public static VRRig GetVRRigFromPlayer(Player p)
        {
            return GorillaGameManager.StaticFindRigForPlayer(p) ?? null;
        }

        public static Player GetPlayerFromVRRig(VRRig p)
        {
            if (p.isLocal) return PhotonNetwork.LocalPlayer;
            if (p.Creator != null) return p.Creator.GetPlayerRef();
            if (p.rigSerializer != null)
            {
                NetPlayer np = NetworkSystem.Instance.GetPlayer(NetworkSystem.Instance.GetOwningPlayerID(p.rigSerializer.gameObject));
                if (np != null) return np.GetPlayerRef();
            }
            return null;
        }

        public static NetPlayer GetNetPlayerFromVRRig(VRRig p)
        {
            if (p.isLocal) return NetworkSystem.Instance.LocalPlayer;
            if (p.Creator != null) return p.Creator;
            if (p.rigSerializer != null)
                return NetworkSystem.Instance.GetPlayer(NetworkSystem.Instance.GetOwningPlayerID(p.rigSerializer.gameObject));
            return null;
        }

        public static PhotonView GetPhotonViewFromVRRig(VRRig p)
        {
            return p?.netView?.GetView;
        }
    }
}