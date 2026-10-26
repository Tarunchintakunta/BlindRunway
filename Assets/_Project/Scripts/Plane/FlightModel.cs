using UnityEngine;

namespace BlindRunway
{
    // Arcade flight, server-only. Comedy over realism: thrust, drag and gravity along the path,
    // a stall sink instead of a lift curve, and light autostabilize toward a gentle glide.
    // World: runway runs along +Z from z=0, centered on x=0, ground at y=0. Units are metres.
    public class FlightModel
    {
        public const float RunwayHalfWidth = 22.5f, G = 9.81f, MaxThrust = 9f, GlideDeg = -3f;

        public Vector3 pos;
        public float pitch, yaw, roll, speed, vs, throttle = 0.5f, fuel = 1f;
        public float cog;              // nose drift from loose cargo, + = nose up
        public bool gearDown, onGround, stopped;
        public int flaps;              // 0..2
        public string crash;           // null while alive
        public float touchdownVs, touchdownX;

        readonly float crosswind, fuelSeconds, runwayLength;

        public FlightModel(Vector3 start, float startSpeed, float crosswind, float fuelSeconds, float runwayLength)
        {
            pos = start;
            speed = startSpeed;
            this.crosswind = crosswind;
            this.fuelSeconds = fuelSeconds;
            this.runwayLength = runwayLength;
        }

        public float AltFt => pos.y * 3.28084f;
        public float Knots => speed * 1.94384f;
        public float StallSpeed => 50f - flaps * 6f;
        public bool Done => stopped || crash != null;
        public Vector3 Forward => Quaternion.Euler(0, yaw, 0) * Vector3.forward;

        public void Step(InputMsg i, float dt)
        {
            if (Done) return;
            if (i.gear && !onGround) gearDown = !gearDown;
            if (i.flaps) flaps = (flaps + 1) % 3;
            throttle = Mathf.Clamp01(throttle + i.throttle * 0.4f * dt);
            if (fuel <= 0) throttle = 0;
            fuel = Mathf.Max(0, fuel - throttle * dt / fuelSeconds);
            float drag = (0.0008f + flaps * 0.0004f + (gearDown ? 0.0003f : 0)) * speed * speed;
            if (onGround) Rollout(i, drag, dt);
            else Fly(i, drag, dt);
        }

        void Fly(InputMsg i, float drag, float dt)
        {
            float authority = 1 - Mathf.Min(0.5f, Mathf.Abs(cog)); // loose cargo makes pitch sluggish
            if (Mathf.Abs(i.pitch) > 0.05f) pitch += i.pitch * 14f * authority * dt;
            else pitch = Mathf.MoveTowards(pitch, GlideDeg, 3f * dt);
            pitch = Mathf.Clamp(pitch + cog * 5f * dt, -20f, 20f);
            yaw += i.yaw * 12f * dt;
            roll = Mathf.Lerp(roll, -i.yaw * 25f, 1 - Mathf.Exp(-3f * dt));

            float rad = pitch * Mathf.Deg2Rad;
            speed = Mathf.Max(25f, speed + (throttle * MaxThrust - drag - G * Mathf.Sin(rad)) * dt);
            vs = speed * Mathf.Sin(rad) - Mathf.Max(0, StallSpeed - speed) * 0.8f;
            pos += Forward * (speed * Mathf.Cos(rad) * dt) + new Vector3(crosswind, vs, 0) * dt;
            if (pos.y <= 0) Touchdown();
        }

        void Touchdown()
        {
            onGround = true;
            touchdownVs = vs;
            touchdownX = pos.x;
            pos.y = 0;
            if (!gearDown) crash = "gear-up";
            else if (Mathf.Abs(pos.x) > RunwayHalfWidth || pos.z < 0 || pos.z > runwayLength) crash = "missed-runway";
            else if (vs < -5.5f || pitch < -8f) crash = "hard-landing";
            pitch = roll = vs = 0;
        }

        // Brakes only bite with the throttle near idle, so someone has to say "CUT THROTTLE".
        void Rollout(InputMsg i, float drag, float dt)
        {
            yaw += i.yaw * 6f * dt;
            float brake = throttle < 0.1f ? 6f : 1f;
            speed = Mathf.Max(0, speed + (throttle * MaxThrust - drag - brake) * dt);
            pos += Forward * (speed * dt) + new Vector3(crosswind * 0.1f * dt, 0, 0);
            if (Mathf.Abs(pos.x) > RunwayHalfWidth || pos.z > runwayLength) crash = "runway-excursion";
            else if (speed < 0.5f) stopped = true;
        }
    }
}
