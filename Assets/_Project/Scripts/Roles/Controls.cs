using System;
using UnityEngine;

namespace BlindRunway
{
    // One message type for every role; the server only reads the fields the sender's seat owns.
    [Serializable]
    public class InputMsg
    {
        public int role;              // only trusted in solo test mode
        public float pitch, yaw, throttle;
        public bool gear, flaps;      // pilot one-shot toggles
        public float moveX, moveZ;
        public bool interact;         // cabin hold-to-interact
        public string say = "";       // callout text, any role
        public int ping;              // navigator: 1 gear switch, 2 flaps switch
    }

    // Keyboard → InputMsg. Poll every frame so one-shot presses survive until the 20 Hz send.
    // ponytail: legacy Input keeps setup zero-config; move to the Input System package for remapping.
    public static class Controls
    {
        static InputMsg pending = new InputMsg();

        public static readonly string[] NavCallouts =
            { "PULL UP", "NOSE DOWN", "LEFT", "RIGHT", "MORE POWER", "LESS POWER", "GEAR DOWN!", "FLAPS!", "IGNORE ALT", "CUT THROTTLE" };
        public static readonly string[] PilotCallouts = { "SAY AGAIN?", "HOW HIGH ARE WE?", "COPY!" };
        public static readonly string[] CabinCallouts = { "CABIN SECURE!", "FIRE IN THE CABIN!", "STOP WOBBLING!" };

        static readonly KeyCode[] Digits =
        {
            KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5,
            KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0,
        };

        public static void Say(string text) => pending.say = text;

        public static void Poll(Role r)
        {
            var p = pending;
            switch (r)
            {
                case Role.Pilot:
                    p.pitch = Axis(KeyCode.S, KeyCode.DownArrow, KeyCode.W, KeyCode.UpArrow); // pull back = nose up
                    p.yaw = Axis(KeyCode.D, KeyCode.RightArrow, KeyCode.A, KeyCode.LeftArrow);
                    p.throttle = Axis(KeyCode.LeftShift, KeyCode.RightShift, KeyCode.LeftControl, KeyCode.RightControl);
                    if (Input.GetKeyDown(KeyCode.G)) p.gear = true;
                    if (Input.GetKeyDown(KeyCode.F)) p.flaps = true;
                    SayKeys(PilotCallouts);
                    break;
                case Role.Navigator:
                    if (Input.GetKeyDown(KeyCode.R)) p.ping = 1;
                    if (Input.GetKeyDown(KeyCode.F)) p.ping = 2;
                    SayKeys(NavCallouts);
                    break;
                case Role.Cabin:
                    p.moveX = Axis(KeyCode.D, KeyCode.RightArrow, KeyCode.A, KeyCode.LeftArrow);
                    p.moveZ = Axis(KeyCode.W, KeyCode.UpArrow, KeyCode.S, KeyCode.DownArrow);
                    p.interact = Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.Space);
                    SayKeys(CabinCallouts);
                    break;
            }
        }

        public static InputMsg Take(Role r)
        {
            var m = pending;
            m.role = (int)r;
            pending = new InputMsg();
            return m;
        }

        static void SayKeys(string[] lines)
        {
            for (int i = 0; i < lines.Length; i++)
                if (Input.GetKeyDown(Digits[i])) pending.say = lines[i];
        }

        static float Axis(KeyCode pos, KeyCode pos2, KeyCode neg, KeyCode neg2) =>
            (Input.GetKey(pos) || Input.GetKey(pos2) ? 1f : 0f) - (Input.GetKey(neg) || Input.GetKey(neg2) ? 1f : 0f);
    }
}
