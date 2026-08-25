using System.Collections.Generic;
using ShibaGTGenesisReborn.Menu;
using ShibaGTGenesisReborn.Mods;
using ShibaGTGenesisReborn.Mods.Custom;
using UnityEngine;

namespace ShibaGTGenesisReborn.Libs
{
    public class StreamerMode : MonoBehaviour
    {
        private static StreamerMode _instance;
        private static bool _isEnabled;
        private static readonly List<GameObject> _hiddenObjects = new List<GameObject>();
        private static readonly Dictionary<Camera, int> _originalCullingMasks = new Dictionary<Camera, int>();

        public static bool IsEnabled => _isEnabled;

        public static void EnsureInitialized()
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("StreamerModeManager");
                _instance = go.AddComponent<StreamerMode>();
                DontDestroyOnLoad(go);
            }
        }

        public static void Toggle() => SetEnabled(!_isEnabled);

        public static void Enable() => SetEnabled(true);

        public static void Disable() => SetEnabled(false);

        public static void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;
            Settings.streamerMode = enabled;
            EnsureInitialized();

            if (enabled)
            {
                ApplyCullingMasks();
                NotificationLib.SendNotification(NotificationLib.NotificationType.Info, "Streamer Mode: Enabled");
            }
            else
            {
                RestoreCullingMasks();
                NotificationLib.SendNotification(NotificationLib.NotificationType.Info, "Streamer Mode: Disabled");
            }
        }

        private void OnEnable()
        {
            Camera.onPreCull += OnCameraPreCull;
            Camera.onPostRender += OnCameraPostRender;
        }

        private void OnDisable()
        {
            Camera.onPreCull -= OnCameraPreCull;
            Camera.onPostRender -= OnCameraPostRender;
            RestoreCullingMasks();
        }

        private void Update()
        {
            if (_isEnabled && Time.frameCount % 60 == 0)
                ApplyCullingMasks();
        }

        public static bool IsRecordingCamera(Camera cam)
        {
            if (cam == null) return false;
            if (cam == Camera.main) return false;
            if (cam.stereoTargetEye != StereoTargetEyeMask.None) return false;
            return true;
        }

        private static void OnCameraPreCull(Camera cam)
        {
            if (!_isEnabled || !IsRecordingCamera(cam)) return;

            _hiddenObjects.Clear();

            HideObject(Main.menu);
            HideObject(Main.canvasObject);
            HideObject(Main.reference);
            HideObject(Main.menuBackground);
            HideObject(GunLib.spherepointer);

            if (NotificationLib.Instance != null && NotificationLib.Instance.RootHUD != null)
                HideObject(NotificationLib.Instance.RootHUD);

            HideObject(mods.PlatR);
            HideObject(mods.PlatL);

            HideObject(BoomboxManager.Obj);
            HideObject(GroshHolder.Obj);
            HideObject(SusTung.Obj);
            HideObject(Vape.Obj);
            HideObject(MaxwellHolder.Obj);
            HideObject(BombManager.activeBombObject);
            HideObject(StunGrenadeManager.heldGrenadeObject);
            HideObject(StunGrenadeManager.thrownGrenadeObject);

            if (FatSealSpammer.Seals != null)
            {
                for (int i = 0; i < FatSealSpammer.Seals.Count; i++)
                    HideObject(FatSealSpammer.Seals[i]);
            }

            HideByNamePrefix("Line");
            HideByNamePrefix("BeaconESP");
            HideByNamePrefix("BoxESP");
            HideByNamePrefix("Tracer");
        }

        private static void OnCameraPostRender(Camera cam)
        {
            if (!_isEnabled || !IsRecordingCamera(cam)) return;

            for (int i = 0; i < _hiddenObjects.Count; i++)
            {
                if (_hiddenObjects[i] != null)
                    _hiddenObjects[i].SetActive(true);
            }
            _hiddenObjects.Clear();
        }

        private static void HideObject(GameObject go)
        {
            if (go != null && go.activeSelf)
            {
                go.SetActive(false);
                _hiddenObjects.Add(go);
            }
        }

        private static void HideByNamePrefix(string prefix)
        {
            GameObject[] allObjs = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            for (int i = 0; i < allObjs.Length; i++)
            {
                GameObject obj = allObjs[i];
                if (obj != null && obj.name.StartsWith(prefix) && obj.activeSelf)
                {
                    obj.SetActive(false);
                    _hiddenObjects.Add(obj);
                }
            }
        }

        public static void ApplyCullingMasks()
        {
            foreach (Camera cam in Camera.allCameras)
            {
                if (IsRecordingCamera(cam))
                {
                    if (!_originalCullingMasks.ContainsKey(cam))
                        _originalCullingMasks[cam] = cam.cullingMask;

                    cam.cullingMask &= ~(1 << 2);
                }
            }
        }

        public static void RestoreCullingMasks()
        {
            foreach (var kvp in _originalCullingMasks)
            {
                if (kvp.Key != null)
                    kvp.Key.cullingMask = kvp.Value;
            }
            _originalCullingMasks.Clear();
        }
    }
}
