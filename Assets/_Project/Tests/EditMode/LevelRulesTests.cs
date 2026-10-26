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
    }
}
