using UnityEngine;

namespace BlindRunway
{
    public static class Settings
    {
        public static float Volume = 0.8f;
        public static bool Shake = true;
        public static bool Colorblind;
        public static float UiScale = 1f;

        public static void Load()
        {
            Volume = PlayerPrefs.GetFloat("br.volume", 0.8f);
            Shake = PlayerPrefs.GetInt("br.shake", 1) == 1;
            Colorblind = PlayerPrefs.GetInt("br.colorblind", 0) == 1;
            UiScale = PlayerPrefs.GetFloat("br.uiscale", 1f);
            AudioListener.volume = Volume;
        }

        public static void Save()
        {
            PlayerPrefs.SetFloat("br.volume", Volume);
            PlayerPrefs.SetInt("br.shake", Shake ? 1 : 0);
            PlayerPrefs.SetInt("br.colorblind", Colorblind ? 1 : 0);
            PlayerPrefs.SetFloat("br.uiscale", UiScale);
            PlayerPrefs.Save();
            AudioListener.volume = Volume;
        }
    }
}
