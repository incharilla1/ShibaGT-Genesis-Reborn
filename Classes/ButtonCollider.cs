using Photon.Pun;
using ShibaGTGenesisReborn.Menu;
using ShibaGTGenesisReborn.Mods;
using UnityEngine;
using static ShibaGTGenesisReborn.Menu.Main;
using static ShibaGTGenesisReborn.Settings;

namespace ShibaGTGenesisReborn.Classes
{
    internal class Button : MonoBehaviour
    {
        public string relatedText;
        public ButtonInfo buttonInfo;

        public static float buttonCooldown = 0f;
        [Setting] public static bool customAudio = false;

        private static void plaything()
        {
            try
            {
                if (customAudio)
                    MenuAudio.PlayClickSound();
                else if (VRRig.LocalRig != null)
                    VRRig.LocalRig.PlayHandTapLocal(8, rightHanded, 0.4f);
            }
            catch { }
        }

        public void Click()
        {
            if (Time.time > buttonCooldown && menu != null)
            {
                buttonCooldown = Time.time + 0.15f;
                if (GorillaTagger.Instance != null)
                {
                    try
                    {
                        GorillaTagger.Instance.StartVibration(rightHanded, GorillaTagger.Instance.tagHapticStrength / 2f, GorillaTagger.Instance.tagHapticDuration / 2f);
                    }
                    catch { }
                }
                plaything();
                Toggle(this.relatedText, this.buttonInfo);
            }
        }

        public void OnTriggerEnter(Collider collider)
        {
            if (collider == buttonCollider || collider == null || buttonCollider == null)
            {
                Click();
            }
        }
    }
}
