using BepInEx;
using ShibaGTGenesisReborn.Classes;
using ShibaGTGenesisReborn.Libs;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using static ShibaGTGenesisReborn.Settings;

namespace ShibaGTGenesisReborn.Menu
{
    public class Intro : MonoBehaviour
    {
        private const float Length = 11f, Land = 6.5f, Fade = 1.5f, MusicVolume = 0.35f, Distance = 0.4f, LogoSize = 300f;
        private const int Rate = 44100, Grid = 10;
        private static readonly Color Blue = new Color(0.25f, 0.45f, 1f);
        private static Intro instance;

        private class Piece { public Vector2 home, from; public float spin, arrive; }
        private class Layer
        {
            public RectTransform stage;
            public RawImage backdrop, glow, flash;
            public readonly List<(RawImage image, RawImage shine)> tiles = new List<(RawImage, RawImage)>();
            public Text name;
        }

        private float start = -1f;
        private Task<float[]> audioTask;
        private readonly List<Piece> pieces = new List<Piece>();
        private readonly List<Layer> layers = new List<Layer>();
        private AudioSource music;

        public static void Begin()
        {
            if (instance == null) instance = new GameObject("GenesisIntro").AddComponent<Intro>();
        }

        private void Start()
        {
            audioTask = Task.Run(DeepNote);
            transform.SetParent(GorillaTagger.Instance.mainCamera.transform, false);

            Texture2D logo = ModsLib.LoadTextureResource("logo.png");
            Texture2D silhouette = null;
            if (logo != null)
            {
                logo.filterMode = FilterMode.Point;
                silhouette = new Texture2D(logo.width, logo.height) { filterMode = FilterMode.Point };
                Color[] pixels = logo.GetPixels();
                for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color(1f, 1f, 1f, pixels[i].a);
                silhouette.SetPixels(pixels);
                silhouette.Apply();

                float cell = LogoSize / Grid;
                for (int x = 0; x < Grid; x++)
                    for (int y = 0; y < Grid; y++)
                    {
                        if (!HasPixels(logo, x, y)) continue;
                        Vector2 home = new Vector2((x + 0.5f) * cell - LogoSize / 2f, (y + 0.5f) * cell - LogoSize / 2f + 70f);
                        pieces.Add(new Piece
                        {
                            home = home,
                            from = home + Random.insideUnitCircle.normalized * Random.Range(900f, 1500f),
                            spin = Random.Range(-720f, 720f),
                            arrive = Land - 0.15f - Random.Range(0f, 3.5f) * (1f - home.magnitude / LogoSize)
                        });
                    }
            }
            Texture2D dot = SoftDot();

            Canvas headset = Build(logo, silhouette, dot);
            headset.renderMode = RenderMode.WorldSpace;
            headset.GetComponent<RectTransform>().sizeDelta = new Vector2(1920f, 1080f);
            headset.transform.localPosition = new Vector3(0f, 0f, Distance);
            headset.transform.localScale = Vector3.one * 0.0005f;
            Canvas desktop = Build(logo, silhouette, dot);
            desktop.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = desktop.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;

            music = gameObject.AddComponent<AudioSource>();
            Animate(0f);
        }

        private void Update()
        {
            if (start < 0f)
            {
                if (!audioTask.IsCompleted) return;
                float[] data = audioTask.Result;
                music.clip = AudioClip.Create("DeepNote", data.Length / 2, 2, Rate, false);
                music.clip.SetData(data, 0);
                music.volume = MusicVolume;
                music.Play();
                start = Time.time;
            }

            float t = Time.time - start;
            Animate(t);
            if (t < Length && !UnityInput.Current.GetKeyDown(KeyCode.Return)) return;
            introDone = true;
            Preferences.SaveSettings();
            Destroy(gameObject);
            instance = null;
        }

        private void Animate(float t)
        {
            float fade = Mathf.Clamp01((Length - t) / Fade);
            float hit = t > Land ? Mathf.Exp(-(t - Land) * 3f) : 0f;
            float build = Mathf.Clamp01(t / Land);
            Vector2 shake = hit > 0.02f ? Random.insideUnitCircle * 40f * hit : Vector2.zero;
            float sweep = Mathf.Lerp(-1.3f, 1.3f, (t - Land - 0.3f) / 0.9f);

            float reveal = Mathf.SmoothStep(0f, 1f, (t - Land - 0.5f) / 1f);

            foreach (Layer layer in layers)
            {
                layer.stage.anchoredPosition = shake;
                layer.stage.localScale = Vector3.one * (1f + 0.06f * (1f - fade));
                layer.backdrop.color = new Color(0f, 0f, 0f, Mathf.Clamp01(fade * 2f));
                layer.glow.color = new Color(Blue.r, Blue.g, Blue.b, (0.05f + 0.25f * build * build + 0.7f * hit) * fade);
                layer.glow.rectTransform.localScale = Vector3.one * (0.6f + 0.4f * build + 1.2f * hit + 0.03f * Mathf.Sin(t * 4f));
                layer.flash.color = new Color(1f, 1f, 1f, 0.8f * Mathf.Pow(hit, 3f));

                for (int i = 0; i < pieces.Count; i++)
                {
                    Piece piece = pieces[i];
                    float p = Mathf.Clamp01(1f - (piece.arrive - t) / 1.4f);
                    float ease = 1f - Mathf.Pow(1f - p, 4f);
                    RectTransform rect = layer.tiles[i].image.rectTransform;
                    rect.anchoredPosition = Vector2.LerpUnclamped(piece.from, piece.home, ease);
                    rect.localRotation = Quaternion.Euler(0f, 0f, piece.spin * (1f - ease));
                    rect.localScale = Vector3.one * Mathf.Lerp(2.5f, 1f, ease) * (1f + 0.08f * hit);
                    layer.tiles[i].image.color = new Color(1f, 1f, 1f, Mathf.Clamp01(p * 3f) * fade);

                    RawImage shine = layer.tiles[i].shine;
                    shine.rectTransform.anchoredPosition = rect.anchoredPosition;
                    shine.rectTransform.localRotation = rect.localRotation;
                    shine.rectTransform.localScale = rect.localScale;
                    float band = (piece.home.x + piece.home.y - 70f) / LogoSize - sweep;
                    float landFlash = p < 1f ? 0f : Mathf.Clamp01(1f - (t - piece.arrive) / 0.25f);
                    shine.color = new Color(1f, 1f, 1f, Mathf.Max(Mathf.Exp(-band * band * 40f), landFlash, hit) * fade);
                }

                layer.name.color = new Color(1f, 1f, 1f, reveal * fade);
                layer.name.rectTransform.anchoredPosition = new Vector2(0f, -170f + 20f * reveal);
            }
        }

        private Canvas Build(Texture logo, Texture silhouette, Texture dot)
        {
            Canvas canvas = new GameObject("IntroCanvas").AddComponent<Canvas>();
            canvas.sortingOrder = 1000;
            canvas.gameObject.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 8f;
            canvas.transform.SetParent(transform, false);

            Layer layer = new Layer();
            layer.backdrop = Image(canvas.transform, null, Vector2.zero, Vector2.zero);
            layer.backdrop.rectTransform.anchorMin = Vector2.one * -1f;
            layer.backdrop.rectTransform.anchorMax = Vector2.one * 2f;
            layer.stage = new GameObject("IntroStage").AddComponent<RectTransform>();
            layer.stage.SetParent(canvas.transform, false);
            layer.glow = Image(layer.stage, dot, new Vector2(0f, 70f), Vector2.one * 800f);

            float cell = LogoSize / Grid;
            foreach (Piece piece in pieces)
            {
                Rect uv = new Rect((piece.home.x + LogoSize / 2f - cell / 2f) / LogoSize, (piece.home.y - 70f + LogoSize / 2f - cell / 2f) / LogoSize, 1f / Grid, 1f / Grid);
                RawImage image = Image(layer.stage, logo, Vector2.zero, Vector2.one * cell);
                image.uvRect = uv;
                layer.tiles.Add((image, null));
            }
            for (int i = 0; i < layer.tiles.Count; i++)
            {
                RawImage shine = Image(layer.stage, silhouette, Vector2.zero, Vector2.one * cell);
                shine.uvRect = layer.tiles[i].image.uvRect;
                layer.tiles[i] = (layer.tiles[i].image, shine);
            }

            layer.name = Label(layer.stage, 44, -150f);
            layer.name.text = PluginInfo.Name;
            layer.flash = Image(canvas.transform, null, Vector2.zero, Vector2.zero);
            layer.flash.rectTransform.anchorMin = Vector2.one * -1f;
            layer.flash.rectTransform.anchorMax = Vector2.one * 2f;

            layers.Add(layer);
            return canvas;
        }

        private static RawImage Image(Transform parent, Texture texture, Vector2 position, Vector2 size)
        {
            RawImage image = new GameObject("IntroImage").AddComponent<RawImage>();
            image.transform.SetParent(parent, false);
            image.texture = texture;
            image.rectTransform.anchoredPosition = position;
            image.rectTransform.sizeDelta = size;
            return image;
        }

        private static Text Label(Transform parent, int size, float y)
        {
            Text text = new GameObject("IntroText").AddComponent<Text>();
            text.transform.SetParent(parent, false);
            text.font = currentFont;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.rectTransform.sizeDelta = new Vector2(1200f, 80f);
            text.rectTransform.anchoredPosition = new Vector2(0f, y);
            return text;
        }

        private static bool HasPixels(Texture2D texture, int x, int y)
        {
            int w = texture.width / Grid, h = texture.height / Grid;
            for (int px = 0; px < w; px++)
                for (int py = 0; py < h; py++)
                    if (texture.GetPixel(x * w + px, y * h + py).a > 0.1f) return true;
            return false;
        }

        private static Texture2D SoftDot()
        {
            Texture2D texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            for (int x = 0; x < 64; x++)
                for (int y = 0; y < 64; y++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f)) / 32f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Pow(Mathf.Clamp01(1f - d), 2.5f)));
                }
            texture.Apply();
            return texture;
        }

        private static float[] DeepNote()
        {
            const int voices = 30;
            int count = (int)(Rate * Length);
            float[] left = new float[count], right = new float[count];
            float[] chord = { 36.71f, 55f, 73.42f, 110f, 146.83f, 220f, 293.66f, 440f, 587.33f, 880f, 1174.66f };
            System.Random random = new System.Random(1983);
            for (int v = 0; v < voices; v++)
            {
                float from = 180f + (float)random.NextDouble() * 220f;
                float to = chord[v % chord.Length] * (1f + ((float)random.NextDouble() - 0.5f) * 0.004f);
                float wobble = 0.3f + (float)random.NextDouble() * 1.5f, offset = (float)random.NextDouble() * 6.28f;
                float gain = to < 80f ? 1.5f : to > 600f ? 0.45f : 1f;
                float pan = (float)random.NextDouble();
                double phase = random.NextDouble(), step = 0;
                for (int i = 0; i < count; i++)
                {
                    if ((i & 63) == 0)
                    {
                        float t = (float)i / Rate;
                        float glide = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(2.5f, Land, t));
                        float chaos = from * (1f + 0.2f * Mathf.Sin(t * wobble + offset) * (1f - glide));
                        step = Mathf.Exp(Mathf.Lerp(Mathf.Log(chaos), Mathf.Log(to), glide)) / Rate;
                    }
                    phase += step;
                    if (phase >= 1) phase -= 1;
                    float s = (float)(2 * phase - 1) - PolyBlep(phase, step);
                    left[i] += s * gain * (1f - pan);
                    right[i] += s * gain * pan;
                }
            }

            Filter(left, 0);
            Filter(right, 1);
            float[] output = Interleave(left, right);
            float peak = 0f;
            foreach (float s in output) peak = Mathf.Max(peak, Mathf.Abs(s));
            for (int i = 0; i < output.Length; i++) output[i] = output[i] / peak * 0.9f;
            return output;
        }

        private static void Filter(float[] data, int side)
        {
            float a = 0f, b = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                float t = (float)i / Rate;
                float cutoff = Mathf.Lerp(0.03f, 0.25f, Mathf.Clamp01(t / Land));
                a += (data[i] - a) * cutoff;
                b += (a - b) * cutoff;
                float envelope = Mathf.Lerp(0.1f, 1f, Mathf.Pow(Mathf.Clamp01(t / Land), 1.5f)) * Mathf.Clamp01((Length - 0.5f - t) / 3f);
                data[i] = b * envelope;
            }

            int[] combs = { 1557, 1617, 1491, 1422 };
            float[] wet = new float[data.Length];
            foreach (int c in combs)
            {
                int delay = c + side * 23;
                for (int i = delay; i < data.Length; i++) wet[i] += (data[i - delay] + wet[i - delay] * 0.84f) * 0.25f;
            }
            for (int i = 0; i < data.Length; i++) data[i] += wet[i] * 0.5f;
        }

        private static float[] Interleave(float[] left, float[] right)
        {
            float[] output = new float[left.Length * 2];
            for (int i = 0; i < left.Length; i++)
            {
                output[i * 2] = left[i];
                output[i * 2 + 1] = right[i];
            }
            return output;
        }

        private static float PolyBlep(double t, double dt)
        {
            if (t < dt) { t /= dt; return (float)(t + t - t * t - 1); }
            if (t > 1 - dt) { t = (t - 1) / dt; return (float)(t * t + t + t + 1); }
            return 0f;
        }
    }
}
