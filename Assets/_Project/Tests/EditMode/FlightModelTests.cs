using NUnit.Framework;
using UnityEngine;

namespace BlindRunway.Tests
{
    public class FlightModelTests
    {
        const float Dt = 0.02f, GlideDeg = -3f;

        static FlightModel NewPlane() => new FlightModel(new Vector3(0, 460, -9000), 70, 0, 600, 2400);

        // Crude "perfect navigator": hold a 3° path to a point 300 m past the threshold, then cut power.
        static void FlyApproach(FlightModel f, float wind = 0, float maxSeconds = 400)
        {
            for (float t = 0; t < maxSeconds && !f.Done; t += Dt)
            {
                var i = new InputMsg();
                float ideal = Mathf.Max(0, 300 - f.pos.z) * Mathf.Tan(3 * Mathf.Deg2Rad);
                float wantPitch = Mathf.Clamp(GlideDeg - (f.pos.y - ideal) * 0.05f, -8, 4);
                if (f.pos.y < 8) wantPitch = -1.5f; // flare: gentle sink
                i.pitch = Mathf.Clamp((wantPitch - f.pitch) * 0.5f, -1, 1);
                i.throttle = f.onGround ? -1 : Mathf.Clamp((65 - f.speed) * 0.2f, -1, 1);
                float wantYaw = f.onGround ? 0 : Mathf.Clamp(-f.pos.x * 0.08f - wind / f.speed * Mathf.Rad2Deg, -12, 12); // crab into the wind
                i.yaw = Mathf.Clamp((wantYaw - f.yaw) * 0.3f, -1, 1);
                f.Step(i, Dt);
            }
        }

        [Test]
        public void GearUpLandingCrashes()
        {
            var f = NewPlane();
            FlyApproach(f);
            Assert.AreEqual("gear-up", f.crash);
        }

        [Test]
        public void StableApproachLandsAndStops()
        {
            var f = NewPlane();
            f.Step(new InputMsg { gear = true, flaps = true }, Dt);
            Assert.IsTrue(f.gearDown);
            FlyApproach(f);
            Assert.IsNull(f.crash);
            Assert.IsTrue(f.stopped, "plane should stop on the runway");
            Assert.Greater(f.pos.z, 0);
            Assert.Less(f.pos.z, 2400);
            Assert.Greater(f.touchdownVs, -5.5f);
        }

        [Test]
        public void IdleNoseUpStallsAndSinks()
        {
            var f = NewPlane();
            for (int k = 0; k < 1500; k++) f.Step(new InputMsg { pitch = 1, throttle = -1 }, Dt);
            Assert.Less(f.speed, f.StallSpeed);
            Assert.Less(f.vs, 0);
        }

        [Test]
        public void FullThrottleBurnsFuel()
        {
            var f = new FlightModel(new Vector3(0, 460, -9000), 70, 0, 10, 2400);
            for (int k = 0; k < 1000; k++) f.Step(new InputMsg { throttle = 1 }, Dt);
            Assert.AreEqual(0f, f.fuel, 0.0001f);
            Assert.AreEqual(0f, f.throttle, 0.0001f);
        }

        [Test]
        public void CrosswindApproachIsLandableWithCorrections()
        {
            var f = new FlightModel(new Vector3(0, 460, -10500), 70, 4, 600, 2400);
            f.Step(new InputMsg { gear = true, flaps = true }, Dt);
            FlyApproach(f, 4);
            Assert.IsNull(f.crash);
            Assert.IsTrue(f.stopped);
        }

        [Test]
        public void AftCargoPushesNoseUpAndDullsPitch()
        {
            var a = NewPlane();
            var b = NewPlane();
            b.cog = 0.6f;
            for (int k = 0; k < 100; k++)
            {
                a.Step(new InputMsg { pitch = -1 }, Dt);
                b.Step(new InputMsg { pitch = -1 }, Dt);
            }
            Assert.Greater(b.pitch, a.pitch);
        }

        [Test]
        public void CrosswindDriftsRight()
        {
            var f = new FlightModel(new Vector3(0, 460, -9000), 70, 4, 600, 2400);
            for (int k = 0; k < 500; k++) f.Step(new InputMsg(), Dt);
            Assert.Greater(f.pos.x, 30);
        }
    }
}
