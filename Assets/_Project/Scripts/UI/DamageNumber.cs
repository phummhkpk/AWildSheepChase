using TMPro;
using UnityEngine;
using UnityEngine.Pool;

namespace AWildSheepChase.UI
{
    public class DamageNumber : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;
        [SerializeField, Min(0.1f)] private float lifetime = 0.8f;
        [SerializeField, Min(0f)] private float riseSpeed = 1.5f;

        private IObjectPool<DamageNumber> pool;
        private Camera viewCamera;
        private Color baseColor = Color.white;
        private float elapsed;
        private bool released = true;

        private void Awake()
        {
            if (text == null)
            {
                text = GetComponentInChildren<TMP_Text>();
            }
        }

        public void SetPool(IObjectPool<DamageNumber> ownerPool)
        {
            pool = ownerPool;
        }

        public void Show(Vector3 position, float amount, Color color, Camera cam)
        {
            transform.position = position;
            viewCamera = cam;
            baseColor = color;
            elapsed = 0f;
            released = false;

            text.text = Mathf.CeilToInt(amount).ToString();
            text.color = color;

            FaceCamera();
        }

        private void Update()
        {
            if (released) return;

            elapsed += Time.deltaTime;
            if (elapsed >= lifetime)
            {
                Release();
                return;
            }

            transform.position += Vector3.up * (riseSpeed * Time.deltaTime);

            // จางหายในครึ่งหลังของอายุ
            float t = elapsed / lifetime;
            float alpha = t < 0.5f ? 1f : 1f - (t - 0.5f) * 2f;
            text.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
        }

        private void LateUpdate()
        {
            if (!released) FaceCamera();
        }

        private void FaceCamera()
        {
            if (viewCamera != null)
            {
                transform.rotation = viewCamera.transform.rotation;
            }
        }

        private void Release()
        {
            released = true;

            if (pool != null)
            {
                pool.Release(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}