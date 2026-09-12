using ExitGames.Client.Photon;
using GorillaNetworking;
using Photon.Pun;
using Photon.Realtime;
using ShibaGTGenesisReborn.Menu;
using ShibaGTGenesisReborn.Mods;
using ShibaGTGenesisReborn.Mods.Custom;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
        private float lastCosmeticCheck;
        private string lastSyncedCosmetics;
        private bool wasInRoom;
        private readonly Dictionary<string, Mesh> meshCache = new Dictionary<string, Mesh>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> loadingProps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private class PropDefinition
        {
            public string Key;
            public string ModelUrl;
            public string TextureUrl;
            public string[] LocalModelFiles;
            public string[] LocalTextureFiles;
            public Vector3 DefaultScale = Vector3.one;
            public bool HasAudio;
            public Func<Mesh> GetStaticMesh;
            public Action<Mesh> SetStaticMesh;
            public Func<Texture2D> GetStaticTexture;
            public Action<Texture2D> SetStaticTexture;
        }

        private static readonly PropDefinition[] PropDefs = new PropDefinition[]
        {
            new PropDefinition
            {
                Key = "Boombox",
                ModelUrl = "https://raw.githubusercontent.com/incharilla1/ShibaGT-Genesis-Reborn/main/Mods/Custom/files/boombox.obj",
                TextureUrl = "https://raw.githubusercontent.com/incharilla1/ShibaGT-Genesis-Reborn/main/Mods/Custom/files/boomboxmesh.png",
                LocalModelFiles = new[] { "boombox.obj" },
                LocalTextureFiles = new[] { "boomboxmesh.png", "boombox.png" },
                DefaultScale = Vector3.one * 1.25f,
                HasAudio = true,
                GetStaticMesh = () => BoomboxManager.CM,
                SetStaticMesh = m => BoomboxManager.CM = m,
                GetStaticTexture = () => BoomboxManager.CT,
                SetStaticTexture = t => BoomboxManager.CT = t
            },
            new PropDefinition
            {
                Key = "Maxwell",
                ModelUrl = "https://raw.githubusercontent.com/incharilla1/assets/refs/heads/main/maxwell.obj",
                TextureUrl = "https://raw.githubusercontent.com/incharilla1/assets/refs/heads/main/Maxwell.png",
                LocalModelFiles = new[] { "maxwell.obj" },
                LocalTextureFiles = new[] { "Maxwell.png", "maxwell.png" },
                DefaultScale = Vector3.one * 0.5f,
                HasAudio = true,
                GetStaticMesh = () => MaxwellHolder.CM,
                SetStaticMesh = m => MaxwellHolder.CM = m,
                GetStaticTexture = () => MaxwellHolder.CT,
                SetStaticTexture = t => MaxwellHolder.CT = t
            },
            new PropDefinition
            {
                Key = "Grosh",
                ModelUrl = "https://raw.githubusercontent.com/incharilla1/ShibaGT-Genesis-Reborn/main/Mods/Custom/files/Grosh.Holdable.obj",
                TextureUrl = "https://raw.githubusercontent.com/incharilla1/ShibaGT-Genesis-Reborn/main/Mods/Custom/files/iidktexture.png",
                LocalModelFiles = new[] { "grosh.obj", "Grosh.Holdable.obj" },
                LocalTextureFiles = new[] { "iidktexture.png", "grosh.png" },
                DefaultScale = new Vector3(0.1f, 0.1f, 0.1f),
                HasAudio = true,
                GetStaticMesh = () => GroshHolder.CM,
                SetStaticMesh = m => GroshHolder.CM = m,
                GetStaticTexture = () => GroshHolder.CT,
                SetStaticTexture = t => GroshHolder.CT = t
            },
            new PropDefinition
            {
                Key = "Tung",
                ModelUrl = "https://raw.githubusercontent.com/incharilla1/ShibaGT-Genesis-Reborn/main/Mods/Custom/files/TungTungTungSahur.obj",
                TextureUrl = "https://raw.githubusercontent.com/incharilla1/ShibaGT-Genesis-Reborn/main/Mods/Custom/files/shaded.png",
                LocalModelFiles = new[] { "tungtung.obj", "TungTungTungSahur.obj" },
                LocalTextureFiles = new[] { "shaded.png", "tungtung.png" },
                DefaultScale = new Vector3(0.045f, 0.045f, 0.045f),
                HasAudio = true,
                GetStaticMesh = () => SusTung.CM,
                SetStaticMesh = m => SusTung.CM = m,
                GetStaticTexture = () => SusTung.CT,
                SetStaticTexture = t => SusTung.CT = t
            },
            new PropDefinition
            {
                Key = "Seal",
                ModelUrl = "https://raw.githubusercontent.com/incharilla1/ShibaGT-Genesis-Reborn/main/Mods/Custom/files/fatseal.obj",
                TextureUrl = "https://raw.githubusercontent.com/incharilla1/ShibaGT-Genesis-Reborn/main/Mods/Custom/files/fatseal.jpeg",
                LocalModelFiles = new[] { "fatseal.obj" },
                LocalTextureFiles = new[] { "fatseal.jpeg", "fatseal.png" },
                DefaultScale = Vector3.one * 0.35f,
                HasAudio = false,
                GetStaticMesh = () => FatSealSpammer.CM,
                SetStaticMesh = m => FatSealSpammer.CM = m,
                GetStaticTexture = () => FatSealSpammer.CT,
                SetStaticTexture = t => FatSealSpammer.CT = t
            },
            new PropDefinition
            {
                Key = "Vape",
                ModelUrl = "https://raw.githubusercontent.com/incharilla1/ShibaGT-Genesis-Reborn/main/Mods/Custom/files/juul.obj",
                TextureUrl = "https://raw.githubusercontent.com/incharilla1/ShibaGT-Genesis-Reborn/main/Mods/Custom/files/JUUL_BOI_Color.png",
                LocalModelFiles = new[] { "vape.obj", "juul.obj" },
                LocalTextureFiles = new[] { "JUUL_BOI_Color.png", "vape.png" },
                DefaultScale = Vector3.one * 2f,
                HasAudio = false,
                GetStaticMesh = () => Vape.CM,
                SetStaticMesh = m => Vape.CM = m,
                GetStaticTexture = () => Vape.CT,
                SetStaticTexture = t => Vape.CT = t
            },
            new PropDefinition
            {
                Key = "Bomb",
                ModelUrl = "https://raw.githubusercontent.com/incharilla1/assets/refs/heads/main/Bomb.obj",
                TextureUrl = "https://raw.githubusercontent.com/incharilla1/assets/refs/heads/main/Bomb.png",
                LocalModelFiles = new[] { "Bomb.obj" },
                LocalTextureFiles = new[] { "Bomb.png" },
                DefaultScale = Vector3.one * 0.35f,
                HasAudio = true,
                GetStaticMesh = () => BombManager.bombMesh,
                SetStaticMesh = m => BombManager.bombMesh = m,
                GetStaticTexture = () => BombManager.bombTexture,
                SetStaticTexture = t => BombManager.bombTexture = t
            }
        };

        private static PropDefinition FindPropDefinition(string propName)
        {
            if (string.IsNullOrEmpty(propName)) return null;
            for (int i = 0; i < PropDefs.Length; i++)
            {
                if (propName.IndexOf(PropDefs[i].Key, StringComparison.OrdinalIgnoreCase) >= 0)
                    return PropDefs[i];
            }
            return null;
        }
        
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
            if (!NetworkEnabled || NetworkSystem.Instance?.InRoom != true)
            {
                if (wasInRoom)
                {
                    wasInRoom = false;
                    OnLocalLeftRoom();
                }
                return;
            }

            wasInRoom = true;

            if (mods.cosmetXEnabled && Time.time - lastCosmeticCheck >= 0.25f)
            {
                lastCosmeticCheck = Time.time;
                string currentStr = ModsLib.GetLocalCosmeticString();
                if (currentStr != lastSyncedCosmetics)
                {
                    lastSyncedCosmetics = currentStr;
                    SendCosmeticUpdate(currentStr);
                }
            }

            if (trackedObjects.Count == 0)
                return;

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

                Transform t = info.gameObject.transform;
                if (t.position != info.position || Quaternion.Angle(t.rotation, info.rotation) > 0.5f)
                {
                    info.position = t.position;
                    info.rotation = t.rotation;
                    pendingSync.Add(kvp.Key);
                }

                if (t.localScale != info.scale)
                {
                    info.scale = t.localScale;
                    SendEvent(ScaleEvent, ReceiverGroup.Others, kvp.Key, info.scale);
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

            if (Time.time - lastSyncTime >= syncInterval)
            {
                SendPendingUpdates();
                lastSyncTime = Time.time;
            }
        }

        private void OnLocalLeftRoom()
        {
            List<string> toRemove = new List<string>();
            int localId = GetLocalPlayerId();
            foreach (var kvp in trackedObjects)
            {
                if (kvp.Value.ownerActorNumber != localId)
                {
                    if (kvp.Value.gameObject != null)
                        Destroy(kvp.Value.gameObject);
                    toRemove.Add(kvp.Key);
                }
            }
            for (int i = 0; i < toRemove.Count; i++)
                trackedObjects.Remove(toRemove[i]);
            pendingSync.Clear();
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
            Vector3? scale = (args.Length > 6 && args[6] is Vector3 s) ? (Vector3?)s : null;
            
            GameObject obj = FindTrackedObject(objectId);
            if (obj != null)
            {
                if (trackedObjects.TryGetValue(objectId, out NetworkedObject info))
                {
                    info.targetPosition = position;
                    info.targetRotation = rotation;
                    info.lastUpdate = Time.time;
                    info.ownerActorNumber = ownerActor;
                    if (scale.HasValue)
                    {
                        info.scale = scale.Value;
                        obj.transform.localScale = scale.Value;
                    }
                }
            }
            else
            {
                TryCreateNetworkedObject(objectId, position, rotation, ownerActor, propName, scale);
            }
        }

        private VRRig FindRigForActor(int actorNumber)
        {
            if (VRRigCache.ActiveRigs != null)
            {
                foreach (VRRig rig in VRRigCache.ActiveRigs)
                {
                    if (rig != null && !rig.isLocal && rig.Creator != null && rig.Creator.ActorNumber == actorNumber)
                        return rig;
                }
            }

            if (NetworkSystem.Instance != null)
            {
                NetPlayer player = NetworkSystem.Instance.GetPlayer(actorNumber);
                if (player != null)
                    return GorillaGameManager.StaticFindRigForPlayer(player);
            }

            return null;
        }

        private void HandleCosmeticSync(object[] args, int senderActorNumber)
        {
            if (args.Length < 3) return;
            string cosmeticString = args[2] as string;
            if (string.IsNullOrEmpty(cosmeticString)) return;

            VRRig targetRig = FindRigForActor(senderActorNumber);
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
                targetRig.RefreshCosmetics();
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

            string targetUrl = url;
            if (!url.Contains("://"))
            {
                if (File.Exists(url))
                {
                    targetUrl = "file://" + Path.GetFullPath(url);
                }
                else
                {
                    string fileName = Path.GetFileName(url);
                    string local = ModsLib.FindLocalAsset(fileName, 
                        Path.Combine(ModsLib.GenesisDirectory, fileName),
                        Path.Combine(BoomboxManager.BoomboxDirectory, fileName));
                    if (!string.IsNullOrEmpty(local) && File.Exists(local))
                    {
                        targetUrl = "file://" + Path.GetFullPath(local);
                    }
                    else if (File.Exists(BoomboxManager.P_Aud))
                    {
                        targetUrl = "file://" + Path.GetFullPath(BoomboxManager.P_Aud);
                    }
                    else
                    {
                        yield break;
                    }
                }
            }

            AudioType type = AudioType.UNKNOWN;
            if (targetUrl.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase)) type = AudioType.MPEG;
            else if (targetUrl.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase)) type = AudioType.OGGVORBIS;
            else if (targetUrl.EndsWith(".wav", StringComparison.OrdinalIgnoreCase)) type = AudioType.WAV;

            using UnityWebRequest req = UnityWebRequestMultimedia.GetAudioClip(targetUrl, type);
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(req);
                if (clip != null)
                {
                    audioClipCache[url] = clip;
                    if (aud != null)
                    {
                        aud.clip = clip;
                        if (!aud.isPlaying) aud.Play();
                    }
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
            if (obj != null && isSmoking && trackedObjects.TryGetValue(objectId, out NetworkedObject info))
            {
                VRRig rig = FindRigForActor(info.ownerActorNumber);
                if (rig != null && rig.headMesh != null)
                {
                }
            }
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
                    
                    if (isPlaying)
                    {
                        aud.time = time;
                        if (aud.clip != null)
                        {
                            if (!aud.isPlaying) aud.Play();
                        }
                        else
                        {
                            string defaultAud = BoomboxManager.P_Aud;
                            if (audioClipCache.TryGetValue(defaultAud, out AudioClip cached))
                            {
                                aud.clip = cached;
                                aud.Play();
                            }
                            else
                            {
                                StartCoroutine(LoadAudioClip(defaultAud, aud));
                            }
                        }
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
                    kvp.Value.propName ?? kvp.Value.gameObject.name,
                    kvp.Value.gameObject.transform.localScale);
                
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
                    kvp.Value.propName ?? kvp.Value.gameObject.name,
                    kvp.Value.gameObject.transform.localScale);
                
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

            if (mods.cosmetXEnabled)
            {
                string cosmeticStr = ModsLib.GetLocalCosmeticString();
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
                objectId = Guid.NewGuid().ToString("N").Substring(0, 16);
            
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
                obj.name,
                info.scale);
            
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
                        info.propName ?? info.gameObject.name,
                        info.gameObject.transform.localScale);
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

        private void TryCreateNetworkedObject(string objectId, Vector3 position, Quaternion rotation, int ownerActor, string propName = "", Vector3? scale = null)
        {
            GameObject obj = CreateRemoteObject(propName, position, rotation, scale);
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

        private GameObject CreateRemoteObject(string propName, Vector3 position, Quaternion rotation, Vector3? scale = null)
        {
            GameObject obj = new GameObject(propName + "_Remote");
            obj.transform.position = position;
            obj.transform.rotation = rotation;

            PropDefinition prop = FindPropDefinition(propName);
            obj.transform.localScale = scale ?? (prop != null ? prop.DefaultScale : Vector3.one);

            MeshFilter mf = obj.AddComponent<MeshFilter>();
            MeshRenderer mr = obj.AddComponent<MeshRenderer>();

            if (prop != null && prop.HasAudio)
            {
                AudioSource aud = obj.AddComponent<AudioSource>();
                aud.spatialBlend = 1f;
                aud.maxDistance = 15f;
                aud.rolloffMode = AudioRolloffMode.Linear;
                if (prop.Key == "Boombox")
                {
                    aud.loop = true;
                    aud.volume = BoomboxManager.Volume;
                }
                else if (prop.Key == "Maxwell")
                {
                    if (MaxwellHolder.MeowClip != null) aud.clip = MaxwellHolder.MeowClip;
                }
                else if (prop.Key == "Tung")
                {
                    if (SusTung.CA != null) aud.clip = SusTung.CA;
                }
            }

            if (prop != null)
            {
                if (TryGetOrLoadAssetsSync(prop, out Mesh mesh, out Texture2D tex))
                {
                    mf.sharedMesh = mesh;
                    mr.material = ModsLib.CreateItemMaterial(tex);
                }
                else
                {
                    mr.enabled = false;
                    StartCoroutine(LoadPropAssetsAsync(prop, obj));
                }
            }
            else
            {
                mr.material = ModsLib.CreateItemMaterial(Texture2D.whiteTexture);
            }

            return obj;
        }

        private bool TryGetOrLoadAssetsSync(PropDefinition prop, out Mesh mesh, out Texture2D tex)
        {
            mesh = null;
            tex = null;

            if (meshCache.TryGetValue(prop.Key, out Mesh cm)) mesh = cm;
            else if (prop.GetStaticMesh?.Invoke() != null) mesh = prop.GetStaticMesh();

            if (textureCache.TryGetValue(prop.Key, out Texture2D ct)) tex = ct;
            else if (prop.GetStaticTexture?.Invoke() != null) tex = prop.GetStaticTexture();

            if (mesh == null)
            {
                string objPath = null;
                for (int i = 0; i < prop.LocalModelFiles.Length; i++)
                {
                    string f = prop.LocalModelFiles[i];
                    objPath = ModsLib.FindLocalAsset(f, Path.Combine(ModsLib.GenesisDirectory, f));
                    if (!string.IsNullOrEmpty(objPath) && File.Exists(objPath)) break;
                }
                if (!string.IsNullOrEmpty(objPath) && File.Exists(objPath))
                {
                    try
                    {
                        mesh = ModsLib.ParseObj(File.ReadAllText(objPath));
                    }
                    catch { }
                }
            }

            if (tex == null)
            {
                string texPath = null;
                for (int i = 0; i < prop.LocalTextureFiles.Length; i++)
                {
                    string f = prop.LocalTextureFiles[i];
                    texPath = ModsLib.FindLocalAsset(f, Path.Combine(ModsLib.GenesisDirectory, f));
                    if (!string.IsNullOrEmpty(texPath) && File.Exists(texPath)) break;
                }
                if (!string.IsNullOrEmpty(texPath) && File.Exists(texPath))
                {
                    try
                    {
                        byte[] bytes = File.ReadAllBytes(texPath);
                        tex = new Texture2D(2, 2);
                        tex.LoadImage(bytes);
                    }
                    catch { }
                }
            }

            if (mesh != null)
            {
                meshCache[prop.Key] = mesh;
                prop.SetStaticMesh?.Invoke(mesh);
            }
            if (tex != null)
            {
                textureCache[prop.Key] = tex;
                prop.SetStaticTexture?.Invoke(tex);
            }

            return mesh != null && tex != null;
        }

        private IEnumerator LoadPropAssetsAsync(PropDefinition prop, GameObject targetObj)
        {
            if (loadingProps.Contains(prop.Key))
            {
                while (loadingProps.Contains(prop.Key))
                    yield return null;

                if (targetObj != null && meshCache.TryGetValue(prop.Key, out Mesh m) && textureCache.TryGetValue(prop.Key, out Texture2D t))
                    ApplyAssetsToObject(targetObj, m, t);
                yield break;
            }

            loadingProps.Add(prop.Key);
            string genesisDir = ModsLib.GenesisDirectory;
            if (!Directory.Exists(genesisDir)) Directory.CreateDirectory(genesisDir);

            Mesh mesh = null;
            Texture2D tex = null;
            meshCache.TryGetValue(prop.Key, out mesh);
            textureCache.TryGetValue(prop.Key, out tex);

            if (mesh == null && !string.IsNullOrEmpty(prop.ModelUrl))
            {
                using UnityWebRequest r = UnityWebRequest.Get(prop.ModelUrl);
                yield return r.SendWebRequest();
                if (r.result == UnityWebRequest.Result.Success && !r.downloadHandler.text.StartsWith("<") && !r.downloadHandler.text.StartsWith("404"))
                {
                    string objData = r.downloadHandler.text;
                    try
                    {
                        mesh = ModsLib.ParseObj(objData);
                        if (prop.LocalModelFiles.Length > 0)
                            File.WriteAllText(Path.Combine(genesisDir, prop.LocalModelFiles[0]), objData);
                    }
                    catch { }
                }
            }

            if (tex == null && !string.IsNullOrEmpty(prop.TextureUrl))
            {
                using UnityWebRequest tr = UnityWebRequestTexture.GetTexture(prop.TextureUrl);
                yield return tr.SendWebRequest();
                if (tr.result == UnityWebRequest.Result.Success)
                {
                    tex = DownloadHandlerTexture.GetContent(tr);
                    if (prop.LocalTextureFiles.Length > 0)
                        File.WriteAllBytes(Path.Combine(genesisDir, prop.LocalTextureFiles[0]), tr.downloadHandler.data);
                }
            }

            if (mesh != null)
            {
                meshCache[prop.Key] = mesh;
                prop.SetStaticMesh?.Invoke(mesh);
            }
            if (tex != null)
            {
                textureCache[prop.Key] = tex;
                prop.SetStaticTexture?.Invoke(tex);
            }

            loadingProps.Remove(prop.Key);

            if (targetObj != null)
                ApplyAssetsToObject(targetObj, mesh, tex);

            foreach (var kvp in trackedObjects)
            {
                if (kvp.Value?.gameObject != null && kvp.Value.propName != null &&
                    kvp.Value.propName.IndexOf(prop.Key, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    ApplyAssetsToObject(kvp.Value.gameObject, mesh, tex);
                }
            }
        }

        private void ApplyAssetsToObject(GameObject obj, Mesh mesh, Texture2D tex)
        {
            if (obj == null) return;
            MeshFilter mf = obj.GetComponent<MeshFilter>();
            MeshRenderer mr = obj.GetComponent<MeshRenderer>();
            if (mf != null && mesh != null) mf.sharedMesh = mesh;
            if (mr != null)
            {
                mr.material = ModsLib.CreateItemMaterial(tex);
                mr.enabled = true;
            }
        }

        private int GetLocalPlayerId()
        {
            if (NetworkSystem.Instance != null)
                return NetworkSystem.Instance.LocalPlayerID;

            if (PhotonNetwork.LocalPlayer != null)
                return PhotonNetwork.LocalPlayer.ActorNumber;

            return -1;
        }

        // no i did NOT skid this from seralyth
        // i couldnt figure out a method myself
        // i figured why not take inspo from something thats already working instead of 
        // making something that works like dogshit that took 10h (which i already did)
        // i was working on it from 6am to 4pm
        // mine looks different enough imo
        public static void SendRigPosition(PhotonView view, Vector3 position, int[] targets = null, bool reliable = false)
        {
            if (!NetworkSystem.Instance.InRoom || view == null) return;

            Vector3 prevView = view.transform.position;
            Vector3 prevRig = VRRig.LocalRig.transform.position;
            view.transform.position = position;
            VRRig.LocalRig.transform.position = position;
            List<object> payload = PhotonNetwork.OnSerializeWrite(view);
            view.transform.position = prevView;
            VRRig.LocalRig.transform.position = prevRig;
            if (payload == null || payload.Count == 0) return;

            PhotonNetwork.RaiseEventBatch batch = new PhotonNetwork.RaiseEventBatch
            {
                Reliable = reliable || view.Synchronization == ViewSynchronization.ReliableDeltaCompressed || view.mixedModeIsReliable,
                Group = view.Group
            };

            if (!PhotonNetwork.serializeViewBatches.TryGetValue(batch, out PhotonNetwork.SerializeViewBatch viewBatch))
                PhotonNetwork.serializeViewBatches[batch] = viewBatch = new PhotonNetwork.SerializeViewBatch(batch, 2);

            viewBatch.Add(payload);
            viewBatch.ObjectUpdates[0] = PhotonNetwork.ServerTimestamp;
            viewBatch.ObjectUpdates[1] = PhotonNetwork.currentLevelPrefix != 0 ? (object)PhotonNetwork.currentLevelPrefix : null;

            PhotonNetwork.NetworkingClient.OpRaiseEvent(
                (byte)(batch.Reliable ? PunEvent.SendSerializeReliable : PunEvent.SendSerialize),
                viewBatch.ObjectUpdates,
                targets != null ? new RaiseEventOptions { TargetActors = targets } : PhotonNetwork.serializeRaiseEvOptions,
                batch.Reliable ? SendOptions.SendReliable : SendOptions.SendUnreliable
            );
            viewBatch.Clear();
        }   
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