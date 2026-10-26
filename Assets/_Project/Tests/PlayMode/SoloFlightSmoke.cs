using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BlindRunway.PlayTests
{
    // Boots the real app and plays Act I alone through the Play Alone path (host-only Netcode on loopback).
    // Any error logged along the way fails the test (Unity Test Framework default).
    public class SoloFlightSmoke
    {
        static IEnumerator WaitFor(System.Func<bool> cond, float seconds)
        {
            for (float t = 0; t < seconds && !cond(); t += Time.unscaledDeltaTime) yield return null;
        }

        static IEnumerator BootAndPlayAlone(int level)
        {
            yield return WaitFor(() => App.I != null, 5);
            Assert.IsNotNull(App.I, "Bootstrap should create the app");
            var s = App.I.Session;
            s.PlayAlone(level);
            yield return WaitFor(() => s.Phase == Phase.Intro, 5);
            Assert.AreEqual(Phase.Intro, s.Phase, "Play Alone goes straight to the intro, no lobby");
            Assert.IsTrue(s.Lobby.Solo);
            s.Act("gotit");
            yield return WaitFor(() => s.Phase == Phase.Playing && s.Snap != null, 5);
            Assert.IsNotNull(s.Snap, "snapshots should arrive");
        }

        [UnityTest]
        public IEnumerator PlayAloneFlightAndSeatSwap()
        {
            yield return BootAndPlayAlone(0);
            var s = App.I.Session;
            Assert.AreEqual("Clear Skies (Lying)", s.Def.title);
            float t0 = s.Snap.time;
            yield return new WaitForSeconds(1.5f);
            Assert.Greater(s.Snap.time, t0 + 1f, "server sim should be running");
            Assert.Less(s.Snap.pos.z, -8000f);
            Assert.IsFalse(s.Snap.nav, "solo Pilot seat must not receive instruments");

            foreach (var role in new[] { Role.Navigator, Role.Cabin, Role.Pilot })
            {
                s.SwitchSeat(role);
                yield return null;
                yield return null;
                Assert.AreEqual(role, s.MyRole);
                yield return new WaitForSeconds(0.3f); // next snapshot follows the driven seat
                Assert.AreEqual(role == Role.Navigator, s.Snap.nav, role + " instruments");
                Assert.AreEqual(role == Role.Cabin, s.Snap.cabin, role + " cabin detail");
                if (role == Role.Cabin) Assert.AreEqual(4, App.I.World.Pax.Count);
            }

            s.Leave();
            yield return WaitFor(() => !Net.Running, 5);
            Assert.AreEqual(Phase.Menu, s.Phase);
        }

        // L01 through to the fail screen with one player: the timer is cut to 3 s for the test, then restored.
        [UnityTest]
        public IEnumerator PlayAloneL01FailsToBlameScreenAndBackToMenu()
        {
            var L01 = Levels.Get(0);
            float oldLimit = L01.timeLimit;
            int unlockedBefore = Progress.Unlocked;
            L01.timeLimit = 3;
            try
            {
                yield return BootAndPlayAlone(0);
                var s = App.I.Session;
                yield return WaitFor(() => s.Phase == Phase.Result, 10);
                Assert.AreEqual(Phase.Result, s.Phase);
                Assert.IsFalse(s.Result.win);
                Assert.AreEqual("timer", s.Result.reason);
                Assert.AreEqual(3, s.Result.cards.Length, "blame cards for all three seats");
                Assert.AreEqual(unlockedBefore, Progress.Unlocked, "a fail unlocks nothing");

                s.Act("retry"); // retry works with one player
                yield return WaitFor(() => s.Phase == Phase.Intro, 5);
                Assert.AreEqual(Phase.Intro, s.Phase);
                s.Leave();
                yield return WaitFor(() => !Net.Running, 5);
                Assert.AreEqual(Phase.Menu, s.Phase);
            }
            finally
            {
                L01.timeLimit = oldLimit;
            }
        }
    }
}
