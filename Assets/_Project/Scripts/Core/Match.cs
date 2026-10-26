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

        // Navigator only
        public bool nav;
        public float altFt, kt, vsFpm, hdg, fuel, distM, wind, cog;
        public float loc, gs;          // -1..1, + = right of centerline / above the 3 deg path
        public bool glitch;
        public string warnings = "";   // '|' separated

        // Everyone: cabin summary. Cabin only: the cabin itself.
        public float panic, secure;
        public bool cabin;
        public Vector2 crew;
        public Passenger[] pax = new Passenger[0];
        public Crate[] crates = new Crate[0];
        public float act;
        public string hint = "";
        public bool fireOn;
        public float fire;
        public Vector2 firePos;
    }

    // Server-side simulation of one level attempt.
    public class Match
    {
        public readonly LevelDefinition L;
        public readonly FlightModel Plane;
        public readonly CabinSim Cabin;
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
            Cabin = new CabinSim(L);
        }

        public bool Glitch
        {
            get
            {
                foreach (var g in L.glitchStarts)
                    if (T >= g && T < g + L.glitchSeconds) return true;
                return false;
            }
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
                case Role.Cabin:
                    Cabin.Apply(m);
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
            Plane.cog = Cabin.Cog;
            Plane.Step(pilot, dt);
            Cabin.Step(dt, Plane, Glitch, T);
        }

        // Ideal height on a 3 deg path to a touchdown point 300 m past the threshold.
        public static float PathHeight(float z) => Mathf.Max(0, 300 - z) * Mathf.Tan(3 * Mathf.Deg2Rad);

        public string Warnings()
        {
            var p = Plane;
            var w = new List<string>();
            if (!p.onGround && p.speed < p.StallSpeed + 5) w.Add("STALL");
            if (!p.onGround && !p.gearDown && p.AltFt < 1000) w.Add("GEAR");
            if (!p.onGround && p.vs < -6 && p.AltFt < 300) w.Add("SINK RATE");
            if (!p.onGround && p.AltFt < 100 && p.vs < -5) w.Add("PULL UP");
            if (p.fuel < 0.15f) w.Add("FUEL LOW");
            if (p.onGround && p.throttle > 0.1f && !p.Done) w.Add("THROTTLE IDLE");
            return string.Join("|", w);
        }

        // Role.None = everything (solo test).
        public Snapshot SnapshotFor(Role r)
        {
            var s = Base();
            if (r == Role.Navigator || r == Role.None) FillInstruments(s);
            if (r == Role.Cabin || r == Role.None) FillCabin(s);
            return s;
        }

        void FillCabin(Snapshot s)
        {
            s.cabin = true;
            s.crew = Cabin.crew;
            s.pax = Cabin.pax.ToArray();
            s.crates = Cabin.crates.ToArray();
            s.fireOn = Cabin.fireOn;
            s.fire = Cabin.fire;
            s.firePos = Cabin.firePos;
            s.act = Cabin.act;
            s.hint = Cabin.hint;
        }

        void FillInstruments(Snapshot s)
        {
            var p = Plane;
            s.nav = true;
            s.altFt = p.AltFt;
            s.kt = p.Knots;
            s.vsFpm = p.vs * 196.85f;
            s.hdg = Mathf.Repeat(p.yaw, 360);
            s.fuel = p.fuel;
            s.distM = -p.pos.z;
            s.wind = L.crosswind;
            s.cog = Plane.cog;
            s.loc = Mathf.Clamp(p.pos.x / 150f, -1, 1);
            s.gs = Mathf.Clamp((p.pos.y - PathHeight(p.pos.z)) / 40f, -1, 1);
            s.warnings = Warnings();
        }

        Snapshot Base()
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
                panic = Cabin.Panic,
                secure = Cabin.Secure,
            };
        }
    }
}
