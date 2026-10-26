using NUnit.Framework;
using UnityEngine;

namespace BlindRunway.Tests
{
    public class LevelRulesTests
    {
        static Match NewMatch(int passengers = 0, int crates = 0)
        {
            var L = ScriptableObject.CreateInstance<LevelDefinition>();
            L.passengers = passengers;
            L.crates = crates;
            L.requiredCrates = crates;
            return new Match(L);
        }

        static void StopAt(Match m, float z)
        {
            m.Plane.pos = new Vector3(0, 0, z);
            m.Plane.onGround = m.Plane.stopped = true;
        }

        [Test]
        public void RunningAttemptHasNoResult() => Assert.IsNull(LevelRules.Check(NewMatch()));

        [Test]
        public void StopInsideGateWins()
        {
            var m = NewMatch();
            StopAt(m, 800);
            var r = LevelRules.Check(m);
            Assert.IsTrue(r.win);
            Assert.Greater(r.stars, 0);
        }

        [Test]
        public void StopPastGateFails()
        {
            var m = NewMatch();
            StopAt(m, 2000);
            var r = LevelRules.Check(m);
            Assert.IsFalse(r.win);
            Assert.AreEqual("missed-gate", r.reason);
        }

        [Test]
        public void LooseCargoFailsLanding()
        {
            var m = NewMatch(0, 1);
            StopAt(m, 800);
            Assert.AreEqual("cargo", LevelRules.Check(m).reason);
        }

        [Test]
        public void TimerRunsOut()
        {
            var m = NewMatch();
            m.T = m.L.timeLimit + 1;
            Assert.AreEqual("timer", LevelRules.Check(m).reason);
        }

        [Test]
        public void CrashFuelAndPanicFail()
        {
            var m = NewMatch();
            m.Plane.crash = "gear-up";
            Assert.AreEqual("gear-up", LevelRules.Check(m).reason);

            m = NewMatch();
            m.Plane.fuel = 0;
            Assert.AreEqual("fuel", LevelRules.Check(m).reason);

            m = NewMatch(2);
            foreach (var p in m.Cabin.pax) p.panic = 1;
            Assert.AreEqual("panic", LevelRules.Check(m).reason);
        }

        [Test]
        public void BlameCardsCoverEverySeatAndRoastTheCulprit()
        {
            var m = NewMatch();
            m.Plane.crash = "gear-up";
            var r = LevelRules.Check(m);
            Assert.AreEqual(3, r.cards.Length);
            Assert.AreEqual(Role.Pilot, r.cards[0].role);
            Assert.AreEqual("BELLY LANDER", r.cards[0].title);
            Assert.AreEqual("SILENT TREATMENT", r.cards[1].title);
        }
    }
}
