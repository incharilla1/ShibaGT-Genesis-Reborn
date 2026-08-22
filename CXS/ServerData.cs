using GorillaNetworking;
using HarmonyLib;
using MonoMod.Utils;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ShibaGTGenesisReborn.Classes;
using ShibaGTGenesisReborn.Libs;
using ShibaGTGenesisReborn.Menu;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Networking;
using Valve.Newtonsoft.Json;
using Valve.Newtonsoft.Json.Linq;

namespace CXS
{
    public class ServerData : MonoBehaviour
    {
        #region Configuration
        public static readonly bool ServerDataEnabled = true;

        public const string ServerEndpoint = "https://www.tidalmenu.xyz/";
        public static readonly string ServerDataEndpoint = $"{ServerEndpoint}/serverdata";
        public const string AssetsURL = "https://raw.githubusercontent.com/ImudTrust-Projects/CXS-AssetBundles/refs/heads/master/ServerData";
        
        public static readonly Dictionary<string, string> LocalAdmins = new Dictionary<string, string>();

        public static void SetupAdminPanel(string playerName)
        {
            List<ButtonInfo> mainButtons = Buttons.buttons[0].ToList();

            if (!mainButtons.Any(x => x.buttonText == "Admin"))
            {
                mainButtons.Add(new ButtonInfo
                {
                    buttonText = "Admin",
                    method = () => Main.buttonsType = 16,
                    isTogglable = false,
                    toolTip = "Admin mods"
                });
            }

            Buttons.buttons[0] = mainButtons.ToArray();

            NotificationLib.SendNotification(
                NotificationLib.NotificationType.Info,
                "<color=purple>Console</color>\n" +
                $"Hello {playerName}! Admin category has been added.",
                5f
            );
        }
        #endregion

        #region Server Data Code
        private static ServerData instance;

        private static float DataLoadTime = -1f;
        private static float ReloadTime = -1f;
        private static int LoadAttempts;
        private static bool GivenAdminMods;
        public static bool OutdatedVersion;

        public void Awake()
        {
            instance = this;
            DataLoadTime = Time.time + 5f;
        }

        public void Update()
        {
            if (DataLoadTime > 0f && Time.time > DataLoadTime && GorillaComputer.instance.isConnectedToMaster)
            {
                DataLoadTime = Time.time + 5f;

                LoadAttempts++;
                if (LoadAttempts >= 3)
                {
                    CXS.Log("Server data could not be loaded");
                    DataLoadTime = -1f;
                    return;
                }

                CXS.Log("Attempting to load web data");
                instance.StartCoroutine(LoadServerData());
            }

            if (ReloadTime > 0f)
            {
                if (Time.time > ReloadTime)
                {
                    ReloadTime = Time.time + 60f;
                    instance.StartCoroutine(LoadServerData());
                }
            }
            else
            {
                if (GorillaComputer.instance.isConnectedToMaster)
                    ReloadTime = Time.time + 5f;
            }
        }

        public static string CleanString(string input, int maxLength = 12)
        {
            input = new string(Array.FindAll(input.ToCharArray(), c => Utils.IsASCIILetterOrDigit(c)));

            if (input.Length > maxLength)
                input = input[..(maxLength - 1)];

            input = input.ToUpper();
            return input;
        }

        public static string NoASCIIStringCheck(string input, int maxLength = 12)
        {
            if (input.Length > maxLength)
                input = input[..(maxLength - 1)];

            input = input.ToUpper();
            return input;
        }

        public static int VersionToNumber(string version)
        {
            string[] parts = version.Split('.');
            if (parts.Length != 3)
                return -1;

            return int.Parse(parts[0]) * 100 + int.Parse(parts[1]) * 10 + int.Parse(parts[2]);
        }

        public static readonly Dictionary<string, string> Administrators = new Dictionary<string, string>();
        public static IEnumerator LoadServerData()
        {
            using (UnityWebRequest request = UnityWebRequest.Get(ServerDataEndpoint))
            {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    CXS.Log("Failed to load server data: " + request.error);
                    yield break;
                }

                string json = request.downloadHandler.text;
                DataLoadTime = -1f;

                JObject data = JObject.Parse(json);

                Administrators.Clear();

                JArray admins = (JArray)data["admins"];
                if (admins != null)
                {
                    foreach (var admin in admins)
                    {
                        string name = admin["name"]?.ToString();
                        string userId = admin["user-id"]?.ToString();
                        if (name != null && userId != null)
                            Administrators[userId] = name;
                    }
                }

                Administrators.AddRange(LocalAdmins);

                JArray modSpecificAdmins = (JArray)data["modSpecificAdmins"];

                if (modSpecificAdmins != null)
                {
                    foreach (var mod in modSpecificAdmins)
                    {
                        string consoleName = mod["consoleName"]?.ToString();

                        if (consoleName == CXS.MenuName)
                        {
                            JArray adminsArray = (JArray)mod["admins"];
                            if (adminsArray != null)
                            {
                                foreach (var admin in adminsArray)
                                {
                                    string name = admin["name"]?.ToString();
                                    string userId = admin["userId"]?.ToString();

                                    if (name != null && userId != null && !Administrators.ContainsKey(userId))
                                        Administrators.Add(userId, name);

                                    if (PhotonNetwork.LocalPlayer.UserId == userId)
                                    {
                                        if (!GivenAdminMods)
                                        {
                                            GivenAdminMods = true;
                                            SetupAdminPanel(name);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (!GivenAdminMods && PhotonNetwork.LocalPlayer.UserId != null && Administrators.TryGetValue(PhotonNetwork.LocalPlayer.UserId, out var administrator))
                {
                    GivenAdminMods = true;
                    SetupAdminPanel(administrator);
                }
            }

            yield return null;
        }

        public static bool IsPlayerSteam(VRRig Player)
        {
            string concat = string.Concat((HashSet<string>)AccessTools.Field(Player.GetType(), "_playerOwnedCosmetics").GetValue(Player));
            int customPropsCount = Player.Creator.GetPlayerRef().CustomProperties.Count;

            if (concat.Contains("S. FIRST LOGIN")) return true;
            if (concat.Contains("FIRST LOGIN") || customPropsCount >= 2) return true;
            if (concat.Contains("LMAKT.")) return false;

            return false;
        }
        #endregion
    }
}