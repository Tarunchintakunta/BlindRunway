using UnityEditor;
using UnityEngine;

namespace BlindRunway.EditorTools
{
    // Windows + macOS build profiles. Dev builds keep the SOLO TEST button. Output goes to Builds/ (gitignored).
    // CI: Unity -batchmode -quit -projectPath . -executeMethod BlindRunway.EditorTools.BuildMenu.BuildAll
    public static class BuildMenu
    {
        static readonly string[] Scenes = { "Assets/_Project/Scenes/Boot.unity" };

        [MenuItem("Blind Runway/Build/macOS")]
        public static void Mac() => Build(BuildTarget.StandaloneOSX, "Builds/macOS/BlindRunway.app", false);

        [MenuItem("Blind Runway/Build/Windows")]
        public static void Windows() => Build(BuildTarget.StandaloneWindows64, "Builds/Windows/BlindRunway.exe", false);

        [MenuItem("Blind Runway/Build/macOS (dev)")]
        public static void MacDev() => Build(BuildTarget.StandaloneOSX, "Builds/macOS-dev/BlindRunway.app", true);

        [MenuItem("Blind Runway/Build/Windows (dev)")]
        public static void WindowsDev() => Build(BuildTarget.StandaloneWindows64, "Builds/Windows-dev/BlindRunway.exe", true);

        public static void BuildAll()
        {
            Mac();
            Windows();
        }

        static void Build(BuildTarget target, string path, bool dev)
        {
            PlayerSettings.productName = "Blind Runway";
            PlayerSettings.runInBackground = true; // several local clients on one machine
            var options = new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = path,
                target = target,
                targetGroup = BuildTargetGroup.Standalone,
                options = dev ? BuildOptions.Development : BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(options);
            Debug.Log($"Blind Runway {target}{(dev ? " dev" : "")}: {report.summary.result} -> {path}");
        }
    }
}
