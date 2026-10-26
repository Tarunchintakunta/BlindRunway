using System;
using UnityEngine;

namespace BlindRunway
{
    // Stub SFX bus: engine, wind and alarm loops plus one-shots, all synthesized at startup.
    // ponytail: procedural placeholders; swap clips for recorded audio and route through an AudioMixer later.
    public class AudioBus : MonoBehaviour
    {
        const int Rate = 44100;
        AudioSource engine, wind, alarm, fx;
        AudioClip ding, thud;
        int lastSeq;
        bool wasOnGround;
        Vector3 lastPos;
        float speedEst;

        void Awake()
        {
            var rng = new System.Random(5);
            float smooth = 0;
            engine = Loop("Engine", Make(1f, t => 0.5f * Saw(t * 90) + 0.25f * Saw(t * 180) + 0.15f * Noise(rng)));
            wind = Loop("Wind", Make(2f, t => smooth = Mathf.Lerp(smooth, Noise(rng), 0.04f)));
            alarm = Loop("Alarm", Make(0.5f, t => t < 0.18f ? 0.6f * Mathf.Sign(Mathf.Sin(t * 880 * Mathf.PI * 2)) : 0));
            ding = Make(0.35f, t => Mathf.Sin(t * 1320 * Mathf.PI * 2) * Mathf.Exp(-t * 12) * 0.5f);
            thud = Make(0.5f, t => (Mathf.Sin(t * 55 * Mathf.PI * 2) + Noise(rng) * 0.6f) * Mathf.Exp(-t * 8));
            fx = gameObject.AddComponent<AudioSource>();
            fx.playOnAwake = false;
        }

        void Update()
        {
            var s = App.I.Session;
            var snap = s.Snap;
            bool flying = s.Phase == Phase.Playing && snap != null;
            if (!flying)
            {
                Set(engine, 0, 1);
                Set(wind, 0, 1);
                Set(alarm, 0, 1);
                return;
            }

            var role = s.MyRole;
            if (Time.deltaTime > 0) speedEst = Mathf.Lerp(speedEst, (snap.pos - lastPos).magnitude / Time.deltaTime, 0.05f);
            lastPos = snap.pos;
            float engineVol = role == Role.Pilot ? 0.45f : role == Role.Cabin ? 0.3f : 0.12f;
            Set(engine, engineVol * (0.4f + snap.throttle * 0.6f), 0.6f + snap.throttle * 0.8f);
            Set(wind, role == Role.Pilot ? Mathf.Clamp01(speedEst / 90f) * 0.35f : 0, 1);
            Set(alarm, role == Role.Navigator && snap.warnings.Length > 0 ? 0.25f : 0, 1);

            foreach (var c in snap.callouts)
            {
                if (c.seq <= lastSeq) continue;
                lastSeq = c.seq;
                if (c.from != role) fx.PlayOneShot(ding, 0.6f);
            }
            if (snap.onGround && !wasOnGround && role != Role.Navigator) fx.PlayOneShot(thud, 0.9f);
            wasOnGround = snap.onGround;
        }

        static void Set(AudioSource a, float volume, float pitch)
        {
            a.volume = volume;
            a.pitch = pitch;
            if (volume > 0 && !a.isPlaying) a.Play();
            if (volume <= 0 && a.isPlaying) a.Stop();
        }

        AudioSource Loop(string name, AudioClip clip)
        {
            var a = gameObject.AddComponent<AudioSource>();
            a.clip = clip;
            a.loop = true;
            a.playOnAwake = false;
            a.volume = 0;
            return a;
        }

        static AudioClip Make(float seconds, Func<float, float> wave)
        {
            int n = (int)(Rate * seconds);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(wave(i / (float)Rate), -1, 1);
            var clip = AudioClip.Create("sfx", n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float Saw(float cycles) => (cycles - Mathf.Floor(cycles)) * 2 - 1;
        static float Noise(System.Random r) => (float)r.NextDouble() * 2 - 1;
    }
}
