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
                if (cam != null)
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

            if (!Physics.Raycast(ray, out RaycastHit hit, 1000f, BypassLayers, QueryTriggerInteraction.Ignore))
                return;

            Vector3 start = GorillaTagger.Instance?.rightHandTransform != null
                ? GorillaTagger.Instance.rightHandTransform.position
                : cam.transform.position;

            UpdateGun(hit, action, lockOn, start);
        }

        public static void StartVrGun(Action action, bool lockOn)
        {
            if (!InputHandler.Instance.RightGrip.IsPressed)
            {
                CleanupPointer();
                return;
            }

            Transform hand = GorillaTagger.Instance.rightHandTransform;

            if (!Physics.Raycast(hand.position, -hand.up, out RaycastHit hit, 1000f, BypassLayers, QueryTriggerInteraction.Ignore))
                return;

            UpdateGun(hit, action, lockOn, hand.position);
        }

        private static void UpdateGun(RaycastHit hit, Action action, bool lockOn, Vector3 start)
        {
            if (spherepointer == null)
                CreatePointer();

            if (lockOn && LockedPlayer == null)
            {
                VRRig rig = hit.collider.GetComponentInParent<VRRig>();
                if (rig != null && rig != GorillaTagger.Instance.offlineVRRig)
                    LockedPlayer = rig;
            }

            Vector3 pos = LockedPlayer != null
                ? LockedPlayer.transform.position
                : hit.point;

            spherepointer.transform.position = pos;
            spherepointer.GetComponent<Renderer>().material.color = PointerColor;

            UpdateLine(start, pos);

            bool pressed = IsXRDeviceActive()
                ? InputHandler.Instance.RightTrigger.IsPressed
                : Mouse.current != null && Mouse.current.leftButton.isPressed;

            if (pressed)
            {
                if (!lockOn || LockedPlayer != null)
                    action.Invoke();
            }
            else
            {
                LockedPlayer = null;
            }
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
            SubsystemManager.GetInstances(list);
            foreach (XRDisplaySubsystem xr in list)
            {
                if (xr.running)
                    return true;
            }
            return false;
        }
    }
}