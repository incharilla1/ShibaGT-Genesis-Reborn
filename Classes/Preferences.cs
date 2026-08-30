using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ShibaGTGenesisReborn.Classes;
using ShibaGTGenesisReborn.Libs;
using ShibaGTGenesisReborn.Menu;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace ShibaGTGenesisReborn
{
    public static class Preferences
    {
        private static string ConfigPath => Path.Combine(ModsLib.GenesisDirectory, "genesisprefs.json");

        private class ButtonState
        {
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public bool Enabled { get; set; }

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public bool Favorite { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string OverlapText { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public InputType? VrKey { get; set; }

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public KeyCode PcKey { get; set; }

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public KeybindMode KeybindMode { get; set; }
        }

        private class SavePayload
        {
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string PresetName { get; set; }

            public Dictionary<string, ButtonState> Buttons { get; set; } = new Dictionary<string, ButtonState>();
            public Dictionary<string, object> Settings { get; set; } = new Dictionary<string, object>();
        }

        private struct SettingAccessor
        {
            public string Key;
            public string FallbackKey;
            public Type Type;
            public Func<object> Getter;
            public Action<object> Setter;
        }

        private static SettingAccessor[] _accessors;
        private static readonly JsonSerializer _serializer = new JsonSerializer
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore
        };

        private static void EnsureAccessors()
        {
            if (_accessors != null) return;
            var list = new List<SettingAccessor>();

            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
            {
                foreach (FieldInfo f in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                {
                    var attr = f.GetCustomAttribute<SettingAttribute>();
                    if (attr != null)
                    {
                        FieldInfo fi = f;
                        list.Add(new SettingAccessor
                        {
                            Key = attr.Key ?? $"{type.Name}.{fi.Name}",
                            FallbackKey = fi.Name,
                            Type = fi.FieldType,
                            Getter = () => fi.GetValue(null),
                            Setter = val => fi.SetValue(null, val)
                        });
                    }
                }

                foreach (PropertyInfo p in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                {
                    var attr = p.GetCustomAttribute<SettingAttribute>();
                    if (attr != null && p.CanRead && p.CanWrite)
                    {
                        PropertyInfo pi = p;
                        list.Add(new SettingAccessor
                        {
                            Key = attr.Key ?? $"{type.Name}.{pi.Name}",
                            FallbackKey = pi.Name,
                            Type = pi.PropertyType,
                            Getter = () => pi.GetValue(null),
                            Setter = val => pi.SetValue(null, val)
                        });
                    }
                }
            }

            _accessors = list.ToArray();
        }

        public static string PresetsDirectory => Path.Combine(ModsLib.GenesisDirectory, "Presets");
        private static int currentPresetIndex = -1;

        private static SavePayload BuildCurrentPayload()
        {
            var buttons = new Dictionary<string, ButtonState>(32);

            for (int i = 0; i < Buttons.buttons.Length; i++)
            {
                if (i == 10 || i == 11 || i == 13 || i == 14 || i == 19 || i == 20 || i == 21 || i == 22 || i == 23) continue;
                foreach (ButtonInfo btn in Buttons.buttons[i])
                {
                    if (btn == null || btn.buttonText == "-" || btn.buttonText == "Save" || btn.buttonText == "Remove All Prefs") continue;

                    bool saveEnabled = btn.enabled && !btn.buttonText.EndsWith("Gun") && btn.buttonText != "Tag All" && btn.buttonText != "Tag Self";
                    if (saveEnabled || btn.isFavorite || !string.IsNullOrEmpty(btn.overlapText) || btn.vrKey.HasValue || btn.pcKey != KeyCode.None)
                    {
                        buttons[btn.buttonText] = new ButtonState
                        {
                            Enabled = saveEnabled,
                            Favorite = btn.isFavorite,
                            OverlapText = btn.overlapText,
                            VrKey = btn.vrKey,
                            PcKey = btn.pcKey,
                            KeybindMode = btn.keybindMode
                        };
                    }
                }
            }

            EnsureAccessors();
            var settings = new Dictionary<string, object>(_accessors.Length);
            for (int i = 0; i < _accessors.Length; i++)
            {
                ref var acc = ref _accessors[i];
                settings[acc.Key] = acc.Getter();
            }

            return new SavePayload { Buttons = buttons, Settings = settings };
        }

        private static void ApplyPayload(SavePayload payload)
        {
            if (payload == null) return;
            EnsureAccessors();

            if (payload.Settings != null)
            {
                for (int i = 0; i < _accessors.Length; i++)
                {
                    ref var acc = ref _accessors[i];
                    if (payload.Settings.TryGetValue(acc.Key, out object val) || payload.Settings.TryGetValue(acc.FallbackKey, out val))
                    {
                        try
                        {
                            if (val is JToken token)
                            {
                                acc.Setter(token.ToObject(acc.Type));
                            }
                            else if (val != null)
                            {
                                acc.Setter(Convert.ChangeType(val, acc.Type));
                            }
                        }
                        catch { }
                    }
                }
            }

            Main.favoriteButtons.Clear();
            if (payload.Buttons != null)
            {
                for (int i = 0; i < Buttons.buttons.Length; i++)
                {
                    if (i == 10 || i == 11 || i == 13 || i == 14 || i == 19 || i == 20 || i == 21 || i == 22 || i == 23) continue;
                    foreach (ButtonInfo btn in Buttons.buttons[i])
                    {
                        if (btn == null || !payload.Buttons.TryGetValue(btn.buttonText, out ButtonState state)) continue;

                        btn.vrKey = state.VrKey;
                        btn.pcKey = state.PcKey;
                        btn.keybindMode = state.KeybindMode;

                        if (!string.IsNullOrEmpty(state.OverlapText))
                        {
                            btn.overlapText = state.OverlapText;
                        }

                        bool allowEnable = !btn.buttonText.EndsWith("Gun") && btn.buttonText != "Tag All" && btn.buttonText != "Tag Self";
                        if (btn.isTogglable && allowEnable && btn.enabled != state.Enabled)
                        {
                            btn.enabled = state.Enabled;
                            if (state.Enabled)
                            {
                                btn.enableMethod?.Invoke();
                                btn.method?.Invoke();
                            }
                            else
                            {
                                btn.disableMethod?.Invoke();
                            }
                        }

                        if (state.Favorite)
                        {
                            btn.isFavorite = true;
                            if (!Main.favoriteButtons.Contains(btn))
                                Main.favoriteButtons.Add(btn);
                        }
                    }
                }
            }

            SyncSettings();
            Main.UpdateFavoritesCategory();
        }

        public static void Save()
        {
            try
            {
                SavePayload payload = BuildCurrentPayload();
                Directory.CreateDirectory(ModsLib.GenesisDirectory);
                using StreamWriter sw = new StreamWriter(ConfigPath);
                using JsonTextWriter jw = new JsonTextWriter(sw);
                _serializer.Serialize(jw, payload);
            }
            catch { }
        }

        public static void Load()
        {
            if (!File.Exists(ConfigPath))
            {
                SyncSettings();
                return;
            }

            try
            {
                using StreamReader sr = new StreamReader(ConfigPath);
                using JsonTextReader jr = new JsonTextReader(sr);
                var payload = _serializer.Deserialize<SavePayload>(jr);
                if (payload == null)
                {
                    SyncSettings();
                    return;
                }

                ApplyPayload(payload);
            }
            catch
            {
                SyncSettings();
            }
        }

        public static void EnsureDirectory()
        {
            if (!Directory.Exists(PresetsDirectory))
                Directory.CreateDirectory(PresetsDirectory);
        }

        public static void OpenFolder()
        {
            EnsureDirectory();
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = PresetsDirectory,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Open presets folder error: {ex}");
            }
        }

        public static void LoadPresetFile(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    NotificationLib.SendNotification(NotificationLib.NotificationType.Alert, "Preset file not found");
                    return;
                }

                string json = File.ReadAllText(path);
                SavePayload payload = JsonConvert.DeserializeObject<SavePayload>(json);
                if (payload == null || (payload.Buttons.Count == 0 && payload.Settings.Count == 0))
                {
                    NotificationLib.SendNotification(NotificationLib.NotificationType.Alert, "Invalid preset file");
                    return;
                }

                ApplyPayload(payload);
                string name = !string.IsNullOrWhiteSpace(payload.PresetName) ? payload.PresetName : Path.GetFileNameWithoutExtension(path);
                NotificationLib.SendNotification(NotificationLib.NotificationType.Info, $"Loaded: {name}");
            }
            catch (Exception ex)
            {
                NotificationLib.SendNotification(NotificationLib.NotificationType.Alert, $"Load failed: {ex.Message}");
            }
        }

        public static void RefreshPresets(bool notify = true)
        {
            EnsureDirectory();
            List<string> presetFiles = new List<string>();
            try
            {
                presetFiles.AddRange(Directory.GetFiles(PresetsDirectory, "*.json", SearchOption.TopDirectoryOnly));
            }
            catch { }

            presetFiles = presetFiles.OrderBy(f => Path.GetFileName(f)).ToList();
            List<ButtonInfo> btnList = new List<ButtonInfo>
            {
                new ButtonInfo { buttonText = "Back", method = () => SettingsMods.safety(), isTogglable = false, toolTip = "Return to Menu Settings" },
                new ButtonInfo { buttonText = "Refresh Presets", method = () => RefreshPresets(true), isTogglable = false, toolTip = "Rescan Presets folder" },
                new ButtonInfo { buttonText = "Open Folder", method = () => OpenFolder(), isTogglable = false, toolTip = "Open Presets folder in Explorer" },
                new ButtonInfo { buttonText = "Export Preset", method = () => ExportPreset(), isTogglable = false, toolTip = "Export current preset & copy to clipboard" },
                new ButtonInfo { buttonText = "Import Preset", method = () => ImportPresetFromClipboard(), isTogglable = false, toolTip = "Import and apply preset from clipboard" },
                new ButtonInfo { buttonText = "Cycle Presets", method = () => CyclePresets(), isTogglable = false, toolTip = "Cycle and load preset files in Genesis/Presets" }
            };

            foreach (string file in presetFiles)
            {
                string name = Path.GetFileNameWithoutExtension(file);
                try
                {
                    string json = File.ReadAllText(file);
                    using var reader = new JsonTextReader(new StringReader(json));
                    var token = JToken.ReadFrom(reader);
                    string customName = token["PresetName"]?.Value<string>() ?? token["presetName"]?.Value<string>() ?? token["Name"]?.Value<string>() ?? token["name"]?.Value<string>();
                    if (!string.IsNullOrWhiteSpace(customName))
                        name = customName.Trim();
                }
                catch { }

                string path = file;
                btnList.Add(new ButtonInfo
                {
                    buttonText = name,
                    toolTip = $"Load preset: {name}",
                    isTogglable = false,
                    method = () => LoadPresetFile(path)
                });
            }

            if (Buttons.buttons.Length > 21)
            {
                Buttons.buttons[21] = btnList.ToArray();
            }

            if (notify)
            {
                NotificationLib.SendNotification(NotificationLib.NotificationType.Info, $"Presets: {presetFiles.Count} preset(s) found");
                if (Main.buttonsType == 21 && Main.menu != null)
                    Main.RecreateMenu();
            }
        }

        public static void ExportPreset()
        {
            try
            {
                EnsureDirectory();
                SavePayload payload = BuildCurrentPayload();
                string json = JsonConvert.SerializeObject(payload, Formatting.Indented);
                string filename = $"preset_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                string filePath = Path.Combine(PresetsDirectory, filename);
                File.WriteAllText(filePath, json);
                UnityEngine.GUIUtility.systemCopyBuffer = json;
                RefreshPresets(false);
                NotificationLib.SendNotification(NotificationLib.NotificationType.Info, "Preset saved & copied to clipboard!");
            }
            catch (Exception ex)
            {
                NotificationLib.SendNotification(NotificationLib.NotificationType.Alert, $"Export failed: {ex.Message}");
            }
        }

        public static void ImportPresetFromClipboard()
        {
            try
            {
                string clipboard = UnityEngine.GUIUtility.systemCopyBuffer;
                if (string.IsNullOrWhiteSpace(clipboard))
                {
                    NotificationLib.SendNotification(NotificationLib.NotificationType.Alert, "empty clipboard");
                    return;
                }

                SavePayload payload = JsonConvert.DeserializeObject<SavePayload>(clipboard);
                if (payload == null || (payload.Buttons.Count == 0 && payload.Settings.Count == 0))
                {
                    NotificationLib.SendNotification(NotificationLib.NotificationType.Alert, "invalid preset copied");
                    return;
                }

                ApplyPayload(payload);
                NotificationLib.SendNotification(NotificationLib.NotificationType.Info, "preset loaded");
            }
            catch (Exception ex)
            {
                NotificationLib.SendNotification(NotificationLib.NotificationType.Alert, $"import failed: {ex.Message}");
            }
        }

        public static void CyclePresets()
        {
            try
            {
                EnsureDirectory();
                string[] files = Directory.GetFiles(PresetsDirectory, "*.json");
                if (files.Length == 0)
                {
                    NotificationLib.SendNotification(NotificationLib.NotificationType.Alert, "no presets in Genesis/Presets");
                    return;
                }

                currentPresetIndex = (currentPresetIndex + 1) % files.Length;
                string targetFile = files[currentPresetIndex];
                string json = File.ReadAllText(targetFile);
                SavePayload payload = JsonConvert.DeserializeObject<SavePayload>(json);
                ApplyPayload(payload);
                string name = !string.IsNullOrWhiteSpace(payload?.PresetName) ? payload.PresetName : Path.GetFileNameWithoutExtension(targetFile);
                NotificationLib.SendNotification(NotificationLib.NotificationType.Info, $"loaded: {name}");
            }
            catch (Exception ex)
            {
                NotificationLib.SendNotification(NotificationLib.NotificationType.Alert, $"load failed: {ex.Message}");
            }
        }

        public static void SyncSettings()
        {
            Mods.mods.ApplyTheme();

            if (Mods.mods.OutlineIndex >= 0 && Mods.mods.OutlineIndex < Mods.mods.outnames.Length)
                Main.outlineColor = Mods.mods.outlines[Mods.mods.OutlineIndex];

            if (Mods.mods.Platcolor >= 0 && Mods.mods.Platcolor < Mods.mods.ColorNames.Length)
                Mods.mods.PlatColor = Mods.mods.PlatColors[Mods.mods.Platcolor];

            switch (Mods.mods.pullmodeIndex)
            {
                case 0: Mods.mods.PullPower = 0.025f; Mods.mods.UpHillPower = 0.02f; break;
                case 1: Mods.mods.PullPower = 0.07f; Mods.mods.UpHillPower = 0.065f; break;
                case 2: Mods.mods.PullPower = 0.001f; Mods.mods.UpHillPower = 0.001f; break;
            }

            if (Main.menu != null)
                Main.RecreateMenu();
        }

        public static void Reset()
        {
            if (File.Exists(ConfigPath)) File.Delete(ConfigPath);
            KeybindManager.ClearAllKeybinds();
            NotificationLib.SendNotification(NotificationLib.NotificationType.Info, "Reset saved preferences");
        }
    }
}
