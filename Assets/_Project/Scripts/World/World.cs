using System.Collections.Generic;
using UnityEngine;

namespace BlindRunway
{
    // Builds the local 3D view for this client's seat from primitives and drives it from snapshots.
    // ponytail: primitives + flat colors stand in for the low-poly art pass.
    public class World : MonoBehaviour
    {
        Transform root;
        Transform plane;
        Transform cabin, crew;
        public readonly List<Transform> Pax = new List<Transform>();
        readonly List<Renderer> paxBody = new List<Renderer>();
        public readonly List<Transform> Crates = new List<Transform>();
        readonly List<Renderer> crateStrap = new List<Renderer>();
        Transform inner, fire;
        Light fireLight, lamp;
        Vector3 shownCrew;
        Role role = Role.None;
        LevelDefinition level;
        Vector3 shownPos;
        float shake;
        readonly Dictionary<Color, Material> mats = new Dictionary<Color, Material>();
        Material baseMat;

        public void Show(Role r, LevelDefinition L)
        {
            if (root != null && r == role && L == level) return;
            Clear();
            role = r;
            level = L;
            root = new GameObject("World").transform;
            if (r == Role.Pilot) BuildPilot();
            else if (r == Role.Cabin) BuildCabin();
            else
            {
                RenderSettings.fog = false;
                ResetCamera();
            }
        }

        public void Clear()
        {
            if (root) Destroy(root.gameObject);
            root = plane = cabin = crew = fire = null;
            Pax.Clear();
            Crates.Clear();
            crateStrap.Clear();
            paxBody.Clear();
            role = Role.None;
            RenderSettings.fog = false;
            ResetCamera();
        }

        static void ResetCamera()
        {
            var cam = App.I.Cam;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Tokens.BgVoid;
            cam.transform.position = Vector3.zero;
            cam.transform.rotation = Quaternion.identity;
        }

        public void Shake(float amount)
        {
            if (Settings.Shake) shake = Mathf.Max(shake, amount);
        }

        // ---- pilot: night approach ----

        void BuildPilot()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = Tokens.BgVoid;
            RenderSettings.fogDensity = level.fogDensity;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.12f, 0.15f, 0.25f);
            var moon = new GameObject("Moon").AddComponent<Light>();
            moon.type = LightType.Directional;
            moon.color = new Color(0.6f, 0.7f, 1f);
            moon.intensity = 0.35f;
            moon.transform.SetParent(root);
            moon.transform.rotation = Quaternion.Euler(35, -30, 0);

            float len = level.runwayLength;
            Box(new Vector3(0, -0.5f, 0), new Vector3(30000, 1, 40000), Tokens.Hex(0x0F1A1C));
            Box(new Vector3(0, 0.02f, len / 2), new Vector3(FlightModel.RunwayHalfWidth * 2, 0.1f, len), Tokens.Hex(0x22262E));
            var edge = Tokens.Hex(0xFFF3D6);
            for (float z = 0; z <= len; z += 60)
            {
                if (z % 120 == 0) Box(new Vector3(0, 0.08f, z + 15), new Vector3(1, 0.05f, 30), Tokens.Hex(0xC9CED8));
                Box(new Vector3(-FlightModel.RunwayHalfWidth - 1, 0.5f, z), Vector3.one, edge, 3);
                Box(new Vector3(FlightModel.RunwayHalfWidth + 1, 0.5f, z), Vector3.one, edge, 3);
            }
            for (float x = -FlightModel.RunwayHalfWidth; x <= FlightModel.RunwayHalfWidth; x += 5)
            {
                Box(new Vector3(x, 0.5f, -2), Vector3.one, Tokens.Success, 3);
                Box(new Vector3(x, 0.5f, len + 2), Vector3.one, Tokens.Danger, 3);
            }
            foreach (var gz in new[] { level.gateStart, level.gateEnd }) // stop-box bars, green like the checklist
                for (int side = -1; side <= 1; side += 2)
                    Box(new Vector3(side * (FlightModel.RunwayHalfWidth + 4), 1.5f, gz), new Vector3(6, 3, 2), Tokens.Success, 4);
            for (float z = level.gateStart; z <= level.gateEnd; z += 40)
                Box(new Vector3(0, 0.12f, z), new Vector3(3, 0.05f, 3), Tokens.Success, 2);
            for (float z = -900; z < 0; z += 60) Box(new Vector3(0, 1, z), new Vector3(2, 2, 2), Tokens.TextPrimary, 4);
            Box(new Vector3(0, 1, -300), new Vector3(30, 1, 1), Tokens.TextPrimary, 4);

            BuildAirport(len);

            var rng = new System.Random(level.number);
            for (int i = 0; i < 260; i++)
            {
                float x = (float)(rng.NextDouble() * 2 - 1) * 9000, z = (float)rng.NextDouble() * 22000 - 12000;
                if (Mathf.Abs(x) < 400) continue; // keep the approach corridor dark
                Box(new Vector3(x, 2, z), new Vector3(6, 4, 6), rng.NextDouble() < 0.7 ? Tokens.Cabin : Tokens.Amber, 2);
            }

            plane = new GameObject("Plane").transform;
            plane.SetParent(root);
            var hull = Tokens.Hex(0xE8ECF4);
            Part(plane, Vector3.zero, new Vector3(3, 3, 24), hull);
            Part(plane, new Vector3(0, -0.3f, 1), new Vector3(28, 0.5f, 4), hull);
            Part(plane, new Vector3(0, 2.5f, -10.5f), new Vector3(0.4f, 4, 3), Tokens.Amber);
            Part(plane, new Vector3(0, 0.6f, -10.5f), new Vector3(9, 0.3f, 2.5f), hull);
            Part(plane, new Vector3(0, 0.4f, 9), new Vector3(2.6f, 1.2f, 3), Tokens.Cyan, 1.5f); // cockpit glow
            shownPos = new Vector3(0, level.startAltitude, -level.startDistance);
            App.I.Cam.transform.position = shownPos + new Vector3(0, 5, -26); // start behind the plane, no swoop
            App.I.Cam.transform.rotation = Quaternion.identity;
        }

        // Taxiway, apron, terminal, tower, hangars, windsock and a ring of hills: readable silhouettes at night.
        void BuildAirport(float len)
        {
            var concrete = Tokens.Hex(0x1C2026);
            Box(new Vector3(90, 0.02f, len * 0.5f), new Vector3(20, 0.1f, len), concrete);                  // parallel taxiway
            for (float z = len * 0.25f; z < len; z += len * 0.25f)
                Box(new Vector3(45, 0.02f, z), new Vector3(70, 0.1f, 18), concrete);                         // connectors
            Box(new Vector3(190, 0.02f, len * 0.5f), new Vector3(180, 0.1f, 600), concrete);                // apron
            for (float z = 0; z <= len; z += 60) Box(new Vector3(79, 0.4f, z), new Vector3(0.6f, 0.6f, 0.6f), Tokens.Cyan, 3); // taxi edge lights

            var building = Tokens.Hex(0x2A3142);
            Box(new Vector3(300, 12, len * 0.5f), new Vector3(40, 24, 420), building);                       // terminal
            for (int i = 0; i < 12; i++)
                Box(new Vector3(279, 14, len * 0.5f - 190 + i * 35), new Vector3(1, 6, 18), Tokens.Cabin, 2); // terminal windows
            Box(new Vector3(260, 25, len * 0.5f + 300), new Vector3(8, 50, 8), building);                    // tower
            Box(new Vector3(260, 54, len * 0.5f + 300), new Vector3(16, 8, 16), Tokens.Cyan, 1.5f);          // tower cab
            Box(new Vector3(260, 60, len * 0.5f + 300), new Vector3(1.5f, 6, 1.5f), Tokens.Danger, 4);        // beacon
            for (int i = 0; i < 3; i++)
                Box(new Vector3(230, 10, len * 0.5f - 420 - i * 90), new Vector3(70, 20, 70), Tokens.Hex(0x232A38)); // hangars

            // windsock near the threshold: points downwind, longer in stronger wind
            float sock = Mathf.Clamp(Mathf.Abs(level.crosswind) * 1.5f, 1, 8);
            Box(new Vector3(-60, 5, 150), new Vector3(0.4f, 10, 0.4f), Tokens.TextMuted);
            Box(new Vector3(-60 + Mathf.Sign(level.crosswind == 0 ? 1 : level.crosswind) * sock * 0.5f, 9.5f, 150), new Vector3(sock, 1.2f, 1.2f), Tokens.Amber, 2);

            var hill = Tokens.Hex(0x0C1416);
            var rng = new System.Random(99);
            for (int i = 0; i < 24; i++)
            {
                float a = i / 24f * Mathf.PI * 2, d = 9000 + (float)rng.NextDouble() * 3000, h = 200 + (float)rng.NextDouble() * 500;
                if (Mathf.Sin(a) < 0 && Mathf.Abs(Mathf.Cos(a) * d) < 3000) continue; // approach path stays clear
                Box(new Vector3(Mathf.Cos(a) * d, h / 2, len * 0.5f + Mathf.Sin(a) * d), new Vector3(2500, h, 2500), hill);
            }
        }

        void Update()
        {
            var snap = App.I.Session.Snap;
            if (snap == null || root == null) return;
            if (role == Role.Pilot) UpdatePilot(snap, Time.deltaTime);
            if (role == Role.Cabin) UpdateCabin(snap, Time.deltaTime);
        }

        // ---- cabin: warm tube of chaos, tilts with the plane ----

        void BuildCabin()
        {
            RenderSettings.fog = false;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.35f, 0.3f, 0.25f);
            cabin = new GameObject("Cabin").transform;
            cabin.SetParent(root);
            inner = new GameObject("Inner").transform; // pivot at cabin middle
            inner.SetParent(cabin, false);
            inner.localPosition = new Vector3(0, 0, -8);

            Part(inner, new Vector3(0, -0.1f, 8), new Vector3(4.4f, 0.2f, 16.4f), Tokens.Hex(0x3A2E2A));
            Part(inner, new Vector3(0, 0.01f, 8), new Vector3(0.9f, 0.02f, 16), Tokens.Hex(0x7A2E3A));          // aisle carpet
            Part(inner, new Vector3(-2.3f, 1.2f, 8), new Vector3(0.2f, 2.4f, 16.4f), Tokens.Hex(0xD9D2C3));
            Part(inner, new Vector3(2.3f, 1.2f, 8), new Vector3(0.2f, 2.4f, 16.4f), Tokens.Hex(0xD9D2C3));
            Part(inner, new Vector3(0, 1.2f, 16.3f), new Vector3(4.4f, 2.4f, 0.2f), Tokens.Hex(0xB8B0A0));
            for (int i = 0; i < 8; i++)
            {
                Part(inner, new Vector3(-2.19f, 1.4f, 3 + i * 1.8f), new Vector3(0.05f, 0.5f, 0.5f), Tokens.BgVoid, 0, PrimitiveType.Cylinder); // portholes
                Part(inner, new Vector3(2.19f, 1.4f, 3 + i * 1.8f), new Vector3(0.05f, 0.5f, 0.5f), Tokens.BgVoid, 0, PrimitiveType.Cylinder);
            }
            lamp = new GameObject("Lamp").AddComponent<Light>();
            lamp.type = LightType.Point;
            lamp.color = Tokens.Cabin;
            lamp.intensity = 2.2f;
            lamp.range = 22;
            lamp.transform.SetParent(inner, false);
            lamp.transform.localPosition = new Vector3(0, 2.2f, 8);

            var seat = Tokens.Hex(0x2E4A7A);
            int seats = Mathf.Max(8, level.passengers + level.passengers % 2);
            for (int i = 0; i < seats; i++)
            {
                var at = new Vector3(i % 2 == 0 ? -1.3f : 1.3f, 0, 3 + (i / 2) * 1.8f);
                Part(inner, at + new Vector3(0, 0.25f, 0), new Vector3(0.9f, 0.5f, 0.8f), seat);
                Part(inner, at + new Vector3(0, 0.75f, -0.45f), new Vector3(0.9f, 1f, 0.15f), seat);
                if (i >= level.passengers) continue;
                var p = Part(inner, at + new Vector3(0, 0.9f, 0), new Vector3(0.55f, 0.45f, 0.55f), Tokens.Hex(0xC9B8A6), 0, PrimitiveType.Capsule);
                var head = Part(p.transform, new Vector3(0, 1.4f, 0), new Vector3(0.9f, 0.9f, 0.9f), Tokens.Hex(0xF0C8A0), 0, PrimitiveType.Sphere);
                Pax.Add(head.transform);
                paxBody.Add(p.GetComponent<Renderer>());
            }

            var crateColor = level.crateLabel == "BAG" ? Tokens.Hex(0x7A4E2E) : Tokens.Cabin;
            for (int i = 0; i < level.crates; i++)
            {
                var c = Part(inner, new Vector3(0, 0.35f, 12.5f + i * 1.2f), new Vector3(0.7f, 0.7f, 0.7f), crateColor);
                crateStrap.Add(Part(c.transform, Vector3.zero, new Vector3(1.04f, 0.2f, 1.04f), Tokens.Danger).GetComponent<Renderer>());
                Crates.Add(c.transform);
            }

            if (level.fireAt >= 0)
            {
                fire = Part(inner, new Vector3(-1.5f, 0.4f, 9f), new Vector3(0.8f, 0.8f, 0.8f), Tokens.Amber, 4, PrimitiveType.Sphere).transform;
                fireLight = new GameObject("FireLight").AddComponent<Light>();
                fireLight.type = LightType.Point;
                fireLight.color = Tokens.Amber;
                fireLight.range = 6;
                fireLight.transform.SetParent(fire, false);
                fire.gameObject.SetActive(false);
            }

            crew = Part(inner, new Vector3(0, 0.9f, 1), new Vector3(0.6f, 0.9f, 0.6f), Tokens.Cabin, 0.4f, PrimitiveType.Capsule).transform;
            Part(crew, new Vector3(0, 1.1f, 0), new Vector3(1.1f, 0.25f, 1.1f), Tokens.Danger, 0, PrimitiveType.Cylinder); // hat
            shownCrew = crew.localPosition;

            var cam = App.I.Cam;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Tokens.Hex(0x1A1410);
            cam.transform.position = CabinCam;
            cam.transform.rotation = Quaternion.Euler(52, 0, 0);
        }

        void UpdateCabin(Snapshot s, float dt)
        {
            if (!s.cabin) return;
            cabin.rotation = Quaternion.Slerp(cabin.rotation, Quaternion.Euler(-s.pitch * 1.5f, 0, s.roll * 0.6f), 1 - Mathf.Exp(-4 * dt));
            shownCrew = Vector3.Lerp(shownCrew, new Vector3(s.crew.x, 0.9f, s.crew.y), 1 - Mathf.Exp(-15 * dt));
            crew.localPosition = shownCrew;
            for (int i = 0; i < paxBody.Count && i < s.pax.Length; i++)
            {
                var p = s.pax[i];
                paxBody[i].material.color = Color.Lerp(p.strapped ? Tokens.Hex(0x6E8FB8) : Tokens.Hex(0xC9B8A6), Tokens.Danger, p.panic);
                var body = paxBody[i].transform;
                float wobble = p.panic > 0.5f ? Mathf.Sin(Time.time * 25 + i) * p.panic * 0.08f : 0;
                float x = p.demanding ? p.seat.x * 0.15f : p.seat.x; // standing in the aisle
                body.localPosition = new Vector3(x + wobble, p.demanding ? 1.2f : 0.9f, p.seat.y);
            }
            for (int i = 0; i < Crates.Count && i < s.crates.Length; i++)
            {
                var c = s.crates[i];
                Crates[i].localPosition = Vector3.Lerp(Crates[i].localPosition, new Vector3(c.pos.x, 0.35f, c.pos.y), 1 - Mathf.Exp(-10 * dt));
                crateStrap[i].material.color = c.secured ? Tokens.Success : Tokens.Danger;
            }
            if (fire != null)
            {
                fire.gameObject.SetActive(s.fireOn);
                float flick = 0.8f + 0.2f * Mathf.Sin(Time.time * 30) + 0.1f * Mathf.Sin(Time.time * 47);
                fire.localScale = Vector3.one * (0.4f + s.fire * 1.2f) * flick;
                fireLight.intensity = 3 * s.fire * flick;
            }
            lamp.intensity = s.flicker && Mathf.PerlinNoise(Time.time * 12, 0) < 0.45f ? 0.2f : 2.2f; // emergency flicker
            App.I.Cam.transform.position = CabinCam;
            ApplyShake(App.I.Cam.transform, dt);
        }

        static readonly Vector3 CabinCam = new Vector3(0, 11, -9);

        void UpdatePilot(Snapshot s, float dt)
        {
            shownPos = Vector3.Lerp(shownPos, s.pos, 1 - Mathf.Exp(-12f * dt));
            plane.position = shownPos;
            plane.rotation = Quaternion.Euler(-s.pitch, s.yaw, s.roll);

            var cam = App.I.Cam.transform;
            var heading = Quaternion.Euler(0, s.yaw, 0);
            var want = shownPos + heading * new Vector3(0, 5, -26);
            cam.position = Vector3.Lerp(cam.position, want, 1 - Mathf.Exp(-6f * dt));
            cam.rotation = Quaternion.Slerp(cam.rotation, Quaternion.LookRotation(shownPos + heading * new Vector3(0, 0, 80) - cam.position), 1 - Mathf.Exp(-6f * dt));
            ApplyShake(cam, dt);
        }

        void ApplyShake(Transform cam, float dt)
        {
            if (shake <= 0) return;
            cam.position += new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0) * shake;
            shake = Mathf.MoveTowards(shake, 0, dt * 2f);
        }

        // ---- primitives ----

        GameObject Box(Vector3 pos, Vector3 scale, Color c, float glow = 0) => Part(root, pos, scale, c, glow);

        GameObject Part(Transform parent, Vector3 pos, Vector3 scale, Color c, float glow = 0, PrimitiveType shape = PrimitiveType.Cube)
        {
            var go = GameObject.CreatePrimitive(shape);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = Mat(go.GetComponent<Renderer>(), c, glow);
            return go;
        }

        // Shared material per color keeps draw calls batchable.
        Material Mat(Renderer r, Color c, float glow)
        {
            var key = new Color(c.r, c.g, c.b, glow);
            if (mats.TryGetValue(key, out var m)) return m;
            if (baseMat == null) baseMat = r.sharedMaterial;
            m = new Material(baseMat) { color = c };
            if (glow > 0)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", c * glow);
            }
            mats[key] = m;
            return m;
        }
    }
}
