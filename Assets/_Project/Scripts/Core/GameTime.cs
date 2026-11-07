using UnityEngine;

namespace AWildSheepChase.Core
{
    /// <summary>
    /// ตัวเดียวที่ควรแตะ Time.timeScale: รวมสถานะ "หยุดเกม" กับ "hit-stop"
    /// เพื่อไม่ให้ hit-stop จบแล้วเผลอปลดการหยุดเกม
    /// </summary>
    public static class GameTime
    {
        private static bool paused;
        private static float hitStopScale = 1f;

        public static bool IsPaused => paused;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Reset();
        }

        public static void SetPaused(bool value)
        {
            paused = value;
            Apply();
        }

        public static void SetHitStopScale(float scale)
        {
            hitStopScale = Mathf.Clamp01(scale);
            Apply();
        }

        /// <summary>คืนเวลาเป็นปกติ (เรียกก่อนโหลด Scene ใหม่)</summary>
        public static void Reset()
        {
            paused = false;
            hitStopScale = 1f;
            Apply();
        }

        private static void Apply()
        {
            Time.timeScale = paused ? 0f : hitStopScale;
            AudioListener.pause = paused;
        }
    }
}