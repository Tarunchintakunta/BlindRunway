using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlindRunway
{
    [Serializable]
    public class Passenger
    {
        public Vector2 seat;
        public float panic;
        public bool strapped, demanding, spiral;
    }

    [Serializable]
    public class Crate
    {
        public Vector2 pos;
        public bool secured;
        public float progress;
    }

    // Server-side cabin: one crew member (the Cabin player) on a 4 x 16 m floor, x in [-2,2], z in [0,16].
    public class CabinSim
    {
        public const float Reach = 1.5f, Walk = 3.2f;
        public Vector2 crew = new Vector2(0, 1);
        public readonly List<Passenger> pax = new List<Passenger>();
        public readonly List<Crate> crates = new List<Crate>();
        public float act;            // 0..1 progress of the current hold-interaction
        public string hint = "";     // what E would do right now

        readonly InputMsg input = new InputMsg();
        int target = -1;
        bool wasOnGround;
        readonly bool shiftsCog;
        readonly string label;

        public CabinSim(LevelDefinition L)
        {
            for (int i = 0; i < L.passengers; i++)
                pax.Add(new Passenger
                {
                    seat = new Vector2(i % 2 == 0 ? -1.3f : 1.3f, 3 + (i / 2) * 1.8f),
                    demanding = i < L.demanding,
                    spiral = i >= L.passengers - L.spiral,
                });
            for (int i = 0; i < L.crates; i++) crates.Add(new Crate { pos = new Vector2(0, 12.5f + i * 1.2f) });
            shiftsCog = L.cargoShiftsCog;
            label = L.crateLabel;
        }

        public int CratesSecured
        {
            get
            {
                int n = 0;
                foreach (var c in crates) if (c.secured) n++;
                return n;
            }
        }

        // Loose cargo aft of the middle pushes the nose up (and forward of it, down).
        public float Cog
        {
            get
            {
                if (!shiftsCog) return 0;
                float cog = 0;
                foreach (var c in crates) if (!c.secured) cog += (c.pos.y - 8f) / 8f * 0.35f;
                return cog;
            }
        }

        public void Apply(InputMsg m)
        {
            input.moveX = Mathf.Clamp(m.moveX, -1, 1);
            input.moveZ = Mathf.Clamp(m.moveZ, -1, 1);
            input.interact = m.interact;
        }

        // Blend of average and worst so one screaming passenger matters but doesn't end the run alone.
        public float Panic
        {
            get
            {
                if (pax.Count == 0) return 0;
                float sum = 0, max = 0;
                foreach (var p in pax)
                {
                    sum += p.panic;
                    max = Mathf.Max(max, p.panic);
                }
                return Mathf.Clamp01(sum / pax.Count * 0.6f + max * 0.4f);
            }
        }

        public float Secure
        {
            get
            {
                int done = CratesSecured, total = pax.Count + crates.Count;
                foreach (var p in pax) if (p.strapped && !p.demanding) done++;
                return total == 0 ? 1 : (float)done / total;
            }
        }

        public void Step(float dt, FlightModel plane, bool glitch)
        {
            var move = Vector2.ClampMagnitude(new Vector2(input.moveX, input.moveZ), 1);
            crew += move * (Walk * dt);
            crew = new Vector2(Mathf.Clamp(crew.x, -1.6f, 1.6f), Mathf.Clamp(crew.y, 0.5f, 15.5f));
            Interact(dt);

            int loose = crates.Count - CratesSecured;
            foreach (var c in crates)
            {
                if (c.secured) continue;
                c.pos += new Vector2(-plane.roll * 0.02f, plane.pitch * 0.1f) * dt; // slides downhill
                c.pos = new Vector2(Mathf.Clamp(c.pos.x, -0.4f, 0.4f), Mathf.Clamp(c.pos.y, 1f, 15.5f));
            }

            bool jolt = plane.onGround && !wasOnGround;
            wasOnGround = plane.onGround;
            float turbulence = Mathf.Abs(plane.roll) * 0.0006f + Mathf.Abs(plane.pitch - FlightModel.GlideDeg) * 0.0008f;
            foreach (var p in pax)
            {
                float rate = 0.003f + turbulence + loose * 0.002f;
                if (p.strapped) rate *= 0.4f;
                if (p.demanding) rate += plane.AltFt < 1000 && !plane.onGround ? 0.06f : 0.012f;
                if (p.spiral && glitch) rate *= 3;
                if (p.strapped && !p.demanding) rate -= 0.006f;
                p.panic = Mathf.Clamp01(p.panic + rate * dt + (jolt && !p.strapped ? 0.15f : 0));
            }
        }

        // ---- hold-to-interact ----
        // Target ids: 0..99 passengers, 100+ cargo, 1000 fire.

        void Interact(float dt)
        {
            int near = Nearest();
            hint = Describe(near);
            int now = input.interact ? near : -1;
            if (now != target)
            {
                target = now;
                act = 0;
            }
            if (target < 0) return;
            act = Mathf.Clamp01(act + dt / Duration(target));
            if (target < 100) WorkPassenger(pax[target], dt);
            else crates[target - 100].progress = act;
            if (act >= 1) Complete(target);
        }

        int Nearest()
        {
            int best = -1;
            float bestD = Reach;
            for (int i = 0; i < crates.Count; i++)
            {
                float d = Vector2.Distance(crew, crates[i].pos);
                if (!crates[i].secured && d < bestD)
                {
                    bestD = d;
                    best = 100 + i;
                }
            }
            for (int i = 0; i < pax.Count; i++)
            {
                var p = pax[i];
                bool needs = !p.strapped || p.demanding || p.panic > 0.3f;
                float d = Vector2.Distance(crew, p.seat);
                if (needs && d < bestD && best < 100) // cargo in reach wins
                {
                    bestD = d;
                    best = i;
                }
            }
            return best;
        }

        string Describe(int id)
        {
            if (id < 0) return "";
            if (id >= 100) return "SECURE " + label;
            var p = pax[id];
            return p.demanding ? "SERVE DRINK" : !p.strapped ? "STRAP IN" : "CALM DOWN";
        }

        static float Duration(int id) => id >= 100 ? 1.5f : 1.2f;

        void WorkPassenger(Passenger p, float dt)
        {
            if (p.strapped && !p.demanding) p.panic = Mathf.Max(0, p.panic - 0.5f * dt); // calming is continuous
        }

        void Complete(int id)
        {
            if (id < 100)
            {
                var p = pax[id];
                p.strapped = true;
                p.demanding = false;
                p.panic = Mathf.Max(0, p.panic - 0.4f);
            }
            else
            {
                crates[id - 100].secured = true;
                crates[id - 100].progress = 1;
            }
            target = -1;
            act = 0;
        }
    }
}
