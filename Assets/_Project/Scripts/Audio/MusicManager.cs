using AWildSheepChase.Core;
using AWildSheepChase.Enemies;
using UnityEngine;

namespace AWildSheepChase.Audio
{
    /// <summary>
    /// เพลงพื้นหลัง 2 แทร็ก (สำรวจ/บอส) ครอสเฟดตามสถานะบอส ลดเสียงตอนหยุดเกม
    /// ถ้าไม่ใส่คลิป จะสร้างเสียงแอมเบียนต์สังเคราะห์ที่วนต่อเนื่องไม่สะดุดแทน
    /// </summary>
    public class MusicManager : MonoBehaviour
    {
        private enum Mood { Explore, Boss, Silent }

        [Tooltip("ไม่บังคับ: ว่าง = ใช้เสียงสังเคราะห์")]
        [SerializeField] private AudioClip exploreClip;
        [Tooltip("ไม่บังคับ: ว่าง = ใช้เสียงสังเคราะห์")]
        [SerializeField] private AudioClip bossClip;
        [Tooltip("ไม่บังคับ: ถ้าไม่ใส่ จะมีแค่แทร็กสำรวจ")]
        [SerializeField] private BossController boss;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.35f;
        [Tooltip("ตอนหยุดเกม/คุยกับ NPC เพลงเหลือสัดส่วนนี้")]
        [SerializeField, Range(0f, 1f)] private float pausedScale = 0.3f;
        [SerializeField, Min(0.1f)] private float fadeSpeed = 0.8f;

        private AudioSource exploreSource;
        private AudioSource bossSource;
        private AudioClip generatedExplore;
        private AudioClip generatedBoss;
        private Mood mood = Mood.Explore;
        private float exploreLevel;
        private float bossLevel;
        private float duck = 1f;

        private void Awake()
        {
            if (exploreClip == null) exploreClip = generatedExplore = BuildDrone(false);
            if (bossClip == null) bossClip = generatedBoss = BuildDrone(true);

            exploreSource = CreateSource(exploreClip);
            bossSource = CreateSource(bossClip);
        }

        private void OnEnable()
        {
            if (boss == null) return;

            boss.Engaged += OnBossEngaged;
            boss.Disengaged += OnBossDisengaged;
            boss.Defeated += OnBossDefeated;
        }

        private void OnDisable()
        {
            if (boss == null) return;

            boss.Engaged -= OnBossEngaged;
            boss.Disengaged -= OnBossDisengaged;
            boss.Defeated -= OnBossDefeated;
        }

        private void OnDestroy()
        {
            if (generatedExplore != null) Destroy(generatedExplore);
            if (generatedBoss != null) Destroy(generatedBoss);
        }

        private void OnBossEngaged() => mood = Mood.Boss;

        private void OnBossDisengaged() => mood = Mood.Explore;

        private void OnBossDefeated() => mood = Mood.Silent;

        private void Update()
        {
            float dt = Time.unscaledDeltaTime * fadeSpeed;

            exploreLevel = Mathf.MoveTowards(exploreLevel, mood == Mood.Explore ? 1f : 0f, dt);
            bossLevel = Mathf.MoveTowards(bossLevel, mood == Mood.Boss ? 1f : 0f, dt);
            duck = Mathf.MoveTowards(duck, GameTime.IsPaused ? pausedScale : 1f, Time.unscaledDeltaTime * 3f);

            exploreSource.volume = exploreLevel * musicVolume * duck * VolumeSettings.Music;
            bossSource.volume = bossLevel * musicVolume * duck * VolumeSettings.Music;
        }

        private AudioSource CreateSource(AudioClip clip)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.ignoreListenerPause = true;
            source.volume = 0f;
            source.Play();
            return source;
        }

        /// <summary>
        /// สร้างเสียงยาว 8 วินาทีที่วนไม่สะดุด: ทุกความถี่เป็นจำนวนเต็มของ 1/8 Hz
        /// จึงจบรอบพอดีที่เฟสเดิม (บอสมีจังหวะเต้น 2 Hz และคอร์ดที่ตึงกว่า)
        /// </summary>
        private static AudioClip BuildDrone(bool boss)
        {
            const int sampleRate = 44100;
            const float seconds = 8f;

            int length = Mathf.CeilToInt(sampleRate * seconds);
            float[] data = new float[length];

            float[] freqs = boss
                ? new[] { 98f, 147f, 138.5f }
                : new[] { 110f, 165f, 220f };
            float pulseHz = boss ? 2f : 0.25f;
            float pulseDepth = boss ? 0.6f : 0.25f;

            for (int i = 0; i < length; i++)
            {
                float t = i / (float)sampleRate;
                float sample = 0f;

                foreach (float f in freqs)
                {
                    sample += Mathf.Sin(2f * Mathf.PI * f * t);
                }

                sample /= freqs.Length;

                float pulse = 1f - pulseDepth + pulseDepth * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * pulseHz * t));
                data[i] = sample * pulse * 0.5f;
            }

            AudioClip clip = AudioClip.Create(boss ? "Music_Boss" : "Music_Explore", length, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}