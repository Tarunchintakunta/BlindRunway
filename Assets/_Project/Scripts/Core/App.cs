using UnityEngine;
using UnityEngine.UI;

namespace BlindRunway
{
    // Root object: owns camera, UI and (later) network session.
    public class App : MonoBehaviour
    {
        public static App I;
        public Camera Cam;
        public Screens Screens;
        Text toast;
        float toastUntil;

        void Awake()
        {
            I = this;
            Application.targetFrameRate = 60;
            Application.runInBackground = true; // several local clients share one machine during tests
            Settings.Load();
            Ui.Init();

            Cam = new GameObject("Camera").AddComponent<Camera>();
            Cam.tag = "MainCamera";
            Cam.clearFlags = CameraClearFlags.SolidColor;
            Cam.backgroundColor = Tokens.BgVoid;
            Cam.farClipPlane = 20000;
            Cam.gameObject.AddComponent<AudioListener>();
            DontDestroyOnLoad(Cam.gameObject);

            var overlay = Ui.MakeCanvas("Toast", 30);
            toast = Ui.Label(overlay, "", 28, Tokens.TextPrimary);
            Ui.Place(toast, new Vector2(0.5f, 0), new Vector2(0, 60), new Vector2(1400, 60));

            Net.Init();
            Screens = gameObject.AddComponent<Screens>();
        }

        void Start() => Screens.Splash();

        public void Toast(string msg)
        {
            toast.text = msg;
            toastUntil = Time.unscaledTime + 4f;
        }

        void Update()
        {
            if (toast.text.Length > 0 && Time.unscaledTime > toastUntil) toast.text = "";
        }
    }
}
