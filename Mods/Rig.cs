using BepInEx;
using GorillaLocomotion;
using ShibaGTGenesisReborn.Classes;
using ShibaGTGenesisReborn.Libs;
using System;
using System.Linq;
using UnityEngine;

namespace ShibaGTGenesisReborn.Mods
{
    public partial class mods
    {
        private static bool gunLooking;
        private static bool allLooking;
        private static bool rigFrozen;
        private static bool rigPaused;
        private static bool ghostHeld;
        private static bool invisHeld;
        private static bool remoteHeld;
        private static VRRig lookFreezeTarget;
        private static VRRig beeTarget;
        private static VRRig stareTarget;
        private static float beeTime;

        public static void LookFreezeGun()
        {
            GunLib.StartGun(() => { }, true);
            if (GunLib.LockedPlayer != null)
                lookFreezeTarget = GunLib.LockedPlayer;
            if (lookFreezeTarget != null && !VRRigCache.ActiveRigs.Contains(lookFreezeTarget))
                lookFreezeTarget = null;

            gunLooking = lookFreezeTarget != null && IsLookingAtRig(lookFreezeTarget);
        }

        public static void LookFreezeAll()
        {
            allLooking = false;
            foreach (VRRig rig in VRRigCache.ActiveRigs)
            {
                if (rig != null && !rig.isLocal && rig != VRRig.LocalRig && IsLookingAtRig(rig))
                {
                    allLooking = true;
                    break;
                }
            }
        }

        public static void DisableLookFreezeGun()
        {
            lookFreezeTarget = null;
            gunLooking = false;
            GunLib.CleanupPointer();
        }

        private static bool IsLookingAtRig(VRRig rig)
        {
            Vector3 direction = (VRRig.LocalRig.head.rigTarget.position - rig.head.rigTarget.position).normalized;
            Vector3 forward = rig.head.rigTarget.parent.rotation * rig.head.syncRotation * Vector3.forward;
            return Vector3.Dot(forward, direction) >= (rigFrozen ? 0.8f : 0.85f);
        }

        public static void GhostMonke() => ghostHeld = InputHandler.Instance.LeftPrimary.IsPressed || UnityInput.Current.GetKey(KeyCode.F);
        public static void InvisMonke() => invisHeld = InputHandler.Instance.RightPrimary.IsPressed || UnityInput.Current.GetKey(KeyCode.B);

        public static void UpdateRigTracking()
        {
            bool pause = ghostHeld || invisHeld || gunLooking || allLooking || remoteHeld;
            if (pause && !rigPaused) TickSystem<object>.RemovePostTickCallback(VRRig.LocalRig);
            else if (!pause && rigPaused) TickSystem<object>.AddPostTickCallback(VRRig.LocalRig);
            rigPaused = pause;

            if (invisHeld) VRRig.LocalRig.transform.position = new Vector3(0f, -999f, 0f);

            rigFrozen = gunLooking || allLooking;
            gunLooking = allLooking = ghostHeld = invisHeld = remoteHeld = false;
        }

        public static void LongArms()
        {
            if (InputHandler.Instance.RightTrigger.IsPressed)
            {
                GTPlayer.Instance.transform.localScale += new Vector3(0.01f, 0.01f, 0.01f);
            }
            if (InputHandler.Instance.LeftTrigger.IsPressed)
            {
                GTPlayer.Instance.transform.localScale -= new Vector3(0.01f, 0.01f, 0.01f);
            }
            else if (InputHandler.Instance.RightPrimary.IsPressed)
            {
                GTPlayer.Instance.transform.localScale = new Vector3(1f, 1f, 1f);
            }
        }

        public static void SpazRig()
        {
            GorillaTagger.Instance.offlineVRRig.head.rigTarget.rotation = UnityEngine.Random.rotation;
            GorillaTagger.Instance.offlineVRRig.leftHand.rigTarget.rotation = UnityEngine.Random.rotation;
            GorillaTagger.Instance.offlineVRRig.rightHand.rigTarget.rotation = UnityEngine.Random.rotation;
        }

        public static void FixHead()
        {
            VRRig.LocalRig.enabled = true;
            GorillaTagger.Instance.offlineVRRig.enabled = true;
            VRRig.LocalRig.head.trackingRotationOffset.x = 0f;
            VRRig.LocalRig.head.trackingRotationOffset.y = 0f;
            VRRig.LocalRig.head.trackingRotationOffset.z = 0f;
        }

        public static void HeadSpinner() => VRRig.LocalRig.head.trackingRotationOffset.y += Time.deltaTime * 360f;

        public static void RigGun()
        {
            GunLib.StartGun(() =>
            {
                remoteHeld = true;
                VRRig.LocalRig.transform.position = GunLib.spherepointer.transform.position + Vector3.up;
            }, false);
        }

        public static void OrbitGun()
        {
            GunLib.StartGun(() =>
            {
                float angle = Time.time * 4f;
                remoteHeld = true;
                VRRig.LocalRig.transform.position = GunLib.LockedPlayer.head.rigTarget.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 1.25f;
                VRRig.LocalRig.transform.LookAt(GunLib.LockedPlayer.head.rigTarget.position);
            }, true);
        }

        public static void JumpscareGun()
        {
            GunLib.StartGun(() =>
            {
                if (Time.time % 1.5f > 0.5f) return;
                PlaceRigByHead(GunLib.LockedPlayer, 0.35f);
            }, true);
        }

        public static void HauntGun() => GunLib.StartGun(() => PlaceRigByHead(GunLib.LockedPlayer, -1f), true);

        private static void PlaceRigByHead(VRRig target, float distance)
        {
            Vector3 head = target.head.rigTarget.position;
            remoteHeld = true;
            VRRig.LocalRig.transform.position = head + target.head.rigTarget.parent.rotation * target.head.syncRotation * Vector3.forward * distance;
            VRRig.LocalRig.transform.LookAt(head);
        }

        public static void StareGun()
        {
            GunLib.StartGun(() => stareTarget = GunLib.LockedPlayer, true);
            if (stareTarget != null && VRRigCache.ActiveRigs.Contains(stareTarget))
                StareAt(stareTarget);
        }

        public static void ClearStare() => stareTarget = null;

        public static void StareNearest()
        {
            Vector3 self = VRRig.LocalRig.transform.position;
            VRRig nearest = null;
            float best = float.MaxValue;
            foreach (VRRig rig in VRRigCache.ActiveRigs)
            {
                if (rig == null || rig.isLocal || rig == VRRig.LocalRig) continue;
                float dist = (rig.transform.position - self).sqrMagnitude;
                if (dist < best)
                {
                    best = dist;
                    nearest = rig;
                }
            }
            if (nearest != null) StareAt(nearest);
        }

        private static void StareAt(VRRig rig) =>
            VRRig.LocalRig.head.rigTarget.rotation = Quaternion.LookRotation(rig.head.rigTarget.position - VRRig.LocalRig.head.rigTarget.position);

        public static void FakeLagRig() => remoteHeld = Time.time % 0.5f > 0.08f;

        public static void Bees()
        {
            if (Time.time > beeTime || beeTarget == null || !VRRigCache.ActiveRigs.Contains(beeTarget))
            {
                VRRig[] others = VRRigCache.ActiveRigs.Where(rig => rig != null && !rig.isLocal && rig != VRRig.LocalRig).ToArray();
                beeTarget = others.Length > 0 ? others[UnityEngine.Random.Range(0, others.Length)] : null;
                beeTime = Time.time + 0.5f;
            }
            if (beeTarget == null) return;

            remoteHeld = true;
            VRRig.LocalRig.transform.position = beeTarget.head.rigTarget.position + Vector3.up * 0.3f;
        }

        public static void CopyGun()
        {
            GunLib.StartGun(() =>
            {
                if (GunLib.LockedPlayer == null) return;

                remoteHeld = true;
                NetworkingLibrary.SendRigPosition(GunLib.LockedPlayer.transform.position);
                VRRig.LocalRig.transform.rotation = GunLib.LockedPlayer.transform.rotation;

                VRRig.LocalRig.head.rigTarget.transform.localPosition = GunLib.LockedPlayer.head.rigTarget.transform.localPosition;
                VRRig.LocalRig.head.rigTarget.transform.localRotation = GunLib.LockedPlayer.head.rigTarget.transform.localRotation;
                VRRig.LocalRig.headConstraint.SetPositionAndRotation(GunLib.LockedPlayer.headConstraint.position, GunLib.LockedPlayer.headConstraint.rotation);
                VRRig.LocalRig.head.trackingRotationOffset = GunLib.LockedPlayer.head.trackingRotationOffset;

                VRRig.LocalRig.leftHand.rigTarget.transform.localPosition = GunLib.LockedPlayer.leftHand.rigTarget.transform.localPosition;
                VRRig.LocalRig.leftHand.rigTarget.transform.localRotation = GunLib.LockedPlayer.leftHand.rigTarget.transform.localRotation;
                VRRig.LocalRig.leftHandTransform.SetPositionAndRotation(GunLib.LockedPlayer.leftHandTransform.position, GunLib.LockedPlayer.leftHandTransform.rotation);

                VRRig.LocalRig.rightHand.rigTarget.transform.localPosition = GunLib.LockedPlayer.rightHand.rigTarget.transform.localPosition;
                VRRig.LocalRig.rightHand.rigTarget.transform.localRotation = GunLib.LockedPlayer.rightHand.rigTarget.transform.localRotation;
                VRRig.LocalRig.rightHandTransform.SetPositionAndRotation(GunLib.LockedPlayer.rightHandTransform.position, GunLib.LockedPlayer.rightHandTransform.rotation);
            }, true);

            if (GunLib.LockedPlayer == null && !VRRig.LocalRig.enabled)
            {
                VRRig.LocalRig.enabled = true;
                GorillaTagger.Instance.offlineVRRig.enabled = true;
            }
        }
    }
}
