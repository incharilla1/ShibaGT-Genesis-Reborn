using UnityEngine;

namespace ShibaGTGenesisReborn.Classes
{
    public class ColorChanger : TimedBehaviour
    {
        public Renderer renderer;
        public ExtGradient colorInfo;
        private Material targetMaterial;

        public override void Start()
        {
            base.Start();
            renderer = GetComponent<Renderer>();
            if (renderer != null)
                targetMaterial = renderer.material;
            Update();
        }

        public override void Update()
        {
            base.Update();
            if (colorInfo == null || renderer == null) return;

            if (!colorInfo.copyRigColors)
            {
                Color color;
                if (colorInfo.isRainbow)
                {
                    float h = (Time.frameCount / 180f) % 1f;
                    color = Color.HSVToRGB(h, 1f, 1f);
                }
                else
                {
                    color = colorInfo.Gradient.Evaluate((Time.time / 2f) % 1f);
                }

                if (targetMaterial != null)
                    targetMaterial.color = color;
                else
                    renderer.material.color = color;
            }
            else if (VRRig.LocalRig != null && VRRig.LocalRig.mainSkin != null)
            {
                renderer.material = VRRig.LocalRig.mainSkin.material;
            }
        }
    }
}
