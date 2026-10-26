using UnityEngine;

namespace BlindRunway
{
    // One asset per level (Resources/Levels). Defaults are Level 01.
    [CreateAssetMenu(fileName = "Level", menuName = "Blind Runway/Level Definition")]
    public class LevelDefinition : ScriptableObject
    {
        public int number = 1;
        public string title = "Clear Skies (Lying)";
        [TextArea] public string brief = "";
        public float timeLimit = 240;

        [Header("Approach (metres, m/s)")]
        public float startDistance = 9000;
        public float startAltitude = 460;
        public float startSpeed = 70;
        public float runwayLength = 2400;
        public float gateStart = 400;
        public float gateEnd = 1600;
        public float crosswind;
        public float fogDensity = 0.0004f;
        public bool rain;
        public float fuelSeconds = 600;

        [Header("Cabin")]
        public int passengers = 4;
        public int crates = 1;
        public string crateLabel = "BAG";
        public bool cargoShiftsCog;
        public int demanding;          // passengers who want service before 1000 ft
        public int spiral;             // passengers who panic 3x during instrument glitches
        public float fireAt = -1;      // seconds, -1 = no fire

        [Header("Navigator")]
        public float[] glitchStarts = new float[0];
        public float glitchSeconds = 8;

        [Header("Win")]
        public float requiredSecure;   // 0..1 share of strapped passengers + secured cargo
        public int requiredCrates = 1;
        public float maxPanic = 1f;
        public bool fuelCallRequired;  // Navigator must read the fuel out at least once
        public string[] objectives = new string[0];
        public string[] coachMarks = new string[0]; // "Pilot: ..." / "Navigator: ..." / "Cabin: ..." / "All: ..."
    }
}
