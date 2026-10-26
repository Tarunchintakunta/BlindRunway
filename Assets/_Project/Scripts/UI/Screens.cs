using UnityEngine;
using UnityEngine.UI;

namespace BlindRunway
{
    // Full-screen menus. Each Show* clears the root and rebuilds; cheap and keeps state out of the view.
    public class Screens : MonoBehaviour
    {
        RectTransform root;

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

        void Host()
        {
            if (Net.Host()) App.I.Toast("Hosting. Join code " + Net.ToCode(Net.LocalIp()));
            else App.I.Toast("Could not host (is port 7777 busy?)");
        }

        void Join(string code)
        {
            if (!Net.TryParseCode(code, out var ip)) { App.I.Toast("That join code looks wrong."); return; }
            if (Net.Join(ip)) App.I.Toast("Connecting to " + ip + "...");
            else App.I.Toast("Could not start client.");
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
