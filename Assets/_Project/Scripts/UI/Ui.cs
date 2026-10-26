using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlindRunway
{
    // Code-built uGUI kit so screens need no prefabs.
    // ponytail: legacy Text with the built-in font; swap to TMP + Space Grotesk/Inter once font assets are imported.
    public static class Ui
    {
        public static Font Font;
        public static Sprite Round, Circle, Star;

        public static void Init()
        {
            Font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Round = MakeSprite(64, 16, true);
            Circle = MakeSprite(128, 64, false);
            Star = MakeStar(128);
            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() == null)
                UnityEngine.Object.DontDestroyOnLoad(new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule)));
        }

        // White antialiased rounded rect (9-sliced) or full circle.
        static Sprite MakeSprite(int size, int radius, bool sliced)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                var core = new Vector2(Mathf.Clamp(p.x, radius, size - radius), Mathf.Clamp(p.y, radius, size - radius));
                float a = Mathf.Clamp01(radius - Vector2.Distance(p, core) + 0.5f);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px);
            tex.Apply();
            var border = sliced ? new Vector4(radius, radius, radius, radius) : Vector4.zero;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, border);
        }

        // Five-point star via angular radius test.
        static Sprite MakeStar(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            float R = size * 0.48f, r = R * 0.45f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - size / 2f, dy = y + 0.5f - size / 2f;
                float ang = Mathf.Repeat(Mathf.Atan2(dx, dy), Mathf.PI * 2 / 5) / (Mathf.PI * 2 / 5); // 0..1 per spike
                float edge = Mathf.Lerp(R, r, 1 - Mathf.Abs(ang * 2 - 1));
                float a = Mathf.Clamp01(edge - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        public static RectTransform MakeCanvas(string name, int order)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            UnityEngine.Object.DontDestroyOnLoad(go);
            var c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = order;
            var s = go.GetComponent<CanvasScaler>();
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = new Vector2(1920, 1080) / Settings.UiScale;
            s.matchWidthOrHeight = 0.5f;
            return (RectTransform)go.transform;
        }

        public static RectTransform Node(Transform parent, string name = "node")
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static Image Img(Transform parent, Color c) => Img(parent, c, Round);

        public static Image Img(Transform parent, Color c, Sprite s)
        {
            var img = Node(parent, "img").gameObject.AddComponent<Image>();
            img.sprite = s;
            img.type = s == Round ? Image.Type.Sliced : Image.Type.Simple;
            img.color = c;
            img.raycastTarget = false;
            return img;
        }

        public static Text Label(Transform parent, string text, int size, Color c,
            TextAnchor anchor = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal)
        {
            var t = Node(parent, "label").gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = c;
            t.alignment = anchor;
            t.fontStyle = style;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        public static Button Btn(Transform parent, string text, Action onClick, Color? fill = null)
        {
            var color = fill ?? Tokens.Cyan;
            var bg = Img(parent, color);
            bg.raycastTarget = true;
            var b = bg.gameObject.AddComponent<Button>();
            b.targetGraphic = bg;
            b.onClick.AddListener(() => onClick());
            var l = Label(bg.transform, text, 28, color.grayscale < 0.5f ? Tokens.TextPrimary : Tokens.BgVoid,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            Fill(l.rectTransform);
            bg.gameObject.AddComponent<Pop>();
            return b;
        }

        public static InputField Field(Transform parent, string placeholder)
        {
            var bg = Img(parent, Tokens.BgPanel);
            bg.raycastTarget = true;
            var f = bg.gameObject.AddComponent<InputField>();
            var t = Label(bg.transform, "", 32, Tokens.TextPrimary);
            t.supportRichText = false;
            Fill(t.rectTransform, 12);
            var p = Label(bg.transform, placeholder, 28, Tokens.TextMuted, TextAnchor.MiddleCenter, FontStyle.Italic);
            Fill(p.rectTransform, 12);
            f.textComponent = t;
            f.placeholder = p;
            f.targetGraphic = bg;
            return f;
        }

        public static RectTransform Fill(RectTransform rt, float inset = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
            return rt;
        }

        // anchor = point on parent (0..1); pivot follows it so margins read naturally. pos/size in px.
        public static RectTransform Place(Component c, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var rt = (RectTransform)c.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static T In<T>(T c, float delay) where T : Component
        {
            c.gameObject.AddComponent<Appear>().delay = delay;
            return c;
        }

        public static T Pulsing<T>(T c) where T : Component
        {
            c.gameObject.AddComponent<Pulse>();
            return c;
        }
    }

    // CTA feedback: 120ms ease-out scale.
    public class Pop : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        float target = 1f;
        public void OnPointerEnter(PointerEventData e) => target = 1.05f;
        public void OnPointerExit(PointerEventData e) => target = 1f;
        public void OnPointerDown(PointerEventData e) => target = 0.96f;
        public void OnPointerUp(PointerEventData e) => target = 1.05f;

        void Update()
        {
            float s = Mathf.Lerp(transform.localScale.x, target, 1f - Mathf.Exp(-Time.unscaledDeltaTime / 0.04f));
            transform.localScale = new Vector3(s, s, 1);
        }
    }

    // Stagger-in: fade + rise after delay.
    public class Appear : MonoBehaviour
    {
        public float delay;
        float t;
        CanvasGroup g;
        Vector2 basePos;

        void Start()
        {
            g = gameObject.AddComponent<CanvasGroup>();
            g.alpha = 0;
            basePos = ((RectTransform)transform).anchoredPosition;
        }

        void Update()
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01((t - delay) / 0.25f), e = 1 - (1 - k) * (1 - k);
            g.alpha = e;
            ((RectTransform)transform).anchoredPosition = basePos + Vector2.down * 30 * (1 - e);
            if (k >= 1) enabled = false;
        }
    }

    // Warning pulse, 1.2s period.
    public class Pulse : MonoBehaviour
    {
        Graphic g;
        void Awake() => g = GetComponent<Graphic>();

        void Update()
        {
            if (!g) return;
            var c = g.color;
            c.a = 0.45f + 0.55f * (0.5f + 0.5f * Mathf.Cos(Time.unscaledTime * Mathf.PI * 2 / 1.2f));
            g.color = c;
        }
    }
}
