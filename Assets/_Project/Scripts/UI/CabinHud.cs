using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BlindRunway
{
    // Cabin: chaos meters, passenger faces (speech), hold-to-interact ring. No outside view, no instruments.
    public class CabinHud : RoleHud
    {
        Image panicFill, ring, portL, portR;
        Text panicPct, secure, hint;
        readonly List<Text> bubbles = new List<Text>();
        readonly List<Image> crateRings = new List<Image>();

        protected override void BuildRole()
        {
            var bar = Ui.Img(root, Tokens.BgPanel.A(0.9f));
            Ui.Place(bar, new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(720, 64));
            Ui.Place(Ui.Label(bar.transform, "PANIC", 22, Tokens.TextMuted, TextAnchor.MiddleLeft, FontStyle.Bold), new Vector2(0, 0.5f), new Vector2(20, 0), new Vector2(120, 30));
            var track = Ui.Img(bar.transform, Tokens.BgVoid);
            Ui.Place(track, new Vector2(0, 0.5f), new Vector2(120, 0), new Vector2(470, 22));
            panicFill = Ui.Img(track.transform, Tokens.Success, Ui.White);
            panicFill.type = Image.Type.Filled;
            panicFill.fillMethod = Image.FillMethod.Horizontal;
            Ui.Fill(panicFill.rectTransform, 3);
            panicPct = Ui.Label(bar.transform, "0%", 26, Tokens.TextPrimary, TextAnchor.MiddleRight, FontStyle.Bold);
            Ui.Place(panicPct, new Vector2(1, 0.5f), new Vector2(-20, 0), new Vector2(100, 34));

            secure = Ui.Label(root, "", 28, Tokens.Cabin, TextAnchor.UpperRight, FontStyle.Bold);
            Ui.Place(secure, new Vector2(1, 1), new Vector2(-48, -48), new Vector2(500, 40));

            portL = Ui.Img(root, Tokens.BgVoid, Ui.Circle);
            Ui.Place(portL, new Vector2(0, 0.5f), new Vector2(40, 0), new Vector2(110, 140));
            portR = Ui.Img(root, Tokens.BgVoid, Ui.Circle);
            Ui.Place(portR, new Vector2(1, 0.5f), new Vector2(-40, 0), new Vector2(110, 140));

            ring = Ui.Img(root, Tokens.Cabin, Ui.Circle);
            ring.type = Image.Type.Filled;
            ring.fillMethod = Image.FillMethod.Radial360;
            ring.fillAmount = 0;
            Ui.Place(ring, new Vector2(0.5f, 0), new Vector2(0, 150), new Vector2(90, 90));
            hint = Ui.Label(root, "", 32, Tokens.TextPrimary, TextAnchor.MiddleCenter, FontStyle.Bold);
            Ui.Place(hint, new Vector2(0.5f, 0), new Vector2(0, 90), new Vector2(900, 50));

            for (int i = 0; i < level.passengers; i++)
            {
                var b = Ui.Label(root, "", 22, Tokens.TextPrimary, TextAnchor.MiddleCenter, FontStyle.Bold);
                ((RectTransform)b.transform).sizeDelta = new Vector2(320, 34);
                bubbles.Add(b);
            }

            for (int i = 0; i < level.crates; i++)
            {
                var r = Ui.Img(root, Tokens.Success, Ui.Circle);
                r.type = Image.Type.Filled;
                r.fillMethod = Image.FillMethod.Radial360;
                ((RectTransform)r.transform).sizeDelta = new Vector2(56, 56);
                crateRings.Add(r);
            }

            Hint("WASD  walk     hold E  interact\n1 CABIN SECURE!  2 FIRE!  3 STOP WOBBLING!", new Vector2(0, 0), new Vector2(48, 40), TextAnchor.LowerLeft);
        }

        protected override void TickRole(Snapshot s, float dt)
        {
            if (!s.cabin) return;
            panicFill.fillAmount = Mathf.Lerp(panicFill.fillAmount, s.panic, 1 - Mathf.Exp(-8 * dt));
            panicFill.color = s.panic < 0.5f ? Color.Lerp(Tokens.Success, Tokens.Amber, s.panic * 2) : Color.Lerp(Tokens.Amber, Tokens.Danger, (s.panic - 0.5f) * 2);
            panicPct.text = Mathf.RoundToInt(s.panic * 100) + "%";
            panicPct.color = s.panic > 0.8f ? Tokens.Danger : Tokens.TextPrimary;
            secure.text = $"CABIN SECURE  {Mathf.RoundToInt(s.secure * 100)}%";

            // portholes: sky above the horizon when the nose is up, ground when it dives
            var sky = Tokens.Hex(0x1C2A4A);
            var ground = Tokens.Hex(0x0E1410);
            portL.color = portR.color = Color.Lerp(ground, sky, Mathf.InverseLerp(-12, 12, s.pitch));

            ring.fillAmount = s.act;
            hint.text = s.hint.Length > 0 ? "E  " + s.hint : "";

            var cam = App.I.Cam;
            var crates = App.I.World.Crates;
            for (int i = 0; i < crateRings.Count; i++)
            {
                var r = crateRings[i];
                bool show = i < crates.Count && i < s.crates.Length && s.crates[i].progress > 0 && !s.crates[i].secured;
                r.enabled = show;
                if (!show) continue;
                r.fillAmount = s.crates[i].progress;
                PlaceAt(r.transform, crates[i].position + Vector3.up * 0.9f);
            }

            var heads = App.I.World.Pax;
            for (int i = 0; i < bubbles.Count; i++)
            {
                var b = bubbles[i];
                if (i >= heads.Count || i >= s.pax.Length)
                {
                    b.text = "";
                    continue;
                }
                var p = s.pax[i];
                b.text = p.demanding ? "DRINK??" : p.panic > 0.75f ? "WE'RE GOING TO DIE" : p.panic > 0.45f ? "IS THIS NORMAL?" : "";
                b.color = p.panic > 0.75f ? Tokens.Danger : Tokens.TextPrimary;
                PlaceAt(b.transform, heads[i].position + Vector3.up * 0.6f);
            }
        }

        void PlaceAt(Transform t, Vector3 world)
        {
            var sp = App.I.Cam.WorldToScreenPoint(world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, sp, null, out var local);
            var rt = (RectTransform)t;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = local;
        }
    }
}
