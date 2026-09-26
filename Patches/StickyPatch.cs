using GorillaLocomotion;
using HarmonyLib;
using UnityEngine;

namespace ShibaGTGenesisReborn.Patches
{
    [HarmonyPatch(typeof(GTPlayer), "LateUpdate")]
    internal static class StickyPatch
    {
        private static void Prefix() => Mods.mods.UpdateStickyMovement();

        private static void Postfix()
        {
            if (Mods.mods.GetStickyPoint(true, out _) || Mods.mods.GetStickyPoint(false, out _))
                GorillaTagger.Instance.rigidbody.linearVelocity = Vector3.zero;
        }

        internal static bool AnchorHand(ref GTPlayer.HandState hand, out Vector3 anchor)
        {
            if (!Mods.mods.GetStickyPoint(hand.isLeftHand, out anchor)) return false;

            hand.isColliding = true;
            hand.isSliding = false;
            hand.slipPercentage = 0f;
            GTPlayer.Instance.anyHandIsColliding = true;
            GTPlayer.Instance.anyHandIsSticking = true;

            return true;
        }

    }

    [HarmonyPatch(typeof(GTPlayer.HandState), nameof(GTPlayer.HandState.FirstIteration))]
    internal static class StickyMovePatch
    {
        private static bool Prefix(ref GTPlayer.HandState __instance, ref Vector3 totalMove, ref int divisor)
        {
            if (!StickyPatch.AnchorHand(ref __instance, out Vector3 anchor)) return true;
            totalMove += anchor - __instance.GetCurrentHandPosition();
            divisor++;
            return false;
        }

    }

    [HarmonyPatch(typeof(GTPlayer.HandState), nameof(GTPlayer.HandState.FinalizeHandPosition))]
    internal static class StickyPositionPatch
    {
        private static bool Prefix(ref GTPlayer.HandState __instance)
        {
            if (!StickyPatch.AnchorHand(ref __instance, out Vector3 anchor)) return true;
            __instance.finalPositionThisFrame = anchor;
            return false;
        }

    }

    [HarmonyPatch(typeof(GTPlayer), "stuckHandsCheckLateUpdate")]
    internal static class StickyReleasePatch
    {
        private static void Prefix(ref bool ___stuckLeft, ref bool ___stuckRight)
        {
            if (Mods.mods.GetStickyPoint(true, out _)) ___stuckLeft = false;
            if (Mods.mods.GetStickyPoint(false, out _)) ___stuckRight = false;
        }
    }
}
