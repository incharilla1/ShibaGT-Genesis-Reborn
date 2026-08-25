using ShibaGTGenesisReborn.Classes;
using UnityEngine;
using static ShibaGTGenesisReborn.Menu.Main;

namespace ShibaGTGenesisReborn
{
    public class Settings
    {
        public static ExtGradient backgroundColor = new ExtGradient { isRainbow = false };
        public static ExtGradient[] buttonColors = new ExtGradient[]
        {
            new ExtGradient{colors = GetSolidGradient(new Color(0.06f, 0.06f, 0.06f)) }, // Disabled
            new ExtGradient{isRainbow = false} // Enabled
        };
        public static Color[] textColors = new Color[]
        {
            Color.white,   // Disabled
            Color.magenta // Enabled
        };

        public static Font currentFont = (Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font);

        [Setting] public static bool fpsCounter = true;
        [Setting] public static bool disconnectButton = true;
        [Setting] public static bool SettingsButton = true;
        [Setting] public static bool FolderButton = true;
        [Setting] public static bool rightHanded = true;
        [Setting] public static bool disableNotifications = true;
        [Setting] public static bool streamerMode = false;

        public static KeyCode keyboardButton = KeyCode.Q;

        public static Vector3 menuSize = new Vector3(0.1f, 1f, 1f); // Depth, Width, Height
        public static int buttonsPerPage = 8;
    }
}