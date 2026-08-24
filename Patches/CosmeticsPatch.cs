using GorillaNetworking;
using HarmonyLib;
using ShibaGTGenesisReborn.Menu;

namespace ShibaGTGenesisReborn.Patches
{
    [HarmonyPatch(typeof(VRRig), nameof(VRRig.IsItemAllowed))]
    internal class VRRigIsItemAllowedPatch
    {
        private static bool Prefix(ref bool __result)
        {
            if (Main.GetIndex("CosmetX")?.enabled == true)
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
            if (Main.GetIndex("CosmetX")?.enabled == true)
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
            if (Main.GetIndex("CosmetX")?.enabled == true)
            {
                __result = true;
                return false;
            }
            return true;
        }
    }
}
