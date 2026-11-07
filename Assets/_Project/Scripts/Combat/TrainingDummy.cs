using AWildSheepChase.Core;
using UnityEngine;

namespace AWildSheepChase.Combat
{
    /// <summary>หุ่นซ้อมสำหรับทดสอบ: ไม่ตาย กระพริบสีและกระเด็นเมื่อโดนตี ลบทิ้งได้ใน Milestone 4</summary>
    [RequireComponent(typeof(Rigidbody))]
    public class TrainingDummy : MonoBehaviour, IDamageable
    {
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private Color flashColor = Color.white;
        [SerializeField, Min(0f)] private float flashDuration = 0.1f;

        public bool IsAlive => true;
        public bool IsInvincible => false;

        private Rigidbody rb;
        private Material material;
        private Color baseColor;
        private float flashEndTime;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();

            if (targetRenderer == null)
            {
                targetRenderer = GetComponentInChildren<Renderer>();
            }

            material = targetRenderer.material;
            baseColor = material.color;
        }

        private void OnDestroy()
        {
            if (material != null)
            {
                Destroy(material);
            }
        }

        public void TakeDamage(in DamageInfo info)
        {
            Debug.Log($"{name} โดน {info.Amount} ดาเมจ");

            Vector3 direction = info.Direction;
            direction.y = 0f;
            rb.AddForce(direction.normalized * info.KnockbackForce, ForceMode.Impulse);

            flashEndTime = Time.unscaledTime + flashDuration;
        }

        private void Update()
        {
            material.color = Time.unscaledTime < flashEndTime ? flashColor : baseColor;
        }
    }
}