using GorillaLocomotion;
using GorillaTagScripts;
using ShibaGTGenesisReborn.Libs;
using UnityEngine;

namespace ShibaGTGenesisReborn.Mods
{
    public partial class mods
    {
        private static BodyTracker localTracker;
        public static bool IsBodyTrackingActive { get; private set; }
        public static bool IsNetworkedBodyTrackingActive;

        public static void EnableBodyTracking()
        {
            VRRig rig = VRRig.LocalRig;
            if (rig == null) return;

            IsBodyTrackingActive = true;

            if (localTracker == null)
                localTracker = rig.gameObject.GetComponent<BodyTracker>() ?? rig.gameObject.AddComponent<BodyTracker>();

            localTracker.enabled = true;
            Menu.Main.GetIndex("bodytrack").overlapText = "Networked Body Tracking";
        }

        public static void DisableBodyTracking()
        {
            IsBodyTrackingActive = false;

            if (localTracker != null)
                localTracker.enabled = false;

            VRRig rig = VRRig.LocalRig;
            GorillaIK ik = GorillaIK.playerIK ?? rig?.GetComponent<GorillaIK>();

            if (ik != null)
            {
                ik.ResetIKData();
                ik.usingUpdatedIK = false;
            }

            if (rig?.bodyTransform != null)
                rig.bodyTransform.localRotation = Quaternion.identity;

            Menu.Main.GetIndex("bodytrack").overlapText = "Body Tracking (CS)";
        }

        public class BodyTracker : MonoBehaviour
        {
            private Quaternion prevProceduralBodyRot = Quaternion.identity;
            private Vector3 prevLeftElbowDir = Vector3.left;
            private Vector3 prevRightElbowDir = Vector3.right;

            private void LateUpdate()
            {
                VRRig rig = VRRig.LocalRig;
                if (rig == null || GorillaTagger.Instance == null) return;

                Transform headTransform = GorillaTagger.Instance.headCollider != null
                    ? GorillaTagger.Instance.headCollider.transform
                    : rig.headConstraint;

                Transform leftHand = GorillaTagger.Instance.leftHandTransform;
                Transform rightHand = GorillaTagger.Instance.rightHandTransform;

                if (headTransform == null || leftHand == null || rightHand == null) return;

                Vector3 headForward = headTransform.forward;
                Vector3 leftRel = leftHand.position - headTransform.position;
                Vector3 rightRel = rightHand.position - headTransform.position;
                Vector3 avgHandRel = (leftRel + rightRel) * 0.5f;

                float reachForward = Vector3.Dot(avgHandRel, headForward);
                float reachDown = Vector3.Dot(avgHandRel, Vector3.down);

                float pitchLean = Mathf.Clamp(reachForward * 28f - reachDown * 12f, -25f, 35f);
                float yawTwist = Mathf.Clamp((Vector3.Dot(leftRel, headForward) - Vector3.Dot(rightRel, headForward)) * 32f, -30f, 30f);
                float rollTilt = Mathf.Clamp((leftRel.y - rightRel.y) * 22f, -20f, 20f);

                Vector3 flatHeadForward = Vector3.ProjectOnPlane(headForward, Vector3.up).normalized;
                if (flatHeadForward.sqrMagnitude < 0.001f) flatHeadForward = Vector3.forward;

                Quaternion baseYawRot = Quaternion.LookRotation(flatHeadForward, Vector3.up);
                Quaternion targetTorsoRot = baseYawRot * Quaternion.Euler(pitchLean, yawTwist, rollTilt);

                prevProceduralBodyRot = Quaternion.Slerp(prevProceduralBodyRot, targetTorsoRot, Time.deltaTime * 14f);

                GorillaIK ik = GorillaIK.playerIK ?? rig.GetComponent<GorillaIK>();
                if (ik != null)
                {
                    ik.usingUpdatedIK = true;
                    ik.canUseUpdatedIK = true;

                    if (ik.bodyBone != null && ik.bodyBone.parent != null)
                        ik.targetBodyRot = Quaternion.Inverse(ik.bodyBone.parent.rotation) * prevProceduralBodyRot;

                    if (ik.projectedBodyRotation != null)
                        ik.projectedBodyRotation.localRotation = ik.targetBodyRot;

                    Vector3 leftHandDir = (leftHand.position - headTransform.position).normalized;
                    Vector3 rightHandDir = (rightHand.position - headTransform.position).normalized;

                    Vector3 targetLeftElbow = Vector3.Cross(leftHandDir, Vector3.up).normalized + (Vector3.down * 0.25f);
                    Vector3 targetRightElbow = Vector3.Cross(Vector3.up, rightHandDir).normalized + (Vector3.down * 0.25f);

                    prevLeftElbowDir = Vector3.Slerp(prevLeftElbowDir, targetLeftElbow, Time.deltaTime * 12f);
                    prevRightElbowDir = Vector3.Slerp(prevRightElbowDir, targetRightElbow, Time.deltaTime * 12f);

                    ik.leftElbowDirection = prevLeftElbowDir;
                    ik.rightElbowDirection = prevRightElbowDir;
                }

                if (rig.bodyTransform != null)
                    rig.bodyTransform.rotation = prevProceduralBodyRot;
            }
        }
    }
}
