using AWildSheepChase.Core;
using UnityEngine;
using UnityEngine.Pool;

namespace AWildSheepChase.UI
{
    public class DamageNumberSpawner : MonoBehaviour
    {
        [SerializeField] private DamageNumber prefab;
        [SerializeField] private Color enemyColor = Color.white;
        [SerializeField] private Color playerColor = new Color(1f, 0.3f, 0.3f);
        [Tooltip("สุ่มเลื่อนซ้าย-ขวาเล็กน้อย กันตัวเลขซ้อนกัน")]
        [SerializeField, Min(0f)] private float horizontalJitter = 0.3f;
        [Tooltip("ปล่อยว่างไว้ = ใช้ Camera.main")]
        [SerializeField] private Camera viewCamera;

        private ObjectPool<DamageNumber> pool;

        private void Awake()
        {
            pool = new ObjectPool<DamageNumber>(
                CreateNumber, OnGetNumber, OnReleaseNumber, OnDestroyNumber,
                true, 20, 60);
        }

        private void OnEnable()
        {
            GameEvents.DamageTaken += OnDamageTaken;
        }

        private void OnDisable()
        {
            GameEvents.DamageTaken -= OnDamageTaken;
        }

        private void OnDestroy()
        {
            pool?.Dispose();
        }

        private void OnDamageTaken(Vector3 position, float amount, bool isPlayer)
        {
            if (prefab == null) return;

            if (viewCamera == null)
            {
                viewCamera = Camera.main;
            }

            Vector3 offset = viewCamera != null
                ? viewCamera.transform.right * Random.Range(-horizontalJitter, horizontalJitter)
                : Vector3.zero;

            DamageNumber number = pool.Get();
            number.Show(position + offset, amount, isPlayer ? playerColor : enemyColor, viewCamera);
        }

        private DamageNumber CreateNumber()
        {
            DamageNumber number = Instantiate(prefab, transform);
            number.SetPool(pool);
            return number;
        }

        private static void OnGetNumber(DamageNumber number) => number.gameObject.SetActive(true);

        private static void OnReleaseNumber(DamageNumber number) => number.gameObject.SetActive(false);

        private static void OnDestroyNumber(DamageNumber number)
        {
            if (number != null)
            {
                Destroy(number.gameObject);
            }
        }
    }
}