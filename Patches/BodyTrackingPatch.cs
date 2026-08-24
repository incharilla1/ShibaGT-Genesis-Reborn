using GorillaTagScripts;
using HarmonyLib;
using ShibaGTGenesisReborn.Mods;

namespace ShibaGTGenesisReborn.Patches
{
    [HarmonyPatch(typeof(VRRig), "ShouldUseNewIKMethod")]
    internal class BodyTrackingNetworkPatch
    {
        private static bool Prefix(VRRig __instance, bool isReceivingNewIKData, ref bool __result)
        {
            if (mods.IsBodyTrackingActive && mods.IsNetworkedBodyTrackingActive)
            {
                __result = true;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(SubscriptionManager), nameof(SubscriptionManager.IsLocalSubscribed))]
    internal class SubscriptionPatch
    {
        private static bool Prefix(ref bool __result)
        {
            if (mods.IsBodyTrackingActive && mods.IsNetworkedBodyTrackingActive)
            {
                __result = true;
                return false;
            }
            return true;
        }
    }
}
