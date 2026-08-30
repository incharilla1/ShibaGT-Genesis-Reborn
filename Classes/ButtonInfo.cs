using System;
using ShibaGTGenesisReborn.Libs;
using UnityEngine;

namespace ShibaGTGenesisReborn.Classes
{
    public enum KeybindMode
    {
        Toggle,
        Hold,
        PressOnce
    }

    public class ButtonInfo
    {
        public string buttonText = "-";
        public string overlapText = null;
        public Action method = null;
        public Action enableMethod = null;
        public Action disableMethod = null;
        public bool enabled = false;
        public bool isTogglable = true;
        public string toolTip;
        public bool isFavorite = false;
        public InputType? vrKey = null;
        public KeyCode pcKey = KeyCode.None;
        public KeybindMode keybindMode = KeybindMode.Toggle;
    }
}