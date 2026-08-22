using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;
using GorillaLocomotion;
using Photon.Pun;
using Photon.Realtime;
using ShibaGTGenesisReborn.Mods;
using ShibaGTGenesisReborn.Mods.Custom;
using UnityEngine;
using UnityEngine.Networking;

namespace ShibaGTGenesisReborn.Libs
{
    public class NetworkingLibrary : MonoBehaviour
    {
        public static NetworkingLibrary Instance { get; private set; }
        public bool NetworkEnabled = true;
        public bool DebugMode = false;
        
        private const byte NetworkByte = 68;
        private const string SyncEvent = "sync";
        private const string DestroyEvent = "destroy";
        private const string RequestEvent = "requestsync";
        private const string ScaleEvent = "scale";
        private const string AudioEvent = "audio";
        private const string AudioClipEvent = "audioclip";
        private const string VapeSmokeEvent = "vapesmoke";
        private const string VisualizerEvent = "visualizer";
        private const string BoomboxAudioEvent = "boomboxaudio";
        
        private readonly Dictionary<string, NetworkedObject> trackedObjects = new Dictionary<string, NetworkedObject>();
        private readonly HashSet<string> pendingSync = new HashSet<string>();
        private float lastSyncTime;
        private const float syncInterval = 0.05f;
        private int eventCount;
        private int syncCount;
        private readonly Dictionary<string, AudioClip> audioClipCache = new Dictionary<string, AudioClip>();
        private bool isSubscribedToPhoton;
        
        private class NetworkedObject
        {
            public GameObject gameObject;
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 scale;
            public string ownerId;
            public float lastUpdate;
            public bool isHeld;
            public bool audioPlaying;
            public float audioTime;
            public string audioClipUrl;
            public bool isVapeSmoking;
            public float visualizerIntensity;
            public bool audioSynced;
        }

        void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
            StartCoroutine(InitNetworkSubscriptions());
        }

        private IEnumerator InitNetworkSubscriptions()
        {
            while (!isSubscribedToPhoton)
            {
                if (PhotonNetwork.NetworkingClient != null)
                {
                    PhotonNetwork.NetworkingClient.EventReceived += OnEventReceived;
                    isSubscribedToPhoton = true;
                }
                else
                {
                    yield return new WaitForSeconds(0.5f);
                }
            }

            while (NetworkSystem.Instance == null)
                yield return new WaitForSeconds(0.5f);

            NetworkSystem.Instance.OnPlayerJoined += OnPlayerJoined;
            NetworkSystem.Instance.OnPlayerLeft += OnPlayerLeft;
        }

        void OnDestroy()
        {
            if (isSubscribedToPhoton && PhotonNetwork.NetworkingClient != null)
                PhotonNetwork.NetworkingClient.EventReceived -= OnEventReceived;

            if (NetworkSystem.Instance != null)
            {
                NetworkSystem.Instance.OnPlayerJoined -= OnPlayerJoined;
                NetworkSystem.Instance.OnPlayerLeft -= OnPlayerLeft;
            }
        }

        void Update()
        {
            if (!NetworkEnabled || NetworkSystem.Instance?.InRoom != true || trackedObjects.Count == 0) 
                return;
            
            if (Time.time - lastSyncTime >= syncInterval)
            {
                SendPendingUpdates();
                lastSyncTime = Time.time;
            }
            
            CleanupDestroyedObjects();
            
            foreach (var kvp in trackedObjects)
            {
                if (kvp.Value.gameObject == null) continue;
                
                if (kvp.Value.gameObject.name.Contains("Boombox"))
                {
                    AudioSource aud = kvp.Value.gameObject.GetComponent<AudioSource>();
                    if (aud != null)
                    {
                        bool isPlaying = aud.isPlaying;
                        if (isPlaying != kvp.Value.audioPlaying || Mathf.Abs(aud.time - kvp.Value.audioTime) > 0.1f)
                        {
                            kvp.Value.audioPlaying = isPlaying;
                            kvp.Value.audioTime = aud.time;
                            if (kvp.Value.ownerId == PhotonNetwork.LocalPlayer?.UserId)
                                SendEvent(BoomboxAudioEvent, ReceiverGroup.Others, kvp.Key, isPlaying, aud.time, aud.volume, aud.pitch);
                        }
                    }
                }
                
                if (kvp.Value.gameObject.name.Contains("Vape"))
                {
                    bool isSmoking = Vape.isExhaling;
                    if (isSmoking != kvp.Value.isVapeSmoking)
                    {
                        kvp.Value.isVapeSmoking = isSmoking;
                        if (kvp.Value.ownerId == PhotonNetwork.LocalPlayer?.UserId)
                            SendEvent(VapeSmokeEvent, ReceiverGroup.Others, kvp.Key, isSmoking);
                    }
                }
            }
        }

        private void OnEventReceived(EventData data)
        {
            if (data.Code != NetworkByte || !NetworkEnabled) 
                return;
            
            try
            {
                if (!(data.CustomData is object[] args) || args.Length == 0) 
                    return;
                
                string command = args[0] as string;
                Player sender = PhotonNetwork.NetworkingClient.CurrentRoom?.GetPlayer(data.Sender);
                
                if (sender == null || sender.UserId == PhotonNetwork.LocalPlayer?.UserId) 
                    return;
                
                eventCount++;
                
                switch (command)
                {
                    case SyncEvent:
                        HandleSync(args);
                        break;
                    case DestroyEvent:
                        HandleDestroy(args);
                        break;
                    case RequestEvent:
                        HandleRequest(sender);
                        break;
                    case ScaleEvent:
                        HandleScale(args);
                        break;
                    case AudioEvent:
                        HandleAudio(args);
                        break;
                    case AudioClipEvent:
                        HandleAudioClip(args);
                        break;
                    case VapeSmokeEvent:
                        HandleVapeSmoke(args);
                        break;
                    case VisualizerEvent:
                        HandleVisualizer(args);
                        break;
                    case BoomboxAudioEvent:
                        HandleBoomboxAudio(args);
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Event error: {e.Message}");
            }
        }

        private void HandleSync(object[] args)
        {
            if (args.Length < 5) 
                return;
            
            string objectId = args[1] as string;
            Vector3 position = (Vector3)args[2];
            Quaternion rotation = (Quaternion)args[3];
            string ownerId = args[4] as string;
            string propName = args.Length > 5 ? args[5] as string : "";
            
            GameObject obj = FindTrackedObject(objectId);
            if (obj != null)
            {
                obj.transform.position = Vector3.Lerp(obj.transform.position, position, 0.5f);
                obj.transform.rotation = Quaternion.Slerp(obj.transform.rotation, rotation, 0.5f);
                
                if (trackedObjects.TryGetValue(objectId, out NetworkedObject info))
                {
                    info.position = position;
                    info.rotation = rotation;
                    info.lastUpdate = Time.time;
                }
            }
            else
            {
                TryCreateNetworkedObject(objectId, position, rotation, ownerId, propName);
            }
        }

        private void HandleScale(object[] args)
        {
            if (args.Length < 3) 
                return;
            
            string objectId = args[1] as string;
            Vector3 scale = (Vector3)args[2];
            
            GameObject obj = FindTrackedObject(objectId);
            if (obj != null)
            {
                obj.transform.localScale = scale;
                if (trackedObjects.TryGetValue(objectId, out NetworkedObject info))
                    info.scale = scale;
            }
        }

        private void HandleAudio(object[] args)
        {
            if (args.Length < 4) 
                return;
            
            string objectId = args[1] as string;
            bool isPlaying = (bool)args[2];
            float time = (float)args[3];
            
            GameObject obj = FindTrackedObject(objectId);
            if (obj != null)
            {
                AudioSource aud = obj.GetComponent<AudioSource>();
                if (aud != null)
                {
                    if (isPlaying && !aud.isPlaying)
                    {
                        aud.time = time;
                        aud.Play();
                    }
                    else if (!isPlaying && aud.isPlaying)
                    {
                        aud.Stop();
                    }
                }
            }
        }

        private void HandleAudioClip(object[] args)
        {
            if (args.Length < 3) 
                return;
            
            string objectId = args[1] as string;
            string clipUrl = args[2] as string;
            
            GameObject obj = FindTrackedObject(objectId);
            if (obj != null)
            {
                AudioSource aud = obj.GetComponent<AudioSource>();
                if (aud != null && !string.IsNullOrEmpty(clipUrl))
                {
                    if (audioClipCache.TryGetValue(clipUrl, out AudioClip clip))
                    {
                        aud.clip = clip;
                    }
                    else
                    {
                        MonoBehaviour mb = obj.GetComponent<MonoBehaviour>() ?? this;
                        mb.StartCoroutine(LoadAudioClip(clipUrl, aud));
                    }
                }
            }
        }

        private IEnumerator LoadAudioClip(string url, AudioSource aud)
        {
            using UnityWebRequest req = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.WAV);
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(req);
                if (clip != null)
                {
                    audioClipCache[url] = clip;
                    if (aud != null) aud.clip = clip;
                }
            }
        }

        private void HandleVapeSmoke(object[] args)
        {
            if (args.Length < 3) 
                return;
            
            string objectId = args[1] as string;
            bool isSmoking = (bool)args[2];
            
            GameObject obj = FindTrackedObject(objectId);
            if (obj != null && obj.name.Contains("Vape") && isSmoking)
                Vape.TriggerExhale();
        }

        private void HandleVisualizer(object[] args)
        {
            if (args.Length < 3) 
                return;
            
            float intensity = (float)args[2];
            BoomboxManager.VisualizerIntensity = intensity;
        }

        private void HandleBoomboxAudio(object[] args)
        {
            if (args.Length < 6) 
                return;
            
            string objectId = args[1] as string;
            bool isPlaying = (bool)args[2];
            float time = (float)args[3];
            float volume = (float)args[4];
            float pitch = (float)args[5];
            
            GameObject obj = FindTrackedObject(objectId);
            if (obj != null && obj.name.Contains("Boombox"))
            {
                AudioSource aud = obj.GetComponent<AudioSource>();
                if (aud != null)
                {
                    aud.volume = volume;
                    aud.pitch = pitch;
                    
                    if (isPlaying && !aud.isPlaying)
                    {
                        aud.time = time;
                        aud.Play();
                    }
                    else if (!isPlaying && aud.isPlaying)
                    {
                        aud.Stop();
                    }
                }
            }
        }

        private void HandleDestroy(object[] args)
        {
            if (args.Length < 2) 
                return;
            
            string objectId = args[1] as string;
            DestroyTrackedObject(objectId);
        }

        private void HandleRequest(Player sender)
        {
            if (trackedObjects.Count == 0) 
                return;
            
            foreach (var kvp in trackedObjects)
            {
                if (kvp.Value.gameObject == null) continue;
                SendEvent(SyncEvent, sender.ActorNumber, 
                    kvp.Key, 
                    kvp.Value.gameObject.transform.position, 
                    kvp.Value.gameObject.transform.rotation, 
                    kvp.Value.ownerId,
                    kvp.Value.gameObject.name);
                
                if (kvp.Value.scale != Vector3.one)
                    SendEvent(ScaleEvent, sender.ActorNumber, kvp.Key, kvp.Value.scale);
                
                if (kvp.Value.gameObject.name.Contains("Boombox"))
                {
                    AudioSource aud = kvp.Value.gameObject.GetComponent<AudioSource>();
                    if (aud != null)
                        SendEvent(BoomboxAudioEvent, sender.ActorNumber, kvp.Key, aud.isPlaying, aud.time, aud.volume, aud.pitch);
                }
                
                if (kvp.Value.gameObject.name.Contains("Vape"))
                    SendEvent(VapeSmokeEvent, sender.ActorNumber, kvp.Key, Vape.isExhaling);
            }
        }

        private void OnPlayerJoined(NetPlayer player)
        {
            if (!NetworkEnabled || NetworkSystem.Instance?.InRoom != true) 
                return;
            
            SendEvent(RequestEvent, player.ActorNumber);
            
            if (trackedObjects.Count == 0) 
                return;
            
            foreach (var kvp in trackedObjects)
            {
                if (kvp.Value.gameObject == null) continue;
                SendEvent(SyncEvent, player.ActorNumber,
                    kvp.Key,
                    kvp.Value.gameObject.transform.position,
                    kvp.Value.gameObject.transform.rotation,
                    kvp.Value.ownerId,
                    kvp.Value.gameObject.name);
                
                if (kvp.Value.scale != Vector3.one)
                    SendEvent(ScaleEvent, player.ActorNumber, kvp.Key, kvp.Value.scale);
            }
        }

        private void OnPlayerLeft(NetPlayer player)
        {
            List<string> toRemove = new List<string>();
            foreach (var kvp in trackedObjects)
            {
                if (kvp.Value.ownerId == player.UserId)
                    toRemove.Add(kvp.Key);
            }
            
            foreach (string id in toRemove)
                DestroyTrackedObject(id);
        }

        public void RegisterObject(GameObject obj)
        {
            if (obj == null || !NetworkEnabled || NetworkSystem.Instance?.InRoom != true) 
                return;
            
            string objectId = FindObjectId(obj);
            if (string.IsNullOrEmpty(objectId))
                objectId = GenerateObjectId();
            
            NetworkedObject info = new NetworkedObject
            {
                gameObject = obj,
                position = obj.transform.position,
                rotation = obj.transform.rotation,
                scale = obj.transform.localScale,
                ownerId = PhotonNetwork.LocalPlayer?.UserId,
                lastUpdate = Time.time,
                isHeld = false,
                audioPlaying = false,
                audioTime = 0f,
                isVapeSmoking = false,
                visualizerIntensity = 0f
            };
            
            trackedObjects[objectId] = info;
            pendingSync.Add(objectId);
            
            SendEvent(SyncEvent, ReceiverGroup.Others,
                objectId,
                info.position,
                info.rotation,
                info.ownerId,
                obj.name);
            
            if (info.scale != Vector3.one)
                SendEvent(ScaleEvent, ReceiverGroup.Others, objectId, info.scale);
            
            if (obj.name.Contains("Boombox"))
            {
                AudioSource aud = obj.GetComponent<AudioSource>();
                if (aud != null && aud.clip != null)
                {
                    string clipUrl = BoomboxManager.P_Aud;
                    if (!string.IsNullOrEmpty(clipUrl))
                        SendEvent(AudioClipEvent, ReceiverGroup.Others, objectId, clipUrl);
                }
            }
        }

        public void UnregisterObject(GameObject obj)
        {
            if (obj == null) 
                return;
            
            string objectId = FindObjectId(obj);
            if (!string.IsNullOrEmpty(objectId))
                DestroyTrackedObject(objectId);
        }

        public void UpdateObjectPosition(GameObject obj)
        {
            if (obj == null || !NetworkEnabled) 
                return;
            
            string objectId = FindObjectId(obj);
            if (!string.IsNullOrEmpty(objectId) && trackedObjects.TryGetValue(objectId, out NetworkedObject info))
            {
                info.position = obj.transform.position;
                info.rotation = obj.transform.rotation;
                info.lastUpdate = Time.time;
                pendingSync.Add(objectId);
            }
        }

        public void UpdateObjectScale(GameObject obj)
        {
            if (obj == null || !NetworkEnabled) 
                return;
            
            string objectId = FindObjectId(obj);
            if (!string.IsNullOrEmpty(objectId) && trackedObjects.TryGetValue(objectId, out NetworkedObject info))
            {
                info.scale = obj.transform.localScale;
                SendEvent(ScaleEvent, ReceiverGroup.Others, objectId, info.scale);
            }
        }

        public void SyncBoomboxAudio(GameObject obj)
        {
            if (obj == null || !NetworkEnabled) 
                return;
            
            string objectId = FindObjectId(obj);
            if (!string.IsNullOrEmpty(objectId) && trackedObjects.TryGetValue(objectId, out NetworkedObject info))
            {
                AudioSource aud = obj.GetComponent<AudioSource>();
                if (aud != null)
                {
                    info.audioPlaying = aud.isPlaying;
                    info.audioTime = aud.time;
                    SendEvent(BoomboxAudioEvent, ReceiverGroup.Others, objectId, aud.isPlaying, aud.time, aud.volume, aud.pitch);
                }
            }
        }

        public void SyncVapeSmoke(GameObject obj, bool isSmoking)
        {
            if (obj == null || !NetworkEnabled) 
                return;
            
            string objectId = FindObjectId(obj);
            if (!string.IsNullOrEmpty(objectId) && trackedObjects.TryGetValue(objectId, out NetworkedObject info))
            {
                info.isVapeSmoking = isSmoking;
                SendEvent(VapeSmokeEvent, ReceiverGroup.Others, objectId, isSmoking);
            }
        }

        public void SendEvent(string command, ReceiverGroup target, params object[] parameters)
        {
            if (NetworkSystem.Instance?.InRoom != true) 
                return;
            
            object[] data = new object[] { command }.Concat(parameters).ToArray();
            PhotonNetwork.RaiseEvent(NetworkByte, data, 
                new RaiseEventOptions { Receivers = target }, 
                SendOptions.SendReliable);
        }

        public void SendEvent(string command, int targetActor, params object[] parameters)
        {
            if (NetworkSystem.Instance?.InRoom != true) 
                return;
            
            object[] data = new object[] { command }.Concat(parameters).ToArray();
            PhotonNetwork.RaiseEvent(NetworkByte, data,
                new RaiseEventOptions { TargetActors = new[] { targetActor } },
                SendOptions.SendReliable);
        }

        public string FindObjectId(GameObject obj)
        {
            if (obj == null) 
                return null;
            
            foreach (var kvp in trackedObjects)
            {
                if (kvp.Value.gameObject == obj)
                    return kvp.Key;
            }
            
            return null;
        }

        public bool IsObjectTracked(GameObject obj) =>
            !string.IsNullOrEmpty(FindObjectId(obj));

        public int GetTrackedCount() =>
            trackedObjects.Count;

        public void ToggleNetwork(bool enable)
        {
            NetworkEnabled = enable;
            
            if (enable && NetworkSystem.Instance?.InRoom == true)
            {
                SendEvent(RequestEvent, ReceiverGroup.Others);
            }
            else if (!enable)
            {
                List<string> keys = trackedObjects.Keys.ToList();
                foreach (string key in keys)
                    DestroyTrackedObject(key);
                
                trackedObjects.Clear();
                pendingSync.Clear();
            }
        }

        private void SendPendingUpdates()
        {
            if (pendingSync.Count == 0) 
                return;
            
            foreach (string objectId in pendingSync.ToList())
            {
                if (trackedObjects.TryGetValue(objectId, out NetworkedObject info) && info.gameObject != null)
                {
                    SendEvent(SyncEvent, ReceiverGroup.Others,
                        objectId,
                        info.gameObject.transform.position,
                        info.gameObject.transform.rotation,
                        info.ownerId,
                        info.gameObject.name);
                    syncCount++;
                }
            }
            
            pendingSync.Clear();
        }

        private void DestroyTrackedObject(string objectId)
        {
            if (!trackedObjects.TryGetValue(objectId, out NetworkedObject info)) 
                return;
            
            if (info.ownerId == PhotonNetwork.LocalPlayer?.UserId)
                SendEvent(DestroyEvent, ReceiverGroup.Others, objectId);
            else if (info.gameObject != null && info.gameObject.name.EndsWith("_Remote"))
                Destroy(info.gameObject);
            
            trackedObjects.Remove(objectId);
            pendingSync.Remove(objectId);
        }

        private void CleanupDestroyedObjects()
        {
            List<string> toRemove = new List<string>();
            foreach (var kvp in trackedObjects)
            {
                if (kvp.Value.gameObject == null)
                    toRemove.Add(kvp.Key);
            }
            
            foreach (string id in toRemove)
            {
                trackedObjects.Remove(id);
                pendingSync.Remove(id);
                
                if (NetworkEnabled && NetworkSystem.Instance?.InRoom == true)
                    SendEvent(DestroyEvent, ReceiverGroup.Others, id);
            }
        }

        private GameObject FindTrackedObject(string objectId) =>
            trackedObjects.TryGetValue(objectId, out NetworkedObject info) ? info.gameObject : null;

        private void TryCreateNetworkedObject(string objectId, Vector3 position, Quaternion rotation, string ownerId, string propName = "")
        {
            GameObject obj = CreateRemoteObject(propName, position, rotation);
            if (obj == null) 
                return;
            
            NetworkedObject info = new NetworkedObject
            {
                gameObject = obj,
                position = position,
                rotation = rotation,
                scale = obj.transform.localScale,
                ownerId = ownerId,
                lastUpdate = Time.time
            };
            
            trackedObjects[objectId] = info;
        }

        private GameObject CreateRemoteObject(string propName, Vector3 position, Quaternion rotation)
        {
            GameObject obj;
            Mesh mesh = null;
            Texture2D texture = null;

            if (propName.Contains("Boombox"))
            {
                mesh = BoomboxManager.CM;
                texture = BoomboxManager.CT;
            }
            else if (propName.Contains("Maxwell"))
            {
                mesh = MaxwellHolder.CM;
                texture = MaxwellHolder.CT;
            }
            else if (propName.Contains("Grosh"))
            {
                mesh = GroshHolder.CM;
                texture = GroshHolder.CT;
            }
            else if (propName.Contains("Tung"))
            {
                mesh = SusTung.CM;
                texture = SusTung.CT;
            }
            else if (propName.Contains("Vape"))
            {
                mesh = Vape.CM;
                texture = Vape.CT;
            }
            else if (propName.Contains("Seal"))
            {
                mesh = FatSealSpammer.CM;
                texture = FatSealSpammer.CT;
            }
            else if (propName.Contains("Bomb"))
            {
                mesh = BombManager.bombMesh;
                texture = BombManager.bombTexture;
            }

            if (mesh != null)
            {
                obj = new GameObject(propName + "_Remote");
                MeshFilter mf = obj.AddComponent<MeshFilter>();
                mf.mesh = mesh;
                MeshRenderer mr = obj.AddComponent<MeshRenderer>();
                Material mat = new Material(Shader.Find("Gorilla/UberShader") ?? Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit"));
                if (texture != null) mat.mainTexture = texture;
                mr.material = mat;
            }
            else
            {
                obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obj.name = propName + "_Remote";
                if (obj.TryGetComponent<Collider>(out var c)) Destroy(c);
            }

            obj.transform.position = position;
            obj.transform.rotation = rotation;
            return obj;
        }

        private string GenerateObjectId() =>
            Guid.NewGuid().ToString("N").Substring(0, 16);
    }

    public static class NetworkExtensions
    {
        public static void RegisterForNetwork(this GameObject obj)
        {
            if (NetworkingLibrary.Instance != null && NetworkingLibrary.Instance.NetworkEnabled)
                NetworkingLibrary.Instance.RegisterObject(obj);
        }
        
        public static void UnregisterFromNetwork(this GameObject obj)
        {
            if (NetworkingLibrary.Instance != null)
                NetworkingLibrary.Instance.UnregisterObject(obj);
        }
        
        public static void UpdateNetworkPosition(this GameObject obj)
        {
            if (NetworkingLibrary.Instance != null && NetworkingLibrary.Instance.NetworkEnabled)
                NetworkingLibrary.Instance.UpdateObjectPosition(obj);
        }
        
        public static void UpdateNetworkScale(this GameObject obj)
        {
            if (NetworkingLibrary.Instance != null && NetworkingLibrary.Instance.NetworkEnabled)
                NetworkingLibrary.Instance.UpdateObjectScale(obj);
        }
        
        public static void SyncBoomboxAudio(this GameObject obj)
        {
            if (NetworkingLibrary.Instance != null && NetworkingLibrary.Instance.NetworkEnabled)
                NetworkingLibrary.Instance.SyncBoomboxAudio(obj);
        }
        
        public static void SyncVapeSmoke(this GameObject obj, bool isSmoking)
        {
            if (NetworkingLibrary.Instance != null && NetworkingLibrary.Instance.NetworkEnabled)
                NetworkingLibrary.Instance.SyncVapeSmoke(obj, isSmoking);
        }
        
        public static bool IsNetworked(this GameObject obj) =>
            NetworkingLibrary.Instance != null && NetworkingLibrary.Instance.IsObjectTracked(obj);
    }
}