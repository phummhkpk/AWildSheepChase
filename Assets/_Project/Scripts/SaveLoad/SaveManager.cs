using System.Collections.Generic;
using AWildSheepChase.Core;
using AWildSheepChase.Dialogue;
using AWildSheepChase.Items;
using AWildSheepChase.Player;
using AWildSheepChase.Vitals;
using AWildSheepChase.World;
using UnityEngine;

namespace AWildSheepChase.SaveLoad
{
    /// <summary>เซฟอัตโนมัติเมื่อเปิดใช้ Checkpoint และโหลดเซฟตอนเริ่ม Scene (ถ้าเมนูหลักสั่งมา)</summary>
    public class SaveManager : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private Health playerHealth;
        [SerializeField] private Inventory playerInventory;
        [SerializeField] private PlayerRespawn playerRespawn;
        [SerializeField] private ItemDatabase itemDatabase;
        [Tooltip("ไม่บังคับ: ถ้าใส่ จะลบเซฟเมื่อชนะเกม")]
        [SerializeField] private GameManager gameManager;

        private Door[] doors = new Door[0];
        private Room[] rooms = new Room[0];
        private Checkpoint[] checkpoints = new Checkpoint[0];
        private ItemPickup[] scenePickups = new ItemPickup[0];
        private DialogueNpc[] npcs = new DialogueNpc[0];
        private readonly HashSet<string> collectedPickups = new HashSet<string>();
        private bool ready;

        private void Awake()
        {
            if (player == null || playerHealth == null || playerInventory == null
                || playerRespawn == null || itemDatabase == null)
            {
                Debug.LogError("SaveManager: ช่องใน Inspector ยังไม่ครบ (ต้องลาก Player ใส่ 4 ช่องแรก และใส่ Item Database)", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            Checkpoint.Activated += OnCheckpointActivated;
            ItemPickup.Collected += OnPickupCollected;
            if (gameManager != null) gameManager.StateChanged += OnGameStateChanged;
        }

        private void OnDisable()
        {
            Checkpoint.Activated -= OnCheckpointActivated;
            ItemPickup.Collected -= OnPickupCollected;
            if (gameManager != null) gameManager.StateChanged -= OnGameStateChanged;
        }

        private void Start()
        {
            // ค้นหาครั้งเดียวตอนเริ่ม (ไม่ใช้ Find ใน Update)
            doors = FindObjectsByType<Door>();
            rooms = FindObjectsByType<Room>();
            checkpoints = FindObjectsByType<Checkpoint>();
            scenePickups = FindObjectsByType<ItemPickup>();
            npcs = FindObjectsByType<DialogueNpc>();
            ready = true;

            WarnDuplicates();

            if (SaveSystem.ConsumeLoadRequest())
            {
                LoadGame();
            }
        }

        private void OnCheckpointActivated(Checkpoint checkpoint)
        {
            SaveGame(checkpoint);
        }

        private void OnPickupCollected(ItemPickup pickup)
        {
            if (!string.IsNullOrEmpty(pickup.Id))
            {
                collectedPickups.Add(pickup.Id);
            }
        }

        private void OnGameStateChanged(GameState previous, GameState next)
        {
            // จบเกมแล้ว ลบเซฟ (ปุ่ม Continue จะหายไป)
            if (next == GameState.Victory)
            {
                SaveSystem.DeleteSave();
            }
        }

        // ---------- Save ----------

        public bool SaveGame(Checkpoint activeCheckpoint)
        {
            if (!ready || activeCheckpoint == null) return false;

            SaveData data = new SaveData
            {
                savedAtUtc = System.DateTime.UtcNow.ToString("o"),
                checkpointId = activeCheckpoint.Id,
                playerHealth = playerHealth.Current
            };

            foreach (KeyValuePair<ItemData, int> pair in playerInventory.Items)
            {
                if (pair.Key == null || string.IsNullOrEmpty(pair.Key.Id)) continue;
                data.items.Add(new ItemSaveEntry(pair.Key.Id, pair.Value));
            }

            foreach (Door door in doors)
            {
                if (door != null && door.IsOpen) data.openDoorIds.Add(door.Id);
            }

            foreach (Checkpoint checkpoint in checkpoints)
            {
                if (checkpoint != null && checkpoint.HasBeenReached)
                {
                    data.reachedCheckpointIds.Add(checkpoint.Id);
                }
            }

            foreach (Room room in rooms)
            {
                if (room == null) continue;

                RoomSaveEntry entry = new RoomSaveEntry { id = room.Id, visited = room.Visited };
                room.GetDeadEnemyIndices(entry.deadEnemyIndices);
                data.rooms.Add(entry);
            }

            data.collectedPickupIds.AddRange(collectedPickups);

            foreach (DialogueNpc npc in npcs)
            {
                if (npc != null && npc.HasTalked && !string.IsNullOrEmpty(npc.Id))
                {
                    data.talkedNpcIds.Add(npc.Id);
                }
            }

            return SaveSystem.Write(data);
        }

        // ---------- Load ----------

        public void LoadGame()
        {
            if (!SaveSystem.TryRead(out SaveData data))
            {
                Debug.LogWarning("SaveManager: โหลดเซฟไม่ได้ จะเริ่มเกมใหม่");
                GameEvents.RequestMessage("Could not load save");
                return;
            }

            if (data.version != SaveData.CurrentVersion)
            {
                Debug.LogWarning("SaveManager: เวอร์ชันไฟล์เซฟไม่ตรง (" + data.version + ") จะเริ่มเกมใหม่");
                GameEvents.RequestMessage("Save is from another version");
                return;
            }

            // 1) ประตู
            HashSet<string> openDoors = new HashSet<string>(data.openDoorIds);
            foreach (Door door in doors)
            {
                if (door != null && openDoors.Contains(door.Id)) door.SetOpenInstant();
            }

            // 2) ห้องและศัตรู
            foreach (RoomSaveEntry entry in data.rooms)
            {
                Room room = FindRoom(entry.id);
                if (room != null) room.RestoreProgress(entry.visited, entry.deadEnemyIndices);
            }

            // 3) Checkpoint
            HashSet<string> reached = new HashSet<string>(data.reachedCheckpointIds);
            Checkpoint spawn = null;
            foreach (Checkpoint checkpoint in checkpoints)
            {
                if (checkpoint == null) continue;

                bool isSaved = checkpoint.Id == data.checkpointId;
                checkpoint.RestoreState(reached.Contains(checkpoint.Id) || isSaved, isSaved);
                if (isSaved) spawn = checkpoint;
            }

            // 4) กระเป๋า
            playerInventory.ClearAll();
            foreach (ItemSaveEntry entry in data.items)
            {
                ItemData item = itemDatabase.Find(entry.id);
                if (item == null)
                {
                    Debug.LogWarning("SaveManager: ไม่พบไอเทม Id '" + entry.id + "' ใน Item Database", this);
                    continue;
                }

                playerInventory.SetCount(item, entry.amount);
            }

            // 5) ของที่เก็บไปแล้ว: ลบ Pickup ที่วางใน Scene ทิ้ง
            collectedPickups.Clear();
            foreach (string id in data.collectedPickupIds)
            {
                collectedPickups.Add(id);
            }

            foreach (ItemPickup pickup in scenePickups)
            {
                if (pickup != null && !string.IsNullOrEmpty(pickup.Id) && collectedPickups.Contains(pickup.Id))
                {
                    Destroy(pickup.gameObject);
                }
            }

            // 5.5) NPC ที่เคยคุยจบแล้ว
            HashSet<string> talkedNpcs = new HashSet<string>(data.talkedNpcIds);
            foreach (DialogueNpc npc in npcs)
            {
                if (npc != null && !string.IsNullOrEmpty(npc.Id))
                {
                    npc.RestoreState(talkedNpcs.Contains(npc.Id));
                }
            }

            // 6) ผู้เล่น
            if (spawn != null)
            {
                playerRespawn.SetSpawn(spawn.RespawnPosition, spawn.RespawnRotation);
                player.Respawn(spawn.RespawnPosition, spawn.RespawnRotation);
            }
            else
            {
                Debug.LogWarning("SaveManager: ไม่พบ Checkpoint Id '" + data.checkpointId + "' ใน Scene", this);
            }

            float fraction = playerHealth.MaxHealth > 0f ? data.playerHealth / playerHealth.MaxHealth : 1f;
            playerHealth.Revive(fraction, 1f);

            GameEvents.RequestMessage("Game loaded");
        }

        private Room FindRoom(string id)
        {
            foreach (Room room in rooms)
            {
                if (room != null && room.Id == id) return room;
            }

            return null;
        }

        // ---------- Debug ----------

        private void WarnDuplicates()
        {
            List<string> ids = new List<string>();

            foreach (Door door in doors) ids.Add(door.Id);
            CheckDuplicates("Door", ids);

            ids.Clear();
            foreach (Room room in rooms) ids.Add(room.Id);
            CheckDuplicates("Room", ids);

            ids.Clear();
            foreach (Checkpoint checkpoint in checkpoints) ids.Add(checkpoint.Id);
            CheckDuplicates("Checkpoint", ids);

            ids.Clear();
            foreach (ItemPickup pickup in scenePickups)
            {
                if (!string.IsNullOrEmpty(pickup.Id)) ids.Add(pickup.Id);
            }

            CheckDuplicates("Pickup", ids);

            ids.Clear();
            foreach (DialogueNpc npc in npcs)
            {
                if (!string.IsNullOrEmpty(npc.Id)) ids.Add(npc.Id);
            }

            CheckDuplicates("NPC", ids);
        }

        private void CheckDuplicates(string kind, List<string> ids)
        {
            HashSet<string> seen = new HashSet<string>();
            foreach (string id in ids)
            {
                if (!seen.Add(id))
                {
                    Debug.LogWarning("SaveManager: " + kind + " Id ซ้ำกัน '" + id + "' เซฟจะผิดพลาด ให้แก้ให้ไม่ซ้ำ", this);
                }
            }
        }

        [ContextMenu("Print Save File Path")]
        private void PrintSavePath()
        {
            Debug.Log("Save file: " + SaveSystem.FilePath + " (มีไฟล์: " + SaveSystem.HasSave + ")");
        }

        [ContextMenu("Delete Save File")]
        private void DeleteSaveFile()
        {
            SaveSystem.DeleteSave();
            Debug.Log("ลบไฟล์เซฟแล้ว");
        }
    }
}