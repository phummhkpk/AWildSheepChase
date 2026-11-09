using System.Collections.Generic;
using AWildSheepChase.Core;
using UnityEngine;

namespace AWildSheepChase.Audio
{
    /// <summary>
    /// ฟัง GameEvents.EffectRequested แล้วเล่นเสียง ถ้าไม่ใส่ไฟล์เสียงใน Overrides จะใช้เสียงสังเคราะห์ด้วยโค้ดแทน
    /// (ทุกเสียงเป็น 2D เพราะเกมมุมมอง top-down)
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [System.Serializable]
        private class ClipOverride
        {
            [SerializeField] private GameEffect effect;
            [SerializeField] private AudioClip clip;

            public GameEffect Effect => effect;
            public AudioClip Clip => clip;
        }

        private readonly struct ToneSpec
        {
            public float StartFrequency { get; }
            public float EndFrequency { get; }
            public float Duration { get; }
            public float Noise { get; }
            public float Volume { get; }

            public ToneSpec(float startFrequency, float endFrequency, float duration, float noise, float volume)
            {
                StartFrequency = startFrequency;
                EndFrequency = endFrequency;
                Duration = duration;
                Noise = noise;
                Volume = volume;
            }
        }

        [SerializeField, Range(0f, 1f)] private float masterVolume = 0.8f;
        [SerializeField, Min(1)] private int voiceCount = 12;
        [SerializeField, Range(0f, 0.5f)] private float pitchVariation = 0.08f;
        [Tooltip("เอฟเฟกต์ชนิดเดียวกันเล่นซ้ำถี่กว่านี้จะถูกข้าม (กันเสียงซ้อนเป็นก้อน)")]
        [SerializeField, Min(0f)] private float minRepeatInterval = 0.04f;
        [Tooltip("ไม่บังคับ: ใส่ไฟล์เสียงจริงแทนเสียงสังเคราะห์")]
        [SerializeField] private ClipOverride[] overrides = new ClipOverride[0];

        private readonly Dictionary<GameEffect, AudioClip> clips = new Dictionary<GameEffect, AudioClip>();
        private readonly List<AudioClip> generatedClips = new List<AudioClip>();
        private AudioSource[] voices;
        private AudioSource uiVoice; // เล่นได้แม้เกมหยุด (AudioListener.pause)
        private float[] lastPlayed;
        private int nextVoice;

        private void Awake()
        {
            voices = new AudioSource[voiceCount];
            for (int i = 0; i < voiceCount; i++)
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                voices[i] = source;
            }

            uiVoice = gameObject.AddComponent<AudioSource>();
            uiVoice.playOnAwake = false;
            uiVoice.spatialBlend = 0f;
            uiVoice.ignoreListenerPause = true;

            foreach (ClipOverride entry in overrides)
            {
                if (entry != null && entry.Clip != null)
                {
                    clips[entry.Effect] = entry.Clip;
                }
            }

            System.Array values = System.Enum.GetValues(typeof(GameEffect));
            lastPlayed = new float[values.Length];

            foreach (GameEffect effect in values)
            {
                lastPlayed[(int)effect] = -10f;

                if (clips.ContainsKey(effect)) continue;

                AudioClip clip = BuildClip(effect, GetSpec(effect));
                generatedClips.Add(clip);
                clips[effect] = clip;
            }
        }

        private void OnEnable()
        {
            GameEvents.EffectRequested += OnEffectRequested;
        }

        private void OnDisable()
        {
            GameEvents.EffectRequested -= OnEffectRequested;
        }

        private void OnDestroy()
        {
            foreach (AudioClip clip in generatedClips)
            {
                if (clip != null) Destroy(clip);
            }
        }

        private void OnEffectRequested(GameEffect effect, Vector3 position)
        {
            if (!clips.TryGetValue(effect, out AudioClip clip)) return;

            float now = Time.unscaledTime;
            int index = (int)effect;
            if (now - lastPlayed[index] < minRepeatInterval) return;
            lastPlayed[index] = now;

            AudioSource source = effect == GameEffect.DialogueBlip ? uiVoice : GetVoice();
            source.clip = clip;
            source.pitch = 1f + UnityEngine.Random.Range(-pitchVariation, pitchVariation);
            source.volume = masterVolume * VolumeSettings.Sfx;
            source.Play();
        }

        private AudioSource GetVoice()
        {
            for (int i = 0; i < voices.Length; i++)
            {
                int index = (nextVoice + i) % voices.Length;
                if (!voices[index].isPlaying)
                {
                    nextVoice = (index + 1) % voices.Length;
                    return voices[index];
                }
            }

            // เต็มทุกช่อง: ตัดเสียงที่เล่นมานานที่สุด
            AudioSource stolen = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Length;
            return stolen;
        }

        private static AudioClip BuildClip(GameEffect effect, ToneSpec spec)
        {
            const int sampleRate = 44100;

            int length = Mathf.Max(1, Mathf.CeilToInt(spec.Duration * sampleRate));
            float[] data = new float[length];
            System.Random rng = new System.Random((int)effect * 7919 + 13);
            float phase = 0f;

            for (int i = 0; i < length; i++)
            {
                float t = i / (float)length;
                float frequency = Mathf.Lerp(spec.StartFrequency, spec.EndFrequency, t);
                phase += 2f * Mathf.PI * frequency / sampleRate;

                float tone = Mathf.Sin(phase);
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                float envelope = Mathf.Min(1f, t * 20f) * (1f - t) * (1f - t);

                data[i] = Mathf.Lerp(tone, noise, spec.Noise) * envelope * spec.Volume;
            }

            AudioClip clip = AudioClip.Create("Sfx_" + effect, length, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // (ความถี่เริ่ม, ความถี่จบ, ความยาววินาที, สัดส่วน noise 0-1, ความดัง)
        private static ToneSpec GetSpec(GameEffect effect)
        {
            switch (effect)
            {
                case GameEffect.SwordSwing: return new ToneSpec(900f, 300f, 0.12f, 0.7f, 0.35f);
                case GameEffect.SwordHit: return new ToneSpec(220f, 80f, 0.14f, 0.5f, 0.6f);
                case GameEffect.RangedShot: return new ToneSpec(700f, 1400f, 0.1f, 0.1f, 0.35f);
                case GameEffect.ProjectileImpact: return new ToneSpec(300f, 100f, 0.08f, 0.6f, 0.3f);
                case GameEffect.PlayerJump: return new ToneSpec(300f, 600f, 0.12f, 0f, 0.3f);
                case GameEffect.PlayerDash: return new ToneSpec(600f, 200f, 0.18f, 0.8f, 0.35f);
                case GameEffect.PlayerHurt: return new ToneSpec(400f, 120f, 0.25f, 0.2f, 0.6f);
                case GameEffect.PlayerDeath: return new ToneSpec(300f, 50f, 0.8f, 0.2f, 0.6f);
                case GameEffect.EnemyHit: return new ToneSpec(260f, 140f, 0.1f, 0.4f, 0.5f);
                case GameEffect.EnemyWindup: return new ToneSpec(200f, 500f, 0.2f, 0f, 0.25f);
                case GameEffect.EnemyDeath: return new ToneSpec(400f, 60f, 0.4f, 0.4f, 0.55f);
                case GameEffect.PickupCollect: return new ToneSpec(700f, 1200f, 0.12f, 0f, 0.35f);
                case GameEffect.PotionUse: return new ToneSpec(400f, 900f, 0.3f, 0f, 0.4f);
                case GameEffect.DoorOpen: return new ToneSpec(90f, 60f, 0.7f, 0.5f, 0.5f);
                case GameEffect.CheckpointActivate: return new ToneSpec(500f, 1000f, 0.35f, 0f, 0.4f);
                case GameEffect.BossRoar: return new ToneSpec(110f, 60f, 1f, 0.5f, 0.7f);
                case GameEffect.BossDefeated: return new ToneSpec(200f, 40f, 1.4f, 0.4f, 0.7f);
                case GameEffect.DialogueBlip: return new ToneSpec(520f, 480f, 0.04f, 0.05f, 0.2f);
                default: return new ToneSpec(440f, 440f, 0.1f, 0f, 0.2f);
            }
        }
    }
}