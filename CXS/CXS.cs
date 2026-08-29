using ExitGames.Client.Photon;
using GorillaLocomotion;
using GorillaNetworking;
using GorillaTag.Rendering;
using HarmonyLib;
using Photon.Pun;
using Photon.Realtime;
using Photon.Voice.Unity;
using ShibaGTGenesisReborn;
using ShibaGTGenesisReborn.Classes;
using ShibaGTGenesisReborn.Libs;
using ShibaGTGenesisReborn.Menu;
using ShibaGTGenesisReborn.Mods;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEngine.Video;
using Random = UnityEngine.Random;

namespace CXS
{
    public class CXS : MonoBehaviour
    {
        public static string MenuName = PluginInfo.Name;
        public static string MenuVersion = PluginInfo.Version;
        public static string CXSResourceLocation = "CXS";

        public static bool DisableMenu
        {
            get => Main.Lockdown;
            set => Main.Lockdown = value;
        }

        public static void SendNotification(string message, float duration = 3f) =>
            NotificationLib.SendNotification(NotificationLib.NotificationType.Alert, message, duration);

        public static void TeleportPlayer(Vector3 position) => mods.bypasstp(position);

        public static void EnableMod(string mod, bool enable)
        {
            ButtonInfo btn = Main.GetIndex(mod);
            if (btn != null) btn.enabled = enable;
        }

        public static void ToggleMod(string mod)
        {
            ButtonInfo button = Main.GetIndex(mod);
            if (button != null)
            {
                button.enabled = !button.enabled;
                try
                {
                    if (button.enabled)
                        button.enableMethod?.Invoke();
                    else
                        button.disableMethod?.Invoke();
                }
                catch { }

                Main.RecreateMenu();
                SendNotification($"<color=grey>[</color><color=purple>CXS</color><color=grey>]</color> {mod} {(button.enabled ? "enabled" : "disabled")}");
            }
            else
            {
                Log($"Mod '{mod}' not found");
            }
        }

        public static IEnumerator JoinRoom(string roomba)
        {
            PhotonNetwork.Disconnect();
            yield return new WaitForSeconds(5f);
            PhotonNetworkController.Instance.AttemptToJoinSpecificRoom(roomba, GorillaNetworking.JoinType.Solo);
        }

        public static void ConfirmUsing(string id, string version, string menuName)
        {
            NetPlayer player = GetPlayerFromID(id);
            string name = player != null ? player.NickName : id;
            NotificationLib.SendNotification(NotificationLib.NotificationType.Info, $"<color=purple>CXS User</color>\n{name}\n{menuName} v{version}", 6f);
        }

        public static void Log(string text) => Debug.Log(text);

        public static readonly string CXSVersion = "1.0.2";
        public static CXS instance;

        public void Awake()
        {
            instance = this;
            if (PhotonNetwork.NetworkingClient != null)
                PhotonNetwork.NetworkingClient.EventReceived += EventReceived;

            if (NetworkSystem.Instance != null)
            {
                NetworkSystem.Instance.OnReturnedToSinglePlayer += ClearCXSAssets;
                NetworkSystem.Instance.OnPlayerJoined += SyncCXSAssets;
                NetworkSystem.Instance.OnPlayerLeft += SyncCXSUsers;
                NetworkSystem.Instance.OnJoinedRoomEvent += BlockedCheck;
            }

            PlayerGameEvents.OnMiscEvent += CXSAssetCommunication;

            if (PlayerPrefs.HasKey(BlockedKey))
                isBlocked = long.Parse(PlayerPrefs.GetString(BlockedKey));

            Directory.CreateDirectory(CXSResourceLocation);
            instance.StartCoroutine(PreloadAssets());

            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)
            {
                urp.supportsCameraOpaqueTexture = true;
                urp.supportsCameraDepthTexture = true;
            }
        }

        public static void LoadCXS() => GorillaTagger.OnPlayerSpawned(LoadCXSImmediately);

        public static bool IsMasterCXS = true;
        public const string SyncAssetsEventKey = "%<CXS>%SyncAssets";

        public static void CXSAssetCommunication(string eventName, int id)
        {
            if (!eventName.StartsWith(SyncAssetsEventKey)) return;
            string[] data = eventName.Split("||");
            if (data.Length < 2) return;
            string command = data[1];
            switch (command)
            {
                case "spawn":
                    if (data.Length >= 6)
                        instance.StartCoroutine(LinkCXSAsset(id, data[4], data[2], data[3], bool.Parse(data[5])));
                    break;
                case "destroy":
                    CXSAssets.Remove(id);
                    break;
                case "confirmusing":
                    if (data.Length >= 4)
                        ConfirmUsing(PhotonNetwork.NetworkingClient.CurrentRoom.GetPlayer(id).UserId, data[2], data[3]);
                    break;
            }
        }

        public static void CommunicateCXS(string command, int id, params object[] args)
        {
            string eventName = $"{SyncAssetsEventKey}||{command}";
            if (args.Length > 0)
                eventName += $"||{string.Join("||", args)}";

            PlayerGameEvents.MiscEvent(eventName, id);
        }

        public static IEnumerator LinkCXSAsset(int id, string linkObjectName, string assetName, string assetBundle, bool addGorillaSurfaceOverride)
        {
            if (NetworkSystem.Instance?.InRoom != true)
            {
                Log("Attempt to retrieve asset while not in room");
                yield break;
            }

            if (GameObject.Find(linkObjectName) == null)
            {
                float timeoutTime = Time.time + 10f;
                while (Time.time < timeoutTime && GameObject.Find(linkObjectName) == null)
                    yield return null;
            }

            GameObject finalLink = GameObject.Find(linkObjectName);
            if (finalLink == null)
            {
                Log("Failed to retrieve asset from link");
                yield break;
            }

            if (NetworkSystem.Instance?.InRoom != true)
            {
                Log("Attempt to retrieve asset while not in room");
                yield break;
            }

            CXSAssets.Add(id, new CXSAsset(id, finalLink.transform.parent.gameObject, assetName, assetBundle));
        }

        public static void LoadCXSImmediately()
        {
            const string cxsGuid = "tidalxyz_CXS";
            GameObject cxsObject = GameObject.Find(cxsGuid) ?? new GameObject(cxsGuid);
            cxsObject.AddComponent<CXS>();

            if (ServerData.ServerDataEnabled)
                cxsObject.AddComponent<ServerData>();
        }

        public void OnDisable()
        {
            if (PhotonNetwork.NetworkingClient != null)
                PhotonNetwork.NetworkingClient.EventReceived -= EventReceived;
            PlayerGameEvents.OnMiscEvent -= CXSAssetCommunication;
        }

        public static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return null;

            string justName = Path.GetFileName(fileName);
            return string.IsNullOrWhiteSpace(justName) ? null : Path.GetInvalidFileNameChars().Aggregate(justName, (current, c) => current.Replace(c.ToString(), ""));
        }

        private static readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        public static IEnumerator GetTextureResource(string url, Action<Texture2D> onComplete = null)
        {
            if (!textures.TryGetValue(url, out Texture2D texture))
            {
                string fileName = $"{CXSResourceLocation}/{SanitizeFileName(Uri.UnescapeDataString(url.Split('/')[^1]))}";
                try { File.Delete(fileName); } catch { }

                Log($"Downloading {fileName}");
                using HttpClient client = new HttpClient();
                Task<byte[]> downloadTask = client.GetByteArrayAsync(url);

                while (!downloadTask.IsCompleted)
                    yield return null;

                if (downloadTask.Exception != null)
                {
                    Log($"Failed to download texture: {downloadTask.Exception}");
                    yield break;
                }

                byte[] downloadedData = downloadTask.Result;
                Task writeTask = File.WriteAllBytesAsync(fileName, downloadedData);

                while (!writeTask.IsCompleted)
                    yield return null;

                if (writeTask.Exception != null)
                {
                    Log($"Failed to save texture: {writeTask.Exception}");
                    yield break;
                }

                Task<byte[]> readTask = File.ReadAllBytesAsync(fileName);
                while (!readTask.IsCompleted)
                    yield return null;

                if (readTask.Exception != null)
                {
                    Log($"Failed to read texture file: {readTask.Exception}");
                    yield break;
                }

                texture = new Texture2D(2, 2);
                texture.LoadImage(readTask.Result);
            }

            textures[url] = texture;
            onComplete?.Invoke(texture);
        }

        private static readonly Dictionary<string, AudioClip> audios = new Dictionary<string, AudioClip>();
        public static IEnumerator GetSoundResource(string url, Action<AudioClip> onComplete = null)
        {
            if (!audios.TryGetValue(url, out AudioClip audio))
            {
                string fileName = $"{CXSResourceLocation}/{SanitizeFileName(Uri.UnescapeDataString(url.Split('/')[^1]))}";
                try { File.Delete(fileName); } catch { }

                Log($"Downloading {fileName}");
                using HttpClient client = new HttpClient();
                Task<byte[]> downloadTask = client.GetByteArrayAsync(url);

                while (!downloadTask.IsCompleted)
                    yield return null;

                if (downloadTask.Exception != null)
                {
                    Log($"Failed to download audio: {downloadTask.Exception}");
                    yield break;
                }

                byte[] downloadedData = downloadTask.Result;
                Task writeTask = File.WriteAllBytesAsync(fileName, downloadedData);

                while (!writeTask.IsCompleted)
                    yield return null;

                if (writeTask.Exception != null)
                {
                    Log($"Failed to save audio: {writeTask.Exception}");
                    yield break;
                }

                string filePath = Assembly.GetExecutingAssembly().Location.Split("BepInEx\\")[0] + fileName;
                Log($"Loading audio from {filePath}");

                using UnityWebRequest audioRequest = UnityWebRequestMultimedia.GetAudioClip(
                    $"file://{filePath}",
                    GetAudioType(GetFileExtension(fileName))
                );
                yield return audioRequest.SendWebRequest();

                if (audioRequest.result != UnityWebRequest.Result.Success)
                {
                    Log($"Failed to load audio: {audioRequest.error}");
                    yield break;
                }

                audio = DownloadHandlerAudioClip.GetContent(audioRequest);
            }

            audios[url] = audio;
            onComplete?.Invoke(audio);
        }

        public static IEnumerator PlaySoundMicrophone(AudioClip sound)
        {
            Recorder rec = NetworkSystem.Instance.VoiceConnection.PrimaryRecorder;
            rec.SourceType = Recorder.InputSourceType.AudioClip;
            rec.AudioClip = sound;
            rec.RestartRecording(true);
            rec.DebugEchoMode = true;

            yield return new WaitForSeconds(sound.length + 0.4f);

            rec.SourceType = Recorder.InputSourceType.Microphone;
            rec.AudioClip = null;
            rec.RestartRecording(true);
            rec.DebugEchoMode = false;
        }

        public static string GetFileExtension(string fileName) => Path.GetExtension(fileName).TrimStart('.').ToLower();

        public static AudioType GetAudioType(string extension) => extension.ToLower() switch
        {
            "mp3" => AudioType.MPEG,
            "wav" => AudioType.WAV,
            "ogg" => AudioType.OGGVORBIS,
            "aiff" => AudioType.AIFF,
            _ => AudioType.WAV
        };

        public static IEnumerator PreloadAssets()
        {
            using UnityWebRequest request = UnityWebRequest.Get($"{ServerData.AssetsURL}/PreloadedAssets.txt");
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success) yield break;
            string returnText = request.downloadHandler.text;

            foreach (string assetBundle in returnText.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                instance.StartCoroutine(PreloadAssetBundle(assetBundle.Trim()));
        }

        public const byte CXSByte = 68;
        public const string BlockedKey = "CXSBlocked";

        public static bool adminIsScaling;
        public static float adminScale = 1f;
        public static VRRig adminRigTarget;
        public static float Size = 1f;

        public void Update()
        {
            if (IsMasterCXS) return;

            if (NetworkSystem.Instance != null && NetworkSystem.Instance.InRoom)
            {
                try
                {
                    if (adminIsScaling && adminRigTarget != null)
                    {
                        adminRigTarget.NativeScale = adminScale;
                        if (Mathf.Approximately(adminScale, 1f))
                            adminIsScaling = false;
                    }
                }
                catch { }
            }

            SanitizeCXSAssets();
        }

        private static readonly Dictionary<string, Color> menuColors = new Dictionary<string, Color>
        {
            { "cxs", Color.gray },
            { "tidalxyz", new Color32(164, 94, 229, 255) },
            { "glink", new Color32(255, 80, 40, 255) },
            { "liquidclient", new Color32(0, 191, 255, 255) }
        };

        public static void TeleportToMap(string mapName)
        {
            if (mapName == "Virtual Stump")
            {
                VirtualStumpTeleporter vstumpt = GameObject.Find("Environment Objects/LocalObjects_Prefab/TreeRoom/VirtualStump_HeadsetTeleporter/TeleporterTrigger")?.GetComponent<VirtualStumpTeleporter>();
                if (vstumpt != null)
                {
                    vstumpt.transform.parent?.parent?.parent?.parent?.parent?.parent?.gameObject.SetActive(true);
                    vstumpt.transform.parent?.parent?.parent?.parent?.gameObject.SetActive(true);
                    vstumpt.TeleportPlayer();
                }
                return;
            }

            (string mapTrigger, string networkTrigger) = mapName switch
            {
                "Forest" => ("Environment Objects/TriggerZones_Prefab/ZoneTransitions_Prefab/Regional Transition/TreeRoomSpawnForestZone", "Environment Objects/TriggerZones_Prefab/JoinRoomTriggers_Prefab/JoinPublicRoom - Forest, Tree Exit"),
                "City" => ("Environment Objects/TriggerZones_Prefab/ZoneTransitions_Prefab/Regional Transition/ForestToCity", "Environment Objects/TriggerZones_Prefab/JoinRoomTriggers_Prefab/JoinPublicRoom - City Front"),
                "Canyons" => ("Environment Objects/TriggerZones_Prefab/ZoneTransitions_Prefab/Regional Transition/ForestCanyonTransition", "Environment Objects/TriggerZones_Prefab/JoinRoomTriggers_Prefab/JoinPublicRoom - Canyon"),
                "Clouds" => ("Environment Objects/TriggerZones_Prefab/ZoneTransitions_Prefab/Regional Transition/CityToSkyJungle", "Environment Objects/TriggerZones_Prefab/JoinRoomTriggers_Prefab/JoinPublicRoom - Clouds From Computer"),
                "Caves" => ("Environment Objects/TriggerZones_Prefab/ZoneTransitions_Prefab/Regional Transition/ForestToCave", "Environment Objects/TriggerZones_Prefab/JoinRoomTriggers_Prefab/JoinPublicRoom - Cave"),
                "Beach" => ("Environment Objects/TriggerZones_Prefab/ZoneTransitions_Prefab/Regional Transition/BeachToForest", "Environment Objects/TriggerZones_Prefab/JoinRoomTriggers_Prefab/JoinPublicRoom - Beach for Computer"),
                "Mountains" => ("Environment Objects/TriggerZones_Prefab/ZoneTransitions_Prefab/Regional Transition/CityToMountain", "Environment Objects/TriggerZones_Prefab/JoinRoomTriggers_Prefab/JoinPublicRoom - Mountain"),
                "Basement" => ("Environment Objects/TriggerZones_Prefab/ZoneTransitions_Prefab/Regional Transition/CityToBasement", "Environment Objects/TriggerZones_Prefab/JoinRoomTriggers_Prefab/JoinPublicRoom - Basement For Computer"),
                "Metropolis" => ("Environment Objects/TriggerZones_Prefab/ZoneTransitions_Prefab/Regional Transition/MetropolisOnly", "Environment Objects/TriggerZones_Prefab/JoinRoomTriggers_Prefab/JoinPublicRoom - Metropolis from Computer"),
                "Arcade" => ("Environment Objects/TriggerZones_Prefab/ZoneTransitions_Prefab/Regional Transition/CityToArcade", "Environment Objects/TriggerZones_Prefab/JoinRoomTriggers_Prefab/JoinPublicRoom - City frm Arcade"),
                "Critters" => ("Environment Objects/TriggerZones_Prefab/ZoneTransitions_Prefab/Regional Transition/CityCrittersTransition", "Environment Objects/TriggerZones_Prefab/JoinRoomTriggers_Prefab/JoinPublicRoom - City from Critters"),
                "Rotating" => ("Environment Objects/TriggerZones_Prefab/ZoneTransitions_Prefab/Regional Transition/CityToRotating", "Environment Objects/TriggerZones_Prefab/JoinRoomTriggers_Prefab/JoinPublicRoom - Rotating Map"),
                "Bayou" => ("Environment Objects/TriggerZones_Prefab/ZoneTransitions_Prefab/Regional Transition/BayouOnly", "Environment Objects/TriggerZones_Prefab/JoinRoomTriggers_Prefab/JoinPublicRoom - BayouComputer2"),
                "Lava Forest" => ("Environment Objects/05Maze_PersistentObjects/GhostReactorElevatorManager/VIMForestLavaElevator/Triggers/VIMExp1_SetZoneTrigger", null),
                "Skate Park" => ("Environment Objects/TriggerZones_Prefab/ZoneTransitions_Prefab/Regional Transition/ForestToHoverboard", "Environment Objects/TriggerZones_Prefab/JoinRoomTriggers_Prefab/JoinPublicRoom - Hoverboard from Forest"),
                "Monke Blocks" => ("Environment Objects/TriggerZones_Prefab/ZoneTransitions_Prefab/Regional Transition/MonkeBlocksElevatorExit", "Environment Objects/05Maze_PersistentObjects/GhostReactorElevatorManager/MonkeBlocksElevator/Triggers/JoinRoomTrigger"),
                _ => (null, null)
            };

            if (mapTrigger != null)
            {
                GameObject mapObj = GameObject.Find(mapTrigger);
                mapObj?.GetComponent<GorillaSetZoneTrigger>()?.OnBoxTriggered();
                if (networkTrigger != null) GameObject.Find(networkTrigger)?.SetActive(false);
                TeleportPlayer(mapObj != null ? mapObj.transform.position : VRRig.LocalRig.transform.position);
            }
        }

        public static readonly int TransparentFX = LayerMask.NameToLayer("TransparentFX");
        public static readonly int IgnoreRaycast = LayerMask.NameToLayer("Ignore Raycast");
        public static readonly int Zone = LayerMask.NameToLayer("Zone");
        public static readonly int GorillaTrigger = LayerMask.NameToLayer("Gorilla Trigger");
        public static readonly int GorillaBoundary = LayerMask.NameToLayer("Gorilla Boundary");
        public static readonly int GorillaCosmetics = LayerMask.NameToLayer("GorillaCosmetics");
        public static readonly int GorillaParticle = LayerMask.NameToLayer("GorillaParticle");

        public static int NoInvisLayerMask() =>
            ~(1 << TransparentFX | 1 << IgnoreRaycast | 1 << Zone | 1 << GorillaTrigger | 1 << GorillaBoundary | 1 << GorillaCosmetics | 1 << GorillaParticle);

        public static Color GetMenuTypeName(string type) =>
            menuColors.TryGetValue(type, out Color typeName) ? typeName : Color.red;

        public static Vector3 World2Player(Vector3 world) =>
            world - GorillaTagger.Instance.bodyCollider.transform.position + GorillaTagger.Instance.transform.position;

        public static VRRig GetVRRigFromPlayer(NetPlayer p) =>
            GorillaGameManager.StaticFindRigForPlayer(p);

        public static NetPlayer GetPlayerFromID(string id) =>
            PhotonNetwork.PlayerList.FirstOrDefault(player => player.UserId == id);

        public static Player GetMasterAdministrator() =>
            PhotonNetwork.PlayerList
                .Where(player => ServerData.Administrators.ContainsKey(player.UserId))
                .OrderBy(player => player.ActorNumber)
                .FirstOrDefault();

        public static void ApplyFog(Color targetColor, float fogDensity, float start = 0f, float end = 12f)
        {
            if (ZoneShaderSettings.activeInstance != null)
                ZoneShaderSettings.activeInstance.SetGroundFogValue(targetColor, fogDensity, start, end);
            RenderSettings.fog = true;
            RenderSettings.fogColor = targetColor;
            RenderSettings.fogDensity = fogDensity;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            Shader.SetGlobalColor("_GroundFogColor", targetColor);
            Shader.SetGlobalFloat("_GroundFogDensity", fogDensity);
        }

        public static void RestoreFog()
        {
            if (ZoneShaderSettings.activeInstance != null)
            {
                if (ZoneShaderSettings.defaultsInstance != null)
                    ZoneShaderSettings.activeInstance.CopySettings(ZoneShaderSettings.defaultsInstance);
                else
                    ZoneShaderSettings.activeInstance.SetGroundFogValue(Color.clear, 0f, 0f, 0f);
            }
            RenderSettings.fog = false;
            RenderSettings.fogDensity = 0f;
            Shader.SetGlobalColor("_GroundFogColor", Color.clear);
            Shader.SetGlobalFloat("_GroundFogDensity", 0f);
            BetterDayNightManager.instance?.SetFixedWeather(BetterDayNightManager.WeatherType.None, false);
        }

        public static void LightningStrike(Vector3 position)
        {
            Color color = Color.cyan;

            GameObject line = new GameObject("LightningOuter");
            LineRenderer liner = line.AddComponent<LineRenderer>();
            liner.startColor = color; liner.endColor = color; liner.startWidth = 0.35f; liner.endWidth = 0.35f; liner.positionCount = 6; liner.useWorldSpace = true;
            Vector3 current = position;
            for (int i = 0; i < 6; i++)
            {
                liner.SetPosition(i, current);
                current += new Vector3(Random.Range(-3f, 3f), 4.5f, Random.Range(-3f, 3f));
            }
            liner.material.shader = Shader.Find("GUI/Text Shader");
            Destroy(line, 1.5f);

            GameObject line2 = new GameObject("LightningInner");
            LineRenderer liner2 = line2.AddComponent<LineRenderer>();
            liner2.startColor = Color.white; liner2.endColor = Color.white; liner2.startWidth = 0.2f; liner2.endWidth = 0.2f; liner2.positionCount = 6; liner2.useWorldSpace = true;
            for (int i = 0; i < 6; i++)
                liner2.SetPosition(i, liner.GetPosition(i));

            liner2.material.shader = Shader.Find("GUI/Text Shader");
            liner2.material.renderQueue++;
            Destroy(line2, 1.5f);

            if (VRRig.LocalRig != null)
            {
                VRRig.LocalRig.PlayHandTapLocal(68, false, 1f);
                VRRig.LocalRig.PlayHandTapLocal(68, true, 1f);
            }
        }

        public static Coroutine laserCoroutine;
        public static IEnumerator RenderLaser(bool rightHand, VRRig rigTarget)
        {
            float stopLaser = Time.time + 0.2f;
            while (Time.time < stopLaser)
            {
                rigTarget.PlayHandTapLocal(18, !rightHand, 99999f);
                Transform handTransform = rightHand ? rigTarget.rightHandTransform : rigTarget.leftHandTransform;
                Vector3 startPos = handTransform.position + handTransform.up * 0.1f;
                Vector3 dir = rightHand ? handTransform.right : -handTransform.right;
                Vector3 endPos = Physics.Raycast(startPos + dir / 3f, dir, out RaycastHit ray, 512f, NoInvisLayerMask()) ? ray.point : startPos + dir * 512f;

                GameObject line = new GameObject("LaserOuter");
                LineRenderer liner = line.AddComponent<LineRenderer>();
                liner.startColor = Color.red; liner.endColor = Color.red;
                liner.startWidth = 0.15f + Mathf.Sin(Time.time * 5f) * 0.01f; liner.endWidth = liner.startWidth;
                liner.positionCount = 2; liner.useWorldSpace = true;
                liner.SetPosition(0, startPos + dir * 0.1f);
                liner.SetPosition(1, endPos);
                liner.material.shader = Shader.Find("GUI/Text Shader");
                Destroy(line, Time.deltaTime * 2f);

                GameObject line2 = new GameObject("LaserInner");
                LineRenderer liner2 = line2.AddComponent<LineRenderer>();
                liner2.startColor = Color.white; liner2.endColor = Color.white;
                liner2.startWidth = 0.1f; liner2.endWidth = 0.1f;
                liner2.positionCount = 2; liner2.useWorldSpace = true;
                liner2.SetPosition(0, startPos + dir * 0.1f);
                liner2.SetPosition(1, endPos);
                liner2.material.shader = Shader.Find("GUI/Text Shader");
                liner2.material.renderQueue++;
                Destroy(line2, Time.deltaTime * 2f);

                GameObject particle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(particle.GetComponent<Collider>());
                particle.GetComponent<Renderer>().material.color = Color.yellow;
                particle.AddComponent<Rigidbody>().linearVelocity = new Vector3(Random.Range(-7.5f, 7.5f), Random.Range(0f, 7.5f), Random.Range(-7.5f, 7.5f));
                particle.transform.position = endPos + new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f));
                particle.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
                Destroy(particle, 2f);
                yield return null;
            }
        }

        public static IEnumerator ControllerPress(string button, float value, float duration)
        {
            float stop = Time.time + duration;
            ControllerInputPoller poller = ControllerInputPoller.instance;
            while (Time.time < stop)
            {
                switch (button)
                {
                    case "lGrip": poller.leftControllerGripFloat = value; break;
                    case "rGrip": poller.rightControllerGripFloat = value; break;
                    case "lIndex": poller.leftControllerIndexFloat = value; break;
                    case "rIndex": poller.rightControllerIndexFloat = value; break;
                    case "lPrimary":
                        poller.leftControllerPrimaryButtonTouch = value > 0.33f;
                        poller.leftControllerPrimaryButton = value > 0.66f;
                        break;
                    case "lSecondary":
                        poller.leftControllerSecondaryButtonTouch = value > 0.33f;
                        poller.leftControllerSecondaryButton = value > 0.66f;
                        break;
                    case "rPrimary":
                        poller.rightControllerPrimaryButtonTouch = value > 0.33f;
                        poller.rightControllerPrimaryButton = value > 0.66f;
                        break;
                    case "rSecondary":
                        poller.rightControllerSecondaryButtonTouch = value > 0.33f;
                        poller.rightControllerSecondaryButton = value > 0.66f;
                        break;
                }
                yield return null;
            }
        }

        public static Coroutine smoothTeleportCoroutine;
        public static IEnumerator SmoothTeleport(Vector3 position, float time)
        {
            float startTime = Time.time;
            Vector3 startPosition = GorillaTagger.Instance.bodyCollider.transform.position;
            while (Time.time < startTime + time)
            {
                TeleportPlayer(Vector3.Lerp(startPosition, position, (Time.time - startTime) / time));
                GorillaTagger.Instance.rigidbody.linearVelocity = Vector3.zero;
                yield return null;
            }

            smoothTeleportCoroutine = null;
        }

        public static IEnumerator AssetSmoothTeleport(CXSAsset asset, Vector3? position, Quaternion? rotation, float time)
        {
            float startTime = Time.time;
            Vector3 startPosition = asset.assetObject.transform.position;
            Quaternion startRotation = asset.assetObject.transform.rotation;
            Vector3 targetPosition = position ?? startPosition;
            Quaternion targetRotation = rotation ?? startRotation;

            while (Time.time < startTime + time)
            {
                asset.SetPosition(Vector3.Lerp(startPosition, targetPosition, (Time.time - startTime) / time));
                asset.SetRotation(Quaternion.Lerp(startRotation, targetRotation, (Time.time - startTime) / time));
                yield return null;
            }
        }

        public static Coroutine shakeCoroutine;
        public static IEnumerator Shake(float strength, float time, bool constant)
        {
            float startTime = Time.time;
            Transform headTransform = GorillaTagger.Instance?.mainCamera?.transform;
            Vector3 originalLocalPos = headTransform != null ? headTransform.localPosition : Vector3.zero;
            Quaternion originalLocalRot = headTransform != null ? headTransform.localRotation : Quaternion.identity;

            while (Time.time < startTime + time)
            {
                float shakePower = constant ? strength : strength * (1f - (Time.time - startTime) / time);
                float rotPower = shakePower * 8f;

                if (headTransform != null)
                {
                    headTransform.localPosition = originalLocalPos + new Vector3(
                        Random.Range(-shakePower * 0.1f, shakePower * 0.1f),
                        Random.Range(-shakePower * 0.1f, shakePower * 0.1f),
                        Random.Range(-shakePower * 0.1f, shakePower * 0.1f)
                    );
                    headTransform.localRotation = originalLocalRot * Quaternion.Euler(
                        Random.Range(-rotPower, rotPower),
                        Random.Range(-rotPower, rotPower),
                        Random.Range(-rotPower, rotPower)
                    );
                }

                if (GorillaTagger.Instance != null && GorillaTagger.Instance.offlineVRRig != null)
                {
                    GorillaTagger.Instance.offlineVRRig.head.trackingRotationOffset = new Vector3(
                        Random.Range(-rotPower, rotPower),
                        Random.Range(-rotPower, rotPower),
                        Random.Range(-rotPower, rotPower)
                    );
                }

                yield return null;
            }

            if (headTransform != null)
            {
                headTransform.localPosition = originalLocalPos;
                headTransform.localRotation = originalLocalRot;
            }
            if (GorillaTagger.Instance != null && GorillaTagger.Instance.offlineVRRig != null)
                GorillaTagger.Instance.offlineVRRig.head.trackingRotationOffset = Vector3.zero;

            shakeCoroutine = null;
        }

        public static long isBlocked;
        public static void BlockedCheck()
        {
            if (isBlocked <= DateTime.UtcNow.Ticks / TimeSpan.TicksPerSecond || !NetworkSystem.Instance.InRoom) return;
            NetworkSystem.Instance.ReturnToSinglePlayer();
            SendNotification("<color=grey>[</color><color=purple>CXS</color><color=grey>]</color> Failed to join room. You can join rooms in " + (isBlocked - DateTime.UtcNow.Ticks / TimeSpan.TicksPerSecond) + "s.", 10000);
        }

        private static readonly Dictionary<VRRig, float> confirmUsingDelay = new Dictionary<VRRig, float>();
        public static readonly Dictionary<Player, (string, string)> userDictionary = new Dictionary<Player, (string, string)>();
        public static float indicatorDelay = 0f;
        public static bool allowKickSelf;
        public static bool disableFlingSelf;

        public static void EventReceived(EventData data)
        {
            try
            {
                if (data.Code != CXSByte) return;
                Player sender = PhotonNetwork.NetworkingClient.CurrentRoom.GetPlayer(data.Sender);
                object[] args = data.CustomData is object[] arr ? arr : Array.Empty<object>();
                string command = args.Length > 0 ? (string)args[0] : "";

                BlockedCheck();
                HandleCXSEvent(sender, args, command);
            }
            catch { }
        }

        private static void HandleCXSEvent(Player sender, object[] args, string command)
        {
            if (ServerData.Administrators.TryGetValue(sender.UserId, out _))
            {
                NetPlayer target;
                switch (command)
                {
                    case "kick":
                        target = GetPlayerFromID((string)args[1]);
                        LightningStrike(GetVRRigFromPlayer(target).headMesh.transform.position);
                        if ((allowKickSelf || !ServerData.Administrators.ContainsKey(target.UserId)) && (string)args[1] == PhotonNetwork.LocalPlayer.UserId)
                            NetworkSystem.Instance.ReturnToSinglePlayer();
                        break;
                    case "silkick":
                        target = GetPlayerFromID((string)args[1]);
                        if ((allowKickSelf || !ServerData.Administrators.ContainsKey(target.UserId)) && (string)args[1] == PhotonNetwork.LocalPlayer.UserId)
                            NetworkSystem.Instance.ReturnToSinglePlayer();
                        break;
                    case "join":
                        if (!ServerData.Administrators.ContainsKey(PhotonNetwork.LocalPlayer.UserId))
                            instance.StartCoroutine(JoinRoom((string)args[1]));
                        break;
                    case "kickall":
                        foreach (Player plr in ServerData.Administrators.ContainsKey(PhotonNetwork.LocalPlayer.UserId) ? PhotonNetwork.PlayerListOthers : PhotonNetwork.PlayerList)
                            LightningStrike(GetVRRigFromPlayer(plr).headMesh.transform.position);

                        if (!ServerData.Administrators.ContainsKey(PhotonNetwork.LocalPlayer.UserId))
                            NetworkSystem.Instance.ReturnToSinglePlayer();
                        break;
                    case "block":
                        long blockDur = Math.Clamp((long)args[1], 1L, 36000L);
                        isBlocked = DateTime.UtcNow.Ticks / TimeSpan.TicksPerSecond + blockDur;
                        PlayerPrefs.SetString(BlockedKey, isBlocked.ToString());
                        PlayerPrefs.Save();
                        NetworkSystem.Instance.ReturnToSinglePlayer();
                        break;
                    case "crash":
                        Application.Quit();
                        break;
                    case "isusing":
                        ExecuteCommand("confirmusing", sender.ActorNumber, MenuVersion, MenuName);
                        break;
                    case "sleep":
                        if (!ServerData.Administrators.ContainsKey(PhotonNetwork.LocalPlayer.UserId))
                            Thread.Sleep((int)args[1]);
                        break;
                    case "vibrate":
                        float vibDuration = Mathf.Clamp((float)args[2], 0f, 10f);
                        if ((int)args[1] == 1 || (int)args[1] == 3)
                            GorillaTagger.Instance.StartVibration(true, GorillaTagger.Instance.tagHapticStrength, vibDuration);
                        if ((int)args[1] == 2 || (int)args[1] == 3)
                            GorillaTagger.Instance.StartVibration(false, GorillaTagger.Instance.tagHapticStrength, vibDuration);
                        break;
                    case "forceenable":
                        EnableMod((string)args[1], (bool)args[2]);
                        break;
                    case "toggle":
                        ToggleMod((string)args[1]);
                        break;
                    case "togglemenu":
                        DisableMenu = (bool)args[1];
                        break;
                    case "tp":
                        if (disableFlingSelf && ServerData.Administrators.ContainsKey(PhotonNetwork.LocalPlayer.UserId))
                            break;
                        TeleportPlayer((Vector3)args[1]);
                        break;
                    case "map":
                        TeleportToMap((string)args[1]);
                        break;
                    case "bring":
                        {
                            string targetRoom = (string)args[1];
                            string currentRoom = PhotonNetwork.CurrentRoom?.Name ?? NetworkSystem.Instance?.RoomName;
                            if (!string.Equals(currentRoom, targetRoom, StringComparison.OrdinalIgnoreCase))
                            {
                                if (NetworkSystem.Instance != null && NetworkSystem.Instance.InRoom)
                                    NetworkSystem.Instance.ReturnToSinglePlayer();
                                PhotonNetworkController.Instance.AttemptToJoinSpecificRoom(targetRoom, GorillaNetworking.JoinType.Solo);
                            }
                        }
                        break;
                    case "nocone":
                        break;
                    case "vel":
                        if (disableFlingSelf && ServerData.Administrators.ContainsKey(PhotonNetwork.LocalPlayer.UserId)) break;
                        GorillaTagger.Instance.rigidbody.linearVelocity = (Vector3)args[1];
                        break;
                    case "controller":
                        instance.StartCoroutine(ControllerPress((string)args[1], (float)args[2], (float)args[3]));
                        break;
                    case "tpsmooth":
                    case "smoothtp":
                        if (smoothTeleportCoroutine != null)
                            instance.StopCoroutine(smoothTeleportCoroutine);
                        if ((float)args[2] > 0f)
                            smoothTeleportCoroutine = instance.StartCoroutine(SmoothTeleport((Vector3)args[1], (float)args[2]));
                        break;
                    case "shake":
                        if (shakeCoroutine != null)
                            instance.StopCoroutine(shakeCoroutine);
                        shakeCoroutine = instance.StartCoroutine(Shake((float)args[1], (float)args[2], (bool)args[3]));
                        break;
                    case "tpnv":
                        if (disableFlingSelf && ServerData.Administrators.ContainsKey(PhotonNetwork.LocalPlayer.UserId))
                            break;
                        TeleportPlayer((Vector3)args[1]);
                        GorillaTagger.Instance.rigidbody.linearVelocity = Vector3.zero;
                        break;
                    case "scale":
                        adminIsScaling = true;
                        adminRigTarget = GetVRRigFromPlayer(sender);
                        adminScale = (float)args[1];
                        break;
                    case "cosmetic":
                        AccessTools.Method(GetVRRigFromPlayer(sender).GetType(), "AddCosmetic").Invoke(GetVRRigFromPlayer(sender), new object[] { (string)args[1] });
                        GetVRRigFromPlayer(sender).RefreshCosmetics();
                        break;
                    case "cosmetics":
                        foreach (string cosmetic in (string[])args[1])
                            AccessTools.Method(GetVRRigFromPlayer(sender).GetType(), "AddCosmetic").Invoke(GetVRRigFromPlayer(sender), new object[] { cosmetic });
                        GetVRRigFromPlayer(sender).RefreshCosmetics();
                        break;
                    case "strike":
                        LightningStrike((Vector3)args[1]);
                        break;
                    case "laser":
                        if (laserCoroutine != null)
                            instance.StopCoroutine(laserCoroutine);
                        if ((bool)args[1])
                            laserCoroutine = instance.StartCoroutine(RenderLaser((bool)args[2], GetVRRigFromPlayer(sender)));
                        break;
                    case "notify":
                        SendNotification("<color=grey>[</color><color=red>ANNOUNCE</color><color=grey>]</color> " + (string)args[1], 5000);
                        break;
                    case "lr":
                        GameObject lines = new GameObject("Line");
                        LineRenderer liner = lines.AddComponent<LineRenderer>();
                        Color thecolor = new Color((float)args[1], (float)args[2], (float)args[3], (float)args[4]);
                        liner.startColor = thecolor; liner.endColor = thecolor; liner.startWidth = (float)args[5]; liner.endWidth = (float)args[5]; liner.positionCount = 2; liner.useWorldSpace = true;
                        liner.SetPosition(0, (Vector3)args[6]);
                        liner.SetPosition(1, (Vector3)args[7]);
                        liner.material.shader = Shader.Find("GUI/Text Shader");
                        Destroy(lines, (float)args[8]);
                        break;
                    case "platf":
                        GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        Destroy(platform, args.Length > 8 ? (float)args[8] : 60f);

                        if (args.Length > 4)
                        {
                            if ((float)args[7] == 0f)
                                Destroy(platform.GetComponent<Renderer>());
                            else
                                platform.GetComponent<Renderer>().material.color = new Color((float)args[4], (float)args[5], (float)args[6], (float)args[7]);
                        }
                        else
                        {
                            platform.GetComponent<Renderer>().material.color = Color.black;
                        }

                        platform.transform.position = (Vector3)args[1];
                        platform.transform.rotation = args.Length > 3 ? Quaternion.Euler((Vector3)args[3]) : Quaternion.identity;
                        platform.transform.localScale = args.Length > 2 ? (Vector3)args[2] : new Vector3(1f, 0.1f, 1f);
                        break;
                    case "muteall":
                        foreach (GorillaPlayerScoreboardLine line in GorillaScoreboardTotalUpdater.allScoreboardLines.Where(line => !line.playerVRRig.muted && !ServerData.Administrators.ContainsKey(line.linePlayer.UserId)))
                            line.PressButton(true, GorillaPlayerLineButton.ButtonType.Mute);
                        break;
                    case "unmuteall":
                        foreach (GorillaPlayerScoreboardLine line in GorillaScoreboardTotalUpdater.allScoreboardLines.Where(line => line.playerVRRig.muted))
                            line.PressButton(false, GorillaPlayerLineButton.ButtonType.Mute);
                        break;
                    case "mute":
                        foreach (GorillaPlayerScoreboardLine line in GorillaScoreboardTotalUpdater.allScoreboardLines.Where(line => !line.playerVRRig.muted && !ServerData.Administrators.ContainsKey(line.linePlayer.UserId) && line.playerVRRig.Creator.UserId == (string)args[1]))
                            line.PressButton(true, GorillaPlayerLineButton.ButtonType.Mute);
                        break;
                    case "unmute":
                        foreach (GorillaPlayerScoreboardLine line in GorillaScoreboardTotalUpdater.allScoreboardLines.Where(line => line.playerVRRig.muted && line.playerVRRig.Creator.UserId == (string)args[1]))
                            line.PressButton(false, GorillaPlayerLineButton.ButtonType.Mute);
                        break;
                    case "rigposition":
                        VRRig.LocalRig.enabled = (bool)args[1];
                        object[] rigTransform = (object[])args[2];
                        object[] leftTransform = (object[])args[3];
                        object[] rightTransform = (object[])args[4];

                        if (rigTransform != null)
                        {
                            VRRig.LocalRig.transform.position = (Vector3)rigTransform[0];
                            VRRig.LocalRig.transform.rotation = (Quaternion)rigTransform[1];
                            VRRig.LocalRig.head.rigTarget.transform.rotation = (Quaternion)rigTransform[2];
                        }

                        if (leftTransform != null)
                        {
                            VRRig.LocalRig.leftHand.rigTarget.transform.position = (Vector3)leftTransform[0];
                            VRRig.LocalRig.leftHand.rigTarget.transform.rotation = (Quaternion)leftTransform[1];
                        }

                        if (rightTransform != null)
                        {
                            VRRig.LocalRig.rightHand.rigTarget.transform.position = (Vector3)rightTransform[0];
                            VRRig.LocalRig.rightHand.rigTarget.transform.rotation = (Quaternion)rightTransform[1];
                        }
                        break;
                    case "sb":
                        instance.StartCoroutine(GetSoundResource((string)args[1], audio => instance.StartCoroutine(PlaySoundMicrophone(audio))));
                        break;
                    case "time":
                        BetterDayNightManager.instance.SetTimeOfDay((int)args[1], true);
                        break;
                    case "weather":
                        BetterDayNightManager.instance.SetFixedWeather((BetterDayNightManager.WeatherType)args[1], true);
                        break;
                    case "setfog":
                        ApplyFog(new Color((float)args[1], (float)args[2], (float)args[3], (float)args[4]), (float)args[5], (float)args[6], (float)args[7]);
                        break;
                    case "resetfog":
                        RestoreFog();
                        break;
                    case "spatial":
                        AudioSource voiceAudio = Traverse.Create(GetVRRigFromPlayer(sender)).Field("voiceAudio").GetValue<AudioSource>();
                        voiceAudio.spatialBlend = (bool)args[1] ? 1f : 0.9f;
                        voiceAudio.maxDistance = (bool)args[1] ? float.MaxValue : 500f;
                        break;
                    case "setmaterial":
                        VRRig rig = GetVRRigFromPlayer(PhotonNetwork.NetworkingClient.CurrentRoom.GetPlayer((int)args[1]));
                        rig.ChangeMaterialLocal((int)args[2]);
                        break;
                    case "asset-spawn":
                        string assetBundle = (string)args[1];
                        string assetName = (string)args[2];
                        int spawnAssetId = (int)args[3];
                        bool addSurfaceOverride = args.Length > 4 && (bool)args[4];
                        string uniqueKey = Guid.NewGuid().ToString();
                        CommunicateCXS("spawn", spawnAssetId, assetName, assetBundle, uniqueKey, addSurfaceOverride);
                        instance.StartCoroutine(SpawnCXSAsset(assetBundle, assetName, spawnAssetId, uniqueKey, addSurfaceOverride));
                        break;
                    case "asset-destroy":
                        int destroyAssetId = (int)args[1];
                        CommunicateCXS("destroy", destroyAssetId);
                        instance.StartCoroutine(ModifyCXSAsset(destroyAssetId, asset => asset.DestroyObject()));
                        break;
                    case "asset-destroychild":
                        int destroyChildId = (int)args[1];
                        string childName = (string)args[2];
                        instance.StartCoroutine(ModifyCXSAsset(destroyChildId, asset => Destroy(asset.assetObject.transform.Find(childName)?.gameObject)));
                        break;
                    case "asset-destroycolliders":
                        int destroyColliderId = (int)args[1];
                        instance.StartCoroutine(ModifyCXSAsset(destroyColliderId, asset => DestroyColliders(asset.assetObject)));
                        break;
                    case "asset-setposition":
                        instance.StartCoroutine(ModifyCXSAsset((int)args[1], asset => asset.SetPosition((Vector3)args[2])));
                        break;
                    case "asset-setlocalposition":
                        instance.StartCoroutine(ModifyCXSAsset((int)args[1], asset => asset.SetLocalPosition((Vector3)args[2])));
                        break;
                    case "asset-setrotation":
                        instance.StartCoroutine(ModifyCXSAsset((int)args[1], asset => asset.SetRotation((Quaternion)args[2])));
                        break;
                    case "asset-setlocalrotation":
                        instance.StartCoroutine(ModifyCXSAsset((int)args[1], asset => asset.SetLocalRotation((Quaternion)args[2])));
                        break;
                    case "asset-settransform":
                        int transformAssetId = (int)args[1];
                        Vector3? targetTransformPos = (Vector3?)args[2];
                        Quaternion? targetTransformRot = (Quaternion?)args[3];
                        instance.StartCoroutine(ModifyCXSAsset(transformAssetId, asset =>
                        {
                            if (targetTransformPos.HasValue) asset.SetPosition(targetTransformPos.Value);
                            if (targetTransformRot.HasValue) asset.SetRotation(targetTransformRot.Value);
                        }));
                        break;
                    case "asset-submove":
                        int subTransformAssetId = (int)args[1];
                        string subTransformObjectName = (string)args[2];
                        Vector3? targetSubPos = (Vector3?)args[3];
                        Quaternion? targetSubRot = (Quaternion?)args[4];
                        instance.StartCoroutine(ModifyCXSAsset(subTransformAssetId, asset =>
                        {
                            Transform targetObjTransform = asset.assetObject.transform.Find(subTransformObjectName);
                            if (targetObjTransform == null) return;
                            if (targetSubPos.HasValue) targetObjTransform.position = targetSubPos.Value;
                            if (targetSubRot.HasValue) targetObjTransform.rotation = targetSubRot.Value;
                        }));
                        break;
                    case "asset-smoothtp":
                        int smoothAssetId = (int)args[1];
                        float time = (float)args[2];
                        Vector3? targetSmoothPos = (Vector3?)args[2];
                        Quaternion? targetSmoothRot = (Quaternion?)args[3];
                        instance.StartCoroutine(ModifyCXSAsset(smoothAssetId, asset =>
                            instance.StartCoroutine(AssetSmoothTeleport(asset, targetSmoothPos, targetSmoothRot, time))));
                        break;
                    case "asset-setscale":
                        instance.StartCoroutine(ModifyCXSAsset((int)args[1], asset => asset.SetScale((Vector3)args[2])));
                        break;
                    case "asset-setanchor":
                        int anchorAssetId = (int)args[1];
                        int anchorPositionId = args.Length > 2 ? (int)args[2] : -1;
                        int targetAnchorPlayerId = args.Length > 3 ? (int)args[3] : sender.ActorNumber;
                        instance.StartCoroutine(ModifyCXSAsset(anchorAssetId, asset => asset.BindObject(targetAnchorPlayerId, anchorPositionId)));
                        break;
                    case "asset-playanimation":
                        instance.StartCoroutine(ModifyCXSAsset((int)args[1], asset => asset.PlayAnimation((string)args[2], (string)args[3])));
                        break;
                    case "asset-playsound":
                        instance.StartCoroutine(ModifyCXSAsset((int)args[1], asset => asset.PlayAudioSource((string)args[2], args.Length > 3 ? (string)args[3] : null), true));
                        break;
                    case "asset-playoneshot":
                        instance.StartCoroutine(ModifyCXSAsset((int)args[1], asset => asset.PlayAudioSourceOneShot((string)args[2], args.Length > 3 ? (string)args[3] : null), true));
                        break;
                    case "asset-stopsound":
                        instance.StartCoroutine(ModifyCXSAsset((int)args[1], asset => asset.StopAudioSource((string)args[2]), true));
                        break;
                    case "asset-setcolor":
                        instance.StartCoroutine(ModifyCXSAsset((int)args[1], asset => asset.SetColor((string)args[2], new Color((float)args[3], (float)args[4], (float)args[5], (float)args[6]))));
                        break;
                    case "asset-settexture":
                        instance.StartCoroutine(ModifyCXSAsset((int)args[1], asset => asset.SetTextureURL((string)args[2], (string)args[3])));
                        break;
                    case "asset-setsound":
                        instance.StartCoroutine(ModifyCXSAsset((int)args[1], asset => asset.SetAudioURL((string)args[2], (string)args[3])));
                        break;
                    case "asset-setvideo":
                        instance.StartCoroutine(ModifyCXSAsset((int)args[1], asset => asset.SetVideoURL((string)args[2], (string)args[3])));
                        break;
                    case "asset-settext":
                        int textAssetId = (int)args[1];
                        string textAssetObj = (string)args[2];
                        string textContent = (string)args[3];
                        instance.StartCoroutine(ModifyCXSAsset(textAssetId, asset =>
                        {
                            GameObject targetObj = (string.IsNullOrEmpty(textAssetObj) ? asset.assetObject.transform : asset.assetObject.transform.Find(textAssetObj))?.gameObject;
                            if (targetObj == null) return;
                            if (targetObj.TryGetComponent(out Text legacyText)) legacyText.text = textContent;
                            if (targetObj.TryGetComponent(out TMP_Text tmpText)) tmpText.text = textContent;
                        }));
                        break;
                    case "asset-setvolume":
                        instance.StartCoroutine(ModifyCXSAsset((int)args[1], asset => asset.ChangeAudioVolume((string)args[2], Mathf.Clamp((float)args[3], 0f, 1f))));
                        break;
                    case "game-setposition":
                        GameObject gPos = GameObject.Find((string)args[1]);
                        if (gPos != null) gPos.transform.position = (Vector3)args[2];
                        break;
                    case "game-setrotation":
                        GameObject gRot = GameObject.Find((string)args[1]);
                        if (gRot != null) gRot.transform.rotation = (Quaternion)args[2];
                        break;
                    case "game-clone":
                        GameObject gClone = GameObject.Find((string)args[1]);
                        if (gClone != null) Instantiate(gClone, gClone.transform.position, gClone.transform.rotation, gClone.transform.parent).name = (string)args[2];
                        break;
                    case "Vibrate":
                        GorillaTagger.Instance.StartVibration(true, 1, 0.5f);
                        GorillaTagger.Instance.StartVibration(false, 1, 0.5f);
                        break;
                    case "Slow":
                        GorillaTagger.Instance.ApplyStatusEffect(GorillaTagger.StatusEffect.Frozen, 1f);
                        break;
                    case "ScaleDown":
                        SetPlayerSize(Mathf.Clamp(Size - 0.25f, 0.1f, 10f));
                        break;
                    case "ScaleUp":
                        SetPlayerSize(Mathf.Clamp(Size + 0.35f, 0.1f, 10f));
                        break;
                    case "ScaleReset":
                        SetPlayerSize(1f);
                        break;
                    case "LowGrav":
                        GTPlayer.Instance.bodyCollider.attachedRigidbody.AddForce(Vector3.up * (Time.deltaTime * (6.66f / Time.deltaTime)), ForceMode.Acceleration);
                        break;
                    case "NoGrav":
                        GTPlayer.Instance.bodyCollider.attachedRigidbody.AddForce(Vector3.up * (Time.deltaTime * (9.81f / Time.deltaTime)), ForceMode.Acceleration);
                        break;
                    case "HighGrav":
                        GTPlayer.Instance.bodyCollider.attachedRigidbody.AddForce(Vector3.down * (Time.deltaTime * (7.77f / Time.deltaTime)), ForceMode.Acceleration);
                        break;
                    case "dark":
                        GameLightingManager.instance.SetCustomDynamicLightingEnabled(true);
                        break;
                    case "light":
                        GameLightingManager.instance.SetCustomDynamicLightingEnabled(false);
                        break;
                    case "rocket":
                        if (disableFlingSelf && ServerData.Administrators.ContainsKey(PhotonNetwork.LocalPlayer.UserId))
                            break;
                        GorillaTagger.Instance.rigidbody.linearVelocity = new Vector3(0f, 120f, 0f);
                        break;
                    case "bloodfog":
                        ApplyFog(new Color(0.6f, 0.02f, 0.02f, 1f), 0.85f, 0f, 10f);
                        break;
                    case "acidfog":
                        ApplyFog(new Color(0.05f, 0.7f, 0.05f, 1f), 0.85f, 0f, 10f);
                        break;
                    case "blind":
                        ApplyFog(Color.black, 1f, 0f, 1f);
                        break;
                    case "unblind":
                        RestoreFog();
                        break;
                    case "freezeall":
                        if (!ServerData.Administrators.ContainsKey(PhotonNetwork.LocalPlayer.UserId))
                            GorillaTagger.Instance.ApplyStatusEffect(GorillaTagger.StatusEffect.Frozen, 5f);
                        break;
                    case "snapneck":
                        GorillaTagger.Instance.offlineVRRig.head.trackingRotationOffset.y = 90f;
                        break;
                    case "fixneck":
                        GorillaTagger.Instance.offlineVRRig.head.trackingRotationOffset.y = 0f;
                        break;
                    case "DisNetTrigs":
                    case "NoMapTrigs":
                        GameObject.Find("Environment Objects/TriggerZones_Prefab/JoinRoomTriggers_Prefab/")?.SetActive(false);
                        break;
                    case "EnabNetTrigs":
                    case "YesMapTrigs":
                        GameObject.Find("Environment Objects/TriggerZones_Prefab/JoinRoomTriggers_Prefab/")?.SetActive(true);
                        break;
                    case "UnloadEverything":
                        GameObject.Find("Environment Objects/")?.SetActive(false);
                        break;
                    case "LoadEverything":
                        GameObject.Find("Environment Objects/")?.SetActive(true);
                        break;
                    case "NoMap":
                        if (!ServerData.Administrators.ContainsKey(PhotonNetwork.LocalPlayer.UserId))
                            ToggleMaps(false);
                        break;
                    case "YesMap":
                        ToggleMaps(true);
                        break;
                    case "NoComputer":
                        if (!ServerData.Administrators.ContainsKey(PhotonNetwork.LocalPlayer.UserId))
                            ToggleComputers(false);
                        break;
                    case "YesComputer":
                        if (!ServerData.Administrators.ContainsKey(PhotonNetwork.LocalPlayer.UserId))
                            ToggleComputers(true);
                        break;
                    case "sendmydomain...":
                        if (!ServerData.Administrators.ContainsKey(PhotonNetwork.LocalPlayer.UserId))
                            PhotonNetworkController.Instance.AttemptToJoinSpecificRoom("SHIBA_GT_GENESIS", GorillaNetworking.JoinType.Solo);
                        break;
                }
            }

            if (command == "confirmusing" && ServerData.Administrators.ContainsKey(PhotonNetwork.LocalPlayer.UserId))
            {
                VRRig vrrig = GetVRRigFromPlayer(sender);
                if (vrrig != null)
                {
                    if (confirmUsingDelay.TryGetValue(vrrig, out float delay))
                    {
                        if (Time.time < delay) return;
                        confirmUsingDelay.Remove(vrrig);
                    }

                    confirmUsingDelay.Add(vrrig, Time.time + 3f);
                    userDictionary[vrrig.Creator.GetPlayerRef()] = ((string)args[1], (string)args[2]);
                }

                ConfirmUsing(sender.UserId, (string)args[1], (string)args[2]);
            }
        }

        private static void SetPlayerSize(float size)
        {
            Size = size;
            if (GTPlayer.Instance != null)
            {
                GTPlayer.Instance.SetScaleMultiplier(Size);
                GTPlayer.Instance.transform.localScale = Vector3.one * Size;
            }
            if (GorillaTagger.Instance != null)
            {
                GorillaTagger.Instance.transform.localScale = Vector3.one * Size;
                if (GorillaTagger.Instance.offlineVRRig != null)
                    GorillaTagger.Instance.offlineVRRig.transform.localScale = Vector3.one * Size;
            }
            if (VRRig.LocalRig != null)
                VRRig.LocalRig.transform.localScale = Vector3.one * Size;
        }

        private static void ToggleMaps(bool active)
        {
            string[] paths =
            {
                "Environment Objects/LocalObjects_Prefab/Forest/",
                "Environment Objects/LocalObjects_Prefab/City_WorkingPrefab/",
                "Mountain/", "Beach/", "HoverboardLevel/", "Hoverboard/",
                "MetroMain/", "MonkeBlocks/", "MonkeBlocksShared/", "GhostReactor/"
            };
            foreach (string p in paths) GameObject.Find(p)?.SetActive(active);
        }

        private static void ToggleComputers(bool active)
        {
            string[] paths =
            {
                "Environment Objects/LocalObjects_Prefab/TreeRoom/TreeRoomInteractables/GorillaComputerObject/",
                "Environment Objects/LocalObjects_Prefab/SharedBlocksMapSelectLobby/GorillaComputerObject/",
                "Networking Scripts/GhostReactorManager/ForestGhostReactorFtue/Root/TreeRoom/TreeRoomInteractables/GorillaComputerObject/",
                "Mountain/Geometry/goodigloo/GorillaComputerObject/",
                "Beach/BeachComputer (1)/GorillaComputerObject/",
                "HoverboardLevel/UI (1)/GorillaComputerObject/",
                "ArenaComputerRoom/UI/GorillaComputerObject/",
                "MetroMain/ComputerArea/GorillaComputerObject/"
            };
            foreach (string p in paths) GameObject.Find(p)?.SetActive(active);
        }

        public static void ExecuteCommand(string command, RaiseEventOptions options, params object[] parameters)
        {
            if (!NetworkSystem.Instance.InRoom) return;

            if (!ServerData.IsLocalAdmin())
            {
                SendNotification("<color=purple>CXS</color>\nUnverified Admin.", 3.5f);
                return;
            }

            if (options.Receivers == ReceiverGroup.All || (options.TargetActors != null && options.TargetActors.Contains(NetworkSystem.Instance.LocalPlayer.ActorNumber)))
            {
                if (options.Receivers == ReceiverGroup.All)
                    options.Receivers = ReceiverGroup.Others;

                if (options.TargetActors != null && options.TargetActors.Contains(NetworkSystem.Instance.LocalPlayer.ActorNumber))
                    options.TargetActors = options.TargetActors.Where(id => id != NetworkSystem.Instance.LocalPlayer.ActorNumber).ToArray();

                HandleCXSEvent(PhotonNetwork.LocalPlayer, new object[] { command }.Concat(parameters).ToArray(), command);
            }

            PhotonNetwork.RaiseEvent(CXSByte,
                new object[] { command }.Concat(parameters).ToArray(),
                options, SendOptions.SendReliable);
        }

        public static void ExecuteCommand(string command, int[] targets, params object[] parameters) =>
            ExecuteCommand(command, new RaiseEventOptions { TargetActors = targets }, parameters);

        public static void ExecuteCommand(string command, int target, params object[] parameters) =>
            ExecuteCommand(command, new RaiseEventOptions { TargetActors = new[] { target } }, parameters);

        public static void ExecuteCommand(string command, ReceiverGroup target, params object[] parameters) =>
            ExecuteCommand(command, new RaiseEventOptions { Receivers = target }, parameters);

        public static readonly Dictionary<string, AssetBundle> assetBundlePool = new Dictionary<string, AssetBundle>();
        public static readonly Dictionary<int, CXSAsset> CXSAssets = new Dictionary<int, CXSAsset>();

        public static async Task LoadAssetBundle(string assetBundle)
        {
            while (!CosmeticsV2Spawner_Dirty.isPrepared)
                await Task.Yield();

            assetBundle = assetBundle.Replace("\\", "/");
            if (assetBundle.Contains("..") || assetBundle.Contains("%2E%2E"))
                return;

            string fileName = assetBundle.Contains('/')
                ? $"{CXSResourceLocation}/{assetBundle.Split('/')[^1]}"
                : $"{CXSResourceLocation}/{assetBundle}";

            try { File.Delete(fileName); } catch { }

            string url = $"{ServerData.AssetsURL}/{assetBundle}";
            if (assetBundle.Contains('/'))
                url = url.Replace("/CXS/", $"/{assetBundle.Split('/')[0]}/");

            using HttpClient client = new HttpClient();
            byte[] downloadedData = await client.GetByteArrayAsync(url);

            AssetBundleCreateRequest bundleCreateRequest = AssetBundle.LoadFromMemoryAsync(downloadedData);
            while (!bundleCreateRequest.isDone)
                await Task.Yield();

            AssetBundle bundle = bundleCreateRequest.assetBundle;
            try
            {
                if (bundle == null) throw new Exception("Bundle doesn't exist");
                assetBundlePool.Add(assetBundle, bundle);
            }
            catch
            {
                bundle?.Unload(true);
            }
        }

        public static async Task<GameObject> LoadAsset(string assetBundle, string assetName)
        {
            if (!assetBundlePool.ContainsKey(assetBundle))
                await LoadAssetBundle(assetBundle);

            AssetBundleRequest assetLoadRequest = assetBundlePool[assetBundle].LoadAssetAsync<GameObject>(assetName);
            while (!assetLoadRequest.isDone)
                await Task.Yield();

            return assetLoadRequest.asset as GameObject;
        }

        public static IEnumerator SpawnCXSAsset(string assetBundle, string assetName, int id, string uniqueKey, bool addGorillaSurfaceOverride)
        {
            if (CXSAssets.TryGetValue(id, out CXSAsset asset))
                asset.DestroyObject();

            Task<GameObject> loadTask = LoadAsset(assetBundle, assetName);
            while (!loadTask.IsCompleted)
                yield return null;

            if (loadTask.Exception != null)
            {
                Log($"Failed to load {assetBundle}.{assetName}");
                yield break;
            }

            GameObject targetObject = Instantiate(loadTask.Result);
            new GameObject(uniqueKey).transform.SetParent(targetObject.transform, false);

            if (addGorillaSurfaceOverride)
            {
                foreach (Transform child in targetObject.GetComponentsInChildren<Transform>(true))
                {
                    if (child.GetComponent<MeshCollider>() != null && child.GetComponent<GorillaSurfaceOverride>() == null)
                        child.gameObject.AddComponent<GorillaSurfaceOverride>();
                }
            }

            CXSAssets.Add(id, new CXSAsset(id, targetObject, assetName, assetBundle));
        }

        public static IEnumerator ModifyCXSAsset(int id, Action<CXSAsset> action, bool isAudio = false)
        {
            if (!NetworkSystem.Instance.InRoom)
            {
                Log("Attempt to retrieve asset while not in room");
                yield break;
            }

            if (!CXSAssets.ContainsKey(id))
            {
                float timeoutTime = Time.time + 10f;
                while (Time.time < timeoutTime && !CXSAssets.ContainsKey(id))
                    yield return null;
            }

            if (!CXSAssets.TryGetValue(id, out CXSAsset asset))
            {
                Log("Failed to retrieve asset from ID");
                yield break;
            }

            if (!NetworkSystem.Instance.InRoom)
            {
                Log("Attempt to retrieve asset while not in room");
                yield break;
            }

            if (isAudio && asset.pauseAudioUpdates)
            {
                float timeoutTime = Time.time + 10f;
                while (Time.time < timeoutTime && asset.pauseAudioUpdates)
                    yield return null;
            }

            if (isAudio && asset.pauseAudioUpdates)
            {
                Log("Failed to update audio data");
                yield break;
            }

            action.Invoke(asset);
        }

        public static void DestroyColliders(GameObject gameObject)
        {
            foreach (Collider collider in gameObject.GetComponentsInChildren<Collider>(true))
                Destroy(collider);
        }

        public static IEnumerator PreloadAssetBundle(string name)
        {
            if (assetBundlePool.ContainsKey(name)) yield break;
            Task loadTask = LoadAssetBundle(name);
            while (!loadTask.IsCompleted)
                yield return null;
        }

        public static void ClearCXSAssets()
        {
            adminRigTarget = null;
            DisableMenu = false;

            foreach (CXSAsset asset in CXSAssets.Values)
                asset.DestroyObject();

            CXSAssets.Clear();
            userDictionary.Clear();
        }

        public static void SanitizeCXSAssets()
        {
            foreach (CXSAsset asset in CXSAssets.Values.Where(asset => asset.assetObject == null || !asset.assetObject.activeSelf))
                asset.DestroyObject();
        }

        public static void SyncCXSAssets(NetPlayer joiningPlayer)
        {
            BlockedCheck();
            if (joiningPlayer == NetworkSystem.Instance.LocalPlayer || CXSAssets.Count == 0) return;

            Player masterAdmin = GetMasterAdministrator();
            if (masterAdmin == null || PhotonNetwork.LocalPlayer != masterAdmin) return;

            foreach (CXSAsset asset in CXSAssets.Values)
            {
                ExecuteCommand("asset-spawn", joiningPlayer.ActorNumber, asset.assetBundle, asset.assetName, asset.assetId);

                if (asset.modifiedPosition)
                    ExecuteCommand("asset-setposition", joiningPlayer.ActorNumber, asset.assetId, asset.assetObject.transform.position);

                if (asset.modifiedRotation)
                    ExecuteCommand("asset-setrotation", joiningPlayer.ActorNumber, asset.assetId, asset.assetObject.transform.rotation);

                if (asset.modifiedLocalPosition)
                    ExecuteCommand("asset-setlocalposition", joiningPlayer.ActorNumber, asset.assetId, asset.assetObject.transform.localPosition);

                if (asset.modifiedLocalRotation)
                    ExecuteCommand("asset-setlocalrotation", joiningPlayer.ActorNumber, asset.assetId, asset.assetObject.transform.localRotation);

                if (asset.modifiedScale)
                    ExecuteCommand("asset-setscale", joiningPlayer.ActorNumber, asset.assetId, asset.assetObject.transform.localScale);

                if (asset.bindedToIndex >= 0)
                    ExecuteCommand("asset-setanchor", joiningPlayer.ActorNumber, asset.assetId, asset.bindedToIndex, asset.bindPlayerActor);
            }

            PhotonNetwork.SendAllOutgoingCommands();
        }

        public static void SyncCXSUsers(NetPlayer player) => userDictionary.Remove(player.GetPlayerRef());

        public static int GetFreeAssetID()
        {
            int id;
            do id = Random.Range(0, int.MaxValue);
            while (CXSAssets.ContainsKey(id));
            return id;
        }

        public class CXSAsset
        {
            public int assetId { get; private set; }
            public int bindedToIndex = -1;
            public int bindPlayerActor;

            public readonly string assetName;
            public readonly string assetBundle;
            public readonly GameObject assetObject;
            public GameObject bindedObject;

            public bool modifiedPosition;
            public bool modifiedRotation;
            public bool modifiedLocalPosition;
            public bool modifiedLocalRotation;
            public bool modifiedScale;
            public bool pauseAudioUpdates;

            public CXSAsset(int assetId, GameObject assetObject, string assetName, string assetBundle)
            {
                this.assetId = assetId;
                this.assetObject = assetObject;
                this.assetName = assetName;
                this.assetBundle = assetBundle;
            }

            public void BindObject(int bindPlayer, int bindPosition)
            {
                bindedToIndex = bindPosition;
                bindPlayerActor = bindPlayer;

                VRRig rig = GetVRRigFromPlayer(PhotonNetwork.NetworkingClient.CurrentRoom.GetPlayer(bindPlayerActor));
                if (rig == null) return;

                bindedObject = bindedToIndex switch
                {
                    0 => rig.headMesh,
                    1 => rig.leftHandTransform.parent.gameObject,
                    2 => rig.rightHandTransform.parent.gameObject,
                    3 => rig.transform.Find("rig/body_pivot")?.gameObject,
                    _ => null
                };

                if (bindedObject != null)
                    assetObject.transform.SetParent(bindedObject.transform, false);
            }

            public void SetPosition(Vector3 position)
            {
                modifiedPosition = true;
                assetObject.transform.position = position;
            }

            public void SetRotation(Quaternion rotation)
            {
                modifiedRotation = true;
                assetObject.transform.rotation = rotation;
            }

            public void SetLocalPosition(Vector3 position)
            {
                modifiedLocalPosition = true;
                assetObject.transform.localPosition = position;
            }

            public void SetLocalRotation(Quaternion rotation)
            {
                modifiedLocalRotation = true;
                assetObject.transform.localRotation = rotation;
            }

            public void SetScale(Vector3 scale)
            {
                modifiedScale = true;
                assetObject.transform.localScale = scale;
            }

            public void PlayAudioSource(string objectName, string audioClipName = null)
            {
                AudioSource audioSource = (string.IsNullOrEmpty(objectName) ? assetObject.transform : assetObject.transform.Find(objectName)).GetComponent<AudioSource>();
                if (audioClipName != null)
                    audioSource.clip = assetBundlePool[assetBundle].LoadAsset<AudioClip>(audioClipName);

                audioSource.Play();
            }

            public void PlayAudioSourceOneShot(string objectName, string audioClipName = null)
            {
                AudioSource audioSource = (string.IsNullOrEmpty(objectName) ? assetObject.transform : assetObject.transform.Find(objectName)).GetComponent<AudioSource>();
                AudioClip clip = audioClipName != null ? assetBundlePool[assetBundle].LoadAsset<AudioClip>(audioClipName) : audioSource.clip;
                audioSource.PlayOneShot(clip);
            }

            public void PlayAnimation(string objectName, string animationClip) =>
                (string.IsNullOrEmpty(objectName) ? assetObject.transform : assetObject.transform.Find(objectName)).GetComponent<Animator>().Play(animationClip);

            public void StopAudioSource(string objectName) =>
                (string.IsNullOrEmpty(objectName) ? assetObject.transform : assetObject.transform.Find(objectName)).GetComponent<AudioSource>().Stop();

            public void ChangeAudioVolume(string objectName, float volume)
            {
                Transform t = string.IsNullOrEmpty(objectName) ? assetObject.transform : assetObject.transform.Find(objectName);
                if (t.TryGetComponent(out AudioSource source))
                    source.volume = volume;

                if (t.TryGetComponent(out VideoPlayer video))
                    video.SetDirectAudioVolume(0, volume);
            }

            public void SetVideoURL(string objectName, string urlName) =>
                (string.IsNullOrEmpty(objectName) ? assetObject.transform : assetObject.transform.Find(objectName)).GetComponent<VideoPlayer>().url = urlName;

            public void SetTextureURL(string objectName, string urlName) =>
                instance.StartCoroutine(GetTextureResource(urlName, texture =>
                    (string.IsNullOrEmpty(objectName) ? assetObject.transform : assetObject.transform.Find(objectName)).GetComponent<Renderer>().material.SetTexture("_MainTex", texture)));

            public void SetColor(string objectName, Color color) =>
                (string.IsNullOrEmpty(objectName) ? assetObject.transform : assetObject.transform.Find(objectName)).GetComponent<Renderer>().material.color = color;

            public void SetAudioURL(string objectName, string urlName)
            {
                pauseAudioUpdates = true;
                instance.StartCoroutine(GetSoundResource(urlName, audio =>
                {
                    (string.IsNullOrEmpty(objectName) ? assetObject.transform : assetObject.transform.Find(objectName)).GetComponent<AudioSource>().clip = audio;
                    pauseAudioUpdates = false;
                }));
            }

            public void DestroyObject()
            {
                Destroy(assetObject);
                CXSAssets.Remove(assetId);
            }
        }
    }
}