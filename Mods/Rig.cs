using GorillaLocomotion;
using ShibaGTGenesisReborn.Classes;
using ShibaGTGenesisReborn.Libs;
using ShibaGTGenesisReborn.Menu;
using System;
using UnityEngine;

namespace ShibaGTGenesisReborn.Mods
{
    public partial class mods
    {
        private static bool Ghost_Toggled = false;
        private static bool Invis_Toggled = false;

        public static void GhostMonke()
        {
            bool isPressed = InputHandler.Instance.LeftPrimary.WasPressed;

            if (isPressed)
            {
                Ghost_Toggled = !Ghost_Toggled;
                VRRig.LocalRig.enabled = !Ghost_Toggled;
            }
        }

        public static void InvisMonke()
        {
            if (InputHandler.Instance.RightPrimary.WasPressed)
                Invis_Toggled = !Invis_Toggled;

            if (Invis_Toggled)
                NetworkingLibrary.SendRigPosition(RigManager.GetPhotonViewFromVRRig(VRRig.LocalRig), new Vector3(0f, -100f, 0f));
            else
            {
                VRRig.LocalRig.enabled = true;
                GorillaTagger.Instance.offlineVRRig.enabled = true;
            }
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
        }

        public static void NormalArms()
        {
            GTPlayer.Instance.transform.localScale = new Vector3(1f, 1f, 1f);
        }

        public static void NoFinger()
        {
            ControllerInputPoller.instance.leftControllerGripFloat = 0f;
            ControllerInputPoller.instance.rightControllerGripFloat = 0f;
            ControllerInputPoller.instance.leftControllerIndexFloat = 0f;
            ControllerInputPoller.instance.rightControllerIndexFloat = 0f;
            ControllerInputPoller.instance.leftControllerPrimaryButton = false;
            ControllerInputPoller.instance.leftControllerSecondaryButton = false;
            ControllerInputPoller.instance.rightControllerPrimaryButton = false;
            ControllerInputPoller.instance.rightControllerSecondaryButton = false;
            ControllerInputPoller.instance.leftControllerPrimaryButtonTouch = false;
            ControllerInputPoller.instance.leftControllerSecondaryButtonTouch = false;
            ControllerInputPoller.instance.rightControllerPrimaryButtonTouch = false;
            ControllerInputPoller.instance.rightControllerSecondaryButtonTouch = false;
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
