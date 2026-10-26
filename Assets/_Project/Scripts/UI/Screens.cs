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
            Logo(r, new Vector2(0, 220));
            string[] labels = { "PLAY", "LEVELS", "SETTINGS", "QUIT" };
            System.Action[] actions =
            {
                Play,
                () => App.I.Toast("Level select arrives soon."),
                () => App.I.Toast("Settings arrive soon."),
                Application.Quit,
            };
            for (int i = 0; i < labels.Length; i++)
            {
                var b = Ui.Btn(r, labels[i], actions[i], i == 0 ? Tokens.Cyan : Tokens.BgPanel);
                Ui.Place(b, new Vector2(0.5f, 0.5f), new Vector2(0, 40 - i * 92), new Vector2(420, 76));
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
            }
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

            for (int i = 0; i < 3; i++) RoleCard(r, (Role)i, L, s.MyRole, i);
            lobbyAnimated = true;
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
}
