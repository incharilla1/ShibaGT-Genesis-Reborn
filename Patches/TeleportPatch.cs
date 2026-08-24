using GorillaLocomotion;
using HarmonyLib;
using UnityEngine;

namespace ShibaGTGenesisReborn.Patches
{
    [HarmonyPatch(typeof(GTPlayer), "AntiTeleportTechnology")] // yes this is a real thing
    public class AntiTeleportPatch
    {
        private static bool Prefix() => false;
    }

    [HarmonyPatch(typeof(GhostReactorShiftManager), "TeleportLocalPlayerIfOutOfBounds")]
    public class GhostReactorOutOfBoundsPatch
    {
        private static bool Prefix() => false;
    }
}
