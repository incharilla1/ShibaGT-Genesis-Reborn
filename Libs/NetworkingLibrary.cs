using GorillaNetworking;
using Photon.Pun;
using Photon.Realtime;
using ShibaGTGenesisReborn.Menu;
using ShibaGTGenesisReborn.Mods;
using ShibaGTGenesisReborn.Mods.Custom;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        private const string CosmeticSyncEvent = "cosmeticsync";
        
        private readonly Dictionary<string, NetworkedObject> trackedObjects = new Dictionary<string, NetworkedObject>();
        private readonly HashSet<string> pendingSync = new HashSet<string>();
        private float lastSyncTime;
        private const float syncInterval = 0.05f;
        private int eventCount;
        private int syncCount;
        private readonly Dictionary<string, AudioClip> audioClipCache = new Dictionary<string, AudioClip>();
        private bool isSubscribed;
        
        private class NetworkedObject
        {
            public GameObject gameObject;
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 targetPosition;
            public Quaternion targetRotation;
            public Vector3 scale;
            public int ownerActorNumber;
            public string propName;
            public float lastUpdate;
            public bool isHeld;
            public bool audioPlaying;
            public float audioTime;
            public bool isVapeSmoking;
            public float visualizerIntensity;
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
            while (NetworkSystem.Instance == null)
                yield return new WaitForSeconds(0.5f);

            if (!isSubscribed)
            {
                NetworkSystem.Instance.OnRaiseEvent += OnEventRaised;
                NetworkSystem.Instance.OnPlayerJoined += OnPlayerJoined;
                NetworkSystem.Instance.OnPlayerLeft += OnPlayerLeft;
                NetworkSystem.Instance.OnJoinedRoomEvent += OnLocalJoinedRoom;
                isSubscribed = true;
            }
        }

        void OnDestroy()
        {
            if (isSubscribed && NetworkSystem.Instance != null)
            {
                NetworkSystem.Instance.OnRaiseEvent -= OnEventRaised;
                NetworkSystem.Instance.OnPlayerJoined -= OnPlayerJoined;
                NetworkSystem.Instance.OnPlayerLeft -= OnPlayerLeft;
                NetworkSystem.Instance.OnJoinedRoomEvent -= OnLocalJoinedRoom;
                isSubscribed = false;
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
            int localId = GetLocalPlayerId();
            
            foreach (var kvp in trackedObjects)
            {
                var info = kvp.Value;
                if (info.gameObject == null) continue;
                
                if (info.ownerActorNumber != localId)
                {
                    float dist = Vector3.Distance(info.gameObject.transform.position, info.targetPosition);
                    if (dist > 5f)
                    {
                        info.gameObject.transform.position = info.targetPosition;
                        info.gameObject.transform.rotation = info.targetRotation;
                    }
                    else
                    {
                        info.gameObject.transform.position = Vector3.Lerp(info.gameObject.transform.position, info.targetPosition, Time.deltaTime * 20f);
                        info.gameObject.transform.rotation = Quaternion.Slerp(info.gameObject.transform.rotation, info.targetRotation, Time.deltaTime * 20f);
                    }
                    continue;
                }
                
                if (info.gameObject.name.Contains("Boombox"))
                {
                    AudioSource aud = info.gameObject.GetComponent<AudioSource>();
                    if (aud != null)
                    {
                        bool isPlaying = aud.isPlaying;
                        if (isPlaying != info.audioPlaying || Mathf.Abs(aud.time - info.audioTime) > 0.1f)
                        {
                            info.audioPlaying = isPlaying;
                            info.audioTime = aud.time;
                            SendEvent(BoomboxAudioEvent, ReceiverGroup.Others, kvp.Key, isPlaying, aud.time, aud.volume, aud.pitch);
                        }
                    }
                }
                
                if (info.gameObject.name.Contains("Vape"))
                {
                    bool isSmoking = Vape.isExhaling;
                    if (isSmoking != info.isVapeSmoking)
                    {
                        info.isVapeSmoking = isSmoking;
                        SendEvent(VapeSmokeEvent, ReceiverGroup.Others, kvp.Key, isSmoking);
                    }
                }
            }
        }

        private void OnEventRaised(byte eventCode, object customData, int senderActorNumber)
        {
            if (eventCode != NetworkByte || !NetworkEnabled) 
                return;
            
            if (senderActorNumber == GetLocalPlayerId()) 
                return;
            
            try
            {
                if (!(customData is object[] args) || args.Length == 0) 
                    return;
                
                string command = args[0] as string;
                eventCount++;
                
                switch (command)
                {
                    case SyncEvent:
                        HandleSync(args, senderActorNumber);
                        break;
                    case DestroyEvent:
                        HandleDestroy(args);
                        break;
                    case RequestEvent:
                        HandleRequest(senderActorNumber);
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
                    case CosmeticSyncEvent:
                        HandleCosmeticSync(args, senderActorNumber);
                        break;
                }
            }
            catch (Exception e)
            {
                if (DebugMode)
                    Debug.LogError($"Event error: {e.Message}");
            }
        }

        private void HandleSync(object[] args, int senderActorNumber)
        {
            if (args.Length < 5) 
                return;
            
            string objectId = args[1] as string;
            Vector3 position = (Vector3)args[2];
            Quaternion rotation = (Quaternion)args[3];
            int ownerActor = args[4] is int actor ? actor : senderActorNumber;
            string propName = args.Length > 5 ? args[5] as string : "";
            
            GameObject obj = FindTrackedObject(objectId);
            if (obj != null)
            {
                if (trackedObjects.TryGetValue(objectId, out NetworkedObject info))
                {
                    info.targetPosition = position;
                    info.targetRotation = rotation;
                    info.lastUpdate = Time.time;
                    info.ownerActorNumber = ownerActor;
                }
            }
            else
            {
                TryCreateNetworkedObject(objectId, position, rotation, ownerActor, propName);
            }
        }

        private void HandleCosmeticSync(object[] args, int senderActorNumber)
        {
            if (args.Length < 3 || CosmeticsController.instance == null) return;
            string cosmeticString = args[2] as string;
            if (string.IsNullOrEmpty(cosmeticString)) return;

            VRRig targetRig = null;
            foreach (VRRig rig in VRRigCache.ActiveRigs)
            {
                if (rig != null && !rig.isOfflineVRRig && rig.Creator != null && rig.Creator.ActorNumber == senderActorNumber)
                {
                    targetRig = rig;
                    break;
                }
            }

            if (targetRig == null && NetworkSystem.Instance != null)
            {
                NetPlayer player = NetworkSystem.Instance.GetPlayer(senderActorNumber);
                if (player != null)
                    targetRig = GorillaGameManager.StaticFindRigForPlayer(player);
            }

            if (targetRig != null && targetRig.cosmeticSet != null && targetRig.cosmeticsObjectRegistry != null)
            {
                string[] items = cosmeticString.Split(',');
                for (int i = 0; i < 16; i++)
                {
                    string itemName = (i < items.Length) ? items[i] : "null";
                    if (string.IsNullOrEmpty(itemName) || itemName == "null" || itemName == "NOTHING")
                    {
                        targetRig.cosmeticSet.items[i] = CosmeticsController.instance.nullItem;
                    }
                    else if (CosmeticsController.instance.allCosmeticsDict.TryGetValue(itemName, out var cosmeticItem))
                    {
                        targetRig.cosmeticSet.items[i] = cosmeticItem;
                    }
                }
                targetRig.SetCosmeticsActive(false);
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
            if (string.IsNullOrEmpty(url)) 
                yield break;

            AudioType type = AudioType.UNKNOWN;
            if (url.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase)) type = AudioType.MPEG;
            else if (url.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase)) type = AudioType.OGGVORBIS;
            else if (url.EndsWith(".wav", StringComparison.OrdinalIgnoreCase)) type = AudioType.WAV;

            string fullUrl = url.Contains("://") ? url : "file://" + url;
            using UnityWebRequest req = UnityWebRequestMultimedia.GetAudioClip(fullUrl, type);
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

        private void OnLocalJoinedRoom()
        {
            if (!NetworkEnabled || NetworkSystem.Instance?.InRoom != true)
                return;

            SendEvent(RequestEvent, ReceiverGroup.Others);
        }

        private void HandleRequest(int senderActorNumber)
        {
            int localId = GetLocalPlayerId();
            foreach (var kvp in trackedObjects)
            {
                if (kvp.Value.gameObject == null || kvp.Value.ownerActorNumber != localId) 
                    continue;

                SendEventToActor(SyncEvent, senderActorNumber, 
                    kvp.Key, 
                    kvp.Value.gameObject.transform.position, 
                    kvp.Value.gameObject.transform.rotation, 
                    kvp.Value.ownerActorNumber,
                    kvp.Value.propName ?? kvp.Value.gameObject.name);
                
                if (kvp.Value.scale != Vector3.one)
                    SendEventToActor(ScaleEvent, senderActorNumber, kvp.Key, kvp.Value.scale);
                
                if (kvp.Value.gameObject.name.Contains("Boombox"))
                {
                    AudioSource aud = kvp.Value.gameObject.GetComponent<AudioSource>();
                    if (aud != null && aud.isPlaying)
                        SendEventToActor(BoomboxAudioEvent, senderActorNumber, kvp.Key, aud.isPlaying, aud.time, aud.volume, aud.pitch);
                }
                
                if (kvp.Value.gameObject.name.Contains("Vape"))
                    SendEventToActor(VapeSmokeEvent, senderActorNumber, kvp.Key, Vape.isExhaling);
            }
        }

        private void OnPlayerJoined(NetPlayer player)
        {
            if (!NetworkEnabled || NetworkSystem.Instance?.InRoom != true || player == null || player.IsLocal) 
                return;

            int localId = GetLocalPlayerId();
            foreach (var kvp in trackedObjects)
            {
                if (kvp.Value.gameObject == null || kvp.Value.ownerActorNumber != localId) 
                    continue;

                SendEventToActor(SyncEvent, player.ActorNumber,
                    kvp.Key,
                    kvp.Value.gameObject.transform.position,
                    kvp.Value.gameObject.transform.rotation,
                    kvp.Value.ownerActorNumber,
                    kvp.Value.propName ?? kvp.Value.gameObject.name);
                
                if (kvp.Value.scale != Vector3.one)
                    SendEventToActor(ScaleEvent, player.ActorNumber, kvp.Key, kvp.Value.scale);

                if (kvp.Value.gameObject.name.Contains("Boombox"))
                {
                    AudioSource aud = kvp.Value.gameObject.GetComponent<AudioSource>();
                    if (aud != null && aud.isPlaying)
                        SendEventToActor(BoomboxAudioEvent, player.ActorNumber, kvp.Key, aud.isPlaying, aud.time, aud.volume, aud.pitch);
                }

                if (kvp.Value.gameObject.name.Contains("Vape"))
                    SendEventToActor(VapeSmokeEvent, player.ActorNumber, kvp.Key, Vape.isExhaling);
            }

            if (VRRig.LocalRig != null && Main.GetIndex("CosmetX")?.enabled == true)
            {
                string cosmeticStr = mods.GetLocalCosmeticString();
                if (!string.IsNullOrEmpty(cosmeticStr))
                    SendEventToActor(CosmeticSyncEvent, player.ActorNumber, GetLocalPlayerId(), cosmeticStr);
            }
        }

        private void OnPlayerLeft(NetPlayer player)
        {
            if (player == null) return;
            int leftActor = player.ActorNumber;
            List<string> toRemove = new List<string>();
            foreach (var kvp in trackedObjects)
            {
                if (kvp.Value.ownerActorNumber == leftActor)
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
                targetPosition = obj.transform.position,
                targetRotation = obj.transform.rotation,
                scale = obj.transform.localScale,
                ownerActorNumber = GetLocalPlayerId(),
                propName = obj.name,
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
                info.ownerActorNumber,
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
                info.targetPosition = obj.transform.position;
                info.targetRotation = obj.transform.rotation;
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
            NetEventOptions options = new NetEventOptions
            {
                Reciever = (NetEventOptions.RecieverTarget)(byte)target
            };
            NetworkSystemRaiseEvent.RaiseEvent(NetworkByte, data, options, reliable: true);
        }

        public void SendEventToActor(string command, int targetActor, params object[] parameters)
        {
            if (NetworkSystem.Instance?.InRoom != true) 
                return;
            
            object[] data = new object[] { command }.Concat(parameters).ToArray();
            NetEventOptions options = new NetEventOptions
            {
                TargetActors = new[] { targetActor }
            };
            NetworkSystemRaiseEvent.RaiseEvent(NetworkByte, data, options, reliable: true);
        }

        public void SendCosmeticUpdate(string cosmeticString)
        {
            if (!NetworkEnabled || NetworkSystem.Instance?.InRoom != true || string.IsNullOrEmpty(cosmeticString)) 
                return;

            SendEvent(CosmeticSyncEvent, ReceiverGroup.Others, GetLocalPlayerId(), cosmeticString);
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
            
            int localId = GetLocalPlayerId();
            foreach (string objectId in pendingSync.ToList())
            {
                if (trackedObjects.TryGetValue(objectId, out NetworkedObject info) && info.gameObject != null && info.ownerActorNumber == localId)
                {
                    SendEvent(SyncEvent, ReceiverGroup.Others,
                        objectId,
                        info.gameObject.transform.position,
                        info.gameObject.transform.rotation,
                        info.ownerActorNumber,
                        info.propName ?? info.gameObject.name);
                    syncCount++;
                }
            }
            
            pendingSync.Clear();
        }

        private void DestroyTrackedObject(string objectId)
        {
            if (!trackedObjects.TryGetValue(objectId, out NetworkedObject info)) 
                return;
            
            int localId = GetLocalPlayerId();
            if (info.ownerActorNumber == localId)
                SendEvent(DestroyEvent, ReceiverGroup.Others, objectId);

            if (info.gameObject != null && info.ownerActorNumber != localId)
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
            
            int localId = GetLocalPlayerId();
            foreach (string id in toRemove)
            {
                if (trackedObjects.TryGetValue(id, out NetworkedObject info))
                {
                    if (info.ownerActorNumber == localId && NetworkEnabled && NetworkSystem.Instance?.InRoom == true)
                        SendEvent(DestroyEvent, ReceiverGroup.Others, id);
                }
                
                trackedObjects.Remove(id);
                pendingSync.Remove(id);
            }
        }

        private GameObject FindTrackedObject(string objectId) =>
            trackedObjects.TryGetValue(objectId, out NetworkedObject info) ? info.gameObject : null;

        private void TryCreateNetworkedObject(string objectId, Vector3 position, Quaternion rotation, int ownerActor, string propName = "")
        {
            GameObject obj = CreateRemoteObject(propName, position, rotation);
            if (obj == null) 
                return;
            
            NetworkedObject info = new NetworkedObject
            {
                gameObject = obj,
                position = position,
                rotation = rotation,
                targetPosition = position,
                targetRotation = rotation,
                scale = obj.transform.localScale,
                ownerActorNumber = ownerActor,
                propName = propName,
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
                if (mesh == null)
                    MaxwellHolder.DownloadAssets();
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
                Shader shader = Shader.Find("GorillaTag/UberShader")
                    ?? Shader.Find("Universal Render Pipeline/Lit")
                    ?? Shader.Find("Standard");
                Material mat = new Material(shader);
                if (texture != null)
                {
                    mat.mainTexture = texture;
                    if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", texture);
                }
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

        private int GetLocalPlayerId()
        {
            if (NetworkSystem.Instance != null)
                return NetworkSystem.Instance.LocalPlayerID;

            if (PhotonNetwork.LocalPlayer != null)
                return PhotonNetwork.LocalPlayer.ActorNumber;

            return -1;
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