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
            bool isLocal = __instance.isLocal || __instance == VRRig.LocalRig || (__instance == GorillaTagger.Instance.offlineVRRig);
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

    [HarmonyPatch(typeof(VRRig), "SetHandEffectData")]
    internal static class SilentHandTapPatch
    {
        public static bool Enabled;

        private static void Prefix(VRRig __instance, ref float handTapVolume)
        {
            if (Enabled && (__instance.isLocal || __instance.isOfflineVRRig))
                handTapVolume = 0f;
        }
    }

    [HarmonyPatch(typeof(VRRig), "RemoteRigUpdate")]
    internal static class RemoteRigPatch
    {
        public static bool NoLerp;
        public static Vector3 Scale = Vector3.one;
        public static float HeadScale = 1f;
        private static readonly AccessTools.FieldRef<VRRig, bool> snap = AccessTools.FieldRefAccess<VRRig, bool>("snapNextRigUpdate");

        private static void Prefix(VRRig __instance)
        {
            if (NoLerp) snap(__instance) = true;
        }

        private static void Postfix(VRRig __instance)
        {
            __instance.transform.localScale = Vector3.Scale(__instance.transform.localScale, Scale);
            __instance.head.rigTarget.localScale = Vector3.one * HeadScale;
        }
    }

    [HarmonyPatch(typeof(VRRig), "PostTick")]
    internal static class LocalRigPatch
    {
        public static bool FrozenFingers, BodyTracking, Noodle, Bobble, Moonwalk;
        private static Vector3 leftPosition, rightPosition, leftVelocity, rightVelocity, wobble, wobbleVelocity, lastVelocity;

        private static void Postfix(VRRig __instance)
        {
            if (!(__instance.isLocal || __instance.isOfflineVRRig)) return;

            if (FrozenFingers)
            {
                foreach (VRMap finger in new VRMap[] { __instance.leftIndex, __instance.leftMiddle, __instance.leftThumb, __instance.rightIndex, __instance.rightMiddle, __instance.rightThumb })
                {
                    finger.calcT = 0f;
                    finger.LerpFinger(1f, false);
                }
            }

            if (!(BodyTracking || Noodle || Bobble || Moonwalk)) return;
            Transform rig = __instance.transform, head = __instance.head.rigTarget, left = __instance.leftHand.rigTarget, right = __instance.rightHand.rigTarget;
            float dt = Mathf.Max(Time.deltaTime, 0.001f);
            Vector3 velocity = GorillaTagger.Instance.rigidbody.linearVelocity;

            if (BodyTracking)
            {
                head.GetPositionAndRotation(out Vector3 headPosition, out Quaternion headRotation);
                Vector3 hands = Vector3.ProjectOnPlane((left.position + right.position) / 2f - headPosition, Vector3.up);
                Vector3 look = Vector3.ProjectOnPlane(headRotation * Vector3.forward, Vector3.up);
                Vector3 facing = Vector3.Slerp(look.normalized, hands.sqrMagnitude > 0.01f ? hands.normalized : look.normalized, 0.4f);
                Vector3 euler = headRotation.eulerAngles;
                Turn(rig, head, left, right, Quaternion.LookRotation(facing) * Quaternion.Euler(Mathf.DeltaAngle(0f, euler.x) * 0.4f, 0f, Mathf.DeltaAngle(0f, euler.z) * 0.3f) * Quaternion.Inverse(rig.rotation), false);
            }

            Vector3 flat = new Vector3(velocity.x, 0f, velocity.z);
            if (Moonwalk && flat.magnitude > 1f)
                Turn(rig, head, left, right, Quaternion.FromToRotation(Vector3.ProjectOnPlane(rig.forward, Vector3.up).normalized, -flat.normalized), true);

            if (Noodle)
            {
                leftPosition = Spring(leftPosition, ref leftVelocity, left.position, dt);
                rightPosition = Spring(rightPosition, ref rightVelocity, right.position, dt);
                left.position = leftPosition;
                right.position = rightPosition;
            }

            if (Bobble)
            {
                Vector3 accel = (velocity - lastVelocity) / dt;
                wobbleVelocity += (new Vector3(accel.z, 0f, -accel.x) * 3f - wobble * 120f) * dt;
                wobbleVelocity *= 1f - 4f * dt;
                wobble = Vector3.ClampMagnitude(wobble + wobbleVelocity * dt * 60f, 40f);
                head.rotation = Quaternion.Euler(wobble.x, Mathf.Sin(Time.time * 7f) * wobble.magnitude * 0.3f, wobble.z) * head.rotation;
            }
            lastVelocity = velocity;
        }

        private static void Turn(Transform rig, Transform head, Transform left, Transform right, Quaternion turn, bool turnHead)
        {
            head.GetPositionAndRotation(out Vector3 headPosition, out Quaternion headRotation);
            left.GetPositionAndRotation(out Vector3 leftPosition, out Quaternion leftRotation);
            right.GetPositionAndRotation(out Vector3 rightPosition, out Quaternion rightRotation);
            rig.SetPositionAndRotation(headPosition + turn * (rig.position - headPosition), turn * rig.rotation);
            head.SetPositionAndRotation(headPosition, turnHead ? turn * headRotation : headRotation);
            left.SetPositionAndRotation(leftPosition, leftRotation);
            right.SetPositionAndRotation(rightPosition, rightRotation);
        }

        private static Vector3 Spring(Vector3 position, ref Vector3 velocity, Vector3 target, float dt)
        {
            if ((position - target).sqrMagnitude > 4f) position = target;
            velocity += (target - position) * 60f * dt;
            velocity *= 1f - 6f * dt;
            return target + Vector3.ClampMagnitude(position + velocity * dt - target, 0.45f);
        }
    }
}
