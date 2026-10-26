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
            else
            {
                RenderSettings.fog = false;
                ResetCamera();
            }
        }

        public void Clear()
        {
            if (root) Destroy(root.gameObject);
            root = plane = null;
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
            for (float z = -900; z < 0; z += 60) Box(new Vector3(0, 1, z), new Vector3(2, 2, 2), Tokens.TextPrimary, 4);
            Box(new Vector3(0, 1, -300), new Vector3(30, 1, 1), Tokens.TextPrimary, 4);

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
        }

        void Update()
        {
            var snap = App.I.Session.Snap;
            if (snap == null || root == null) return;
            if (role == Role.Pilot) UpdatePilot(snap, Time.deltaTime);
        }

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
