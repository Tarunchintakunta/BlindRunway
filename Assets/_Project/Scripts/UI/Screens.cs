using UnityEngine;
using UnityEngine.UI;

namespace BlindRunway
{
    // Full-screen menus. Each Show* clears the root and rebuilds; cheap and keeps state out of the view.
    public class Screens : MonoBehaviour
    {
        RectTransform root;
        Phase shown = Phase.Menu;
        bool lobbyAnimated;

        void Awake() => root = Ui.MakeCanvas("Screens", 10);

        public RectTransform Clear(bool backdrop = true)
        {
            CancelInvoke();
            foreach (Transform c in root) Destroy(c.gameObject);
            if (backdrop) Ui.Fill(Ui.Img(root, Tokens.BgVoid, null).rectTransform);
            return root;
        }

        public void Splash()
        {
            var r = Clear();
            Stars(r, 80);
            PlaneSilhouette(r, new Vector2(0, 140), 1.4f);
            Logo(r, new Vector2(0, -40));
            Invoke(nameof(Menu), 2.2f);
        }

        public void Menu()
        {
            var r = Clear();
            Stars(r, 60);
            Clouds(r);
            var plane = PlaneSilhouette(r, new Vector2(0, 360), 0.35f);
            plane.gameObject.AddComponent<Drift>().Set(90, 0.02f, 1.6f);
            var glow = Ui.Img(r, Tokens.Cyan.A(0.07f), Ui.Circle);
            Ui.Place(glow, new Vector2(0.5f, 0.5f), new Vector2(0, 220), new Vector2(1500, 320));
            Logo(r, new Vector2(0, 220));
            string[] labels = { "PLAY ALONE", "PLAY WITH FRIENDS", "LEVELS", "SETTINGS", "QUIT" };
            System.Action[] actions =
            {
                () => LevelSelect(-1, RouteMode.Solo),
                Play,
                () => LevelSelect(-1, RouteMode.Ask),
                () => App.I.Overlay.Settings(null),
                Application.Quit,
            };
            for (int i = 0; i < labels.Length; i++)
            {
                var b = Ui.Btn(r, labels[i], actions[i], i == 0 ? Tokens.Cyan : i == 1 ? Tokens.Amber : Tokens.BgPanel);
                Ui.Place(b, new Vector2(0.5f, 0.5f), new Vector2(0, 60 - i * 88), new Vector2(460, 72));
                Ui.In(b, 0.08f * i);
            }
        }

        public void Play()
        {
            var r = Clear();
            Stars(r, 60);
            var h = Ui.Label(r, "FIND YOUR CREW", 64, Tokens.TextPrimary, TextAnchor.MiddleCenter, FontStyle.Bold);
            Ui.Place(h, new Vector2(0.5f, 0.5f), new Vector2(0, 300), new Vector2(1200, 90));

            var host = Ui.Btn(r, "HOST A FLIGHT", Host);
            Ui.Place(host, new Vector2(0.5f, 0.5f), new Vector2(0, 140), new Vector2(520, 84));

            var hint = Ui.Label(r, "or join with the host's code (blank = this computer)", 26, Tokens.TextMuted);
            Ui.Place(hint, new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(1200, 40));
            var field = Ui.Field(r, "C0A8-0105");
            field.characterLimit = 15;
            Ui.Place(field, new Vector2(0.5f, 0.5f), new Vector2(-110, -50), new Vector2(300, 76));
            var join = Ui.Btn(r, "JOIN", () => Join(field.text), Tokens.BgPanel);
            Ui.Place(join, new Vector2(0.5f, 0.5f), new Vector2(170, -50), new Vector2(200, 76));

            var back = Ui.Btn(r, "BACK", Menu, Tokens.BgPanel);
            Ui.Place(back, new Vector2(0, 0), new Vector2(48, 48), new Vector2(220, 70));
        }

        void Host() => App.I.Session.Host();

        // Route map: five airports joined by a dashed flight path.
        // Solo: fly alone. Friends: host a lobby. Ask: offer both (opened from LEVELS).
        public enum RouteMode { Solo, Friends, Ask }

        public void LevelSelect(int focus = -1, RouteMode mode = RouteMode.Ask)
        {
            var s = App.I.Session;
            if (focus < 0) focus = s.PickedLevel;
            var r = Clear();
            Stars(r, 60);
            var title = Ui.Label(r, "ROUTES", 56, Tokens.TextPrimary, TextAnchor.UpperLeft, FontStyle.Bold);
            Ui.Place(title, new Vector2(0, 1), new Vector2(48, -48), new Vector2(600, 70));

            int n = Levels.Count;
            var pts = new Vector2[n];
            for (int i = 0; i < n; i++)
                pts[i] = new Vector2(-640 + i * (1280f / Mathf.Max(1, n - 1)), 120 + Mathf.Sin(i * 1.3f) * 110);
            for (int i = 0; i + 1 < n; i++) Dashes(r, pts[i], pts[i + 1]);

            for (int i = 0; i < n; i++)
            {
                int idx = i;
                var def = Levels.Get(i);
                bool sel = i == focus, open = Progress.IsUnlocked(i), done = Progress.Cleared(i);
                var lit = Progress.AllCleared ? Tokens.Success : Tokens.Cyan; // finale: every airport lights up
                var glow = Ui.Img(r, (done ? Tokens.Success : lit).A(sel ? 0.3f : done ? 0.2f : 0.08f), Ui.Circle);
                Ui.Place(glow, new Vector2(0.5f, 0.5f), pts[i], new Vector2(150, 150));
                if (sel) Ui.Pulsing(glow);
                var node = Ui.Img(r, sel ? lit : open ? Tokens.BgPanel : Tokens.BgVoid, Ui.Circle);
                node.raycastTarget = true;
                Ui.Place(node, new Vector2(0.5f, 0.5f), pts[i], new Vector2(96, 96));
                var b = node.gameObject.AddComponent<Button>();
                b.targetGraphic = node;
                b.onClick.AddListener(() => LevelSelect(idx, mode));
                node.gameObject.AddComponent<Pop>();
                Ui.In(node, 0.08f * i);
                for (int k = 0; k < 8; k++) // airport lights ring
                {
                    float a = k / 8f * Mathf.PI * 2;
                    var lamp = Ui.Img(node.transform, open ? Tokens.Cabin : Tokens.TextMuted.A(0.3f), Ui.Circle);
                    Ui.Place(lamp, new Vector2(0.5f, 0.5f), new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 60, new Vector2(8, 8));
                }
                Ui.Fill(Ui.Label(node.transform, $"{def.number:00}", 34, sel ? Tokens.BgVoid : Tokens.TextPrimary, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform);
                var tag = open ? def.title.ToUpperInvariant() : "LOCKED";
                if (done) tag += "\n" + new string('*', Progress.Stars(i)) + new string('.', 3 - Progress.Stars(i));
                var name = Ui.Label(r, tag, 22, sel ? Tokens.TextPrimary : Tokens.TextMuted, TextAnchor.UpperCenter, FontStyle.Bold);
                Ui.Place(name, new Vector2(0.5f, 0.5f), pts[i] + new Vector2(0, -95), new Vector2(260, 60));
            }

            var f = Levels.Get(focus);
            var panel = Ui.Img(r, Tokens.BgPanel);
            Ui.Place(panel, new Vector2(0.5f, 0), new Vector2(0, 150), new Vector2(1100, 260));
            var t = panel.transform;
            Ui.Place(Ui.Label(t, $"LEVEL {f.number:00}  -  {f.title.ToUpperInvariant()}", 34, Tokens.Cyan, TextAnchor.UpperLeft, FontStyle.Bold), new Vector2(0, 1), new Vector2(32, -24), new Vector2(1040, 44));
            Ui.Place(Ui.Label(t, f.brief, 24, Tokens.TextPrimary, TextAnchor.UpperLeft), new Vector2(0, 1), new Vector2(32, -78), new Vector2(1040, 60));
            var obj = Ui.Label(t, "-  " + string.Join("\n-  ", f.objectives), 22, Tokens.TextMuted, TextAnchor.UpperLeft);
            Ui.Place(obj, new Vector2(0, 1), new Vector2(32, -130), new Vector2(700, 120));

            bool canFly = Progress.IsUnlocked(focus);
            float x = -48;
            if (mode != RouteMode.Friends)
            {
                var solo = Ui.Btn(r, canFly ? "FLY SOLO" : $"CLEAR LEVEL {focus:00} FIRST", () => s.PlayAlone(focus), canFly ? Tokens.Cyan : Tokens.BgPanel);
                solo.interactable = canFly;
                Ui.Place(solo, new Vector2(1, 0), new Vector2(x, 48), new Vector2(380, 76));
                x -= 410;
            }
            if (mode != RouteMode.Solo && canFly)
            {
                var host = Ui.Btn(r, "HOST FOR FRIENDS", () =>
                {
                    s.PickedLevel = focus;
                    s.Host();
                }, Tokens.Amber);
                Ui.Place(host, new Vector2(1, 0), new Vector2(x, 48), new Vector2(380, 76));
            }
            var back = Ui.Btn(r, "BACK", Menu, Tokens.BgPanel);
            Ui.Place(back, new Vector2(0, 0), new Vector2(48, 48), new Vector2(220, 70));
        }

        static void Dashes(Transform r, Vector2 a, Vector2 b)
        {
            var d = b - a;
            float len = d.magnitude, ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            for (float x = 70; x < len - 70; x += 28)
            {
                var dash = Ui.Img(r, Tokens.TextMuted.A(0.5f), null);
                Ui.Place(dash, new Vector2(0.5f, 0.5f), a + d.normalized * x, new Vector2(14, 3));
                dash.transform.localRotation = Quaternion.Euler(0, 0, ang);
            }
        }
        void Join(string code) => App.I.Session.Join(code);

        // Called whenever Session state changes.
        public void Route()
        {
            var phase = App.I.Session.Phase;
            if (phase != Phase.Lobby) lobbyAnimated = false;
            switch (phase)
            {
                case Phase.Menu:
                    if (shown != Phase.Menu) Menu();
                    break;
                case Phase.Lobby:
                    Lobby();
                    break;
                case Phase.Result:
                    if (shown != Phase.Result) Result();
                    break;
                case Phase.Intro:
                    if (shown != Phase.Intro) RoleIntro();
                    break;
                case Phase.Playing:
                    if (shown != Phase.Playing) Clear(false);
                    App.I.World.Show(App.I.Session.MyRole, App.I.Session.Def); // seat or solo view changed
                    App.I.Hud.Show(App.I.Session.MyRole, App.I.Session.Def);
                    break;
            }
            if (phase != Phase.Playing) App.I.Hud.Clear();
            if (phase == Phase.Menu || phase == Phase.Lobby || phase == Phase.Intro) App.I.World.Clear(); // Result keeps the frozen world
            shown = phase;
        }

        public void Lobby()
        {
            var s = App.I.Session;
            var L = s.Lobby;
            var r = Clear();
            Stars(r, 40);

            var title = Ui.Label(r, "CREW LOBBY", 56, Tokens.TextPrimary, TextAnchor.UpperLeft, FontStyle.Bold);
            Ui.Place(title, new Vector2(0, 1), new Vector2(48, -48), new Vector2(800, 70));
            var leave = Ui.Btn(r, "LEAVE", () => s.Leave(), Tokens.BgPanel);
            Ui.Place(leave, new Vector2(0, 0), new Vector2(48, 48), new Vector2(220, 70));

            if (L.players == 0)
            {
                var wait = Ui.Pulsing(Ui.Label(r, "Connecting to the host...", 40, Tokens.TextMuted));
                Ui.Place(wait, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200, 60));
                return;
            }

            var codeHint = Ui.Label(r, "JOIN CODE", 24, Tokens.TextMuted, TextAnchor.UpperRight);
            Ui.Place(codeHint, new Vector2(1, 1), new Vector2(-48, -48), new Vector2(400, 30));
            var code = Ui.Label(r, L.code, 52, Tokens.Cyan, TextAnchor.UpperRight, FontStyle.Bold);
            Ui.Place(code, new Vector2(1, 1), new Vector2(-48, -80), new Vector2(500, 64));
            var crew = Ui.Label(r, $"CREW {L.players}/3", 26, Tokens.TextMuted, TextAnchor.UpperLeft);
            Ui.Place(crew, new Vector2(0, 1), new Vector2(50, -120), new Vector2(400, 36));

            LevelPicker(r, L, s.IsHost);
            for (int i = 0; i < 3; i++) RoleCard(r, (Role)i, L, s.MyRole, i);
            lobbyAnimated = true;

            var me = s.MyRole;
            if (me != Role.None)
            {
                bool ready = L.ready[(int)me];
                var rb = Ui.Btn(r, ready ? "NOT READY" : "READY", () => s.Act("ready"), ready ? Tokens.BgPanel : Tokens.Success);
                Ui.Place(rb, new Vector2(1, 0), new Vector2(s.IsHost ? -340 : -48, 48), new Vector2(260, 76));
            }
            if (s.IsHost)
            {
                bool can = Session.CanStart(L);
                var start = Ui.Btn(r, "START", () => s.Act("start"), can ? Tokens.Cyan : Tokens.BgPanel);
                start.interactable = can;
                Ui.Place(start, new Vector2(1, 0), new Vector2(-48, 48), new Vector2(260, 76));
            }
            if (s.IsHost && L.players == 1 && (Application.isEditor || Debug.isDebugBuild))
            {
                var solo = Ui.Btn(r, "SOLO TEST (F1-F3 swap seats)", () => s.Act("solo"), Tokens.BgPanel);
                Ui.Place(solo, new Vector2(0.5f, 0), new Vector2(0, 48), new Vector2(560, 70));
            }
            string hint = L.players < 3 ? $"Waiting for {3 - L.players} more crew. Share the join code."
                : me == Role.None ? "Take a seat."
                : Session.CanStart(L) ? (s.IsHost ? "Everyone is ready. Start when you like." : "Everyone is ready. Waiting for the host.")
                : "Starts when all three seats are ready.";
            var h = Ui.Label(r, hint, 26, Tokens.TextMuted);
            Ui.Place(h, new Vector2(0.5f, 0), new Vector2(0, 150), new Vector2(1200, 40));
        }

        static void LevelPicker(Transform r, LobbyState L, bool host)
        {
            var def = Levels.Get(L.level);
            var name = Ui.Label(r, $"LEVEL {def.number:00}  -  {def.title.ToUpperInvariant()}", 34, Tokens.TextPrimary, TextAnchor.MiddleCenter, FontStyle.Bold);
            Ui.Place(name, new Vector2(0.5f, 1), new Vector2(0, -60), new Vector2(1000, 50));
            var brief = Ui.Label(r, def.brief, 24, Tokens.TextMuted);
            Ui.Place(brief, new Vector2(0.5f, 1), new Vector2(0, -105), new Vector2(1000, 36));
            if (!host) return;
            if (L.level > 0)
            {
                var prev = Ui.Btn(r, "<", () => App.I.Session.Act("level", 0, L.level - 1), Tokens.BgPanel);
                Ui.Place(prev, new Vector2(0.5f, 1), new Vector2(-560, -55), new Vector2(64, 60));
            }
            if (L.level < Progress.Unlocked)
            {
                var next = Ui.Btn(r, ">", () => App.I.Session.Act("level", 0, L.level + 1), Tokens.BgPanel);
                Ui.Place(next, new Vector2(0.5f, 1), new Vector2(560, -55), new Vector2(64, 60));
            }
        }

        void RoleCard(Transform r, Role role, LobbyState L, Role mine, int i)
        {
            var tint = Roles.Tint(role);
            var card = Ui.Img(r, Tokens.BgPanel);
            Ui.Place(card, new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 540, 10), new Vector2(480, 600));
            if (!lobbyAnimated) Ui.In(card, 0.08f * i);
            var t = card.transform;

            Ui.Place(Ui.Img(t, tint), new Vector2(0.5f, 1), Vector2.zero, new Vector2(480, 10));
            var glyph = Ui.Img(t, tint.A(0.15f), Ui.Circle);
            Ui.Place(glyph, new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(120, 120));
            var letter = Ui.Label(glyph.transform, Roles.Names[i].Substring(0, 1), 64, tint, TextAnchor.MiddleCenter, FontStyle.Bold);
            Ui.Fill(letter.rectTransform);
            var name = Ui.Label(t, Roles.Names[i], 40, Tokens.TextPrimary, TextAnchor.MiddleCenter, FontStyle.Bold);
            Ui.Place(name, new Vector2(0.5f, 1), new Vector2(0, -175), new Vector2(440, 50));

            var body = "";
            foreach (var c in Roles.Can[i]) body += "+  " + c + "\n";
            foreach (var c in Roles.Cannot[i]) body += "-  " + c + "\n";
            var bullets = Ui.Label(t, body, 24, Tokens.TextMuted, TextAnchor.UpperLeft);
            bullets.lineSpacing = 1.3f;
            Ui.Place(bullets, new Vector2(0.5f, 1), new Vector2(0, -245), new Vector2(400, 220));

            long owner = L.seat[i];
            string status = owner == -1 ? "OPEN SEAT" : role == mine ? "YOU" : "CREWMATE";
            if (owner != -1 && L.ready[i]) status += "  -  READY";
            var st = Ui.Label(t, status, 26, owner == -1 ? Tokens.TextMuted : L.ready[i] ? Tokens.Success : tint, TextAnchor.MiddleCenter, FontStyle.Bold);
            Ui.Place(st, new Vector2(0.5f, 0), new Vector2(0, 110), new Vector2(440, 40));

            if (owner == -1)
            {
                var take = Ui.Btn(t, "TAKE SEAT", () => App.I.Session.Act("claim", i), tint);
                Ui.Place(take, new Vector2(0.5f, 0), new Vector2(0, 28), new Vector2(300, 66));
            }
        }

        // Full-bleed role card before each flight.
        public void RoleIntro()
        {
            var s = App.I.Session;
            var role = s.Lobby.solo ? Role.Pilot : s.MyRole;
            var r = Clear();
            if (role == Role.None)
            {
                Ui.Place(Ui.Label(r, "Spectating...", 40, Tokens.TextMuted), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800, 60));
                return;
            }
            int i = (int)role;
            var tint = Roles.Tint(role);
            Ui.Fill(Ui.Img(r, tint.A(0.08f), null).rectTransform);
            var disc = Ui.Img(r, tint.A(0.12f), Ui.Circle);
            Ui.Place(disc, new Vector2(0, 0.5f), new Vector2(-200, 0), new Vector2(1100, 1100));
            var letter = Ui.Label(disc.transform, Roles.Names[i].Substring(0, 1), 520, tint, TextAnchor.MiddleCenter, FontStyle.Bold);
            Ui.Fill(letter.rectTransform);
            Ui.In(disc, 0);

            var lvl = s.Def != null ? $"LEVEL {s.Def.number:00}  -  {s.Def.title.ToUpperInvariant()}" : "";
            Ui.Place(Ui.Label(r, lvl, 26, Tokens.TextMuted, TextAnchor.UpperLeft, FontStyle.Bold), new Vector2(0.5f, 1), new Vector2(380, -80), new Vector2(900, 40));
            var you = Ui.Label(r, "YOU ARE THE", 32, Tokens.TextMuted, TextAnchor.UpperLeft, FontStyle.Bold);
            Ui.Place(you, new Vector2(0.5f, 0.5f), new Vector2(380, 300), new Vector2(900, 44));
            var name = Ui.Label(r, Roles.Names[i], 120, tint, TextAnchor.UpperLeft, FontStyle.Bold);
            Ui.Place(name, new Vector2(0.5f, 0.5f), new Vector2(380, 230), new Vector2(900, 150));
            Ui.In(name, 0.1f);
            var tag = Ui.Label(r, Roles.Taglines[i], 32, Tokens.TextPrimary, TextAnchor.UpperLeft);
            Ui.Place(tag, new Vector2(0.5f, 0.5f), new Vector2(380, 70), new Vector2(900, 50));
            for (int k = 0; k < Roles.Can[i].Length; k++)
            {
                var b = Ui.Label(r, "+   " + Roles.Can[i][k], 34, Tokens.TextPrimary, TextAnchor.UpperLeft, FontStyle.Bold);
                Ui.Place(b, new Vector2(0.5f, 0.5f), new Vector2(380, -10 - k * 60), new Vector2(900, 50));
                Ui.In(b, 0.25f + k * 0.08f);
            }
            var no = Ui.Label(r, "-   " + string.Join("   -   ", Roles.Cannot[i]), 26, Tokens.Danger, TextAnchor.UpperLeft);
            Ui.Place(no, new Vector2(0.5f, 0.5f), new Vector2(380, -200), new Vector2(900, 80));
            var keys = Ui.Label(r, Roles.ControlsLine[i], 22, Tokens.TextMuted, TextAnchor.UpperLeft);
            Ui.Place(keys, new Vector2(0.5f, 0.5f), new Vector2(380, -290), new Vector2(900, 60));
            if (s.Lobby.solo)
                Ui.Place(Ui.Label(r, "SOLO TEST: F1 Pilot  F2 Navigator  F3 Cabin once airborne", 22, Tokens.Amber, TextAnchor.UpperLeft), new Vector2(0.5f, 0.5f), new Vector2(380, -340), new Vector2(900, 40));

            Button got = null;
            got = Ui.Btn(r, "GOT IT", () =>
            {
                s.Act("gotit");
                got.interactable = false;
                got.GetComponentInChildren<Text>().text = "WAITING FOR CREW...";
            }, tint);
            Ui.Place(got, new Vector2(1, 0), new Vector2(-48, 48), new Vector2(380, 84));
        }

        public void Result()
        {
            var s = App.I.Session;
            var res = s.Result ?? new ResultMsg { reason = "unknown" };
            var r = Clear(false);
            if (!res.win) App.I.World.Shake(1.5f);
            Ui.Fill(Ui.Img(r, Tokens.BgVoid.A(0.72f), null).rectTransform);

            var head = Ui.Label(r, res.win ? "LANDED" : "WHY WE DIED", 96, res.win ? Tokens.Success : Tokens.Danger, TextAnchor.MiddleCenter, FontStyle.Bold);
            Ui.Place(head, new Vector2(0.5f, 0.5f), new Vector2(0, 330), new Vector2(1400, 120));
            var why = Ui.Label(r, LevelRules.Describe(res.reason), 34, Tokens.TextPrimary);
            Ui.Place(why, new Vector2(0.5f, 0.5f), new Vector2(0, 245), new Vector2(1400, 50));

            if (res.win)
            {
                for (int i = 0; i < 3; i++)
                {
                    var star = Ui.Img(r, i < res.stars ? Tokens.Cabin : Tokens.TextMuted.A(0.25f), Ui.Star);
                    Ui.Place(star, new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 130, 140), new Vector2(110, 110));
                    Ui.In(star, 0.3f + i * 0.15f);
                }
                Confetti(r);
            }

            for (int i = 0; i < res.cards.Length; i++)
            {
                var c = res.cards[i];
                var tint = Roles.Tint(c.role);
                var card = Ui.Img(r, Tokens.BgPanel);
                Ui.Place(card, new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 470, res.win ? -110 : -40), new Vector2(430, 280));
                card.transform.localRotation = Quaternion.Euler(0, 0, (1 - i) * 3f);
                Ui.In(card, 0.5f + i * 0.12f);
                var t = card.transform;
                Ui.Place(Ui.Img(t, tint), new Vector2(0.5f, 1), Vector2.zero, new Vector2(430, 8));
                Ui.Place(Ui.Label(t, Roles.Names[(int)c.role], 20, tint, TextAnchor.UpperLeft, FontStyle.Bold), new Vector2(0, 1), new Vector2(24, -26), new Vector2(380, 26));
                Ui.Place(Ui.Label(t, c.title, 32, Tokens.TextPrimary, TextAnchor.UpperLeft, FontStyle.Bold), new Vector2(0, 1), new Vector2(24, -62), new Vector2(390, 80));
                Ui.Place(Ui.Label(t, c.body, 24, Tokens.TextMuted, TextAnchor.UpperLeft), new Vector2(0, 1), new Vector2(24, -150), new Vector2(390, 110));
            }
            if (res.campaignClear) CampaignClear(r);
            ResultButtons(r, res);
        }

        static void CampaignClear(Transform r)
        {
            var badge = Ui.Img(r, Tokens.Cabin);
            Ui.Place(badge, new Vector2(0, 1), new Vector2(48, -48), new Vector2(460, 120));
            Ui.In(badge, 1.2f);
            Ui.Place(Ui.Label(badge.transform, "CAMPAIGN CLEAR", 34, Tokens.BgVoid, TextAnchor.MiddleCenter, FontStyle.Bold), new Vector2(0.5f, 1), new Vector2(0, -14), new Vector2(440, 46));
            Ui.Place(Ui.Label(badge.transform, "ACT I", 26, Tokens.BgVoid, TextAnchor.MiddleCenter, FontStyle.Bold), new Vector2(0.5f, 0), new Vector2(0, 14), new Vector2(440, 40));
            var teaser = Ui.Label(r, "Coming soon: routes 6-30. Sandstorms, volcanic ash, a VIP meltdown.", 24, Tokens.TextMuted, TextAnchor.UpperLeft);
            Ui.Place(teaser, new Vector2(0, 1), new Vector2(48, -184), new Vector2(560, 70));
        }

        // Tasteful, not casino: 36 flakes, once.
        static void Confetti(Transform r)
        {
            var rng = new System.Random();
            Color[] colors = { Tokens.Cyan, Tokens.Amber, Tokens.Cabin, Tokens.Success };
            for (int i = 0; i < 36; i++)
            {
                var f = Ui.Img(r, colors[i % colors.Length], null);
                Ui.Place(f, new Vector2((float)rng.NextDouble(), 1), new Vector2(0, 20), new Vector2(10, 18));
                var fall = f.gameObject.AddComponent<Fall>();
                fall.speed = 300 + (float)rng.NextDouble() * 300;
                fall.spin = (float)rng.NextDouble() * 360 - 180;
                fall.delay = (float)rng.NextDouble() * 0.8f;
            }
        }

        void ResultButtons(Transform r, ResultMsg res)
        {
            var s = App.I.Session;
            if (!s.IsHost)
            {
                var wait = Ui.Pulsing(Ui.Label(r, "Waiting for the host...", 28, Tokens.TextMuted));
                Ui.Place(wait, new Vector2(1, 0), new Vector2(-48, 60), new Vector2(600, 50));
                return;
            }
            var lobby = Ui.Btn(r, "LOBBY", () => s.Act("lobby"), Tokens.BgPanel);
            Ui.Place(lobby, new Vector2(1, 0), new Vector2(-340, 48), new Vector2(260, 76));
            bool next = res.win && res.level + 1 <= Progress.Unlocked && res.level + 1 < Levels.Count;
            var retry = Ui.Btn(r, "RETRY", () => s.Act("retry"), next ? Tokens.BgPanel : Tokens.Cyan);
            Ui.Place(retry, new Vector2(1, 0), new Vector2(next ? -340 : -48, 48), new Vector2(260, 76));
            ((RectTransform)lobby.transform).anchoredPosition = new Vector2(next ? -632 : -340, 48);
            if (next)
            {
                var go = Ui.Btn(r, "NEXT ROUTE", () => s.Act("next"));
                Ui.Place(go, new Vector2(1, 0), new Vector2(-48, 48), new Vector2(260, 76));
            }
        }

        // ---- shared visuals ----

        public static void Logo(Transform r, Vector2 pos)
        {
            var title = Ui.Label(r, "B L I N D   R U N W A Y", 92, Tokens.Cyan, TextAnchor.MiddleCenter, FontStyle.Bold);
            Ui.Place(title, new Vector2(0.5f, 0.5f), pos, new Vector2(1600, 120));
            var tag = Ui.Label(r, "Three seats. Zero shared truth.", 30, Tokens.TextMuted);
            Ui.Place(tag, new Vector2(0.5f, 0.5f), pos + new Vector2(0, -80), new Vector2(1200, 50));
        }

        public static void Stars(Transform r, int count)
        {
            var rng = new System.Random(7);
            for (int i = 0; i < count; i++)
            {
                var s = Ui.Img(r, Tokens.TextPrimary.A(0.2f + (float)rng.NextDouble() * 0.5f), Ui.Circle);
                float d = 2 + (float)rng.NextDouble() * 4;
                Ui.Place(s, new Vector2((float)rng.NextDouble(), (float)rng.NextDouble()), Vector2.zero, new Vector2(d, d));
            }
        }

        // Three cloud layers: far ones slow and faint, near ones fast and bold; all lean with the mouse.
        static void Clouds(Transform r)
        {
            var rng = new System.Random(11);
            for (int layer = 0; layer < 3; layer++)
                for (int i = 0; i < 5; i++)
                {
                    float w = 300 + layer * 160 + (float)rng.NextDouble() * 200;
                    var c = Ui.Img(r, Tokens.BgPanel.A(0.35f + layer * 0.2f));
                    Ui.Place(c, new Vector2((float)rng.NextDouble(), 0.12f + layer * 0.08f + (float)rng.NextDouble() * 0.2f), Vector2.zero, new Vector2(w, w * 0.22f));
                    c.gameObject.AddComponent<Drift>().Set(12 + layer * 22, 0.004f + layer * 0.008f, 0);
                }
        }

        public static RectTransform PlaneSilhouette(Transform r, Vector2 pos, float scale)
        {
            var p = Ui.Node(r, "plane");
            Ui.Place(p, new Vector2(0.5f, 0.5f), pos, new Vector2(400, 200) * scale);
            var c = Tokens.TextPrimary.A(0.9f);
            Ui.Place(Ui.Img(p, c), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360, 40) * scale);              // fuselage
            Ui.Place(Ui.Img(p, c), new Vector2(0.5f, 0.5f), new Vector2(10, 0) * scale, new Vector2(70, 260) * scale); // wings
            Ui.Place(Ui.Img(p, c), new Vector2(0.5f, 0.5f), new Vector2(-160, 0) * scale, new Vector2(36, 110) * scale); // tail
            return p;
        }
    }

    public class Fall : MonoBehaviour
    {
        public float speed, spin, delay;

        void Update()
        {
            delay -= Time.unscaledDeltaTime;
            if (delay > 0) return;
            var rt = (RectTransform)transform;
            rt.anchoredPosition += Vector2.down * (speed * Time.unscaledDeltaTime);
            rt.Rotate(new Vector3(0, 0, spin * Time.unscaledDeltaTime));
            if (rt.anchoredPosition.y < -1300) Destroy(gameObject);
        }
    }

    // Horizontal drift with wrap-around plus mouse parallax; optional gentle bob.
    public class Drift : MonoBehaviour
    {
        float speed, parallax, bob, x;
        Vector2 start;

        public void Set(float pxPerSecond, float mouseParallax, float bobPx)
        {
            speed = pxPerSecond;
            parallax = mouseParallax;
            bob = bobPx;
        }

        void Start() => start = ((RectTransform)transform).anchoredPosition;

        void Update()
        {
            x = Mathf.Repeat(x + speed * Time.unscaledDeltaTime + 1400, 2800) - 1400;
            var mouse = (Vector2)Input.mousePosition - new Vector2(Screen.width, Screen.height) * 0.5f;
            ((RectTransform)transform).anchoredPosition = start + new Vector2(x, Mathf.Sin(Time.unscaledTime * 1.3f) * bob) - mouse * parallax;
        }
    }
}
