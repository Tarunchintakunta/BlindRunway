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

        public void Build(RectTransform r, LevelDefinition L)
        {
            root = r;
            level = L;
            BuildRole();
            callout = Ui.Label(root, "", 54, Tokens.TextPrimary, TextAnchor.MiddleCenter, FontStyle.Bold);
            Ui.Place(callout, new Vector2(0.5f, 1), new Vector2(0, -120), new Vector2(1600, 80));
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
}
