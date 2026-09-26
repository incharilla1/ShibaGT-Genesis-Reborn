using GorillaLocomotion;
using ShibaGTGenesisReborn.Libs;
using UnityEngine;

namespace ShibaGTGenesisReborn.Mods
{
    public partial class mods
    {
        public static bool stickyHands;
        private static readonly Collider[] stickySurfaces = new Collider[2];
        private static readonly Vector3[] stickyPoints = new Vector3[2];
        private static bool? stickyGravity;

        private static void UpdateStickyHand(bool left)
        {
            int index = left ? 0 : 1;
            if (InputHandler.Instance == null ||
                (left ? InputHandler.Instance.LeftGrip.IsPressed : InputHandler.Instance.RightGrip.IsPressed))
            {
                stickySurfaces[index] = null;
                return;
            }

            GTPlayer player = GTPlayer.Instance;
            Vector3 position = left ? player.LeftHand.GetCurrentHandPosition() : player.RightHand.GetCurrentHandPosition();
            float radius = (player.minimumRaycastDistance + 0.02f) * player.scale;
            Collider surface = stickySurfaces[index];
            if (surface != null && surface.enabled && surface.gameObject.activeInHierarchy &&
                (surface.ClosestPoint(position) - position).sqrMagnitude <= radius * radius * 4f) return;

            float distance = float.PositiveInfinity;

            stickySurfaces[index] = null;

            foreach (Collider collider in Physics.OverlapSphere(position, radius, player.locomotionEnabledLayers, QueryTriggerInteraction.Ignore))
            {
                if (collider.transform.IsChildOf(player.transform) || collider.GetComponentInParent<VRRig>() != null) continue;
                float hitDistance = (collider.ClosestPoint(position) - position).sqrMagnitude;
                if (hitDistance >= distance) continue;
                distance = hitDistance;
                stickySurfaces[index] = collider;
                stickyPoints[index] = collider.transform.InverseTransformPoint(position);
            }
        }

        public static bool GetStickyPoint(bool left, out Vector3 position)
        {
            position = Vector3.zero;
            GameObject platform = left ? PlatL : PlatR;
            if (stickyPlatforms && platform != null && InputHandler.Instance != null &&
                (left ? InputHandler.Instance.LeftGrip.IsPressed : InputHandler.Instance.RightGrip.IsPressed))
            {
                position = platform.transform.position;
                return true;
            }

            int index = left ? 0 : 1;
            Collider surface = stickySurfaces[index];

            if (!stickyHands || surface == null || !surface.enabled || !surface.gameObject.activeInHierarchy) return false;
            position = surface.transform.TransformPoint(stickyPoints[index]);

            return true;
        }

        public static void UpdateStickyMovement()
        {
            if (stickyHands)
            {
                UpdateStickyHand(true);
                UpdateStickyHand(false);
            }

            if (!GetStickyPoint(true, out _) && !GetStickyPoint(false, out _))
            {
                ResetStickyGravity();
                return;
            }

            Rigidbody rb = GorillaTagger.Instance.rigidbody;
            stickyGravity ??= rb.useGravity;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
        }

        private static void ResetStickyGravity()
        {
            if (!stickyGravity.HasValue) return;
            GorillaTagger.Instance.rigidbody.useGravity = stickyGravity.Value;
            stickyGravity = null;
        }

        public static void ResetPlatforms()
        {
            Object.Destroy(PlatL);
            Object.Destroy(PlatR);
            PlatL = PlatR = null;
            ResetStickyGravity();
        }
    }
}
