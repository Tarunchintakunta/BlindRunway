using UnityEngine;
using UnityEngine.UI;

namespace BlindRunway
{
    // Owns the in-level HUD canvas and swaps the role-specific HUD when the seat changes.
    public class Hud : MonoBehaviour
    {
        RectTransform root;
        RoleHud current;
        Role role = Role.None;

        void Awake() => root = Ui.MakeCanvas("Hud", 5);

        public void Show(Role r, LevelDefinition L)
        {
            if (current != null && r == role) return;
            Clear();
            role = r;
            current = r == Role.Pilot ? new PilotHud() : r == Role.Navigator ? new NavigatorHud() : r == Role.Cabin ? new CabinHud() : (RoleHud)null;
            current?.Build(root, L);
            if (App.I.Session.Lobby.Solo) SeatBar(r);
        }

        static readonly string[] Short = { "PILOT", "NAV", "CABIN" };

        // Solo hot-seat bar: always shows which seat you're in; click or F1-F3 to switch.
        void SeatBar(Role active)
        {
            var bar = Ui.Img(root, Tokens.BgVoid.A(0.85f));
            Ui.Place(bar, new Vector2(0.5f, 0), new Vector2(0, 10), new Vector2(560, 60));
            for (int i = 0; i < 3; i++)
            {
                var seat = (Role)i;
                var tint = Roles.Tint(seat);
                bool on = seat == active;
                var pill = Ui.Btn(bar.transform, $"F{i + 1}  {Short[i]}", () => App.I.Session.SwitchSeat(seat), on ? tint : Tokens.BgPanel);
                Ui.Place(pill, new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 182, 0), new Vector2(170, 44));
                var label = pill.GetComponentInChildren<Text>();
                label.fontSize = 22;
                if (!on) label.color = tint;
            }
        }

        public void Clear()
        {
            foreach (Transform c in root) Destroy(c.gameObject);
            current = null;
            role = Role.None;
        }

        void Update()
        {
            var s = App.I.Session.Snap;
            if (current != null && s != null) current.Tick(s, Time.deltaTime);
        }
    }

    public abstract class RoleHud
    {
        protected RectTransform root;
        protected LevelDefinition level;
        Text callout;
        float calloutAge = 99;
        int lastSeq;
        readonly System.Collections.Generic.List<string> coach = new System.Collections.Generic.List<string>();
        Image coachBox;
        Text coachText;
        float coachT;
        const float CoachSeconds = 7;

        public void Build(RectTransform r, LevelDefinition L)
        {
            root = r;
            level = L;
            BuildRole();
            callout = Ui.Label(root, "", 54, Tokens.TextPrimary, TextAnchor.MiddleCenter, FontStyle.Bold);
            Ui.Place(callout, new Vector2(0.5f, 1), new Vector2(0, -120), new Vector2(1600, 80));
            BuildCoach();
        }

        // First-time coach marks for this seat ("Pilot: ...") plus shared ones ("All: ...").
        void BuildCoach()
        {
            var me = App.I.Session.MyRole;
            if (me == Role.None || PlayerPrefs.GetInt(CoachKey(me), 0) == 1) return;
            var prefix = Roles.Names[(int)me] + ":";
            foreach (var m in level.coachMarks)
            {
                if (m.ToUpperInvariant().StartsWith(prefix)) coach.Add(m.Substring(prefix.Length).Trim());
                else if (m.StartsWith("All:")) coach.Add(m.Substring(4).Trim());
                else if (m.StartsWith("Solo:") && App.I.Session.Lobby.Solo && me == Role.Pilot) coach.Add(m.Substring(5).Trim()); // once: solo starts in the Pilot seat
            }
            if (coach.Count == 0) return;
            coachBox = Ui.Img(root, Tokens.BgPanel.A(0.92f));
            Ui.Place(coachBox, new Vector2(0.5f, 1), new Vector2(0, -200), new Vector2(900, 72));
            Ui.Place(Ui.Img(coachBox.transform, Roles.Tint(me), null), new Vector2(0, 0.5f), Vector2.zero, new Vector2(8, 72));
            coachText = Ui.Label(coachBox.transform, coach[0], 28, Tokens.TextPrimary);
            Ui.Fill(coachText.rectTransform, 16);
        }

        string CoachKey(Role r) => $"br.coach.{level.number}.{(int)r}";

        void TickCoach(float dt)
        {
            if (coachBox == null) return;
            coachT += dt;
            int i = (int)(coachT / CoachSeconds);
            if (i >= coach.Count)
            {
                Object.Destroy(coachBox.gameObject);
                coachBox = null;
                PlayerPrefs.SetInt(CoachKey(App.I.Session.MyRole), 1);
                return;
            }
            coachText.text = coach[i];
            float k = coachT % CoachSeconds;
            coachBox.color = Tokens.BgPanel.A(0.92f * Mathf.Clamp01(Mathf.Min(k * 4, (CoachSeconds - k) * 4)));
            coachText.color = Tokens.TextPrimary.A(coachBox.color.a);
        }

        protected abstract void BuildRole();
        protected abstract void TickRole(Snapshot s, float dt);
        protected virtual void OnCallout(Callout c) { }

        public void Tick(Snapshot s, float dt)
        {
            foreach (var c in s.callouts)
            {
                if (c.seq <= lastSeq) continue;
                lastSeq = c.seq;
                callout.text = (c.from == App.I.Session.MyRole ? "YOU" : Roles.Names[(int)c.from]) + ":  " + c.text;
                callout.color = Roles.Tint(c.from);
                calloutAge = 0;
                OnCallout(c);
            }
            calloutAge += dt;
            callout.color = callout.color.A(Mathf.Clamp01(3.5f - calloutAge));
            TickCoach(dt);
            TickRole(s, dt);
        }

        protected Text Hint(string text, Vector2 anchor, Vector2 pos, TextAnchor align)
        {
            var t = Ui.Label(root, text, 22, Tokens.TextMuted, align);
            t.lineSpacing = 1.25f;
            Ui.Place(t, anchor, pos, new Vector2(620, 220));
            return t;
        }
    }

    // Pilot: a grimy windshield and the controls in hand. Never a number.
    public class PilotHud : RoleHud
    {
        RectTransform throttleHandle;
        Text gearState;
        Image gearGlow, flapsGlow;
        Image[] flapNotches = new Image[3];
        Text listen;
        float sinceNav;
        bool wasOnGround;

        protected override void BuildRole()
        {
            var rng = new System.Random(3);
            for (int i = 0; i < 14; i++) // windshield grime, kept to the edges
            {
                float d = 160 + (float)rng.NextDouble() * 320;
                var anchor = new Vector2(rng.NextDouble() < 0.5 ? (float)rng.NextDouble() * 0.2f : 0.8f + (float)rng.NextDouble() * 0.2f, (float)rng.NextDouble());
                Ui.Place(Ui.Img(root, Tokens.BgVoid.A(0.18f + (float)rng.NextDouble() * 0.2f), Ui.Circle), anchor, Vector2.zero, new Vector2(d, d * 0.8f));
            }
            Ui.Place(Ui.Img(root, Tokens.BgVoid.A(0.85f), null), new Vector2(0.5f, 0), Vector2.zero, new Vector2(4000, 40));

            var panel = Ui.Img(root, Tokens.BgPanel.A(0.9f));
            Ui.Place(panel, new Vector2(1, 0), new Vector2(-48, 48), new Vector2(420, 300));
            var p = panel.transform;

            // throttle lever: position only
            Ui.Place(Ui.Img(p, Tokens.BgVoid), new Vector2(0, 0.5f), new Vector2(40, 0), new Vector2(26, 230));
            throttleHandle = Ui.Place(Ui.Img(p, Tokens.Amber), new Vector2(0, 0), new Vector2(28, 35), new Vector2(50, 24));
            Ui.Place(Ui.Label(p, "THR", 20, Tokens.TextMuted), new Vector2(0, 1), new Vector2(28, 0), new Vector2(50, 30));

            gearGlow = Ui.Img(p, Tokens.Cyan.A(0));
            Ui.Place(gearGlow, new Vector2(0.5f, 1), new Vector2(40, -30), new Vector2(180, 110));
            var gearBox = Ui.Img(p, Tokens.BgVoid);
            Ui.Place(gearBox, new Vector2(0.5f, 1), new Vector2(40, -38), new Vector2(164, 94));
            Ui.Place(Ui.Label(gearBox.transform, "GEAR  [G]", 20, Tokens.TextMuted), new Vector2(0.5f, 1), new Vector2(0, -6), new Vector2(160, 30));
            gearState = Ui.Label(gearBox.transform, "UP", 34, Tokens.TextPrimary, TextAnchor.MiddleCenter, FontStyle.Bold);
            Ui.Place(gearState, new Vector2(0.5f, 0), new Vector2(0, 8), new Vector2(160, 46));

            flapsGlow = Ui.Img(p, Tokens.Cyan.A(0));
            Ui.Place(flapsGlow, new Vector2(0.5f, 0), new Vector2(40, 22), new Vector2(180, 110));
            var flapsBox = Ui.Img(p, Tokens.BgVoid);
            Ui.Place(flapsBox, new Vector2(0.5f, 0), new Vector2(40, 30), new Vector2(164, 94));
            Ui.Place(Ui.Label(flapsBox.transform, "FLAPS  [F]", 20, Tokens.TextMuted), new Vector2(0.5f, 1), new Vector2(0, -6), new Vector2(160, 30));
            for (int i = 0; i < 3; i++)
            {
                flapNotches[i] = Ui.Img(flapsBox.transform, Tokens.TextMuted);
                Ui.Place(flapNotches[i], new Vector2(0.5f, 0), new Vector2((i - 1) * 46, 16), new Vector2(36, 26));
            }

            listen = Ui.Pulsing(Ui.Label(root, "LISTEN TO NAV", 72, Tokens.Amber, TextAnchor.MiddleCenter, FontStyle.Bold));
            Ui.Place(listen, new Vector2(0.5f, 0.5f), new Vector2(0, 120), new Vector2(1200, 100));

            if (level.rain)
                for (int i = 0; i < 60; i++)
                {
                    var drop = Ui.Img(root, Tokens.TextPrimary.A(0.18f), null);
                    Ui.Place(drop, new Vector2((float)rng.NextDouble(), 1), Vector2.zero, new Vector2(2, 40));
                    drop.transform.localRotation = Quaternion.Euler(0, 0, -12);
                    drop.gameObject.AddComponent<RainStreak>().offset = (float)rng.NextDouble();
                }

            Hint("W/S  pitch   A/D  turn\nShift/Ctrl  throttle\n1 SAY AGAIN?  2 HOW HIGH?  3 COPY!", new Vector2(0, 0), new Vector2(48, 48), TextAnchor.LowerLeft);
        }

        protected override void OnCallout(Callout c)
        {
            if (c.from == Role.Navigator) sinceNav = 0;
        }

        protected override void TickRole(Snapshot s, float dt)
        {
            throttleHandle.anchoredPosition = new Vector2(28, 35 + s.throttle * 205);
            gearState.text = s.gear ? "DOWN" : "UP";
            gearState.color = s.gear ? Tokens.Success : Tokens.TextPrimary;
            float glow = 0.35f + 0.35f * Mathf.Sin(Time.time * 10);
            gearGlow.color = Tokens.Cyan.A(s.pingGear ? glow : 0);
            flapsGlow.color = Tokens.Cyan.A(s.pingFlaps ? glow : 0);
            for (int i = 0; i < 3; i++) flapNotches[i].color = i == s.flaps ? Tokens.Cyan : Tokens.TextMuted.A(0.4f);

            sinceNav += dt;
            listen.enabled = sinceNav > 20 && !s.onGround;
            if (s.onGround && !wasOnGround) App.I.World.Shake(0.8f);
            wasOnGround = s.onGround;
        }
    }

    // Windshield rain: loops top to bottom with a slight slant.
    public class RainStreak : MonoBehaviour
    {
        public float offset;

        void Update()
        {
            var rt = (RectTransform)transform;
            float t = Mathf.Repeat(Time.time * 0.9f + offset, 1f);
            rt.anchoredPosition = new Vector2(t * 120, -t * 1200);
        }
    }
}
