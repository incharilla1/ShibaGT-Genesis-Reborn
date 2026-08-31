using HarmonyLib;
using UnityEngine;

namespace ShibaGTGenesisReborn.Patches
{
    [HarmonyPatch(typeof(VRRig), "OnDisable")]
    internal static class GhostPatch
    {
        private static bool Prefix(VRRig __instance)
        {
            if (__instance == null) return true;
            bool isLocal = __instance.isLocal || __instance == VRRig.LocalRig || (GorillaTagger.Instance != null && __instance == GorillaTagger.Instance.offlineVRRig);
            return !isLocal;
        }
    }

    [HarmonyPatch(typeof(VRRigJobManager), "DeregisterVRRig")]
    public static class GhostPatch2
    {
        private static bool Prefix(VRRig rig)
        {
            if (rig == null) return true;
            bool isLocal = rig.isLocal || rig == VRRig.LocalRig || (rig == GorillaTagger.Instance.offlineVRRig);
            return !isLocal;
        }
    }
}
