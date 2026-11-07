using System;
using AWildSheepChase.CameraSystem;
using UnityEngine;

namespace AWildSheepChase.Core
{
    /// <summary>
    /// ศูนย์กลาง event ที่ระบบต่าง ๆ ใช้คุยกันโดยไม่ผูกคลาสกันตรง ๆ
    /// ผู้ฟังต้อง subscribe ใน OnEnable และ unsubscribe ใน OnDisable เสมอ
    /// </summary>
    public static class GameEvents
    {
        public static event Action<CameraShakeProfile> CameraShakeRequested;
        public static event Action<float> HitStopRequested;

        /// <summary>(ตำแหน่งในโลก, ดาเมจ, เป็นผู้เล่นหรือไม่)</summary>
        public static event Action<Vector3, float, bool> DamageTaken;

        /// <summary>ข้อความสั้น ๆ แจ้งผู้เล่น เช่น "Locked: needs Dungeon Key"</summary>
        public static event Action<string> MessageRequested;

        /// <summary>(เอฟเฟกต์, ตำแหน่งในโลก) AudioManager และ VfxManager ฟัง</summary>
        public static event Action<GameEffect, Vector3> EffectRequested;

        public static void RequestCameraShake(CameraShakeProfile profile)
        {
            if (profile == null) return;
            CameraShakeRequested?.Invoke(profile);
        }

        public static void RequestHitStop(float duration)
        {
            if (duration <= 0f) return;
            HitStopRequested?.Invoke(duration);
        }

        public static void RaiseDamageTaken(Vector3 position, float amount, bool isPlayer)
        {
            DamageTaken?.Invoke(position, amount, isPlayer);
        }

        public static void RequestMessage(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            MessageRequested?.Invoke(message);
        }

        public static void RequestEffect(GameEffect effect, Vector3 position)
        {
            EffectRequested?.Invoke(effect, position);
        }
    }
}