using System;
using UnityEngine;

namespace BlindRunway
{
    [Serializable]
    public class BlameCard
    {
        public Role role;
        public string title, body;
    }

    [Serializable]
    public class ResultMsg
    {
        public int level;
        public bool win;
        public int stars;
        public string reason = "";
        public BlameCard[] cards = new BlameCard[0];
        public bool campaignClear;
    }

    public static class LevelRules
    {
        // Null while the attempt is still running.
        public static ResultMsg Check(Match m)
        {
            var p = m.Plane;
            string fail;
            if (p.crash != null) fail = p.crash;
            else if (p.fuel <= 0 && !p.onGround) fail = "fuel";
            else if (m.Cabin.Panic >= 1) fail = "panic";
            else if (m.TimeLeft <= 0) fail = "timer";
            else if (p.stopped) fail = LandingVerdict(m);
            else return null;

            var r = new ResultMsg { win = fail == null, reason = fail ?? "landed" };
            if (r.win) r.stars = Stars(m);
            r.cards = Blame(m, fail);
            return r;
        }

        // Stopped on the runway: did we stop where we had to, with the cabin in shape?
        static string LandingVerdict(Match m)
        {
            var L = m.L;
            if (m.Plane.pos.z < L.gateStart || m.Plane.pos.z > L.gateEnd) return "missed-gate";
            if (m.Cabin.CratesSecured < Mathf.Min(L.requiredCrates, m.Cabin.crates.Count)) return "cargo";
            if (m.Cabin.Secure < L.requiredSecure) return "cabin";
            if (m.Cabin.fireOn) return "fire";
            if (m.Cabin.Panic > L.maxPanic) return "panic";
            if (L.fuelCallRequired && !m.FuelCalled) return "no-fuel-call";
            return null;
        }

        public static string Describe(string reason)
        {
            switch (reason)
            {
                case "gear-up": return "Landed with the gear up.";
                case "missed-runway": return "Touched down off the runway.";
                case "hard-landing": return "Hit the runway way too hard.";
                case "runway-excursion": return "Rolled off the runway.";
                case "fuel": return "Ran out of fuel in the air.";
                case "panic": return "The cabin hit 100% panic.";
                case "timer": return "Ran out of time.";
                case "missed-gate": return "Stopped outside the gate box.";
                case "cargo": return "Landed with the cargo loose.";
                case "cabin": return "Too many passengers unbuckled.";
                case "fire": return "Landed with the cabin on fire.";
                case "no-fuel-call": return "Nobody called the fuel. Procedure says that's a fail.";
                default: return "Landed. Somehow.";
            }
        }

        // One card per seat: the seat that caused the failure gets the headline roast, the rest get stats.
        // Funny, never mean.
        static BlameCard[] Blame(Match m, string fail)
        {
            var cards = new[] { PilotCard(m), NavCard(m), CabinCard(m) };
            BlameCard main = null;
            switch (fail)
            {
                case "gear-up": main = Card(Role.Pilot, "BELLY LANDER", "The gear lever was right there. It even glowed."); break;
                case "missed-runway": main = Card(Role.Pilot, "SCENIC ROUTE", "Touched down on the grass. The grass did not consent."); break;
                case "hard-landing": main = Card(Role.Pilot, "SPINE COMPRESSOR", "Arrived vertically. Chiropractors are on standby."); break;
                case "runway-excursion": main = Card(Role.Pilot, "THROTTLE ROMANTIC", "Never let go of the throttle. Neither did the plane."); break;
                case "missed-gate": main = Card(Role.Pilot, "OVERSHOOT ARTIST", "Parked outside the box. Ground crew is walking over. Slowly."); break;
                case "fuel": main = Card(Role.Navigator, "FUEL? NEVER HEARD OF HER", "The gauge said empty for a while. Nobody read it out."); break;
                case "no-fuel-call": main = Card(Role.Navigator, "GAUGE WHISPERER", "Saw the fuel the whole time. Kept it a secret."); break;
                case "timer": main = Card(Role.Navigator, "SCENIC TOUR GUIDE", "Out of time. The passengers have missed three connections."); break;
                case "panic": main = Card(Role.Cabin, "MUTINY MANAGER", "Panic hit 100%. A passenger is now holding the drinks cart hostage."); break;
                case "cargo": main = Card(Role.Cabin, "CARGO LIBERATOR", "The cargo was free-range at touchdown."); break;
                case "cabin": main = Card(Role.Cabin, "SEATBELT SKEPTIC", "Too many unbuckled souls for this landing."); break;
                case "fire": main = Card(Role.Cabin, "ARSON ADJACENT", "The fire was still going. It was cozy, though."); break;
            }
            if (main != null) cards[(int)main.role] = main;
            return cards;
        }

        static BlameCard PilotCard(Match m)
        {
            if (m.GlitchTrust > 3) return Card(Role.Pilot, "TRUSTED THE FIBS", "Chased a lying altimeter for a while. Bold.");
            if (m.SayAgain > 3) return Card(Role.Pilot, "SELECTIVE HEARING", $"Asked \"say again?\" {m.SayAgain} times.");
            return Card(Role.Pilot, "STICK AND RUDDER", "Flew blind and mostly listened. Mostly.");
        }

        static BlameCard NavCard(Match m)
        {
            int n = m.Said[(int)Role.Navigator];
            if (n == 0) return Card(Role.Navigator, "SILENT TREATMENT", "Zero callouts. The Pilot flew on vibes.");
            if (n >= 25) return Card(Role.Navigator, "PODCAST HOST", $"{n} callouts. Some were even useful.");
            return Card(Role.Navigator, "STEADY VOICE", $"{n} callouts. Calm, clear, mildly smug.");
        }

        static BlameCard CabinCard(Match m)
        {
            if (m.Cabin.Secure >= 0.999f) return Card(Role.Cabin, "SEATBELT SHERIFF", "Every soul and every crate locked down.");
            if (m.PeakPanic > 0.8f) return Card(Role.Cabin, "VIBES: TERRIBLE", $"Panic peaked at {Mathf.RoundToInt(m.PeakPanic * 100)}%.");
            return Card(Role.Cabin, "AISLE WALKER", $"Cabin {Mathf.RoundToInt(m.Cabin.Secure * 100)}% secure. The rest were free spirits.");
        }

        static BlameCard Card(Role r, string title, string body) => new BlameCard { role = r, title = title, body = body };

        // 1 for landing, +1 fully secure cabin, +1 soft touchdown on the centerline.
        public static int Stars(Match m)
        {
            int stars = 1;
            if (m.Cabin.Secure >= 0.999f) stars++;
            if (Mathf.Abs(m.Plane.touchdownX) < 6 && m.Plane.touchdownVs > -3) stars++;
            return stars;
        }
    }
}
