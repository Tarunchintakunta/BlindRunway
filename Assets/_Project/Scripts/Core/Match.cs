using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlindRunway
{
    [Serializable]
    public class Callout
    {
        public int seq;
        public Role from;
        public string text;
    }

    // What one client is allowed to know this tick. Fields a role must not see stay at defaults.
    [Serializable]
    public class Snapshot
    {
        public float time, timeLeft;
        public Vector3 pos;
        public float pitch, yaw, roll, throttle;
        public bool gear, onGround;
        public int flaps;
        public bool pingGear, pingFlaps;
        public Callout[] callouts = new Callout[0];
    }

    // Server-side simulation of one level attempt.
    public class Match
    {
        public readonly LevelDefinition L;
        public readonly FlightModel Plane;
        public float T;
        public readonly List<Callout> Callouts = new List<Callout>();

        readonly InputMsg pilot = new InputMsg();
        bool gearReq, flapsReq;
        float pingGearUntil = -1, pingFlapsUntil = -1;
        int seq;

        public Match(LevelDefinition level)
        {
            L = level;
            Plane = new FlightModel(new Vector3(0, L.startAltitude, -L.startDistance), L.startSpeed,
                L.crosswind, L.fuelSeconds, L.runwayLength);
        }

        public float TimeLeft => Mathf.Max(0, L.timeLimit - T);

        public void Apply(Role r, InputMsg m)
        {
            if (!string.IsNullOrEmpty(m.say)) AddCallout(r, m.say);
            switch (r)
            {
                case Role.Pilot:
                    pilot.pitch = Mathf.Clamp(m.pitch, -1, 1);
                    pilot.yaw = Mathf.Clamp(m.yaw, -1, 1);
                    pilot.throttle = Mathf.Clamp(m.throttle, -1, 1);
                    gearReq |= m.gear;
                    flapsReq |= m.flaps;
                    break;
                case Role.Navigator:
                    if (m.ping == 1) pingGearUntil = T + 3;
                    if (m.ping == 2) pingFlapsUntil = T + 3;
                    break;
            }
        }

        public void AddCallout(Role from, string text)
        {
            Callouts.Add(new Callout { seq = ++seq, from = from, text = text });
            if (Callouts.Count > 6) Callouts.RemoveAt(0);
        }

        public void Step(float dt)
        {
            T += dt;
            pilot.gear = gearReq;
            pilot.flaps = flapsReq;
            gearReq = flapsReq = false;
            Plane.Step(pilot, dt);
        }

        // Role.None = everything (solo test).
        public Snapshot SnapshotFor(Role r)
        {
            var p = Plane;
            return new Snapshot
            {
                time = T,
                timeLeft = TimeLeft,
                pos = p.pos,
                pitch = p.pitch,
                yaw = p.yaw,
                roll = p.roll,
                throttle = p.throttle,
                gear = p.gearDown,
                onGround = p.onGround,
                flaps = p.flaps,
                pingGear = T < pingGearUntil,
                pingFlaps = T < pingFlapsUntil,
                callouts = Callouts.ToArray(),
            };
        }
    }
}
