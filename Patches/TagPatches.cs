using GorillaGameModes;
using GorillaLocomotion;
using HarmonyLib;
using UnityEngine;

namespace ShibaGTGenesisReborn.Patches
{
    [HarmonyPatch(typeof(VRRig), nameof(VRRig.IsPositionInRange))]
    public class IsPositionInRangePatch
    {
        private static bool Prefix(ref bool __result)
        {
            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(VRRig), nameof(VRRig.CheckTagDistanceRollback))]
    public class CheckTagDistanceRollbackPatch
    {
        private static bool Prefix(ref bool __result)
        {
            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(GorillaTagManager), nameof(GorillaTagManager.ReportTag))]
    public class TagManagerCooldownPatch
    {
        private static void Prefix(GorillaTagManager __instance)
        {
            __instance.lastTag = 0f;
        }
    }

    [HarmonyPatch(typeof(GorillaTagCompetitiveManager), nameof(GorillaTagCompetitiveManager.ReportTag))]
    public class CompTagCooldownPatch
    {
        private static void Prefix(GorillaTagCompetitiveManager __instance)
        {
            __instance.lastTag = 0f;
        }
    }
}
