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

        [Setting] public static int fontIndex;
        public static readonly string[] fontNames = { "Arial", "Comic Sans", "Consolas", "Impact", "Verdana", "Segoe UI" };
        private static readonly string[] osFontNames = { null, "Comic Sans MS", "Consolas", "Impact", "Verdana", "Segoe UI" };
        private static Font loadedFont;
        private static int loadedFontIndex = -1;
        public static Font currentFont
        {
            get
            {
                int index = fontIndex % fontNames.Length;
                if (loadedFont == null || loadedFontIndex != index)
                {
                    loadedFont = index == 0 ? Resources.GetBuiltinResource<Font>("Arial.ttf") : Font.CreateDynamicFontFromOSFont(osFontNames[index], 32);
                    loadedFontIndex = index;
                }
                return loadedFont;
            }
        }

        [Setting] public static int menuSizeIndex = 1;
        public static readonly string[] menuSizeNames = { "Small", "Normal", "Big", "Huge" };
        public static readonly float[] menuSizes = { 0.8f, 1f, 1.25f, 1.5f };
        [Setting] public static int textCaseIndex;
        public static readonly string[] textCaseNames = { "Normal", "UPPER", "lower" };
        [Setting] public static bool hideTitle;
        [Setting] public static bool hideFavoriteHearts;

        [Setting] public static bool autoSaveSettings = true;

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

        [Setting] public static string menuTitle = PluginInfo.Name;
        public static bool isChangingTitle = false;
        public static string titleInput = "";

        public static string searchQuery = "";
        public static bool isSearching = false;
        public static Vector3 pinnedMenuPosition;
        public static Quaternion pinnedMenuRotation;

        public static KeyCode keyboardButton = KeyCode.Q;

        public static Vector3 menuSize = new Vector3(0.1f, 1f, 1f); // Depth, Width, Height
        public static int buttonsPerPage = 8;
        [Setting] public static bool newMenuDesign, flatMenu, transparentMenu, introDone;
        [Setting] public static int transparencyIndex = 4, roundingIndex = 2, outlineThicknessIndex = 1;
        public static readonly string[] outlineThicknessNames = { "Thin", "Normal", "Thick", "Extra Thick" };
        public static readonly float[] outlineThicknesses = { 0.5f, 1f, 1.6f, 2.3f };
        public static readonly string[] transparencyNames = { "0%", "10%", "20%", "30%", "40%", "50%", "60%", "70%", "80%", "90%", "100%" };
        public static readonly string[] roundingNames = { "Off", "Low", "Medium", "High", "Full" };
        public static readonly float[] roundings = { 0f, 0.6f, 1f, 1.5f, 2.2f };
        [Setting] public static bool confirmDangerous = true;
        [Setting] public static bool joystickScroll;
        [Setting] public static int menuReleaseIndex;
        public static readonly string[] menuReleaseNames = { "Close", "Pin", "Float Away", "Physical", "Explode" };

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
