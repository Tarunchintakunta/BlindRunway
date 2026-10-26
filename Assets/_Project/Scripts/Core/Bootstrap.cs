using UnityEngine;

namespace BlindRunway
{
    // Single entry point. Everything (camera, UI, net, worlds) is built in code, so Boot.unity stays empty
    // and pressing Play from any scene works.
    public static class Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Object.FindAnyObjectByType<App>() != null) return;
            var go = new GameObject("BlindRunway");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<App>();
        }
    }
}
