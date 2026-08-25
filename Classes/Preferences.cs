using Newtonsoft.Json;
using ShibaGTGenesisReborn.Classes;
using ShibaGTGenesisReborn.Libs;
using ShibaGTGenesisReborn.Menu;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

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
        }

        private class SavePayload
        {
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

        public static void Save()
        {
            var buttons = new Dictionary<string, ButtonState>(32);

            for (int i = 0; i < Buttons.buttons.Length; i++)
            {
                if (i == 10 || i == 11 || i == 13 || i == 14) continue;
                foreach (ButtonInfo btn in Buttons.buttons[i])
                {
                    if (btn == null || btn.buttonText == "-" || btn.buttonText == "Save" || btn.buttonText == "Remove All Prefs") continue;

                    if (btn.enabled || btn.isFavorite || !string.IsNullOrEmpty(btn.overlapText))
                    {
                        buttons[btn.buttonText] = new ButtonState
                        {
                            Enabled = btn.enabled,
                            Favorite = btn.isFavorite,
                            OverlapText = btn.overlapText
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

            Directory.CreateDirectory(ModsLib.GenesisDirectory);
            using StreamWriter sw = new StreamWriter(ConfigPath);
            using JsonTextWriter jw = new JsonTextWriter(sw);
            _serializer.Serialize(jw, new SavePayload { Buttons = buttons, Settings = settings });
        }

        public static void Load()
        {
            if (!File.Exists(ConfigPath)) return;

            try
            {
                using StreamReader sr = new StreamReader(ConfigPath);
                using JsonTextReader jr = new JsonTextReader(sr);
                var payload = _serializer.Deserialize<SavePayload>(jr);
                if (payload == null) return;

                EnsureAccessors();
                if (payload.Settings != null)
                {
                    for (int i = 0; i < _accessors.Length; i++)
                    {
                        ref var acc = ref _accessors[i];
                        if (payload.Settings.TryGetValue(acc.Key, out object val) || payload.Settings.TryGetValue(acc.FallbackKey, out val))
                        {
                            try { acc.Setter(Convert.ChangeType(val, acc.Type)); } catch { }
                        }
                    }
                }

                Main.favoriteButtons.Clear();
                if (payload.Buttons != null)
                {
                    for (int i = 0; i < Buttons.buttons.Length; i++)
                    {
                        if (i == 10 || i == 11 || i == 13 || i == 14) continue;
                        foreach (ButtonInfo btn in Buttons.buttons[i])
                        {
                            if (btn == null || !payload.Buttons.TryGetValue(btn.buttonText, out ButtonState state)) continue;

                            if (!string.IsNullOrEmpty(state.OverlapText) && btn.overlapText != null && btn.method != null && !btn.isTogglable)
                            {
                                int safety = 0;
                                while (btn.overlapText != state.OverlapText && safety++ < 30)
                                    btn.method.Invoke();
                            }

                            if (btn.isTogglable && btn.enabled != state.Enabled)
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

                Main.UpdateFavoritesCategory();
            }
            catch { }
        }

        public static void Reset()
        {
            if (File.Exists(ConfigPath)) File.Delete(ConfigPath);
            NotificationLib.SendNotification(NotificationLib.NotificationType.Info, "Reset saved preferences");
        }
    }
}
