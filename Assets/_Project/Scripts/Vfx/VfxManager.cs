using System.Collections.Generic;
using AWildSheepChase.Core;
using UnityEngine;
using UnityEngine.Pool;

namespace AWildSheepChase.Vfx
{
    /// <summary>
    /// ฟัง GameEvents.EffectRequested แล้วพ่นอนุภาคด้วย Particle System ที่สร้างด้วยโค้ดและเก็บไว้ใน ObjectPool
    /// ต้องใส่ Material สำหรับอนุภาค (ดูขั้นตอนใน Editor)
    /// </summary>
    public class VfxManager : MonoBehaviour
    {
        private readonly struct VfxPreset
        {
            public Color Color { get; }
            public int Count { get; }
            public float Speed { get; }
            public float Size { get; }
            public float Lifetime { get; }
            public float Gravity { get; }

            public VfxPreset(Color color, int count, float speed, float size, float lifetime, float gravity)
            {
                Color = color;
                Count = count;
                Speed = speed;
                Size = size;
                Lifetime = lifetime;
                Gravity = gravity;
            }
        }

        private struct ActiveVfx
        {
            public ParticleSystem System;
            public float ReleaseTime;
        }

        [SerializeField] private Material particleMaterial;
        [Tooltip("คูณขนาดอนุภาคทั้งหมด")]
        [SerializeField, Min(0.1f)] private float sizeScale = 1f;

        private ObjectPool<ParticleSystem> pool;
        private readonly List<ActiveVfx> active = new List<ActiveVfx>();
        private Gradient fadeGradient;
        private AnimationCurve shrinkCurve;

        private void Awake()
        {
            if (particleMaterial == null)
            {
                Debug.LogWarning("VfxManager: ยังไม่ได้ใส่ Particle Material อนุภาคจะเป็นสีชมพู", this);
            }

            fadeGradient = new Gradient();
            fadeGradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });

            shrinkCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

            pool = new ObjectPool<ParticleSystem>(
                CreateSystem, OnGetSystem, OnReleaseSystem, OnDestroySystem, true, 8, 40);
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
            pool?.Dispose();
        }

        private void Update()
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                if (Time.time < active[i].ReleaseTime) continue;

                pool.Release(active[i].System);
                active.RemoveAt(i);
            }
        }

        private void OnEffectRequested(GameEffect effect, Vector3 position)
        {
            if (!TryGetPreset(effect, out VfxPreset preset)) return;

            ParticleSystem system = pool.Get();

            ParticleSystem.MainModule main = system.main;
            main.gravityModifier = preset.Gravity;

            ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams
            {
                position = position,
                startColor = preset.Color,
                startSize = preset.Size * sizeScale,
                startLifetime = preset.Lifetime
            };

            for (int i = 0; i < preset.Count; i++)
            {
                emit.velocity = UnityEngine.Random.onUnitSphere * (preset.Speed * UnityEngine.Random.Range(0.4f, 1f));
                system.Emit(emit, 1);
            }

            active.Add(new ActiveVfx { System = system, ReleaseTime = Time.time + preset.Lifetime + 0.1f });
        }

        // (สี, จำนวน, ความเร็ว, ขนาด, อายุ, แรงโน้มถ่วง) เอฟเฟกต์ที่ไม่อยู่ในตารางนี้ = ไม่มีภาพ มีแต่เสียง
        private static bool TryGetPreset(GameEffect effect, out VfxPreset preset)
        {
            switch (effect)
            {
                case GameEffect.EnemyHit:
                    preset = new VfxPreset(new Color(1f, 0.95f, 0.6f), 10, 5f, 0.14f, 0.35f, 2f);
                    return true;
                case GameEffect.ProjectileImpact:
                    preset = new VfxPreset(new Color(1f, 0.6f, 0.2f), 8, 3f, 0.1f, 0.3f, 0f);
                    return true;
                case GameEffect.PlayerHurt:
                    preset = new VfxPreset(new Color(1f, 0.2f, 0.2f), 14, 4f, 0.14f, 0.4f, 2f);
                    return true;
                case GameEffect.EnemyDeath:
                    preset = new VfxPreset(new Color(0.9f, 0.7f, 0.5f), 30, 5f, 0.2f, 0.7f, 1f);
                    return true;
                case GameEffect.PlayerDash:
                    preset = new VfxPreset(new Color(0.6f, 0.9f, 1f), 6, 1.5f, 0.25f, 0.3f, 0f);
                    return true;
                case GameEffect.PickupCollect:
                    preset = new VfxPreset(new Color(1f, 0.9f, 0.3f), 10, 2f, 0.1f, 0.5f, -0.5f);
                    return true;
                case GameEffect.PotionUse:
                    preset = new VfxPreset(new Color(0.4f, 1f, 0.5f), 16, 2f, 0.12f, 0.7f, -0.8f);
                    return true;
                case GameEffect.CheckpointActivate:
                    preset = new VfxPreset(new Color(0.3f, 0.9f, 1f), 24, 3f, 0.12f, 0.9f, -1f);
                    return true;
                case GameEffect.DoorOpen:
                    preset = new VfxPreset(new Color(0.6f, 0.5f, 0.4f), 20, 2f, 0.3f, 0.8f, 0f);
                    return true;
                case GameEffect.BossRoar:
                    preset = new VfxPreset(new Color(1f, 0.3f, 0.2f), 40, 6f, 0.2f, 0.8f, 0f);
                    return true;
                case GameEffect.BossDefeated:
                    preset = new VfxPreset(new Color(1f, 0.85f, 0.3f), 80, 7f, 0.25f, 1.2f, 2f);
                    return true;
                default:
                    preset = default;
                    return false;
            }
        }

        // ---------- Pool ----------

        private ParticleSystem CreateSystem()
        {
            GameObject go = new GameObject("Vfx");
            go.transform.SetParent(transform, false);

            ParticleSystem system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = system.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 200;

            // เราใช้ Emit() เองทั้งหมด จึงปิดการปล่อยอัตโนมัติและรูปทรงเริ่มต้น
            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = false;

            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = false;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = system.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = fadeGradient;

            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = system.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, shrinkCurve);

            ParticleSystemRenderer particleRenderer = go.GetComponent<ParticleSystemRenderer>();
            particleRenderer.sharedMaterial = particleMaterial;
            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            particleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            particleRenderer.receiveShadows = false;

            return system;
        }

        private static void OnGetSystem(ParticleSystem system) => system.gameObject.SetActive(true);

        private static void OnReleaseSystem(ParticleSystem system)
        {
            system.Clear();
            system.gameObject.SetActive(false);
        }

        private static void OnDestroySystem(ParticleSystem system)
        {
            if (system != null)
            {
                Destroy(system.gameObject);
            }
        }
    }
}