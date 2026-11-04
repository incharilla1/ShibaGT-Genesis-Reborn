using UnityEngine;

namespace ShibaGTGenesisReborn.Classes
{
    public class ColorChanger : TimedBehaviour
    {
        public Renderer renderer;
        public ExtGradient colorInfo;
        public float alpha = 1f;

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

            Color color;
            if (colorInfo.copyRigColors)
            {
                color = Color.white;
                if (VRRig.LocalRig.mainSkin?.material != null)
                    color = VRRig.LocalRig.mainSkin.material.color;
                else if (GorillaTagger.Instance.offlineVRRig.mainSkin?.material != null)
                    color = GorillaTagger.Instance.offlineVRRig.mainSkin.material.color;
            }
            else if (colorInfo.isRainbow)
                color = Color.HSVToRGB((Time.time * 0.4f) % 1f, 1f, 1f);
            else if (colorInfo.colors != null && colorInfo.colors.Length > 0)
                color = colorInfo.colors[0].color;
            else return;

            color.a = alpha;
            renderer.material.color = color;
        }
    }
}