using UnityEngine;

namespace ShibaGTGenesisReborn.Classes
{
    public class ColorChanger : TimedBehaviour
    {
        public Renderer renderer;
        public ExtGradient colorInfo;

        public override void Start()
        {
            base.Start();
            renderer = GetComponent<Renderer>();
            Update();
        }

        public override void Update()
        {
            base.Update();
            if (colorInfo == null || renderer == null) return;

            if (colorInfo.copyRigColors)
            {
                Color rigColor = Color.white;
                if (VRRig.LocalRig?.mainSkin?.material != null)
                    rigColor = VRRig.LocalRig.mainSkin.material.color;
                else if (GorillaTagger.Instance?.offlineVRRig?.mainSkin?.material != null)
                    rigColor = GorillaTagger.Instance.offlineVRRig.mainSkin.material.color;

                renderer.material.color = rigColor;
                return;
            }

            if (colorInfo.isRainbow)
            {
                renderer.material.color = Color.HSVToRGB((Time.time * 0.4f) % 1f, 1f, 1f);
                return;
            }

            if (colorInfo.colors != null && colorInfo.colors.Length > 0)
            {
                renderer.material.color = colorInfo.colors[0].color;
            }
        }
    }
}
