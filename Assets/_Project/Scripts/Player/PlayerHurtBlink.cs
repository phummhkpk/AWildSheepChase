using AWildSheepChase.Vitals;
using UnityEngine;

namespace AWildSheepChase.Player
{
    /// <summary>ตัวละครกะพริบช่วง i-frame หลังโดนตี (ไม่กะพริบตอน Dash)</summary>
    [RequireComponent(typeof(Health))]
    public class PlayerHurtBlink : MonoBehaviour
    {
        [SerializeField, Min(0.02f)] private float blinkInterval = 0.08f;

        private Health health;
        private Renderer[] renderers;
        private bool wasBlinking;

        private void Awake()
        {
            health = GetComponent<Health>();
            renderers = GetComponentsInChildren<Renderer>();
        }

        private void OnDisable()
        {
            if (wasBlinking)
            {
                SetVisible(true);
                wasBlinking = false;
            }
        }

        private void Update()
        {
            bool blinking = health.IsAlive && health.IsInIFrames;

            if (blinking)
            {
                bool visible = ((int)(Time.time / blinkInterval) & 1) == 0;
                SetVisible(visible);
                wasBlinking = true;
            }
            else if (wasBlinking)
            {
                SetVisible(true);
                wasBlinking = false;
            }
        }

        private void SetVisible(bool visible)
        {
            foreach (Renderer r in renderers)
            {
                if (r != null) r.enabled = visible;
            }
        }
    }
}