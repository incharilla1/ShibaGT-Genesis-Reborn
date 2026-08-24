using GorillaLocomotion;
using HarmonyLib;

namespace ShibaGTGenesisReborn.Patches
{
    [HarmonyPatch(typeof(GTPlayer), "AntiTeleportTechnology")]
    public class TeleportPatch
    {
        private static bool Prefix() => false;
    }
}
