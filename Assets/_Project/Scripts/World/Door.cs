using System;
using AWildSheepChase.Core;
using AWildSheepChase.Items;
using UnityEngine;
using UnityEngine.AI;

namespace AWildSheepChase.World
{
    public enum DoorMode { Key, RoomCleared, Free }

    /// <summary>
    /// ประตูเลื่อนจมลงพื้น Collider บน object นี้ต้องเป็น Is Trigger (ไว้ตรวจว่าผู้เล่นเข้าใกล้)
    /// ส่วนที่ชนจริงคือ Panel (ลูก) ซึ่งสคริปต์จะปิดเมื่อประตูเปิดจบ
    /// </summary>
    public class Door : MonoBehaviour
    {
        private enum DoorState { Closed, Opening, Open }

        [Tooltip("รหัสประตู ใช้ตอน Save/Load ห้ามซ้ำกัน")]
        [SerializeField] private string doorId = "door";
        [SerializeField] private DoorMode mode = DoorMode.Key;

        [Header("Key mode")]
        [SerializeField] private KeyItemData requiredKey;
        [SerializeField] private bool consumeKey = true;

        [Header("Room Cleared mode")]
        [SerializeField] private Room requiredRoom;

        [Header("Parts")]
        [Tooltip("ส่วนที่เคลื่อนที่ (ลูก) ต้องเป็นลูกของ object นี้")]
        [SerializeField] private Transform panel;
        [Tooltip("Collider ที่ชนจริงของ Panel")]
        [SerializeField] private Collider blockingCollider;
        [Tooltip("ไม่บังคับ: ใช้บัง NavMesh ตอนประตูปิด")]
        [SerializeField] private NavMeshObstacle navObstacle;

        [Header("Animation")]
        [Tooltip("ระยะที่ Panel เลื่อนไปเมื่อเปิด (Local space ของ object นี้) ควรมากกว่าความสูงประตู")]
        [SerializeField] private Vector3 openOffset = new Vector3(0f, -3.2f, 0f);
        [SerializeField, Min(0.05f)] private float openDuration = 0.8f;
        [SerializeField, Min(0f)] private float messageCooldown = 2f;

        public event Action<Door> Opened;

        public string Id => doorId;
        public bool IsOpen => state == DoorState.Open;

        private DoorState state = DoorState.Closed;
        private Vector3 closedPosition;
        private Vector3 openPosition;
        private float progress;
        private Inventory touching;
        private float nextMessageTime;

        private string LockedMessage
        {
            get
            {
                if (mode == DoorMode.Key)
                {
                    return requiredKey != null ? "Locked: needs " + requiredKey.DisplayName : "Locked";
                }

                return "Defeat all enemies to open";
            }
        }

        private void Awake()
        {
            if (panel == null)
            {
                Debug.LogError("Door: ยังไม่ได้ลาก Panel ใส่ช่อง Panel", this);
                enabled = false;
                return;
            }

            closedPosition = panel.localPosition;
            openPosition = closedPosition + openOffset;

            if (mode == DoorMode.Key && requiredKey == null)
            {
                Debug.LogWarning("Door: โหมด Key แต่ยังไม่ได้ใส่ Required Key ประตูนี้จะไม่มีวันเปิด", this);
            }

            if (mode == DoorMode.RoomCleared && requiredRoom == null)
            {
                Debug.LogWarning("Door: โหมด Room Cleared แต่ยังไม่ได้ใส่ Required Room ประตูนี้จะไม่มีวันเปิด", this);
            }
        }

        private void OnEnable()
        {
            if (mode == DoorMode.RoomCleared && requiredRoom != null)
            {
                requiredRoom.Cleared += OnRoomCleared;
            }
        }

        private void OnDisable()
        {
            if (requiredRoom != null)
            {
                requiredRoom.Cleared -= OnRoomCleared;
            }
        }

        private void Start()
        {
            if (mode == DoorMode.RoomCleared && requiredRoom != null && requiredRoom.IsCleared)
            {
                SetOpenInstant();
            }
        }

        private void Update()
        {
            switch (state)
            {
                case DoorState.Opening:
                    UpdateOpening();
                    break;
                case DoorState.Closed:
                    if (touching != null && mode != DoorMode.RoomCleared && IsUnlockedFor(touching))
                    {
                        TryOpen(touching);
                    }
                    break;
            }
        }

        /// <summary>เปิดทันทีโดยไม่เล่นอนิเมชันและไม่หักกุญแจ (ใช้ตอนโหลดเซฟ)</summary>
        public void SetOpenInstant()
        {
            if (panel == null) return;

            state = DoorState.Open;
            panel.localPosition = openPosition;

            if (navObstacle != null) navObstacle.enabled = false;
            if (blockingCollider != null) blockingCollider.enabled = false;
        }

        private bool IsUnlockedFor(Inventory inventory)
        {
            switch (mode)
            {
                case DoorMode.Key: return requiredKey != null && inventory.Has(requiredKey);
                case DoorMode.Free: return true;
                default: return false;
            }
        }

        private void TryOpen(Inventory opener)
        {
            if (mode == DoorMode.Key && consumeKey && !opener.Remove(requiredKey)) return;

            BeginOpen();
            GameEvents.RequestMessage(mode == DoorMode.Key ? "Door unlocked" : "Door opened");
        }

        private void OnRoomCleared(Room room)
        {
            if (state != DoorState.Closed) return;

            BeginOpen();
            GameEvents.RequestMessage("Door opened");
        }

        private void BeginOpen()
        {
            state = DoorState.Opening;
            progress = 0f;

            // ปิด Obstacle ทันทีเพื่อให้ศัตรูเริ่มคำนวณเส้นทางผ่านประตูได้
            if (navObstacle != null) navObstacle.enabled = false;
        }

        private void UpdateOpening()
        {
            progress += Time.deltaTime / openDuration;
            float t = Mathf.Clamp01(progress);
            float eased = t * t * (3f - 2f * t);
            panel.localPosition = Vector3.Lerp(closedPosition, openPosition, eased);

            if (t >= 1f)
            {
                state = DoorState.Open;
                if (blockingCollider != null) blockingCollider.enabled = false;
                Opened?.Invoke(this);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            Inventory inventory = other.GetComponentInParent<Inventory>();
            if (inventory == null) return;

            touching = inventory;

            if (state == DoorState.Closed && Time.time >= nextMessageTime && !IsUnlockedFor(inventory))
            {
                nextMessageTime = Time.time + messageCooldown;
                GameEvents.RequestMessage(LockedMessage);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (touching != null && other.GetComponentInParent<Inventory>() == touching)
            {
                touching = null;
            }
        }
    }
}