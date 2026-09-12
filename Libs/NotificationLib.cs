using ShibaGTGenesisReborn.Classes;
using ShibaGTGenesisReborn.Menu;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ShibaGTGenesisReborn.Libs
{
    public class NotificationLib : MonoBehaviour
    {
        public enum NotificationType
        {
            Enabled,
            Disabled,
            Saved,
            Loaded,
            Deleted,
            Room,
            Error,
            Alert,
            Info,
            AntiCheat
        }

        private static readonly Dictionary<string, float> _notificationTimestamps = new Dictionary<string, float>();
        private static readonly List<string> _expiredKeys = new List<string>(8);

        private const float DEFAULT_NOTIFICATION_TIME = 3f;
        private const float FADE_DURATION = 0.4f;

        private GameObject _hudObj;
        private GameObject _hudObj2;
        private GameObject _mainCamera;

        private Text _notificationText;
        private Material _notificationMaterial;
        private CanvasGroup _canvasGroup;

        private readonly List<GameObject> _trackedObjects = new List<GameObject>();
        private bool _hasInitialized;

        private float _currentAlpha;
        private float _targetAlpha;
        private Action _onFadeComplete;

        public static bool inRoom;
        public static bool RoomNotifications = true;

        private static readonly Dictionary<NotificationType, string> _typeColors = new Dictionary<NotificationType, string>
        {
            { NotificationType.Enabled, "#00FF00" },
            { NotificationType.Disabled, "#FF4040" },
            { NotificationType.Saved, "#00AAFF" },
            { NotificationType.Loaded, "#00FFFF" },
            { NotificationType.Deleted, "#FF8C00" },
            { NotificationType.Room, "#C040FF" },
            { NotificationType.Error, "#FF0000" },
            { NotificationType.Alert, "#FFD700" },
            { NotificationType.Info, "#B0B0B0" },
            { NotificationType.AntiCheat, "#FFD700" }
        };

        public static string PreviousNotification { get; private set; }
        public static bool IsEnabled { get; set; } = true;
        public static NotificationLib Instance { get; private set; }
        public GameObject RootHUD => _hudObj2;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            UpdateNotifications();
        }

        public void Init()
        {
            if (_hasInitialized) return;

            _mainCamera = GameObject.Find("Main Camera");
            if (_mainCamera == null) return;

            _hudObj2 = CreateAndTrackHUDObject("HUD_Notification_Parent");
            _hudObj2.layer = 2;
            _hudObj2.transform.position = _mainCamera.transform.position + new Vector3(-1.5f, 0f, -4.5f);

            _hudObj = CreateAndTrackHUDObject("HUD_Notification", _hudObj2.transform);
            _hudObj.layer = 2;

            Canvas canvas = _hudObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = _mainCamera.GetComponent<Camera>();

            _canvasGroup = _hudObj.AddComponent<CanvasGroup>();

            CanvasScaler scaler = _hudObj.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10f;

            _hudObj.AddComponent<GraphicRaycaster>();

            RectTransform rect = _hudObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(5f, 5f);
            rect.localScale = Vector3.one;
            rect.localPosition = new Vector3(0f, 0f, 1.6f);
            rect.rotation = Quaternion.Euler(0f, -250f, 0f);

            _notificationText = CreateTextElement("NotificationText", _hudObj, new Vector3(-1.2f, -0.75f, 0f), new Vector2(300f, 70f), 7);
            _notificationText.font = Settings.currentFont ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            _notificationText.fontStyle = FontStyle.Bold;
            _notificationText.alignment = TextAnchor.MiddleCenter;

            Shader shader = Shader.Find("GUI/Text Shader") ?? Shader.Find("UI/Default") ?? Shader.Find("Sprites/Default");
            if (shader != null)
            {
                _notificationMaterial = new Material(shader);
                _notificationText.material = _notificationMaterial;
            }

            SetAlpha(0f);
            _hudObj.SetActive(false);

            _hasInitialized = true;
        }

        private Text CreateTextElement(string name, GameObject parent, Vector3 position, Vector2 size, int fontSize)
        {
            GameObject obj = new GameObject(name);
            obj.transform.parent = parent.transform;

            Text text = obj.AddComponent<Text>();
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.rectTransform.sizeDelta = size;
            text.rectTransform.localScale = new Vector3(0.01f, 0.01f, 1f);
            text.rectTransform.localPosition = position;

            _trackedObjects.Add(obj);
            return text;
        }

        private GameObject CreateAndTrackHUDObject(string name, Transform parent = null)
        {
            GameObject obj = new GameObject(name);
            if (parent != null)
                obj.transform.parent = parent;

            _trackedObjects.Add(obj);
            return obj;
        }

        public void UpdateNotifications()
        {
            if (!_hasInitialized)
                Init();

            if (_mainCamera == null)
                _mainCamera = GameObject.Find("Main Camera");

            if (_hudObj2 != null && _mainCamera != null)
                _hudObj2.transform.SetPositionAndRotation(_mainCamera.transform.position, _mainCamera.transform.rotation);

            ProcessExpiredNotifications();
            UpdateFadeAnimation();
        }

        private void ProcessExpiredNotifications()
        {
            if (_notificationTimestamps.Count == 0)
                return;

            _expiredKeys.Clear();
            float time = Time.time;

            foreach (var notification in _notificationTimestamps)
            {
                if (time >= notification.Value)
                    _expiredKeys.Add(notification.Key);
            }

            if (_expiredKeys.Count > 0)
            {
                for (int i = 0; i < _expiredKeys.Count; i++)
                    _notificationTimestamps.Remove(_expiredKeys[i]);

                if (_notificationTimestamps.Count == 0)
                {
                    StartFade(0f, FADE_DURATION, UpdateNotificationText);
                }
                else
                {
                    UpdateNotificationText();
                }
            }
        }

        private void UpdateFadeAnimation()
        {
            if (Mathf.Approximately(_currentAlpha, _targetAlpha))
                return;

            float step = FADE_DURATION > 0f ? Time.deltaTime / FADE_DURATION : 1f;
            _currentAlpha = Mathf.MoveTowards(_currentAlpha, _targetAlpha, step);
            SetAlpha(_currentAlpha);

            if (Mathf.Approximately(_currentAlpha, _targetAlpha))
            {
                _currentAlpha = _targetAlpha;
                SetAlpha(_currentAlpha);

                Action callback = _onFadeComplete;
                _onFadeComplete = null;
                callback?.Invoke();

                if (_currentAlpha <= 0f)
                {
                    if (_hudObj != null && _hudObj.activeSelf)
                        _hudObj.SetActive(false);

                    PreviousNotification = null;
                }
            }
        }

        private void SetAlpha(float alpha)
        {
            float t = Mathf.Clamp01(alpha);
            float smoothAlpha = t * t * (3f - 2f * t);

            if (_canvasGroup != null)
                _canvasGroup.alpha = smoothAlpha;

            if (_notificationMaterial != null && _notificationMaterial.HasProperty("_Color"))
            {
                Color color = _notificationMaterial.color;
                color.a = smoothAlpha;
                _notificationMaterial.color = color;
            }

            if (_notificationText != null)
            {
                Color textColor = _notificationText.color;
                textColor.a = smoothAlpha;
                _notificationText.color = textColor;
            }
        }

        private void StartFade(float targetAlpha, float duration, Action onComplete = null)
        {
            _targetAlpha = targetAlpha;
            _onFadeComplete = onComplete;

            if (_targetAlpha > 0f && _hudObj != null && !_hudObj.activeSelf)
                _hudObj.SetActive(true);

            if (duration <= 0f)
            {
                _currentAlpha = targetAlpha;
                SetAlpha(targetAlpha);
                onComplete?.Invoke();
                _onFadeComplete = null;
            }
        }

        private void UpdateNotificationText()
        {
            if (_notificationText != null)
                _notificationText.text = string.Join(Environment.NewLine, _notificationTimestamps.Keys);
        }

        public static void SendNotification(NotificationType type, string content, float duration = DEFAULT_NOTIFICATION_TIME)
        {
            if (!IsEnabled || string.IsNullOrEmpty(content) || Instance == null)
                return;

            if (!Instance._hasInitialized)
                Instance.Init();

            if (Instance._notificationText == null)
                return;

            if (!_typeColors.TryGetValue(type, out string color))
                color = "#FFFFFF";

            string text = string.Format("<color={0}>{1}</color> : {2}", color, type.ToString(), content);

            if (text == PreviousNotification && _notificationTimestamps.ContainsKey(text))
                return;

            _notificationTimestamps[text] = Time.time + duration;
            PreviousNotification = text;

            Instance.UpdateNotificationText();
            Instance.StartFade(1f, FADE_DURATION);
        }

        public static void ClearAllNotifications()
        {
            _notificationTimestamps.Clear();
            PreviousNotification = null;

            if (Instance != null)
                Instance.StartFade(0f, FADE_DURATION, Instance.UpdateNotificationText);
        }

        private void OnDestroy()
        {
            if (_hudObj2 != null)
                Destroy(_hudObj2);

            if (_notificationMaterial != null)
                Destroy(_notificationMaterial);
        }
    }
}