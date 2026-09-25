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
        private static VRRig lookFreezeTarget;

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
            GunLib.CleanupPointer();
        }

        private static bool IsLookingAtRig(VRRig rig)
        {
            Transform head = rig.headConstraint;
            Transform target = VRRig.LocalRig.headConstraint;
            Vector3 direction = (target.position - head.position).normalized;
            return Vector3.Dot(head.forward, direction) >= (rigFrozen ? 0.82f : 0.9f);
        }

        public static void GhostMonke() => ghostHeld = InputHandler.Instance.LeftPrimary.IsPressed || UnityInput.Current.GetKey(KeyCode.F);
        public static void InvisMonke() => invisHeld = InputHandler.Instance.RightPrimary.IsPressed || UnityInput.Current.GetKey(KeyCode.B);

        public static void UpdateRigTracking()
        {
            bool pause = ghostHeld || invisHeld || gunLooking || allLooking;
            if (pause) 
            {
                VRRig.LocalRig.enabled = false;
                GorillaTagger.Instance.offlineVRRig.enabled = false;
                TickSystem<object>.RemovePostTickCallback(VRRig.LocalRig); 
            }
            else if (rigPaused) TickSystem<object>.AddPostTickCallback(VRRig.LocalRig);
            rigPaused = pause;

            if (invisHeld) VRRig.LocalRig.transform.position = new Vector3(0f, -999f, 0f);

            rigFrozen = gunLooking || allLooking;
            gunLooking = allLooking = ghostHeld = invisHeld = false;
        }

        private static Vector3 armlen = new Vector3(1f, 1f, 1f);
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
            System.Random random = new System.Random();
            GorillaTagger.Instance.offlineVRRig.head.rigTarget.eulerAngles = new Vector3(random.Next(0, 360), random.Next(0, 360), random.Next(0, 360));
            GorillaTagger.Instance.offlineVRRig.leftHand.rigTarget.eulerAngles = new Vector3(random.Next(0, 360), random.Next(0, 360), random.Next(0, 360));
            GorillaTagger.Instance.offlineVRRig.rightHand.rigTarget.eulerAngles = new Vector3(random.Next(0, 360), random.Next(0, 360), random.Next(0, 360));
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

        public static void CopyGun()
        {
            GunLib.StartGun(() =>
            {
                if (GunLib.LockedPlayer == null) return;

                NetworkingLibrary.SendRigPosition(RigManager.GetPhotonViewFromVRRig(VRRig.LocalRig), GunLib.LockedPlayer.transform.position);
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
