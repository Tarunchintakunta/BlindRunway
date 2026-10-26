using UnityEngine;

namespace BlindRunway
{
    // Color tokens from docs/UI-DESIGN-SYSTEM.md. Colorblind mode swaps instrument hues for Okabe-Ito.
    public static class Tokens
    {
        public static readonly Color BgVoid = Hex(0x0B1020);
        public static readonly Color BgPanel = Hex(0x141C2E);
        public static readonly Color TextPrimary = Hex(0xF4F7FF);
        public static readonly Color TextMuted = Hex(0x8B95B0);
        public static readonly Color Cabin = Hex(0xFFD56A);

        public static Color Cyan => Settings.Colorblind ? Hex(0x56B4E9) : Hex(0x3DE7FF);
        public static Color Amber => Settings.Colorblind ? Hex(0xE69F00) : Hex(0xFFB020);
        public static Color Danger => Settings.Colorblind ? Hex(0xD55E00) : Hex(0xFF3B5C);
        public static Color Success => Settings.Colorblind ? Hex(0x009E73) : Hex(0x3DFF9A);

        public static Color Hex(int rgb, float a = 1f) =>
            new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, a);

        public static Color A(this Color c, float a)
        {
            c.a = a;
            return c;
        }
    }
}
