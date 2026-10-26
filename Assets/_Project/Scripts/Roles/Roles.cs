using UnityEngine;

namespace BlindRunway
{
    public enum Role { None = -1, Pilot = 0, Navigator = 1, Cabin = 2 }

    public static class Roles
    {
        public static readonly string[] Names = { "PILOT", "NAVIGATOR", "CABIN" };

        public static readonly string[][] Can =
        {
            new[] { "Fly: pitch, yaw, throttle", "Gear and flaps", "Talk back to the Navigator" },
            new[] { "Full instrument panel", "Runway map, wind, fuel", "Callouts and pings to the Pilot" },
            new[] { "Strap and calm passengers", "Secure cargo", "Fight cabin fires" },
        };

        public static readonly string[] Taglines =
        {
            "You fly. You see nothing useful. Trust the voice.",
            "You see everything. You touch nothing. Talk.",
            "Many problems, one aisle. Keep them calm.",
        };

        public static readonly string[] ControlsLine =
        {
            "W/S pitch   A/D turn   Shift/Ctrl throttle   G gear   F flaps   1-3 talk",
            "1-0 callouts   SPACE/V/B read alt/speed/fuel   R/F ping gear/flaps",
            "WASD walk   hold E interact   1-3 talk",
        };

        public static readonly string[][] Cannot =
        {
            new[] { "See altitude, speed or heading", "See warnings or fuel" },
            new[] { "Touch the flight controls" },
            new[] { "See outside or the instruments" },
        };

        public static Color Tint(Role r)
        {
            switch (r)
            {
                case Role.Pilot: return Tokens.Amber;
                case Role.Navigator: return Tokens.Cyan;
                case Role.Cabin: return Tokens.Cabin;
                default: return Tokens.TextMuted;
            }
        }
    }
}
