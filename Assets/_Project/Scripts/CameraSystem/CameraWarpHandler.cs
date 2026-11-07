using AWildSheepChase.Player;
using Unity.Cinemachine;
using UnityEngine;

namespace AWildSheepChase.CameraSystem
{
    /// <summary>เมื่อผู้เล่นถูกย้ายตำแหน่งทันที (เกิดใหม่/โหลดเซฟ) สั่งให้ Cinemachine Camera ตามไปทันที ไม่ไถล</summary>
    [RequireComponent(typeof(CinemachineCamera))]
    public class CameraWarpHandler : MonoBehaviour
    {
        [SerializeField] private PlayerController player;

        private CinemachineCamera cam;

        private void Awake()
        {
            cam = GetComponent<CinemachineCamera>();

            if (player == null)
            {
                Debug.LogWarning("CameraWarpHandler: ยังไม่ได้ลาก Player ใส่ช่อง Player", this);
            }
        }

        private void OnEnable()
        {
            if (player != null) player.Warped += OnPlayerWarped;
        }

        private void OnDisable()
        {
            if (player != null) player.Warped -= OnPlayerWarped;
        }

        private void OnPlayerWarped(Vector3 delta)
        {
            cam.OnTargetObjectWarped(player.transform, delta);
        }
    }
}