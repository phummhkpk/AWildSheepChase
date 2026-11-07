using System.Collections.Generic;
using UnityEngine;

namespace AWildSheepChase.CameraSystem
{
    /// <summary>
    /// กำแพงที่บังระหว่างกล้องกับผู้เล่นจะถูกสลับเป็น Material โปร่งใส แล้วคืนของเดิมเมื่อไม่บังแล้ว
    /// (วางบน object ว่างใน Scene ไม่ต้องติดกับกล้อง)
    /// </summary>
    public class WallFader : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [Tooltip("Layer ของกำแพงที่ให้โปร่งใสได้ (เช่น Ground)")]
        [SerializeField] private LayerMask occluderLayers;
        [SerializeField] private Material fadeMaterial;
        [Tooltip("ปล่อยว่าง = ใช้ Camera.main")]
        [SerializeField] private Camera viewCamera;
        [SerializeField, Min(0f)] private float targetHeight = 1f;
        [Tooltip("ความกว้างของ 'เส้นสายตา' ยิ่งมากยิ่งโปร่งกำแพงข้าง ๆ ด้วย")]
        [SerializeField, Min(0.05f)] private float probeRadius = 0.5f;
        [SerializeField, Min(0.02f)] private float checkInterval = 0.05f;

        private readonly RaycastHit[] hits = new RaycastHit[16];
        private readonly Dictionary<Renderer, Material[]> faded = new Dictionary<Renderer, Material[]>();
        private readonly HashSet<Renderer> occluding = new HashSet<Renderer>();
        private readonly List<Renderer> toRestore = new List<Renderer>();
        private float nextCheckTime;

        private void Awake()
        {
            if (target == null || fadeMaterial == null)
            {
                Debug.LogWarning("WallFader: ยังไม่ได้ลาก Target (Player) หรือ Fade Material", this);
                enabled = false;
            }
        }

        private void OnDisable()
        {
            foreach (KeyValuePair<Renderer, Material[]> pair in faded)
            {
                if (pair.Key != null) pair.Key.sharedMaterials = pair.Value;
            }

            faded.Clear();
        }

        private void Update()
        {
            if (Time.unscaledTime < nextCheckTime) return;
            nextCheckTime = Time.unscaledTime + checkInterval;

            if (viewCamera == null)
            {
                viewCamera = Camera.main;
                if (viewCamera == null) return;
            }

            occluding.Clear();

            Vector3 from = viewCamera.transform.position;
            Vector3 to = target.position + Vector3.up * targetHeight;
            Vector3 delta = to - from;
            float distance = delta.magnitude;

            if (distance > 0.5f)
            {
                int count = Physics.SphereCastNonAlloc(
                    from, probeRadius, delta / distance, hits, distance - 0.3f,
                    occluderLayers, QueryTriggerInteraction.Ignore);

                for (int i = 0; i < count; i++)
                {
                    Renderer r = hits[i].collider.GetComponent<Renderer>();
                    if (r == null) continue;

                    occluding.Add(r);
                    Fade(r);
                }
            }

            RestoreUnblocked();
        }

        private void Fade(Renderer r)
        {
            if (faded.ContainsKey(r)) return;

            faded[r] = r.sharedMaterials;

            Material[] replacement = new Material[r.sharedMaterials.Length];
            for (int i = 0; i < replacement.Length; i++)
            {
                replacement[i] = fadeMaterial;
            }

            r.sharedMaterials = replacement;
        }

        private void RestoreUnblocked()
        {
            toRestore.Clear();

            foreach (KeyValuePair<Renderer, Material[]> pair in faded)
            {
                if (pair.Key == null || !occluding.Contains(pair.Key))
                {
                    toRestore.Add(pair.Key);
                }
            }

            foreach (Renderer r in toRestore)
            {
                if (r != null) r.sharedMaterials = faded[r];
                faded.Remove(r);
            }
        }
    }
}