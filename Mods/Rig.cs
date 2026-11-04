using BepInEx;
using GorillaLocomotion;
using Photon.Pun;
using ShibaGTGenesisReborn.Classes;
using ShibaGTGenesisReborn.Libs;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ShibaGTGenesisReborn.Mods
{
    public partial class mods
    {
        private static bool gunLooking, allLooking, rigFrozen;
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

        [Setting] public static int ghostModeIndex;
        public static readonly string[] ghostModeNames = { "Hold", "Toggle" };
        [Setting] public static bool showRealPosition;
        private static bool ghostHeld, invisHeld, ghostToggled, invisToggled, ghostWasPressed, invisWasPressed;
        private static GameObject[] realMarkers;

        private static bool GhostInput(bool pressed, ref bool wasPressed, ref bool toggled)
        {
            if (ghostModeIndex % ghostModeNames.Length == 0) return pressed;
            if (pressed && !wasPressed) toggled = !toggled;
            wasPressed = pressed;
            return toggled;
        }

        public static void GhostMonke() => ghostHeld = GhostInput((Settings.rightHanded ? InputHandler.Instance.LeftPrimary : InputHandler.Instance.RightPrimary).IsPressed || UnityInput.Current.GetKey(KeyCode.F), ref ghostWasPressed, ref ghostToggled);
        public static void InvisMonke() => invisHeld = GhostInput(InputHandler.Instance.RightSecondary.IsPressed || UnityInput.Current.GetKey(KeyCode.B), ref invisWasPressed, ref invisToggled);
        public static void ResetGhostToggles() => ghostToggled = invisToggled = false;

        private static void UpdateRealMarkers(bool show)
        {
            if (!show)
            {
                if (realMarkers != null)
                    foreach (GameObject marker in realMarkers)
                        if (marker != null) marker.SetActive(false);
                return;
            }

            if (realMarkers == null || realMarkers[0] == null)
            {
                realMarkers = new GameObject[4];
                float[] sizes = { 0.25f, 0.35f, 0.1f, 0.1f };
                for (int i = 0; i < realMarkers.Length; i++)
                {
                    GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    UnityEngine.Object.Destroy(marker.GetComponent<Collider>());
                    Material material = marker.GetComponent<Renderer>().material;
                    material.shader = TextShader;
                    material.color = new Color(1f, 1f, 1f, 0.35f);
                    marker.transform.localScale = Vector3.one * sizes[i];
                    realMarkers[i] = marker;
                }
            }

            realMarkers[0].transform.position = GorillaTagger.Instance.headCollider.transform.position;
            realMarkers[1].transform.position = GorillaTagger.Instance.bodyCollider.transform.position;
            realMarkers[2].transform.position = GorillaTagger.Instance.leftHandTransform.position;
            realMarkers[3].transform.position = GorillaTagger.Instance.rightHandTransform.position;
            foreach (GameObject marker in realMarkers)
                marker.SetActive(true);
        }

        private static bool rigPaused, remoteHeld;

        public static void UpdateRigTracking()
        {
            bool pause = ghostHeld || invisHeld || gunLooking || allLooking || remoteHeld;
            if (pause && !rigPaused) TickSystem<object>.RemovePostTickCallback(VRRig.LocalRig);
            else if (!pause && rigPaused) TickSystem<object>.AddPostTickCallback(VRRig.LocalRig);
            rigPaused = pause;

            UpdateRealMarkers(pause && showRealPosition);
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
            else if (InputHandler.Instance.RightSecondary.IsPressed)
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

        private static VRRig stareTarget;

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

        private static float rapidTapTime;
        private static bool rapidTapLeft;

        public static void RapidHandTaps()
        {
            if (Time.time < rapidTapTime || !PhotonNetwork.InRoom) return;
            rapidTapTime = Time.time + 0.1f;
            rapidTapLeft = !rapidTapLeft;
            GorillaTagger.Instance.myVRRig.SendRPC("RPC_PlayHandTap", RpcTarget.All, Mathf.Max(GorillaTagger.Instance.audioClipIndex, 0), rapidTapLeft, 0.6f);
        }

        public static void TPose()
        {
            if (!InputHandler.Instance.RightSecondary.IsPressed) return;
            remoteHeld = true;
            VRRig rig = VRRig.LocalRig;
            Transform body = GorillaTagger.Instance.bodyCollider.transform;
            rig.transform.SetPositionAndRotation(body.position + Vector3.up * 0.15f, body.rotation);
            rig.head.rigTarget.rotation = body.rotation;
            rig.leftHand.rigTarget.SetPositionAndRotation(rig.transform.position - rig.transform.right, rig.transform.rotation * Quaternion.Euler(rig.leftHand.trackingRotationOffset));
            rig.rightHand.rigTarget.SetPositionAndRotation(rig.transform.position + rig.transform.right, rig.transform.rotation * Quaternion.Euler(rig.rightHand.trackingRotationOffset));
        }

        public static void FakeLagRig() => remoteHeld = Time.time % 0.5f > 0.08f;

        private static readonly Queue<(float time, Vector3 position, Quaternion rotation, Quaternion head, Vector3 left, Quaternion leftRotation, Vector3 right, Quaternion rightRotation)> rigHistory =
            new Queue<(float, Vector3, Quaternion, Quaternion, Vector3, Quaternion, Vector3, Quaternion)>();

        public static void DelayedRig()
        {
            Transform body = GorillaTagger.Instance.bodyCollider.transform;
            Transform left = GorillaTagger.Instance.leftHandTransform, right = GorillaTagger.Instance.rightHandTransform;
            rigHistory.Enqueue((Time.time, body.position + Vector3.up * 0.15f, body.rotation, GorillaTagger.Instance.headCollider.transform.rotation, left.position, left.rotation, right.position, right.rotation));
            while (rigHistory.Peek().time < Time.time - 2f) rigHistory.Dequeue();

            (_, Vector3 position, Quaternion rotation, Quaternion head, Vector3 leftPosition, Quaternion leftRotation, Vector3 rightPosition, Quaternion rightRotation) = rigHistory.Peek();
            VRRig rig = VRRig.LocalRig;
            remoteHeld = true;
            rig.transform.SetPositionAndRotation(position, rotation);
            rig.head.rigTarget.rotation = head;
            rig.leftHand.rigTarget.SetPositionAndRotation(leftPosition, leftRotation * Quaternion.Euler(rig.leftHand.trackingRotationOffset));
            rig.rightHand.rigTarget.SetPositionAndRotation(rightPosition, rightRotation * Quaternion.Euler(rig.rightHand.trackingRotationOffset));
        }

        public static void DisableDelayedRig() => rigHistory.Clear();

        private static Rigidbody ragdollBody, ragdollLeft, ragdollRight;

        public static void Ragdoll()
        {
            if (ragdollBody == null)
            {
                VRRig local = VRRig.LocalRig;
                ragdollBody = RagdollPart(PrimitiveType.Capsule, local.transform.position, new Vector3(0.35f, 0.3f, 0.35f));
                ragdollBody.rotation = local.transform.rotation;
                ragdollBody.linearVelocity = GorillaTagger.Instance.rigidbody.linearVelocity;
                ragdollBody.AddTorque(UnityEngine.Random.insideUnitSphere * 3f, ForceMode.VelocityChange);
                ragdollLeft = RagdollPart(PrimitiveType.Sphere, local.leftHand.rigTarget.position, Vector3.one * 0.12f);
                ragdollRight = RagdollPart(PrimitiveType.Sphere, local.rightHand.rigTarget.position, Vector3.one * 0.12f);
                foreach (Rigidbody hand in new[] { ragdollLeft, ragdollRight })
                {
                    SpringJoint joint = hand.gameObject.AddComponent<SpringJoint>();
                    joint.connectedBody = ragdollBody;
                    joint.spring = 60f;
                    joint.damper = 2f;
                    joint.maxDistance = 0.55f;
                }
            }

            VRRig rig = VRRig.LocalRig;
            remoteHeld = true;
            rig.transform.SetPositionAndRotation(ragdollBody.position, ragdollBody.rotation);
            rig.head.rigTarget.rotation = ragdollBody.rotation;
            rig.leftHand.rigTarget.SetPositionAndRotation(ragdollLeft.position, ragdollBody.rotation * Quaternion.Euler(rig.leftHand.trackingRotationOffset));
            rig.rightHand.rigTarget.SetPositionAndRotation(ragdollRight.position, ragdollBody.rotation * Quaternion.Euler(rig.rightHand.trackingRotationOffset));
        }

        public static void DisableRagdoll()
        {
            foreach (Rigidbody part in new[] { ragdollBody, ragdollLeft, ragdollRight })
                if (part != null) UnityEngine.Object.Destroy(part.gameObject);
        }

        private static Rigidbody RagdollPart(PrimitiveType type, Vector3 position, Vector3 scale)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            UnityEngine.Object.Destroy(part.GetComponent<Renderer>());
            part.transform.position = position;
            part.transform.localScale = scale;
            Collider collider = part.GetComponent<Collider>();
            Physics.IgnoreCollision(collider, GorillaTagger.Instance.bodyCollider);
            Physics.IgnoreCollision(collider, GorillaTagger.Instance.headCollider);
            Rigidbody body = part.AddComponent<Rigidbody>();
            body.interpolation = RigidbodyInterpolation.Interpolate;
            return body;
        }

        private static VRRig beeTarget;
        private static float beeTime;

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
