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
                () => App.I.Toast("Multiplayer arrives next."),
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
