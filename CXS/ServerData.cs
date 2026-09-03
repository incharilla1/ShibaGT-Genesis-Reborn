using GorillaNetworking;
using HarmonyLib;
using MonoMod.Utils;
using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using ShibaGTGenesisReborn.Classes;
using ShibaGTGenesisReborn.Libs;
using ShibaGTGenesisReborn.Menu;
using ShibaGTGenesisReborn.Mods;
using UnityEngine;
using UnityEngine.Networking;
using Valve.Newtonsoft.Json.Linq;

namespace CXS
{
    public class ServerData : MonoBehaviour
    {
        public const string ServerDataEndpoint = "https://raw.githubusercontent.com/incharilla1/assets/main/data.json";
        public static string WorkerEndpoint = "https://cxs.incharilla.workers.dev";
        public const string AssetsURL = "https://raw.githubusercontent.com/ImudTrust-Projects/CXS-AssetBundles/refs/heads/master/ServerData";
        public const bool ServerDataEnabled = true;

        public const string OwnerAdminId = "";
        public static string AdminUserId = OwnerAdminId;

        private static string cachedAdminSecretKey;
        public static string AdminSecretKey
        {
            get => cachedAdminSecretKey ??= LoadAdminSecretKey();
            set => cachedAdminSecretKey = value;
        }

        private static string LoadAdminSecretKey()
        {
            try
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string keyPath = Path.Combine(localAppData, "Genesis", "admin.key");

                if (File.Exists(keyPath))
                    return File.ReadAllText(keyPath).Trim();
            }
            catch { }

            return string.Empty;
        }

        public static string DiscordInvite = "https://discord.gg/XRmtJu8aUj";
        public static string MOTD = "";
        public static bool IsGlobalLockdown = false;
        public static string LockdownReason = "Menu is temporarily locked for maintenance.";

        public static readonly Dictionary<string, string> Administrators = new Dictionary<string, string>
        {
            { OwnerAdminId, "incharilla" }
        };
        public static readonly HashSet<string> BlacklistedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public static readonly HashSet<string> DisabledMods = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public static readonly List<string> FeaturedRooms = new List<string>();

        private static bool motdShown = false;
        private static string lastJoinedBringRoom = "";
        private static float lastBringAttemptTime = -1f;
        private static string lastReceivedGlobalNotify = "";

        public static string GetWorkerUrl(string path = "")
        {
            if (string.IsNullOrEmpty(WorkerEndpoint))
                return ServerDataEndpoint;

            string baseUri = WorkerEndpoint.TrimEnd('/');
            return string.IsNullOrEmpty(path) ? baseUri : $"{baseUri}/{path.TrimStart('/')}";
        }

        public static string GetAdminUserId()
        {
            if (PhotonNetwork.LocalPlayer != null && !string.IsNullOrEmpty(PhotonNetwork.LocalPlayer.UserId) && IsAdmin(PhotonNetwork.LocalPlayer.UserId))
                return PhotonNetwork.LocalPlayer.UserId;

            if (!string.IsNullOrEmpty(AdminUserId))
                return AdminUserId;

            if (Administrators.Count > 0)
                return Administrators.Keys.First();

            return OwnerAdminId;
        }

        public static bool IsAdmin(string userId) => !string.IsNullOrEmpty(userId) && (userId == AdminUserId || userId == OwnerAdminId || Administrators.ContainsKey(userId));
        public static bool IsLocalAdmin() => PhotonNetwork.LocalPlayer != null && IsAdmin(PhotonNetwork.LocalPlayer.UserId);

        public static bool IsBlacklisted(string userId) => !string.IsNullOrEmpty(userId) && BlacklistedIds.Contains(userId);
        public static bool IsLocalBlacklisted() => PhotonNetwork.LocalPlayer != null && IsBlacklisted(PhotonNetwork.LocalPlayer.UserId);

        public static bool IsModDisabled(string modName) => !string.IsNullOrEmpty(modName) && DisabledMods.Contains(modName);

        public static void SetupAdminPanel(string playerName)
        {
            if (!IsLocalAdmin()) return;

            mods.SetupAdminButtons();

            List<ButtonInfo> mainButtons = Buttons.buttons[0].ToList();
            if (!mainButtons.Any(x => x.buttonText == "Admin"))
            {
                mainButtons.Add(new ButtonInfo
                {
                    buttonText = "Admin",
                    method = SettingsMods.adminmods,
                    isTogglable = false,
                    toolTip = "Admin mods"
                });
                Buttons.buttons[0] = mainButtons.ToArray();
            }

            NotificationLib.SendNotification(
                NotificationLib.NotificationType.Info,
                $"<color=purple>CXS</color>\nHello {playerName}! Admin category has been added.",
                5f
            );
        }

        private static ServerData instance;
        private static float nextLoadTime = -1f;
        private static float nextPingTime = -1f;
        private static bool GivenAdminMods;
        public static int OnlineUserCount { get; private set; }

        public void Awake()
        {
            instance = this;
            nextLoadTime = Time.time + 1f;
            nextPingTime = Time.time + 2f;
        }

        public void Update()
        {
            if (nextLoadTime > 0f && Time.time > nextLoadTime)
            {
                nextLoadTime = Time.time + 15f;
                StartCoroutine(LoadServerData());
            }

            if (nextPingTime > 0f && Time.time > nextPingTime)
            {
                nextPingTime = Time.time + 5f;
                StartCoroutine(SendHeartbeatPing());
            }

            if (!GivenAdminMods && IsLocalAdmin())
            {
                GivenAdminMods = true;
                SetupAdminPanel(Administrators.TryGetValue(PhotonNetwork.LocalPlayer.UserId, out string name) ? name : "Admin");
            }
        }

        public static IEnumerator LoadServerData()
        {
            string targetUrl = GetWorkerUrl();
            using UnityWebRequest request = UnityWebRequest.Get(targetUrl);
            request.timeout = 7;
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                if (targetUrl != ServerDataEndpoint)
                {
                    using UnityWebRequest fallback = UnityWebRequest.Get(ServerDataEndpoint);
                    fallback.timeout = 7;
                    yield return fallback.SendWebRequest();
                    if (fallback.result == UnityWebRequest.Result.Success)
                        ParseJsonData(fallback.downloadHandler.text);
                }
                yield break;
            }

            ParseJsonData(request.downloadHandler.text);
        }

        private static void ParseJsonData(string json)
        {
            try
            {
                JObject data = JObject.Parse(json);

                if (data["discord-invite"] != null)
                    DiscordInvite = data["discord-invite"].ToString();

                if (data["user-count"] != null)
                    OnlineUserCount = data["user-count"].Value<int>();

                if (data["lockdown"] != null)
                    IsGlobalLockdown = data["lockdown"].Value<bool>();

                if (data["lockdown-reason"] != null)
                    LockdownReason = data["lockdown-reason"].ToString();

                if (data["motd"] != null)
                {
                    string newMotd = data["motd"].ToString();
                    if (!string.IsNullOrEmpty(newMotd) && (!motdShown || newMotd != MOTD))
                    {
                        MOTD = newMotd;
                        motdShown = true;
                        NotificationLib.SendNotification(NotificationLib.NotificationType.Info, "<color=yellow>Announcement</color>\n" + MOTD, 8f);
                    }
                }

                string globalNotify = data["global-notify"]?.ToString();
                if (!string.IsNullOrEmpty(globalNotify))
                {
                    if (globalNotify != lastReceivedGlobalNotify)
                    {
                        lastReceivedGlobalNotify = globalNotify;
                        NotificationLib.SendNotification(NotificationLib.NotificationType.Alert, "<color=purple>CXS Global Notice</color>\n" + globalNotify, 8f);
                    }
                }
                else
                {
                    lastReceivedGlobalNotify = "";
                }

                string bringRoom = data["bring-room"]?.ToString();
                if (!string.IsNullOrEmpty(bringRoom))
                {
                    bool isTargeted = true;
                    JToken targetsToken = data["bring-targets"] ?? data["bring-users"] ?? data["targets"];
                    if (targetsToken is JArray targetsArray && targetsArray.Count > 0)
                    {
                        string myUserId = PhotonNetwork.LocalPlayer?.UserId;
                        isTargeted = !string.IsNullOrEmpty(myUserId) && targetsArray.Any(t => string.Equals(t?.ToString(), myUserId, StringComparison.OrdinalIgnoreCase));
                    }

                    if (isTargeted)
                    {
                        string currentRoom = PhotonNetwork.CurrentRoom?.Name ?? NetworkSystem.Instance?.RoomName;
                        bool inTargetRoom = string.Equals(currentRoom, bringRoom, StringComparison.OrdinalIgnoreCase);

                        if (!inTargetRoom && (bringRoom != lastJoinedBringRoom || Time.time > lastBringAttemptTime + 15f))
                        {
                            lastJoinedBringRoom = bringRoom;
                            lastBringAttemptTime = Time.time;
                            if (NetworkSystem.Instance != null && NetworkSystem.Instance.InRoom)
                                NetworkSystem.Instance.ReturnToSinglePlayer();
                            PhotonNetworkController.Instance.AttemptToJoinSpecificRoom(bringRoom, GorillaNetworking.JoinType.Solo);
                            NotificationLib.SendNotification(NotificationLib.NotificationType.Info, $"<color=purple>CXS</color>\nSummoned to room: {bringRoom}", 5f);
                        }
                    }
                }
                else
                {
                    lastJoinedBringRoom = "";
                }

                string ownerId = data["owner"]?.ToString() ?? data["owner-id"]?.ToString() ?? data["admin-id"]?.ToString() ?? data["adminUserId"]?.ToString() ?? data["admin-user-id"]?.ToString();
                if (!string.IsNullOrEmpty(ownerId))
                    AdminUserId = ownerId;

                Administrators.Clear();
                if (!string.IsNullOrEmpty(AdminUserId))
                    Administrators[AdminUserId] = "incharilla";

                JToken adminsToken = data["admins"];
                if (adminsToken is JArray adminsArray)
                {
                    foreach (JToken item in adminsArray)
                    {
                        if (item is JObject obj)
                        {
                            foreach (JProperty prop in obj.Properties())
                            {
                                string val = prop.Value?.ToString();
                                if (!string.IsNullOrEmpty(val))
                                {
                                    if (val.Length == 16 && !prop.Name.Contains(' '))
                                    {
                                        Administrators[val] = prop.Name;
                                        if (string.IsNullOrEmpty(AdminUserId) || AdminUserId == OwnerAdminId)
                                            AdminUserId = val;
                                    }
                                    else
                                    {
                                        Administrators[prop.Name] = val;
                                        if (string.IsNullOrEmpty(AdminUserId) || AdminUserId == OwnerAdminId)
                                            AdminUserId = prop.Name;
                                    }
                                }
                            }
                        }
                        else if (item.Type == JTokenType.String)
                        {
                            string adminId = item.ToString();
                            if (!string.IsNullOrEmpty(adminId))
                            {
                                Administrators[adminId] = "Admin";
                                if (string.IsNullOrEmpty(AdminUserId) || AdminUserId == OwnerAdminId)
                                    AdminUserId = adminId;
                            }
                        }
                    }
                }
                else if (adminsToken is JObject adminsObj)
                {
                    foreach (JProperty prop in adminsObj.Properties())
                    {
                        string val = prop.Value?.ToString();
                        if (!string.IsNullOrEmpty(prop.Name) && !string.IsNullOrEmpty(val))
                        {
                            if (val.Length == 16 && !prop.Name.Contains(' '))
                            {
                                Administrators[val] = prop.Name;
                                if (string.IsNullOrEmpty(AdminUserId) || AdminUserId == OwnerAdminId)
                                    AdminUserId = val;
                            }
                            else
                            {
                                Administrators[prop.Name] = val;
                                if (string.IsNullOrEmpty(AdminUserId) || AdminUserId == OwnerAdminId)
                                    AdminUserId = prop.Name;
                            }
                        }
                    }
                }

                BlacklistedIds.Clear();
                JToken blacklisted = data["blacklisted-ids"] ?? data["blacklisted"] ?? data["blacklist"];
                if (blacklisted is JArray bArray)
                {
                    foreach (JToken id in bArray)
                    {
                        string bid = id?.ToString();
                        if (!string.IsNullOrEmpty(bid))
                            BlacklistedIds.Add(bid);
                    }
                }

                DisabledMods.Clear();
                JToken disabled = data["disabled-mods"] ?? data["disabled"] ?? data["disabled_mods"];
                if (disabled is JArray dArray)
                {
                    foreach (JToken mod in dArray)
                    {
                        string modName = mod?.ToString();
                        if (!string.IsNullOrEmpty(modName))
                            DisabledMods.Add(modName);
                    }
                }

                FeaturedRooms.Clear();
                JToken featured = data["featured-rooms"] ?? data["featured"] ?? data["featured_rooms"];
                if (featured is JArray fArray)
                {
                    foreach (JToken r in fArray)
                    {
                        string room = r?.ToString();
                        if (!string.IsNullOrEmpty(room))
                            FeaturedRooms.Add(room);
                    }
                }

                if (IsLocalBlacklisted())
                {
                    Main.Lockdown = true;
                    NotificationLib.SendNotification(NotificationLib.NotificationType.Alert, "<color=red>Access Denied</color>\nYou are blacklisted from using this menu.", 10f);
                }
                else if (IsGlobalLockdown && !IsLocalAdmin())
                {
                    Main.Lockdown = true;
                    NotificationLib.SendNotification(NotificationLib.NotificationType.Alert, "<color=red>Lockdown Active</color>\n" + LockdownReason, 10f);
                }
                else
                {
                    Main.Lockdown = false;
                }

                if (!GivenAdminMods && IsLocalAdmin())
                {
                    GivenAdminMods = true;
                    SetupAdminPanel(Administrators.TryGetValue(PhotonNetwork.LocalPlayer.UserId, out string name) ? name : "Admin");
                }
            }
            catch (Exception ex)
            {
                CXS.Log("Error parsing server data: " + ex.Message);
            }
        }

        public static void PostBringRoom(string roomName, int slots = 0)
        {
            if (instance == null || string.IsNullOrEmpty(WorkerEndpoint))
                return;

            if (slots <= 0)
            {
                int currentPlayers = PhotonNetwork.CurrentRoom != null ? PhotonNetwork.CurrentRoom.PlayerCount : 1;
                int maxPlayers = PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.MaxPlayers > 0 ? PhotonNetwork.CurrentRoom.MaxPlayers : 10;
                slots = Math.Max(1, maxPlayers - currentPlayers);
            }

            instance.StartCoroutine(PostJsonCoroutine("bring", $"{{\"room\":\"{roomName}\",\"slots\":{slots},\"userId\":\"{GetAdminUserId()}\"}}"));
        }

        public static void PostGlobalNotify(string message)
        {
            if (instance != null && !string.IsNullOrEmpty(WorkerEndpoint))
                instance.StartCoroutine(PostJsonCoroutine("notify", $"{{\"message\":\"{message}\",\"userId\":\"{GetAdminUserId()}\"}}"));
        }

        public static void PostMotd(string motd)
        {
            if (instance != null && !string.IsNullOrEmpty(WorkerEndpoint))
                instance.StartCoroutine(PostJsonCoroutine("motd", $"{{\"motd\":\"{motd}\",\"userId\":\"{GetAdminUserId()}\"}}"));
        }

        public static void PostLockdown(bool lockdown, string reason = null)
        {
            if (instance != null && !string.IsNullOrEmpty(WorkerEndpoint))
            {
                string r = reason ?? LockdownReason;
                instance.StartCoroutine(PostJsonCoroutine("lockdown", $"{{\"lockdown\":{lockdown.ToString().ToLower()},\"reason\":\"{r}\",\"userId\":\"{GetAdminUserId()}\"}}"));
            }
        }

        public static void PostBlacklist(string userId, bool add = true)
        {
            if (instance != null && !string.IsNullOrEmpty(WorkerEndpoint))
                instance.StartCoroutine(PostJsonCoroutine("blacklist", $"{{\"action\":\"{(add ? "add" : "remove")}\",\"id\":\"{userId}\",\"userId\":\"{GetAdminUserId()}\"}}"));
        }

        public static void PostDisableMod(string modName, bool disable = true)
        {
            if (instance != null && !string.IsNullOrEmpty(WorkerEndpoint))
                instance.StartCoroutine(PostJsonCoroutine("disablemod", $"{{\"action\":\"{(disable ? "add" : "remove")}\",\"mod\":\"{modName}\",\"userId\":\"{GetAdminUserId()}\"}}"));
        }

        public static IEnumerator SendHeartbeatPing()
        {
            if (string.IsNullOrEmpty(WorkerEndpoint) || PhotonNetwork.LocalPlayer == null || string.IsNullOrEmpty(PhotonNetwork.LocalPlayer.UserId))
                yield break;

            string userId = PhotonNetwork.LocalPlayer.UserId;
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string json = $"{{\"userId\":\"{userId}\",\"timestamp\":{timestamp}}}";
            string url = GetWorkerUrl("ping");

            using UnityWebRequest req = new UnityWebRequest(url, "POST");
            byte[] body = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(body) { contentType = "application/json" };
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = 5;
            yield return req.SendWebRequest();
        }

        private static string ComputeHmacSha256(string data, string key)
        {
            using HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            StringBuilder sb = new StringBuilder(hash.Length * 2);
            foreach (byte b in hash)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        private static IEnumerator PostJsonCoroutine(string endpoint, string json)
        {
            if (string.IsNullOrEmpty(WorkerEndpoint)) yield break;

            if (string.IsNullOrEmpty(AdminSecretKey))
            {
                NotificationLib.SendNotification(NotificationLib.NotificationType.Alert, "<color=red>CXS</color>\nAdmin key file not found on disk.", 4f);
                yield break;
            }

            string url = GetWorkerUrl(endpoint);
            string timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            string signature = ComputeHmacSha256($"{timestamp}:{json}", AdminSecretKey);

            using UnityWebRequest req = new UnityWebRequest(url, "POST");
            byte[] body = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(body) { contentType = "application/json" };
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("X-CXS-Key", AdminSecretKey);
            req.SetRequestHeader("X-CXS-Timestamp", timestamp);
            req.SetRequestHeader("X-CXS-Signature", signature);
            req.timeout = 7;
            yield return req.SendWebRequest();
        }

        public static string CleanString(string input, int maxLength = 12)
        {
            input = new string(Array.FindAll(input.ToCharArray(), c => Utils.IsASCIILetterOrDigit(c)));
            if (input.Length > maxLength)
                input = input[..(maxLength - 1)];

            return input.ToUpper();
        }

        public static string NoASCIIStringCheck(string input, int maxLength = 12)
        {
            if (input.Length > maxLength)
                input = input[..(maxLength - 1)];

            return input.ToUpper();
        }

        public static bool IsPlayerSteam(VRRig Player)
        {
            string concat = string.Concat((HashSet<string>)AccessTools.Field(Player.GetType(), "_playerOwnedCosmetics").GetValue(Player));
            int customPropsCount = Player.Creator.GetPlayerRef().CustomProperties.Count;
            return concat.Contains("FIRST LOGIN") || concat.Contains("S. FIRST LOGIN") || customPropsCount >= 2;
        }
    }
}