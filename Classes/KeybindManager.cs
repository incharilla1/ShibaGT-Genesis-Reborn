using System;
using System.Collections.Generic;
using BepInEx;
using ShibaGTGenesisReborn.Libs;
using ShibaGTGenesisReborn.Menu;
using UnityEngine;

namespace ShibaGTGenesisReborn.Classes
{
    public static class KeybindManager
    {
        public static ButtonInfo ListeningButton;
        public static KeybindMode DefaultMode = KeybindMode.Toggle;
        public static float ListenTimeout;

        private static readonly KeyCode[] AllKeys = (KeyCode[])Enum.GetValues(typeof(KeyCode));

        public static void Update()
        {
            if (Main.Lockdown) return;

            if (ListeningButton != null)
            {
                ListenForInput();
                return;
            }

            for (int i = 1; i <= 9; i++)
            {
                ButtonInfo[] category = Buttons.buttons[i];
                if (category == null) continue;

                for (int j = 0; j < category.Length; j++)
                {
                    ButtonInfo btn = category[j];
                    if (!IsBindableMod(btn, i) || (!btn.vrKey.HasValue && btn.pcKey == KeyCode.None)) continue;

                    bool vrDown = btn.vrKey.HasValue && InputHandler.Instance != null && InputHandler.Instance.GetInput(btn.vrKey.Value).WasPressed;
                    bool pcDown = btn.pcKey != KeyCode.None && UnityInput.Current != null && UnityInput.Current.GetKeyDown(btn.pcKey);
                    bool vrHeld = btn.vrKey.HasValue && InputHandler.Instance != null && InputHandler.Instance.GetInput(btn.vrKey.Value).IsPressed;
                    bool pcHeld = btn.pcKey != KeyCode.None && UnityInput.Current != null && UnityInput.Current.GetKey(btn.pcKey);
                    bool vrUp = btn.vrKey.HasValue && InputHandler.Instance != null && InputHandler.Instance.GetInput(btn.vrKey.Value).WasReleased;
                    bool pcUp = btn.pcKey != KeyCode.None && UnityInput.Current != null && UnityInput.Current.GetKeyUp(btn.pcKey);

                    switch (btn.keybindMode)
                    {
                        case KeybindMode.Toggle:
                            if (vrDown || pcDown)
                                Main.Toggle(btn.buttonText, btn);
                            break;

                        case KeybindMode.Hold:
                            if ((vrDown || pcDown) && !btn.enabled)
                                Main.Toggle(btn.buttonText, btn);
                            else if ((vrUp || pcUp) && btn.enabled && !vrHeld && !pcHeld)
                                Main.Toggle(btn.buttonText, btn);
                            break;

                        case KeybindMode.PressOnce:
                            if (vrDown || pcDown)
                                btn.method?.Invoke();
                            break;
                    }
                }
            }
        }

        public static bool IsBindableMod(ButtonInfo btn, int categoryIndex)
        {
            if (btn == null || categoryIndex == 0 || categoryIndex >= 10) return false;

            string text = btn.buttonText;
            if (string.IsNullOrEmpty(text) || text == "-" || text == "Save" || text == "home" || text == "Back" || text == "Version Check" || text == "Remove All Prefs")
                return false;

            if (text == "Menu" || text == "Movement" || text == "Projectiles" || text == "Presets" || text == "Keybinds" ||
                text == "Players in Room" || text == "Boombox Audios" || text == "Soundboard")
                return false;

            if (categoryIndex == 1 && !btn.isTogglable)
                return false;

            return true;
        }

        private static void ListenForInput()
        {
            if (Time.time > ListenTimeout)
            {
                CancelListening("Keybinding timed out");
                return;
            }

            InputType menuVrButton = Settings.rightHanded ? InputType.RightPrimary : InputType.LeftSecondary;

            if (InputHandler.Instance != null)
            {
                if (InputHandler.Instance.GetInput(menuVrButton).WasPressed)
                {
                    CancelListening("Keybinding cancelled");
                    return;
                }

                InputType[] pressed = InputHandler.Instance.GetCurrentlyPressedInputs();
                for (int i = 0; i < pressed.Length; i++)
                {
                    if (pressed[i] == menuVrButton) continue;
                    ListeningButton.vrKey = pressed[i];
                    ListeningButton.keybindMode = DefaultMode;
                    NotificationLib.SendNotification(NotificationLib.NotificationType.Info, $"Bound {ListeningButton.buttonText} to VR: {pressed[i]}");
                    ListeningButton = null;
                    Preferences.Save();
                    RefreshKeybindMenu();
                    return;
                }
            }

            if (UnityInput.Current != null)
            {
                if (UnityInput.Current.GetKeyDown(KeyCode.Escape))
                {
                    CancelListening("Keybinding cancelled");
                    return;
                }

                for (int i = 0; i < AllKeys.Length; i++)
                {
                    KeyCode key = AllKeys[i];
                    if (key == KeyCode.None || key == Settings.keyboardButton) continue;
                    if (UnityInput.Current.GetKeyDown(key))
                    {
                        ListeningButton.pcKey = key;
                        ListeningButton.keybindMode = DefaultMode;
                        NotificationLib.SendNotification(NotificationLib.NotificationType.Info, $"Bound {ListeningButton.buttonText} to PC: {key}");
                        ListeningButton = null;
                        Preferences.Save();
                        RefreshKeybindMenu();
                        return;
                    }
                }
            }
        }

        public static void CancelListening(string reason = "Keybinding cancelled")
        {
            ListeningButton = null;
            NotificationLib.SendNotification(NotificationLib.NotificationType.Info, reason);
            RefreshKeybindMenu();
        }

        public static void RefreshKeybindMenu()
        {
            List<ButtonInfo> keybindButtons = new List<ButtonInfo>();

            if (ListeningButton != null)
            {
                keybindButtons.Add(new ButtonInfo { buttonText = "Cancel Binding", method = () => CancelListening(), isTogglable = false, toolTip = $"Cancel binding for {ListeningButton.buttonText}" });
            }

            keybindButtons.Add(new ButtonInfo { buttonText = "Back", method = () => SettingsMods.safety(), isTogglable = false, toolTip = "Return to Menu Settings" });
            keybindButtons.Add(new ButtonInfo { buttonText = "Add Keybind", method = () => SettingsMods.keybindPicker(), isTogglable = false, toolTip = "Select a mod to bind" });
            keybindButtons.Add(new ButtonInfo { buttonText = "Cycle Mode", overlapText = $"Mode: {DefaultMode}", method = () => CycleDefaultMode(), isTogglable = false, toolTip = "Cycle default bind mode" });
            keybindButtons.Add(new ButtonInfo { buttonText = "Clear All Keybinds", method = () => ClearAllKeybinds(), isTogglable = false, toolTip = "Remove all keybinds" });

            for (int i = 1; i <= 9; i++)
            {
                ButtonInfo[] category = Buttons.buttons[i];
                if (category == null) continue;

                for (int j = 0; j < category.Length; j++)
                {
                    ButtonInfo btn = category[j];
                    if (!IsBindableMod(btn, i) || (!btn.vrKey.HasValue && btn.pcKey == KeyCode.None)) continue;

                    ButtonInfo targetBtn = btn;
                    string vrStr = targetBtn.vrKey.HasValue ? targetBtn.vrKey.Value.ToString() : "-";
                    string pcStr = targetBtn.pcKey != KeyCode.None ? targetBtn.pcKey.ToString() : "-";

                    keybindButtons.Add(new ButtonInfo
                    {
                        buttonText = targetBtn.buttonText,
                        overlapText = $"{targetBtn.buttonText} [{vrStr}|{pcStr}] ({targetBtn.keybindMode})",
                        toolTip = "Click to cycle mode / clear bind",
                        isTogglable = false,
                        method = () => CycleOrClearBind(targetBtn)
                    });
                }
            }

            if (Buttons.buttons.Length > 22)
            {
                Buttons.buttons[22] = keybindButtons.ToArray();
            }

            if (Main.buttonsType == 22 && Main.menu != null)
            {
                Main.RecreateMenu();
            }
        }

        public static void RefreshPickerMenu()
        {
            List<ButtonInfo> pickerButtons = new List<ButtonInfo>
            {
                new ButtonInfo { buttonText = "Back", method = () => SettingsMods.keybinds(), isTogglable = false, toolTip = "Return to Keybinds" }
            };

            for (int i = 1; i <= 9; i++)
            {
                ButtonInfo[] category = Buttons.buttons[i];
                if (category == null) continue;

                for (int j = 0; j < category.Length; j++)
                {
                    ButtonInfo btn = category[j];
                    if (!IsBindableMod(btn, i)) continue;

                    ButtonInfo targetBtn = btn;
                    pickerButtons.Add(new ButtonInfo
                    {
                        buttonText = targetBtn.buttonText,
                        toolTip = $"Bind {targetBtn.buttonText}",
                        isTogglable = false,
                        method = () => StartListening(targetBtn)
                    });
                }
            }

            if (Buttons.buttons.Length > 23)
            {
                Buttons.buttons[23] = pickerButtons.ToArray();
            }

            if (Main.buttonsType == 23 && Main.menu != null)
            {
                Main.RecreateMenu();
            }
        }

        public static void StartListening(ButtonInfo btn)
        {
            ListeningButton = btn;
            ListenTimeout = Time.time + 5f;
            NotificationLib.SendNotification(NotificationLib.NotificationType.Info, $"Press VR button or PC key for {btn.buttonText} (Menu button/Esc to cancel)...");
            SettingsMods.keybinds();
        }

        public static void CycleDefaultMode()
        {
            DefaultMode = DefaultMode switch
            {
                KeybindMode.Toggle => KeybindMode.Hold,
                KeybindMode.Hold => KeybindMode.PressOnce,
                _ => KeybindMode.Toggle
            };

            RefreshKeybindMenu();
        }

        public static void CycleOrClearBind(ButtonInfo btn)
        {
            if (btn.keybindMode == KeybindMode.Toggle)
            {
                btn.keybindMode = KeybindMode.Hold;
            }
            else if (btn.keybindMode == KeybindMode.Hold)
            {
                btn.keybindMode = KeybindMode.PressOnce;
            }
            else
            {
                btn.vrKey = null;
                btn.pcKey = KeyCode.None;
                btn.keybindMode = KeybindMode.Toggle;
                NotificationLib.SendNotification(NotificationLib.NotificationType.Info, $"Cleared keybind for {btn.buttonText}");
            }

            Preferences.Save();
            RefreshKeybindMenu();
        }

        public static void ClearAllKeybinds()
        {
            for (int i = 1; i <= 9; i++)
            {
                ButtonInfo[] category = Buttons.buttons[i];
                if (category == null) continue;

                for (int j = 0; j < category.Length; j++)
                {
                    ButtonInfo btn = category[j];
                    if (btn != null && IsBindableMod(btn, i))
                    {
                        btn.vrKey = null;
                        btn.pcKey = KeyCode.None;
                        btn.keybindMode = KeybindMode.Toggle;
                    }
                }
            }

            Preferences.Save();
            RefreshKeybindMenu();
            NotificationLib.SendNotification(NotificationLib.NotificationType.Info, "Cleared all keybinds");
        }
    }
}
