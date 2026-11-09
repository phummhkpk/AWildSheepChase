using AWildSheepChase.Core;
using UnityEngine;

namespace AWildSheepChase.Dialogue
{
    /// <summary>
    /// NPC ที่คุยได้ ต้องมี Collider (ไม่ใช่ Trigger) บน Layer Interactable
    /// คุยจบ (ถึง node ที่ติ๊ก Completes Dialogue) แล้วครั้งต่อไปจะใช้บทสนทนาแบบคุยซ้ำ
    /// </summary>
    public class DialogueNpc : MonoBehaviour, IInteractable
    {
        [Tooltip("รหัส NPC ใช้ตอน Save/Load ห้ามซ้ำกัน (ปล่อยว่าง = ไม่ถูกบันทึก)")]
        [SerializeField] private string npcId = "npc";
        [SerializeField] private DialogueRunner runner;
        [SerializeField] private DialogueData firstConversation;
        [Tooltip("ห้ามมี node ที่แจกไอเทมในบทนี้ ไม่งั้นผู้เล่นจะรับซ้ำได้")]
        [SerializeField] private DialogueData repeatConversation;
        [SerializeField] private string promptText = "Talk";
        [SerializeField] private bool facePlayer = true;

        public string Id => npcId;

        /// <summary>true เมื่อเคยคุยถึง node ที่ติ๊ก Completes Dialogue แล้ว</summary>
        public bool HasTalked { get; private set; }

        public string PromptText => promptText;

        public bool CanInteract => runner != null && !runner.IsActive && firstConversation != null;

        private void Awake()
        {
            if (runner == null)
            {
                Debug.LogWarning("DialogueNpc: ยังไม่ได้ลาก DialogueRunner ใส่ช่อง Runner", this);
            }

            if (firstConversation == null)
            {
                Debug.LogWarning("DialogueNpc: ยังไม่ได้ใส่ First Conversation", this);
            }
        }

        /// <summary>กู้สถานะจากเซฟ (ไม่แจกไอเทมและไม่เล่นบทสนทนา)</summary>
        public void RestoreState(bool talked)
        {
            HasTalked = talked;
        }

        public void Interact(GameObject interactor)
        {
            if (!CanInteract) return;

            if (facePlayer)
            {
                Vector3 direction = interactor.transform.position - transform.position;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
                }
            }

            DialogueData data = HasTalked && repeatConversation != null ? repeatConversation : firstConversation;
            runner.StartDialogue(data, OnConversationCompleted);
        }

        private void OnConversationCompleted()
        {
            HasTalked = true;
        }
    }
}