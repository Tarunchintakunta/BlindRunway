using System;
using UnityEngine;
using UnityEngine.UI;

namespace BlindRunway
{
    // Modal layer above everything: pause menu (Esc in flight) and settings. Online game, so pause never stops time.
    public class Overlay : MonoBehaviour
    {
        RectTransform root;
        public bool Open { get; private set; }

        void Awake() => root = Ui.MakeCanvas("Overlay", 20);

        void Update()
        {
            var phase = App.I.Session.Phase;
            if (!Input.GetKeyDown(KeyCode.Escape)) return;
            if (Open) Close();
            else if (phase == Phase.Playing || phase == Phase.Intro) Pause();
        }

        public void Close()
        {
            foreach (Transform c in root) Destroy(c.gameObject);
            Open = false;
        }

        RectTransform Modal(string title, float height)
        {
            Close();
            Open = true;
            var dim = Ui.Img(root, Tokens.BgVoid.A(0.7f), null);
            dim.raycastTarget = true;
            Ui.Fill(dim.rectTransform);
            var panel = Ui.Img(root, Tokens.BgPanel);
            Ui.Place(panel, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640, height));
            Ui.Place(Ui.Img(panel.transform, Tokens.Cyan), new Vector2(0.5f, 1), Vector2.zero, new Vector2(640, 8));
            var t = Ui.Label(panel.transform, title, 44, Tokens.TextPrimary, TextAnchor.MiddleCenter, FontStyle.Bold);
            Ui.Place(t, new Vector2(0.5f, 1), new Vector2(0, -36), new Vector2(600, 60));
            return (RectTransform)panel.transform;
        }

        public void Pause()
        {
            var s = App.I.Session;
            bool solo = s.Lobby.Solo;
            var p = Modal("PAUSED", solo ? 560 : 470);
            int row = 0;
            Row(p, row++, Ui.Btn(p, "RESUME", Close));
            if (solo && s.Phase == Phase.Playing)
            {
                for (int i = 0; i < 3; i++) // switch seat
                {
                    var seat = (Role)i;
                    var b = Ui.Btn(p, Roles.Names[i], () =>
                    {
                        s.SwitchSeat(seat);
                        Close();
                    }, seat == s.MyRole ? Roles.Tint(seat) : Tokens.BgVoid);
                    Ui.Place(b, new Vector2(0.5f, 1), new Vector2((i - 1) * 176, -120 - row * 92), new Vector2(164, 72));
                }
                row++;
            }
            Row(p, row++, Ui.Btn(p, "SETTINGS", () => Settings(Pause), Tokens.BgVoid));
            Row(p, row, Ui.Btn(p, solo ? "QUIT TO MENU" : "LEAVE FLIGHT", () =>
            {
                Close();
                s.Leave(solo ? null : "You left the flight.");
            }, Tokens.Danger));
            var note = Ui.Label(p, "The plane keeps flying while you're here.", 22, Tokens.TextMuted);
            Ui.Place(note, new Vector2(0.5f, 0), new Vector2(0, 24), new Vector2(600, 30));
        }

        public void Settings(Action back)
        {
            var p = Modal("SETTINGS", 620);
            Stepper(p, 0, "VOLUME", () => $"{Mathf.RoundToInt(BlindRunway.Settings.Volume * 100)}%",
                d => BlindRunway.Settings.Volume = Mathf.Clamp01(BlindRunway.Settings.Volume + d * 0.1f), back);
            Stepper(p, 1, "UI SCALE", () => $"{BlindRunway.Settings.UiScale:0.0}x",
                d => BlindRunway.Settings.UiScale = Mathf.Clamp(BlindRunway.Settings.UiScale + d * 0.1f, 0.8f, 1.5f), back);
            Toggle(p, 2, "SCREEN SHAKE", () => BlindRunway.Settings.Shake, v => BlindRunway.Settings.Shake = v, back);
            Toggle(p, 3, "COLORBLIND PALETTE", () => BlindRunway.Settings.Colorblind, v => BlindRunway.Settings.Colorblind = v, back);
            Row(p, 4, Ui.Btn(p, "BACK", () =>
            {
                if (back != null) back();
                else Close();
            }, Tokens.BgVoid));
        }

        void Stepper(RectTransform p, int row, string label, Func<string> value, Action<int> change, Action back)
        {
            Line(p, row, label, value());
            var minus = Ui.Btn(p, "-", () => Apply(() => change(-1), back), Tokens.BgVoid);
            Ui.Place(minus, new Vector2(1, 1), new Vector2(-150, -120 - row * 92), new Vector2(64, 64));
            var plus = Ui.Btn(p, "+", () => Apply(() => change(1), back), Tokens.BgVoid);
            Ui.Place(plus, new Vector2(1, 1), new Vector2(-40, -120 - row * 92), new Vector2(64, 64));
        }

        void Toggle(RectTransform p, int row, string label, Func<bool> get, Action<bool> set, Action back)
        {
            Line(p, row, label, "");
            var on = get();
            var b = Ui.Btn(p, on ? "ON" : "OFF", () => Apply(() => set(!get()), back), on ? Tokens.Success : Tokens.BgVoid);
            Ui.Place(b, new Vector2(1, 1), new Vector2(-40, -120 - row * 92), new Vector2(174, 64));
        }

        static void Line(RectTransform p, int row, string label, string value)
        {
            Ui.Place(Ui.Label(p, label, 26, Tokens.TextPrimary, TextAnchor.MiddleLeft, FontStyle.Bold), new Vector2(0, 1), new Vector2(40, -120 - row * 92), new Vector2(300, 64));
            if (value.Length > 0)
                Ui.Place(Ui.Label(p, value, 28, Tokens.Cyan, TextAnchor.MiddleRight, FontStyle.Bold), new Vector2(1, 1), new Vector2(-230, -120 - row * 92), new Vector2(140, 64));
        }

        // Save, re-scale every canvas, redraw this modal.
        void Apply(Action change, Action back)
        {
            change();
            BlindRunway.Settings.Save();
            foreach (var s in FindObjectsByType<CanvasScaler>())
                s.referenceResolution = new Vector2(1920, 1080) / BlindRunway.Settings.UiScale;
            Settings(back);
        }

        static void Row(RectTransform p, int row, Button b) =>
            Ui.Place(b, new Vector2(0.5f, 1), new Vector2(0, -120 - row * 92), new Vector2(520, 72));
    }
}
