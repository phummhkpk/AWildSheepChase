using System;
using UnityEngine;

namespace AWildSheepChase.Audio
{
    public enum VolumeChannel { Master, Music, Sfx }

    /// <summary>ค่าระดับเสียง 0-1 ของแต่ละช่อง เก็บใน PlayerPrefs (static class เหมือน GameTime และ Loc)</summary>
    public static class VolumeSettings
    {
        private const string MasterKey = "volume_master";
        private const string MusicKey = "volume_music";
        private const string SfxKey = "volume_sfx";

        private static float master = 1f;
        private static float music = 1f;
        private static float sfx = 1f;
        private static bool loaded;

        public static event Action Changed;

        public static float Master => Get(VolumeChannel.Master);
        public static float Music => Get(VolumeChannel.Music);
        public static float Sfx => Get(VolumeChannel.Sfx);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            loaded = false;
            Changed = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplyOnStart()
        {
            EnsureLoaded();
        }

        private static void EnsureLoaded()
        {
            if (loaded) return;

            master = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterKey, 1f));
            music = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicKey, 1f));
            sfx = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxKey, 1f));
            loaded = true;

            AudioListener.volume = master;
        }

        public static float Get(VolumeChannel channel)
        {
            EnsureLoaded();

            switch (channel)
            {
                case VolumeChannel.Master: return master;
                case VolumeChannel.Music: return music;
                default: return sfx;
            }
        }

        public static void Set(VolumeChannel channel, float value)
        {
            EnsureLoaded();
            value = Mathf.Clamp01(value);

            switch (channel)
            {
                case VolumeChannel.Master:
                    master = value;
                    PlayerPrefs.SetFloat(MasterKey, value);
                    AudioListener.volume = value;
                    break;
                case VolumeChannel.Music:
                    music = value;
                    PlayerPrefs.SetFloat(MusicKey, value);
                    break;
                default:
                    sfx = value;
                    PlayerPrefs.SetFloat(SfxKey, value);
                    break;
            }

            Changed?.Invoke();
        }

        /// <summary>เขียน PlayerPrefs ลงดิสก์ (เรียกตอนปิดหน้าต่างตั้งค่า ไม่เรียกทุกครั้งที่ลาก slider)</summary>
        public static void Save()
        {
            PlayerPrefs.Save();
        }
    }
}