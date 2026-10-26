using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BlindRunway.PlayTests
{
    // Boots the real app, hosts over Netcode, runs SOLO TEST through intro into flight and checks every seat's view.
    // Any error logged along the way fails the test (Unity Test Framework default).
    public class SoloFlightSmoke
    {
        static IEnumerator WaitFor(System.Func<bool> cond, float seconds)
        {
            for (float t = 0; t < seconds && !cond(); t += Time.unscaledDeltaTime) yield return null;
        }

        [UnityTest]
        public IEnumerator HostSoloIntroFlightAndSeatSwap()
        {
            yield return WaitFor(() => App.I != null, 5);
            Assert.IsNotNull(App.I, "Bootstrap should create the app");
            var s = App.I.Session;

            s.Host();
            yield return WaitFor(() => s.Phase == Phase.Lobby && s.Lobby.players == 1, 5);
            Assert.AreEqual(1, s.Lobby.players);

            s.Act("solo");
            yield return WaitFor(() => s.Phase == Phase.Intro, 5);
            Assert.AreEqual(Phase.Intro, s.Phase);
            Assert.AreEqual("Clear Skies (Lying)", s.Def.title);

            s.Act("gotit");
            yield return WaitFor(() => s.Phase == Phase.Playing && s.Snap != null, 5);
            Assert.IsNotNull(s.Snap, "snapshots should arrive");
            float t0 = s.Snap.time;
            yield return new WaitForSeconds(1.5f);
            Assert.Greater(s.Snap.time, t0 + 1f, "server sim should be running");
            Assert.Less(s.Snap.pos.z, -8000f);
            Assert.IsTrue(s.Snap.nav && s.Snap.cabin, "solo view gets every seat's data");

            foreach (var role in new[] { Role.Navigator, Role.Cabin, Role.Pilot })
            {
                s.SoloView = role;
                App.I.Screens.Route();
                yield return null;
                yield return null;
                Assert.AreEqual(role, s.MyRole);
                if (role == Role.Cabin) Assert.AreEqual(4, App.I.World.Pax.Count);
            }

            s.Leave();
            yield return WaitFor(() => !Net.Running, 5);
            Assert.AreEqual(Phase.Menu, s.Phase);
        }
    }
}
