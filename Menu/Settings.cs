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
        [Setting] public static bool SearchButton = true;
        [Setting] public static bool rightHanded = true;
        [Setting] public static bool disableNotifications = true;
        [Setting] public static bool streamerMode = false;
        [Setting] public static bool disableVRViewHUD = false;
        [Setting] public static bool barkMenu = false;
        public static bool barkMenuOpen = false;

        public static string searchQuery = "";
        public static bool isSearching = false;
        public static bool showSearchKeyboard = true;
        public static Vector3 pinnedMenuPosition;
        public static Quaternion pinnedMenuRotation;

        public static KeyCode keyboardButton = KeyCode.Q;

        public static Vector3 menuSize = new Vector3(0.1f, 1f, 1f); // Depth, Width, Height
        public static int buttonsPerPage = 8;

        [Setting] public static int openAnimIndex = 0;
        public static readonly string[] openAnimNames =
        {
            "None",
            "Pop",
            "Smooth Scale",
            "Slide",
            "Drop",
            "Fold",
            "Elastic"
        };

        [Setting] public static int buttonStyleIndex = 0;
        public static readonly string[] buttonStyleNames =
        {
            "Classic",
            "Slim",
            "Chunky 3D",
            "Bordered",
            "Compact"
        };

        [Setting] public static int textSizeIndex = 0;
        public static readonly string[] textSizeNames =
        {
            "Auto",
            "Small",
            "Medium",
            "Large"
        };

        [Setting] public static int pageButtonIndex = 0;
        public static readonly string[] pageButtonNames =
        {
            "ShibaGT",
            "Sides",
            "Grip",
            "Trigger"
        };

        [Setting] public static bool roundedMenu = false;
    }
}