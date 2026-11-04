using ShibaGTGenesisReborn.Classes;
using ShibaGTGenesisReborn.Libs;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static ShibaGTGenesisReborn.Menu.Buttons;
using static ShibaGTGenesisReborn.Settings;

namespace ShibaGTGenesisReborn.Menu
{
    public partial class Main
    {
        public static ButtonInfo lastToggled;
        public static float lastToggleTime;

        private static Color surfaceColor, dimColor;
        private static float Rounding => roundings[roundingIndex % roundings.Length];
        private const float rowHeight = 0.066f, panelDepth = 0.015f, panelFront = 0.5675f;

        private static void BuildNewMenu()
        {
            canvasObject.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 3000f;

            surfaceColor = Color.Lerp(buttonColors[0].colors[0].color, Color.white, 0.1f);
            dimColor = Color.Lerp(textColors[0], backgroundColor.colors[0].color, 0.45f);

            string title = isChangingTitle ? titleInput + "_" : isSearching ? $"Search: {searchQuery}_" : hideTitle ? "" : string.IsNullOrEmpty(menuTitle) ? PluginInfo.Name : menuTitle;
            AddText(title, 0f, 0.43f, 0.56f, 0.055f, TextAnchor.MiddleCenter, textColors[0]);
            AddText($"{pageNumber + 1}/{LastPage + 1}", 0f, 0.385f, 0.2f, 0.026f, TextAnchor.MiddleCenter, dimColor);
            if (pageButtonIndex < 2)
            {
                AddPanel(0.375f, 0.42f, 0.13f, rowHeight, rowHeight / 2f, surfaceColor, relatedText: "PreviousPage");
                AddText("‹", 0.375f, 0.42f, 0.1f, 0.05f, TextAnchor.MiddleCenter, textColors[0]);
                AddPanel(-0.375f, 0.42f, 0.13f, rowHeight, rowHeight / 2f, surfaceColor, relatedText: "NextPage");
                AddText("›", -0.375f, 0.42f, 0.1f, 0.05f, TextAnchor.MiddleCenter, textColors[0]);
            }

            ButtonInfo[] active = CurrentButtons.Skip(pageNumber * buttonsPerPage).Take(buttonsPerPage).ToArray();
            for (int i = 0; i < active.Length; i++)
                AddRow(active[i], 0.31f - i * 0.09f);

            List<(string action, string label, Texture icon)> footer = new List<(string, string, Texture)> { ("home", null, ModsLib.GetHomeTexture()) };
            if (SettingsButton) footer.Add(("Settings", null, ModsLib.GetSettingsTexture()));
            if (FolderButton) footer.Add(("GenesisFolder", null, ModsLib.GetFolderTexture()));
            if (SearchButton) footer.Add(("Search", null, ModsLib.GetSearchTexture()));
            if (disconnectButton) footer.Add(("Disconnect", "Leave", null));

            float slot = 0.88f / footer.Count;
            for (int i = 0; i < footer.Count; i++)
            {
                float y = 0.44f - slot * (i + 0.5f);
                AddPanel(y, -0.418f, slot - 0.04f, 0.07f, 0.035f, surfaceColor, relatedText: footer[i].action);
                if (footer[i].icon != null) AddIcon(footer[i].icon, y, -0.418f, 0.018f, textColors[0]);
                else AddText(footer[i].label, y, -0.418f, slot - 0.07f, 0.035f, TextAnchor.MiddleCenter, textColors[0]);
            }

            if (isSearching || isChangingTitle) CreateKeyboard();

            if (skipMenuIntro) return;
            List<Transform> parts = new List<Transform>();
            foreach (Transform child in menu.transform)
                if (child != menuBackground.transform && child.GetComponent<Renderer>() != null) parts.Add(child);
            foreach (Transform child in canvasObject.transform)
                parts.Add(child);
            foreach (Transform part in parts)
                part.gameObject.AddComponent<MenuIntro>().delay = Mathf.Max(0f, 0.17f - part.position.z) * 0.8f;
        }

        private static void AddRow(ButtonInfo info, float z)
        {
            bool favorite = !hideFavoriteHearts;
            float left = favorite ? 0.35f : 0.44f;
            float right = -0.44f;
            if (info.pmButton)
            {
                AddPanel(left - 0.04f, z, 0.08f, rowHeight, rowHeight / 2f, surfaceColor, relatedText: "minus_" + info.buttonText, info: info);
                AddText("-", left - 0.04f, z, 0.06f, 0.045f, TextAnchor.MiddleCenter, textColors[0]);
                AddPanel(-0.40f, z, 0.08f, rowHeight, rowHeight / 2f, surfaceColor, relatedText: info.buttonText, info: info);
                AddText("+", -0.40f, z, 0.06f, 0.045f, TextAnchor.MiddleCenter, textColors[0]);
                left -= 0.1f;
                right = -0.35f;
            }
            AddPanel((left + right) / 2f, z, left - right, rowHeight, rowHeight / 2f, surfaceColor, relatedText: info.pmButton ? null : info.buttonText, info: info);

            if (favorite)
            {
                AddPanel(0.405f, z, 0.07f, rowHeight, rowHeight / 2f, surfaceColor, relatedText: "fav_" + info.buttonText, info: info);
                AddIcon(ModsLib.GetHeartTexture(), 0.405f, z, 0.016f, info.isFavorite ? Color.yellow : dimColor);
            }

            string label = info.overlapText ?? info.buttonText;
            if (textCaseIndex % textCaseNames.Length == 1) label = label.ToUpperInvariant();
            else if (textCaseIndex % textCaseNames.Length == 2) label = label.ToLowerInvariant();
            bool described = !string.IsNullOrEmpty(info.toolTip) && info.toolTip != info.buttonText;
            AddText(label, (left - 0.34f) / 2f, described ? z + 0.012f : z, left + 0.26f, described ? 0.034f : 0.042f, TextAnchor.MiddleLeft, info.enabled ? textColors[1] : textColors[0], info.enabled ? 1 : 0);
            if (described)
                AddText(info.toolTip, (left - 0.34f) / 2f, z - 0.017f, left + 0.26f, 0.019f, TextAnchor.MiddleLeft, dimColor);

            if (info.pmButton) return;
            if (!info.isTogglable)
            {
                AddText("›", -0.39f, z, 0.06f, 0.04f, TextAnchor.MiddleCenter, dimColor);
                return;
            }

            AddPanel(-0.37f, z, 0.11f, 0.034f, 0.017f, info.enabled ? textColors[1] : Color.Lerp(surfaceColor, Color.white, 0.15f), depth: panelFront + 0.006f);
            GameObject knob = AddPanel(info.enabled ? -0.402f : -0.338f, z, 0.0306f, 0.024f, 0f, Color.white, depth: panelFront + 0.014f, shape: PrimitiveType.Sphere);
            if (info == lastToggled && Time.unscaledTime - lastToggleTime < 0.5f)
                knob.AddComponent<MenuSlide>().from = new Vector3(panelFront + 0.014f, info.enabled ? -0.338f : -0.402f, z);
        }

        private static GameObject AddPanel(float y, float z, float width, float height, float radius, Color color, string relatedText = null, ButtonInfo info = null, float depth = 0.56f, PrimitiveType shape = PrimitiveType.Cube)
        {
            bool clickable = relatedText != null;
            GameObject panel = GameObject.CreatePrimitive(shape);
            Collider collider = panel.GetComponent<Collider>();
            if (!clickable) UnityEngine.Object.Destroy(collider);
            else
            {
                collider.isTrigger = true;
                Classes.Button button = panel.AddComponent<Classes.Button>();
                button.relatedText = relatedText;
                button.buttonInfo = info;
            }

            panel.transform.parent = menu.transform;
            panel.transform.rotation = Quaternion.identity;
            panel.transform.localScale = new Vector3(flatMenu ? 0.002f : clickable ? panelDepth : 0.012f, width, height);
            panel.transform.localPosition = new Vector3(depth, y, z);

            panel.GetComponent<Renderer>().material.color = color;
            if (clickable && (buttonColors[0].isRainbow || buttonColors[0].copyRigColors))
            {
                ColorChanger changer = panel.AddComponent<ColorChanger>();
                changer.colorInfo = buttonColors[0];
                changer.Start();
            }
            if (shape == PrimitiveType.Cube && Rounding > 0f) ApplyRoundedMesh(panel, width * 0.3f, height * 0.3825f, radius * 0.17f * Rounding);
            if (clickable && showOutline) OutlineObj(panel, outlineColor, outlineColor, false, 3);
            return panel;
        }

        private static void AddText(string content, float y, float z, float width, float height, TextAnchor anchor, Color color, int state = 0, float depth = 0.56f)
        {
            Text text = new GameObject { transform = { parent = canvasObject.transform } }.AddComponent<Text>();
            text.font = currentFont;
            text.text = content;
            text.fontSize = 1;
            text.supportRichText = true;
            text.fontStyle = FontStyle.Bold;
            text.alignment = anchor;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 0;
            text.resizeTextMaxSize = 40;
            SetUiColor(text, color, state);

            RectTransform rect = text.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width * 0.3f, height * 0.3825f);
            rect.localPosition = new Vector3((depth + panelDepth / 2f + 0.03f) * 0.1f, y * 0.3f, z * 0.3825f);
            rect.rotation = Quaternion.Euler(180f, 90f, 90f);
        }

        private static void AddIcon(Texture icon, float y, float z, float size, Color color)
        {
            RawImage image = new GameObject { transform = { parent = canvasObject.transform } }.AddComponent<RawImage>();
            image.texture = icon;
            image.color = color;
            RectTransform rect = image.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(size, size);
            rect.localPosition = new Vector3((panelFront + 0.03f) * 0.1f, y * 0.3f, z * 0.3825f);
            rect.rotation = Quaternion.Euler(180f, 90f, 90f);
        }
    }
}
