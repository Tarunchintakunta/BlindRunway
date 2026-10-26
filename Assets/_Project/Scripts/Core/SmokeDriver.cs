using System;
using System.Collections;
using UnityEngine;

namespace BlindRunway
{
    // Headless multi-process smoke test. Launch three players:
    //   BlindRunway -batchmode -nographics -smoke 0   (host, takes Pilot)
    //   BlindRunway -batchmode -nographics -smoke 1   (joins 127.0.0.1, takes Navigator)
    //   BlindRunway -batchmode -nographics -smoke 2   (joins, takes Cabin)
    // Each drives lobby -> ready -> intro -> flight, checks what its seat may see, logs "[SMOKE] PASS|FAIL" and quits (exit 0/1).
    public class SmokeDriver : MonoBehaviour
    {
        int seat = -1;

        public static void TryStart(GameObject host)
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-smoke");
            if (i < 0 || i + 1 >= args.Length || !int.TryParse(args[i + 1], out int seat) || seat < 0 || seat > 2) return;
            host.AddComponent<SmokeDriver>().seat = seat;
        }

        IEnumerator Start()
        {
            var s = App.I.Session;
            Log($"seat {seat} starting");
            if (seat == 0) s.Host();
            else
            {
                yield return new WaitForSecondsRealtime(2); // let the host bind first
                s.Join("");
            }

            yield return Until(() => s.Phase == Phase.Lobby && s.Lobby.players > 0, 20, "reach lobby");
            yield return new WaitForSecondsRealtime(0.5f);
            s.Act("claim", seat);
            yield return Until(() => s.MyRole == (Role)seat, 10, "claim seat");
            s.Act("ready");
            yield return Until(() => s.Lobby.ready[seat] || s.Phase != Phase.Lobby, 10, "ready"); // host may start instantly and clear ready flags

            if (seat == 0)
            {
                yield return Until(() => Session.CanStart(s.Lobby), 40, "all three ready");
                s.Act("start");
            }
            yield return Until(() => s.Phase == Phase.Intro, 20, "intro");
            s.Act("gotit");
            yield return Until(() => s.Phase == Phase.Playing && s.Snap != null, 20, "flight snapshots");
            yield return new WaitForSecondsRealtime(4);

            var snap = s.Snap;
            var role = (Role)seat;
            Check(snap.time > 3, "server sim advancing");
            Check(snap.nav == (role == Role.Navigator), "instruments only for the Navigator");
            Check(snap.cabin == (role == Role.Cabin), "cabin detail only for the Cabin");
            Check(role != Role.Pilot || snap.altFt == 0, "Pilot snapshot has no altitude");
            Check(s.Lobby.players == 3, "three players connected");
            Log($"PASS seat {seat} ({role}) t={snap.time:0.0}s alt={(snap.nav ? snap.altFt.ToString("0") : "hidden")}");
            yield return new WaitForSecondsRealtime(seat == 0 ? 4 : 1); // host leaves last
            Application.Quit(0);
        }

        IEnumerator Until(Func<bool> cond, float seconds, string what)
        {
            for (float t = 0; t < seconds; t += Time.unscaledDeltaTime)
            {
                if (cond()) yield break;
                yield return null;
            }
            Fail("timed out waiting to " + what);
        }

        void Check(bool ok, string what)
        {
            if (!ok) Fail(what);
        }

        void Fail(string why)
        {
            Log($"FAIL seat {seat}: {why}");
            Application.Quit(1);
            enabled = false;
            StopAllCoroutines();
        }

        static void Log(string msg) => Debug.Log("[SMOKE] " + msg);
    }
}
