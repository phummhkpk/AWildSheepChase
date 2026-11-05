using System.Collections;
using AWildSheepChase.Core;
using UnityEngine;

namespace AWildSheepChase.Combat
{
    public class HitStopManager : MonoBehaviour
    {
        [Tooltip("ความเร็วเวลาระหว่าง hit-stop (0 = หยุดสนิท)")]
        [SerializeField, Range(0f, 1f)] private float hitStopTimeScale = 0.05f;
        [SerializeField, Min(0f)] private float maxDuration = 0.15f;

        private float endTimeUnscaled;
        private bool running;

        private void OnEnable()
        {
            GameEvents.HitStopRequested += OnHitStopRequested;
        }

        private void OnDisable()
        {
            GameEvents.HitStopRequested -= OnHitStopRequested;

            if (running)
            {
                Time.timeScale = 1f;
                running = false;
            }
        }

        private void OnHitStopRequested(float duration)
        {
            duration = Mathf.Min(duration, maxDuration);

            float newEnd = Time.unscaledTime + duration;
            if (newEnd > endTimeUnscaled)
            {
                endTimeUnscaled = newEnd;
            }

            if (!running)
            {
                StartCoroutine(HitStopRoutine());
            }
        }

        private IEnumerator HitStopRoutine()
        {
            running = true;
            Time.timeScale = hitStopTimeScale;

            while (Time.unscaledTime < endTimeUnscaled)
            {
                yield return null;
            }

            Time.timeScale = 1f;
            running = false;
        }
    }
}