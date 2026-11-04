using UnityEngine;

namespace ShibaGTGenesisReborn.Classes
{
    public class MenuIntro : MonoBehaviour
    {
        public float delay;
        private Vector3 target;
        private float start;

        private void Awake()
        {
            target = transform.localScale;
            start = Time.unscaledTime;
            transform.localScale = Vector3.zero;
        }

        private void Update()
        {
            float t = (Time.unscaledTime - start - delay) / 0.22f;
            if (t <= 0f) return;
            if (t >= 1f)
            {
                transform.localScale = target;
                Destroy(this);
                return;
            }
            transform.localScale = target * Mathf.Lerp(0.6f, 1f, 1f - Mathf.Pow(1f - t, 5f));
        }
    }

    public class MenuDrift : MonoBehaviour
    {
        private Vector3 scale;
        private float start;

        private void Start()
        {
            scale = transform.localScale;
            start = Time.unscaledTime;
        }

        private void Update()
        {
            float t = Mathf.Clamp01((Time.unscaledTime - start) / 2.5f);
            transform.position += new Vector3(Mathf.Sin(t * 6f) * 0.03f, 0.2f, 0f) * Time.unscaledDeltaTime;
            transform.Rotate(0f, 0f, 20f * Time.unscaledDeltaTime);
            transform.localScale = scale * (1f - t * t);
        }
    }

    public class MenuSlide : MonoBehaviour
    {
        public Vector3 from;
        private Vector3 to;
        private float start;

        private void Start()
        {
            to = transform.localPosition;
            start = Time.unscaledTime;
            transform.localPosition = from;
        }

        private void Update()
        {
            float t = (Time.unscaledTime - start) / 0.18f;
            if (t >= 1f)
            {
                transform.localPosition = to;
                Destroy(this);
                return;
            }
            transform.localPosition = Vector3.Lerp(from, to, 1f - Mathf.Pow(1f - t, 4f));
        }
    }
}
