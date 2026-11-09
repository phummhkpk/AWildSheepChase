using System;
using System.Collections;
using AWildSheepChase.Core;
using AWildSheepChase.Items;
using AWildSheepChase.Player;
using UnityEngine;

namespace AWildSheepChase.Dialogue
{
    /// <summary>เดินบทสนทนา: หยุดเกม ปิด input ผู้เล่น แจกไอเทม และยิง event ให้ UI (ไม่ใช่ Singleton ลากอ้างอิงผ่าน Inspector)</summary>
    public class DialogueRunner : MonoBehaviour
    {
        [SerializeField] private PlayerInputHandler playerInput;
        [SerializeField] private Inventory playerInventory;

        public event Action<DialogueData> Started;
        public event Action<DialogueNode> NodeShown;
        public event Action Ended;

        public bool IsActive { get; private set; }
        public DialogueNode CurrentNode { get; private set; }

        private DialogueData current;
        private int currentIndex;
        private Action completionCallback;
        private Coroutine reenableRoutine;

        private void Awake()
        {
            if (playerInput == null)
            {
                Debug.LogWarning("DialogueRunner: ยังไม่ได้ลาก Player ใส่ช่อง Player Input (กดข้ามบทสนทนาอาจไปฟัน/กระโดดด้วย)", this);
            }

            if (playerInventory == null)
            {
                Debug.LogWarning("DialogueRunner: ยังไม่ได้ลาก Player ใส่ช่อง Player Inventory (บทสนทนาจะแจกไอเทมไม่ได้)", this);
            }
        }

        private void OnDisable()
        {
            if (reenableRoutine != null)
            {
                StopCoroutine(reenableRoutine);
                reenableRoutine = null;
                SetPlayerInput(true);
            }

            if (!IsActive) return;

            IsActive = false;
            CurrentNode = null;
            current = null;
            completionCallback = null;
            GameTime.SetPaused(false);
            SetPlayerInput(true);
            Ended?.Invoke();
        }

        /// <summary>เริ่มบทสนทนา คืน false ถ้าเริ่มไม่ได้ onCompleted จะถูกเรียกเมื่อผู้เล่นมาถึง node ที่ติ๊ก Completes Dialogue</summary>
        public bool StartDialogue(DialogueData data, Action onCompleted = null)
        {
            if (IsActive || GameTime.IsPaused || data == null || data.NodeCount == 0) return false;

            if (reenableRoutine != null)
            {
                StopCoroutine(reenableRoutine);
                reenableRoutine = null;
            }

            current = data;
            completionCallback = onCompleted;
            IsActive = true;

            GameTime.SetPaused(true);
            SetPlayerInput(false);

            Started?.Invoke(data);
            ShowNode(0);
            return true;
        }

        /// <summary>ไปต่อ (UI เรียกเมื่อผู้เล่นกดข้าม) ไม่ทำอะไรถ้า node นี้มีตัวเลือก</summary>
        public void Advance()
        {
            if (!IsActive || CurrentNode == null || CurrentNode.HasChoices) return;

            switch (CurrentNode.Flow)
            {
                case NodeFlow.Next:
                    if (current.IsValidIndex(currentIndex + 1)) ShowNode(currentIndex + 1);
                    else End();
                    break;
                case NodeFlow.Jump:
                    ShowNode(CurrentNode.JumpTarget);
                    break;
                default:
                    End();
                    break;
            }
        }

                /// <summary>เงื่อนไขของตัวเลือกครบหรือไม่ (ไม่มีเงื่อนไข = ครบเสมอ)</summary>
        public bool IsChoiceAvailable(DialogueChoice choice)
        {
            if (choice.RequiredItem == null) return true;
            if (playerInventory == null) return false;

            return playerInventory.Has(choice.RequiredItem, choice.RequiredAmount);
        }

        /// <summary>ตัวเลือกนี้ควรแสดงบนจอหรือไม่</summary>
        public bool IsChoiceVisible(DialogueChoice choice)
        {
            return choice.WhenUnmet == UnmetBehavior.Disable || IsChoiceAvailable(choice);
        }

        public void Choose(int choiceIndex)
        {
            if (!IsActive || CurrentNode == null || !CurrentNode.HasChoices) return;
            if (choiceIndex < 0 || choiceIndex >= CurrentNode.Choices.Length) return;

            DialogueChoice choice = CurrentNode.Choices[choiceIndex];
            if (!IsChoiceAvailable(choice)) return;

            if (choice.TakeItem && choice.RequiredItem != null)
            {
                playerInventory.Remove(choice.RequiredItem, choice.RequiredAmount);
                GameEvents.RequestMessage("-" + choice.RequiredAmount + " " + choice.RequiredItem.DisplayName);
            }

            if (choice.TargetNode < 0) End();
            else ShowNode(choice.TargetNode);
        }

        private void ShowNode(int index)
        {
            // ข้าม node ที่เงื่อนไขไม่ผ่าน (จำกัดจำนวนรอบกัน Jump วนไม่รู้จบ)
            int guard = current.NodeCount + 1;
            while (current.IsValidIndex(index) && !IsNodeVisible(current.GetNode(index)) && guard-- > 0)
            {
                DialogueNode skipped = current.GetNode(index);

                if (skipped.HasChoices) { index = -1; break; }

                switch (skipped.Flow)
                {
                    case NodeFlow.Next: index = index + 1; break;
                    case NodeFlow.Jump: index = skipped.JumpTarget; break;
                    default: index = -1; break;
                }
            }

            if (guard < 0 || !current.IsValidIndex(index))
            {
                if (index >= 0 || guard < 0)
                {
                    Debug.LogWarning("DialogueRunner: ไม่มี node ที่แสดงได้ใน " + current.name + " (จบบทสนทนา)", this);
                }

                End();
                return;
            }

            currentIndex = index;
            CurrentNode = current.GetNode(index);

            GiveItem(CurrentNode);

            if (CurrentNode.CompletesDialogue && completionCallback != null)
            {
                Action callback = completionCallback;
                completionCallback = null;
                callback();
            }

            NodeShown?.Invoke(CurrentNode);
        }

        private void GiveItem(DialogueNode node)
        {
            if (node.GiveItem == null || playerInventory == null) return;

            int added = playerInventory.Add(node.GiveItem, node.GiveAmount);
            if (added <= 0)
            {
                GameEvents.RequestMessage("Inventory full: " + node.GiveItem.DisplayName);
            }
        }

        private void End()
        {
            if (!IsActive) return;

            IsActive = false;
            CurrentNode = null;
            current = null;
            completionCallback = null;

            GameTime.SetPaused(false);
            Ended?.Invoke();

            // คืน input ช้าไป 1 เฟรม กันปุ่มที่ใช้ข้ามประโยคสุดท้ายหลุดไปเป็นกระโดด/ฟัน
            reenableRoutine = StartCoroutine(ReenableInputNextFrame());
        }

        private IEnumerator ReenableInputNextFrame()
        {
            yield return null;

            reenableRoutine = null;
            SetPlayerInput(true);
        }

        private void SetPlayerInput(bool enabled)
        {
            if (playerInput != null)
            {
                playerInput.enabled = enabled;
            }
        }

        private bool IsNodeVisible(DialogueNode node)
        {
            if (node.ConditionItem == null) return true;

            bool has = playerInventory != null
                && playerInventory.Has(node.ConditionItem, node.ConditionAmount);
            return node.InvertCondition ? !has : has;
        }
    }
}