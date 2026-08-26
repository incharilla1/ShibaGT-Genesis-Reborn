using GorillaNetworking;
using HarmonyLib;
using ShibaGTGenesisReborn.Mods;

namespace ShibaGTGenesisReborn.Patches
{
    [HarmonyPatch(typeof(VRRig), nameof(VRRig.IsItemAllowed))]
    internal class VRRigIsItemAllowedPatch
    {
        private static bool Prefix(ref bool __result)
        {
            if (mods.cosmetXEnabled)
            {
                __result = true;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(VRRig), nameof(VRRig.HasCosmetic))]
    internal class VRRigHasCosmeticPatch
    {
        private static bool Prefix(ref bool __result)
        {
            if (mods.cosmetXEnabled)
            {
                __result = true;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(CosmeticsController), nameof(CosmeticsController.IsOwnedByPlayFabID))]
    internal class CosmeticsControllerIsOwnedPatch
    {
        private static bool Prefix(ref bool __result)
        {
            if (mods.cosmetXEnabled)
            {
                __result = true;
                return false;
            }
            return true;
        }
    }
}
