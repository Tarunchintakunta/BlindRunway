using System;
using UnityEngine;

namespace BlindRunway
{
    [Serializable]
    public class ResultMsg
    {
        public int level;
        public bool win;
        public int stars;
        public string reason = "";
    }

    public static class LevelRules
    {
        // Null while the attempt is still running.
        public static ResultMsg Check(Match m)
        {
            var p = m.Plane;
            string fail;
            if (m.TimeLeft <= 0) fail = "timer";
            else if (p.stopped) fail = LandingVerdict(m);
            else return null;

            var r = new ResultMsg { win = fail == null, reason = fail ?? "landed" };
            if (r.win) r.stars = Stars(m);
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
            return null;
        }

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
