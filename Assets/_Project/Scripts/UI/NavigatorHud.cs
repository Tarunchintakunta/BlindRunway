using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BlindRunway
{
    // Navigator: glass cockpit. Sees everything, touches nothing; talks via callouts and pings.
    public class NavigatorHud : RoleHud
    {
        Text title, timer, warnings;
        readonly Text[] values = new Text[5];
        RectTransform pathDot, mapPlane, cogDiamond;
        Image ilsFrame;
        float mapScaleZ;
        const float MapScaleX = 2f; // px per metre sideways
        const float MapH = 600, MapZMin = -10000;
        readonly List<CheckItem> checks = new List<CheckItem>();

        protected class CheckItem
        {
            public System.Func<Snapshot, bool> done;
            public Image dot;
            public Text label;
            public bool was;
        }

        protected override void BuildRole()
        {
            Ui.Fill(Ui.Img(root, Tokens.BgVoid, null).rectTransform);
            title = Ui.Label(root, $"L{level.number:00}  {level.title.ToUpperInvariant()}", 30, Tokens.TextMuted, TextAnchor.UpperLeft, FontStyle.Bold);
            Ui.Place(title, new Vector2(0, 1), new Vector2(48, -40), new Vector2(900, 40));
            timer = Ui.Label(root, "", 44, Tokens.TextPrimary, TextAnchor.UpperRight, FontStyle.Bold);
            Ui.Place(timer, new Vector2(1, 1), new Vector2(-48, -32), new Vector2(300, 56));

            string[] names = { "ALT  FT", "IAS  KT", "V/S  FPM", "HDG", "FUEL  %" };
            for (int i = 0; i < 5; i++)
            {
                var tile = Ui.Img(root, Tokens.BgPanel);
                Ui.Place(tile, new Vector2(0, 1), new Vector2(48, -110 - i * 132), new Vector2(330, 116));
                Ui.Place(Ui.Label(tile.transform, names[i], 20, Tokens.TextMuted, TextAnchor.UpperLeft), new Vector2(0, 1), new Vector2(18, -12), new Vector2(300, 28));
                values[i] = Ui.Label(tile.transform, "--", 54, Tokens.Cyan, TextAnchor.LowerRight, FontStyle.Bold);
                Ui.Place(values[i], new Vector2(1, 0), new Vector2(-18, 8), new Vector2(300, 70));
            }

            // ILS: dot = you, center = on the 3 deg path and centerline
            ilsFrame = Ui.Img(root, Tokens.BgPanel);
            Ui.Place(ilsFrame, new Vector2(0.5f, 0.5f), new Vector2(-40, 60), new Vector2(520, 520));
            var f = ilsFrame.transform;
            Ui.Place(Ui.Img(f, Tokens.Cyan.A(0.35f), null), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(2, 460));
            Ui.Place(Ui.Img(f, Tokens.Cyan.A(0.35f), null), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(460, 2));
            for (int i = 1; i <= 2; i++)
            {
                var ring = Ui.Img(f, Tokens.Cyan.A(0.12f), Ui.Circle);
                Ui.Place(ring, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(i * 200, i * 200));
            }
            pathDot = Ui.Place(Ui.Img(f, Tokens.Cyan, Ui.Circle), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(34, 34));
            Ui.Place(Ui.Label(f, "YOU  vs  GLIDE PATH", 20, Tokens.TextMuted), new Vector2(0.5f, 1), new Vector2(0, -10), new Vector2(400, 30));
            Ui.Place(Ui.Label(f, "HIGH", 18, Tokens.TextMuted), new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(100, 24));
            Ui.Place(Ui.Label(f, "LOW", 18, Tokens.TextMuted), new Vector2(0.5f, 0), new Vector2(0, 14), new Vector2(100, 24));

            if (level.cargoShiftsCog) // CoG diamond: drifts up when loose cargo sits aft
            {
                var track = Ui.Img(f, Tokens.BgVoid, null);
                Ui.Place(track, new Vector2(1, 0.5f), new Vector2(-24, 0), new Vector2(8, 400));
                Ui.Place(Ui.Label(f, "CoG", 18, Tokens.Amber), new Vector2(1, 0.5f), new Vector2(-8, 220), new Vector2(60, 24));
                var d = Ui.Img(f, Tokens.Amber, null);
                cogDiamond = Ui.Place(d, new Vector2(1, 0.5f), new Vector2(-18, 0), new Vector2(22, 22));
                d.transform.localRotation = Quaternion.Euler(0, 0, 45);
            }

            if (level.crosswind != 0) // wind vector: arrow length scales with strength
            {
                var arrow = Ui.Img(f, Tokens.Amber, null);
                float len = 40 + Mathf.Abs(level.crosswind) * 20;
                Ui.Place(arrow, new Vector2(0.5f, 0), new Vector2(0, 50), new Vector2(len, 6));
                var head = Ui.Label(f, level.crosswind > 0 ? ">" : "<", 40, Tokens.Amber, TextAnchor.MiddleCenter, FontStyle.Bold);
                Ui.Place(head, new Vector2(0.5f, 0), new Vector2(Mathf.Sign(level.crosswind) * len / 2, 50), new Vector2(40, 50));
                Ui.Place(Ui.Label(f, "WIND", 18, Tokens.Amber), new Vector2(0.5f, 0), new Vector2(0, 74), new Vector2(100, 24));
            }

            warnings = Ui.Pulsing(Ui.Label(root, "", 40, Tokens.Amber, TextAnchor.MiddleCenter, FontStyle.Bold));
            Ui.Place(warnings, new Vector2(0.5f, 0.5f), new Vector2(-40, -250), new Vector2(900, 60));

            // runway map (top-down, x stretched so the runway is readable)
            var map = Ui.Img(root, Tokens.BgPanel);
            Ui.Place(map, new Vector2(1, 1), new Vector2(-48, -110), new Vector2(360, MapH + 60));
            mapScaleZ = MapH / (level.runwayLength - MapZMin);
            var runway = Ui.Img(map.transform, Tokens.TextMuted.A(0.6f), null);
            Ui.Place(runway, new Vector2(0.5f, 0), new Vector2(0, 30 + (0 - MapZMin) * mapScaleZ), new Vector2(FlightModel.RunwayHalfWidth * 2 * MapScaleX, level.runwayLength * mapScaleZ));
            var gate = Ui.Img(map.transform, Tokens.Success.A(0.35f), null);
            Ui.Place(gate, new Vector2(0.5f, 0), new Vector2(0, 30 + (level.gateStart - MapZMin) * mapScaleZ), new Vector2(40, (level.gateEnd - level.gateStart) * mapScaleZ));
            mapPlane = Ui.Place(Ui.Img(map.transform, Tokens.Cyan, Ui.Circle), new Vector2(0.5f, 0), Vector2.zero, new Vector2(18, 18));
            Ui.Place(Ui.Label(map.transform, "RUNWAY MAP", 20, Tokens.TextMuted), new Vector2(0.5f, 1), new Vector2(0, -8), new Vector2(340, 28));
            var windLabel = level.crosswind == 0 ? "WIND  CALM" : $"WIND  {(level.crosswind > 0 ? "->" : "<-")}  {Mathf.Abs(level.crosswind):0} m/s";
            Ui.Place(Ui.Label(map.transform, windLabel, 22, Tokens.Amber), new Vector2(0.5f, 0), new Vector2(0, 2), new Vector2(340, 28));

            BuildChecklist();
            Hint("1 PULL UP  2 NOSE DOWN  3 LEFT  4 RIGHT\n5 MORE POWER  6 LESS POWER  7 GEAR  8 FLAPS\n9 IGNORE ALT  0 CUT THROTTLE\nSPACE read alt   V read speed   B read fuel\nR ping gear   F ping flaps",
                new Vector2(0, 0), new Vector2(48, 40), TextAnchor.LowerLeft);
        }

        protected virtual void BuildChecklist()
        {
            AddCheck("GEAR DOWN", s => s.gear);
            AddCheck("FLAPS SET", s => s.flaps > 0);
        }

        protected void AddCheck(string label, System.Func<Snapshot, bool> done)
        {
            int i = checks.Count;
            var dot = Ui.Img(root, Tokens.TextMuted.A(0.4f), Ui.Circle);
            Ui.Place(dot, new Vector2(0.5f, 0), new Vector2(-260 + i * 210 - 70, 170), new Vector2(26, 26));
            var t = Ui.Label(root, label, 22, Tokens.TextMuted, TextAnchor.MiddleLeft, FontStyle.Bold);
            Ui.Place(t, new Vector2(0.5f, 0), new Vector2(-260 + i * 210 + 30, 170), new Vector2(170, 30));
            checks.Add(new CheckItem { done = done, dot = dot, label = t });
        }

        protected override void TickRole(Snapshot s, float dt)
        {
            if (!s.nav) return;
            int left = Mathf.CeilToInt(s.timeLeft);
            timer.text = $"{left / 60}:{left % 60:00}";
            timer.color = s.timeLeft < 30 ? Tokens.Danger : Tokens.TextPrimary;

            var c = s.glitch ? Tokens.Amber : Tokens.Cyan;
            values[0].text = $"{Mathf.RoundToInt(s.altFt / 10) * 10:N0}";
            values[1].text = $"{Mathf.RoundToInt(s.kt)}";
            values[2].text = $"{Mathf.RoundToInt(s.vsFpm / 50) * 50:+#;-#;0}";
            values[3].text = $"{Mathf.RoundToInt(s.hdg):000}";
            values[4].text = $"{Mathf.RoundToInt(s.fuel * 100)}";
            for (int i = 0; i < 5; i++) values[i].color = c;
            values[4].color = s.fuel < 0.15f ? Tokens.Danger : c;
            if (s.glitch && Random.value < 0.15f) values[0].text = $"{Random.Range(0, 99999):N0}";

            pathDot.anchoredPosition = Vector2.Lerp(pathDot.anchoredPosition, new Vector2(s.loc, s.gs) * 220, 1 - Mathf.Exp(-10 * dt));
            if (cogDiamond != null) cogDiamond.anchoredPosition = new Vector2(-18, Mathf.Clamp(s.cog, -1, 1) * 190);
            mapPlane.anchoredPosition = new Vector2(Mathf.Clamp(s.pos.x * MapScaleX, -170, 170), 30 + (Mathf.Max(s.pos.z, MapZMin) - MapZMin) * mapScaleZ);
            warnings.text = s.warnings.Replace("|", "    ");

            foreach (var k in checks)
            {
                bool d = k.done(s);
                k.dot.color = d ? Tokens.Success : Tokens.TextMuted.A(0.4f);
                k.label.color = d ? Tokens.Success : Tokens.TextMuted;
                if (d && !k.was) k.dot.transform.localScale = Vector3.one * 1.8f; // pop
                k.dot.transform.localScale = Vector3.Lerp(k.dot.transform.localScale, Vector3.one, 1 - Mathf.Exp(-12 * dt));
                k.was = d;
            }

            if (Input.GetKeyDown(KeyCode.Space)) Controls.Say($"ALT {Mathf.RoundToInt(s.altFt / 50) * 50} FT");
            if (Input.GetKeyDown(KeyCode.V)) Controls.Say($"SPEED {Mathf.RoundToInt(s.kt / 5) * 5} KNOTS");
            if (Input.GetKeyDown(KeyCode.B)) Controls.Say($"FUEL {Mathf.RoundToInt(s.fuel * 100)} PERCENT");
        }
    }
}
