using NUnit.Framework;
using UnityEngine;

namespace BlindRunway.Tests
{
    public class MatchTests
    {
        static Match NewMatch() => new Match(ScriptableObject.CreateInstance<LevelDefinition>());

        [Test]
        public void OnlyNavigatorGetsInstruments()
        {
            var m = NewMatch();
            m.Step(0.02f);
            var nav = m.SnapshotFor(Role.Navigator);
            Assert.IsTrue(nav.nav);
            Assert.Greater(nav.altFt, 1000);
            foreach (var r in new[] { Role.Pilot, Role.Cabin })
            {
                var s = m.SnapshotFor(r);
                Assert.IsFalse(s.nav, r + " must not get instruments");
                Assert.AreEqual(0f, s.altFt, 0f);
                Assert.AreEqual(0f, s.kt, 0f);
                Assert.AreEqual("", s.warnings);
            }
        }

        [Test]
        public void PilotOwnsControlsNavigatorOnlyPings()
        {
            var m = NewMatch();
            m.Apply(Role.Navigator, new InputMsg { gear = true, ping = 1, say = "GEAR DOWN!" });
            m.Step(0.02f);
            Assert.IsFalse(m.Plane.gearDown, "navigator input must not move controls");
            Assert.IsTrue(m.SnapshotFor(Role.Pilot).pingGear);
            Assert.AreEqual("GEAR DOWN!", m.SnapshotFor(Role.Pilot).callouts[0].text);

            m.Apply(Role.Pilot, new InputMsg { gear = true });
            m.Step(0.02f);
            Assert.IsTrue(m.Plane.gearDown);
        }

        [Test]
        public void GlitchWindowLiesToNavigatorOnly()
        {
            var L = ScriptableObject.CreateInstance<LevelDefinition>();
            L.glitchStarts = new[] { 1f };
            var m = new Match(L);
            for (int k = 0; k < 75; k++) m.Step(0.02f); // t = 1.5 s, inside the window
            Assert.IsTrue(m.Glitch);
            var nav = m.SnapshotFor(Role.Navigator);
            Assert.IsTrue(nav.glitch);
            Assert.Greater(nav.altFt, m.Plane.AltFt + 600);
            Assert.IsTrue(m.SnapshotFor(Role.Cabin).flicker);
            for (int k = 0; k < 450; k++) m.Step(0.02f); // t = 10.5 s, window over
            Assert.IsFalse(m.SnapshotFor(Role.Navigator).glitch);
        }
    }
}
