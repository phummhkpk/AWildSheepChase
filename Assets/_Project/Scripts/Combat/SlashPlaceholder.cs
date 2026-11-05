using UnityEngine;

namespace AWildSheepChase.Combat
{
    /// <summary>แสดงวัตถุชั่วคราวตอน hitbox ทำงาน เพื่อดูจังหวะการฟันก่อนมี Animator</summary>
    public class SlashPlaceholder : MonoBehaviour
    {
        [SerializeField] private PlayerCombat combat;
        [SerializeField] private GameObject slashVisual;
        [SerializeField, Min(0.02f)] private float visibleDuration = 0.12f;

        private float hideTime;

        private void Awake()
        {
            if (slashVisual != null)
            {
                slashVisual.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (combat != null) combat.MeleeHitboxActivated += OnHitboxActivated;
        }

        private void OnDisable()
        {
            if (combat != null) combat.MeleeHitboxActivated -= OnHitboxActivated;
        }

        private void OnHitboxActivated(int stepIndex)
        {
            if (slashVisual == null) return;
            slashVisual.SetActive(true);
            hideTime = Time.time + visibleDuration;
        }

        private void Update()
        {
            if (slashVisual != null && slashVisual.activeSelf && Time.time >= hideTime)
            {
                slashVisual.SetActive(false);
            }
        }
    }
}