using NUnit.Framework;
using UnityEngine;

namespace BlindRunway.Tests
{
    public class CabinSimTests
    {
        const float Dt = 0.02f;

        static LevelDefinition Level(int passengers = 2, int demanding = 0)
        {
            var L = ScriptableObject.CreateInstance<LevelDefinition>();
            L.passengers = passengers;
            L.demanding = demanding;
            L.crates = 0;
            return L;
        }

        static FlightModel Cruise() => new FlightModel(new Vector3(0, 460, -9000), 70, 0, 600, 2400);

        // Walk the crew to a point, then hold E for a while.
        static void WalkAndHold(CabinSim c, FlightModel f, Vector2 to, float holdSeconds)
        {
            for (int k = 0; k < 600 && Vector2.Distance(c.crew, to) > 0.6f; k++)
            {
                var d = (to - c.crew).normalized;
                c.Apply(new InputMsg { moveX = d.x, moveZ = d.y });
                c.Step(Dt, f, false, 0);
            }
            c.Apply(new InputMsg { interact = true });
            for (float t = 0; t < holdSeconds; t += Dt) c.Step(Dt, f, false, 0);
            c.Apply(new InputMsg());
        }

        [Test]
        public void HoldingEStrapsNearestPassenger()
        {
            var c = new CabinSim(Level());
            var f = Cruise();
            Assert.AreEqual(0f, c.Secure, 0.001f);
            WalkAndHold(c, f, c.pax[0].seat, 1.5f);
            Assert.IsTrue(c.pax[0].strapped);
            Assert.AreEqual(0.5f, c.Secure, 0.001f);
        }

        [Test]
        public void IgnoredDemandingPassengerPanicsBelow1000Ft()
        {
            var c = new CabinSim(Level(2, 1));
            var low = new FlightModel(new Vector3(0, 200, -3000), 70, 0, 600, 2400);
            for (float t = 0; t < 20; t += Dt) c.Step(Dt, low, false, 0);
            Assert.Greater(c.pax[0].panic, 0.9f);
            Assert.Less(c.pax[1].panic, 0.3f);
        }

        [Test]
        public void ServingClearsDemand()
        {
            var c = new CabinSim(Level(2, 1));
            WalkAndHold(c, Cruise(), c.pax[0].seat, 1.5f);
            Assert.IsFalse(c.pax[0].demanding);
            Assert.IsTrue(c.pax[0].strapped);
        }

        [Test]
        public void LooseCargoSlidesAftAndShiftsCog()
        {
            var L = Level(0);
            L.crates = 2;
            L.cargoShiftsCog = true;
            var c = new CabinSim(L);
            var noseUp = Cruise();
            noseUp.pitch = 10;
            float before = c.Cog;
            for (float t = 0; t < 5; t += Dt) c.Step(Dt, noseUp, false, 0);
            Assert.Greater(c.Cog, before);
            WalkAndHold(c, noseUp, c.crates[0].pos, 2f);
            Assert.AreEqual(1, c.CratesSecured);
            Assert.AreEqual(0.5f, c.Secure, 0.001f);
        }

        [Test]
        public void FireStartsOnTimeAndCanBeExtinguished()
        {
            var L = Level(2);
            L.fireAt = 1;
            var c = new CabinSim(L);
            var f = Cruise();
            for (float t = 0; t < 3; t += Dt) c.Step(Dt, f, false, t);
            Assert.IsTrue(c.fireOn);
            c.Apply(new InputMsg { moveX = -1, moveZ = 1 });
            float time = 3;
            for (int k = 0; k < 600 && Vector2.Distance(c.crew, c.firePos) > 1f; k++, time += Dt) c.Step(Dt, f, false, time);
            c.Apply(new InputMsg { interact = true });
            for (float t = 0; t < 3; t += Dt, time += Dt) c.Step(Dt, f, false, time);
            Assert.IsTrue(c.fireOut);
            Assert.IsFalse(c.fireOn);
        }

        [Test]
        public void ThirstyPassengerBlocksAisleUntilServed()
        {
            var c = new CabinSim(Level(4, 1));
            var f = Cruise();
            c.Apply(new InputMsg { moveZ = 1 });
            for (float t = 0; t < 5; t += Dt) c.Step(Dt, f, false, 0);
            Assert.Less(c.crew.y, c.pax[0].seat.y);
            c.Apply(new InputMsg { interact = true });
            for (float t = 0; t < 1.5f; t += Dt) c.Step(Dt, f, false, 0);
            Assert.IsFalse(c.pax[0].demanding);
            c.Apply(new InputMsg { moveZ = 1 });
            for (float t = 0; t < 5; t += Dt) c.Step(Dt, f, false, 0);
            Assert.Greater(c.crew.y, 10);
        }
    }
}
