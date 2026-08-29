using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace ShibaGTGenesisReborn.Libs
{
    public class GunLib
    {
        public static GameObject spherepointer;
        public static VRRig LockedPlayer;

        public static float GunLineWidth = 0.012f;
        public static float SphereSize = 0.15f;

        private static LineRenderer gunLine;

        public static readonly string[] bypassLayers =
        {
            "GorillaTrigger",
            "GorillaBoundary",
            "GorillaHand",
            "GorillaObject",
            "Zone",
            "Water",
            "GorillaCosmetics",
            "GorillaParticle",
        };

        public static LayerMask BypassLayers => ~LayerMask.GetMask(bypassLayers);

        public static Color GunColor =>
            Settings.backgroundColor.colors[0].color;

        public static Color PointerColor =>
            Color.Lerp(GunColor, Color.white, 0.35f);

        public static Color LockedColor => Color.black;

        public static void StartGun(Action action, bool lockOn)
        {
            if (IsXRDeviceActive())
                StartVrGun(action, lockOn);
            else
                StartPcGun(action, lockOn);
        }

        public static Camera GetPcCamera()
        {
            if (GorillaTagger.Instance?.thirdPersonCamera != null)
            {
                Camera cam = GorillaTagger.Instance.thirdPersonCamera.GetComponentInChildren<Camera>();
                if (cam != null && cam.isActiveAndEnabled)
                    return cam;
            }

            return Camera.main ?? GorillaTagger.Instance?.mainCamera?.GetComponent<Camera>();
        }

        public static void StartPcGun(Action action, bool lockOn)
        {
            if (Mouse.current == null || !Mouse.current.rightButton.isPressed)
            {
                CleanupPointer();
                return;
            }

            Camera cam = GetPcCamera();
            if (cam == null)
                return;

            Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());

            if (!Physics.Raycast(ray, out RaycastHit hit, 1000f, BypassLayers, QueryTriggerInteraction.Collide))
                hit.point = ray.origin + ray.direction * 100f;

            Vector3 start = GorillaTagger.Instance?.rightHandTransform != null
                ? GorillaTagger.Instance.rightHandTransform.position
                : cam.transform.position;

            UpdateGun(ray, hit, action, lockOn, start);
        }

        public static void StartVrGun(Action action, bool lockOn)
        {
            if (!InputHandler.Instance.RightGrip.IsPressed)
            {
                CleanupPointer();
                return;
            }

            Transform hand = GorillaTagger.Instance.rightHandTransform;
            if (hand == null)
                return;

            Ray ray = new Ray(hand.position, -hand.up);

            if (!Physics.Raycast(ray, out RaycastHit hit, 1000f, BypassLayers, QueryTriggerInteraction.Collide))
                hit.point = ray.origin + ray.direction * 100f;

            UpdateGun(ray, hit, action, lockOn, hand.position);
        }

        private static void UpdateGun(Ray ray, RaycastHit hit, Action action, bool lockOn, Vector3 start)
        {
            if (spherepointer == null)
                CreatePointer();

            bool pressed = IsXRDeviceActive()
                ? InputHandler.Instance.RightTrigger.IsPressed
                : Mouse.current != null && Mouse.current.leftButton.isPressed;

            if (pressed)
            {
                if (lockOn && LockedPlayer == null)
                {
                    VRRig targetRig = hit.collider?.GetComponentInParent<VRRig>();
                    if (targetRig == null || targetRig.isLocal || targetRig == VRRig.LocalRig)
                    {
                        float closestRayDist = 0.65f;
                        foreach (VRRig rig in VRRigCache.ActiveRigs)
                        {
                            if (rig == null || rig.isLocal || rig == VRRig.LocalRig) continue;
                            Vector3 rigPos = rig.headConstraint != null ? rig.headConstraint.position : rig.transform.position;
                            float rayDist = Vector3.Cross(ray.direction, rigPos - ray.origin).magnitude;
                            float distAlongRay = Vector3.Dot(ray.direction, rigPos - ray.origin);
                            if (distAlongRay > 0f && rayDist < closestRayDist)
                            {
                                closestRayDist = rayDist;
                                targetRig = rig;
                            }
                        }
                    }

                    if (targetRig != null && !targetRig.isLocal && targetRig != VRRig.LocalRig)
                        LockedPlayer = targetRig;
                }

                if (!lockOn || LockedPlayer != null)
                    action.Invoke();
            }
            else
            {
                LockedPlayer = null;
            }

            Vector3 pos = (lockOn && LockedPlayer != null)
                ? (LockedPlayer.headConstraint != null ? LockedPlayer.headConstraint.position : LockedPlayer.transform.position)
                : hit.point;

            spherepointer.transform.position = pos;
            spherepointer.GetComponent<Renderer>().material.color = (lockOn && LockedPlayer != null) ? LockedColor : PointerColor;

            UpdateLine(start, pos);
        }

        private static void CreatePointer()
        {
            spherepointer = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            UnityEngine.Object.Destroy(spherepointer.GetComponent<Collider>());
            spherepointer.transform.localScale = Vector3.one * SphereSize;
            spherepointer.GetComponent<Renderer>().material.shader = Shader.Find("GUI/Text Shader");
            CreateLine();
        }

        private static void CreateLine()
        {
            GameObject obj = new GameObject("GunLine");
            gunLine = obj.AddComponent<LineRenderer>();
            gunLine.positionCount = 2;
            gunLine.startWidth = GunLineWidth;
            gunLine.endWidth = GunLineWidth;
            gunLine.material = new Material(Shader.Find("GUI/Text Shader"));
            gunLine.numCapVertices = 5;
            gunLine.numCornerVertices = 5;
        }

        private static void UpdateLine(Vector3 start, Vector3 end)
        {
            if (gunLine == null)
                return;

            gunLine.SetPosition(0, start);
            gunLine.SetPosition(1, end);
            gunLine.startWidth = GunLineWidth;
            gunLine.endWidth = GunLineWidth;
            gunLine.startColor = GunColor;
            gunLine.endColor = GunColor;
        }

        public static Vector3 GetPointerPos() =>
            spherepointer != null ? spherepointer.transform.position : Vector3.zero;

        public static void ChangeGunLineSize(bool increase)
        {
            GunLineWidth = Mathf.Clamp(GunLineWidth + (increase ? 0.002f : -0.002f), 0.001f, 0.05f);
        }

        public static void ChangeGunSphereScale(bool increase)
        {
            SphereSize = Mathf.Clamp(SphereSize + (increase ? 0.02f : -0.02f), 0.05f, 0.5f);
            if (spherepointer != null)
                spherepointer.transform.localScale = Vector3.one * SphereSize;
        }

        public static void ResetGunDefaults()
        {
            GunLineWidth = 0.012f;
            SphereSize = 0.15f;
            if (spherepointer != null)
                spherepointer.transform.localScale = Vector3.one * SphereSize;
        }

        public static void CleanupPointer()
        {
            if (spherepointer != null)
                UnityEngine.Object.Destroy(spherepointer);
            if (gunLine != null)
                UnityEngine.Object.Destroy(gunLine.gameObject);

            spherepointer = null;
            gunLine = null;
            LockedPlayer = null;
        }

        public static bool IsXRDeviceActive()
        {
            List<XRDisplaySubsystem> list = new List<XRDisplaySubsystem>();
            SubsystemManager.GetSubsystems(list);
            foreach (XRDisplaySubsystem xr in list)
            {
                if (xr.running)
                    return true;
            }
            return false;
        }
    }
}