using AWildSheepChase.Core;
using AWildSheepChase.Dialogue;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace AWildSheepChase.UI
{
    /// <summary>
    /// กล่องบทสนทนา: ตัวหนังสือค่อย ๆ ขึ้น (ใช้เวลาจริง ทำงานได้ตอนเกมหยุด) และปุ่มตัวเลือก
    /// ต้องวางบน object ที่เปิดอยู่เสมอ (เช่น Canvas ชื่อ HUD) ห้ามวางบนตัวกล่องที่ถูกซ่อน
    /// </summary>
    public class DialogueUI : MonoBehaviour
    {
        [SerializeField] private DialogueRunner runner;
        [SerializeField] private UIPanel panel;
        [SerializeField] private TMP_Text speakerText;
        [SerializeField] private TMP_Text bodyText;
        [Tooltip("สัญลักษณ์ที่โผล่เมื่อข้อความขึ้นครบและกดไปต่อได้ (ไม่บังคับ)")]
        [SerializeField] private GameObject continueIndicator;
        [Tooltip("ปุ่มตัวเลือก (จำนวนปุ่ม = จำนวนตัวเลือกสูงสุดที่แสดงได้)")]
        [SerializeField] private Button[] choiceButtons = new Button[0];
        [SerializeField, Min(1f)] private float charactersPerSecond = 45f;
        [Tooltip("เล่นเสียงบลิปทุกกี่ตัวอักษร (0 = ปิดเสียง)")]
        [SerializeField, Min(0)] private int blipEveryCharacters = 2;

        private InputAction advanceAction;
        private UnityAction[] choiceHandlers;
        private DialogueNode currentNode;
        private bool typing;
        private float progress;
        private int totalCharacters;
        private int shownFrame = -1;
        private int lastBlipIndex;
        // ปุ่มที่ i บนจอ -> ลำดับตัวเลือกในไฟล์ข้อมูล (เพราะตัวเลือกที่ซ่อนอยู่ถูกข้าม)
        private int[] choiceMap = new int[0];

        private void Awake()
        {
            advanceAction = new InputAction("DialogueAdvance", InputActionType.Button);
            advanceAction.AddBinding("<Keyboard>/space");
            advanceAction.AddBinding("<Keyboard>/enter");
            advanceAction.AddBinding("<Mouse>/leftButton");
            advanceAction.AddBinding("<Gamepad>/buttonSouth");

            choiceHandlers = new UnityAction[choiceButtons.Length];
            choiceMap = new int[choiceButtons.Length];
            for (int i = 0; i < choiceButtons.Length; i++)
            {
                int index = i;
                choiceHandlers[i] = () => OnChoiceClicked(index);
            }

            if (runner == null || panel == null || bodyText == null)
            {
                Debug.LogWarning("DialogueUI: ช่อง Runner / Panel / Body Text ยังไม่ครบ", this);
            }
        }

        private void OnEnable()
        {
            if (runner != null)
            {
                runner.NodeShown += OnNodeShown;
                runner.Ended += OnEnded;
            }

            for (int i = 0; i < choiceButtons.Length; i++)
            {
                if (choiceButtons[i] != null) choiceButtons[i].onClick.AddListener(choiceHandlers[i]);
            }

            advanceAction.performed += OnAdvancePerformed;
            advanceAction.Enable();
        }

        private void OnDisable()
        {
            if (runner != null)
            {
                runner.NodeShown -= OnNodeShown;
                runner.Ended -= OnEnded;
            }

            for (int i = 0; i < choiceButtons.Length; i++)
            {
                if (choiceButtons[i] != null) choiceButtons[i].onClick.RemoveListener(choiceHandlers[i]);
            }

            advanceAction.performed -= OnAdvancePerformed;
            advanceAction.Disable();
        }

        private void OnDestroy()
        {
            advanceAction?.Dispose();
        }

        private void Start()
        {
            HideChoices();
            if (continueIndicator != null) continueIndicator.SetActive(false);
            if (panel != null) panel.Hide();
        }

        private void Update()
        {
            if (!typing) return;

            progress += charactersPerSecond * Time.unscaledDeltaTime;
            int visible = Mathf.FloorToInt(progress);

            if (visible >= totalCharacters)
            {
                FinishTyping();
                return;
            }

            bodyText.maxVisibleCharacters = visible;
            PlayBlips(visible);
        }

        private void OnNodeShown(DialogueNode node)
        {
            currentNode = node;

            // เปิดกล่องก่อนใส่ข้อความ เพื่อให้ TMP คำนวณจำนวนตัวอักษรได้ถูกต้อง
            if (!panel.IsVisible) panel.Show();

            shownFrame = Time.frameCount;

            if (speakerText != null) speakerText.text = node.Speaker;

            bodyText.text = node.Text;
            bodyText.ForceMeshUpdate();
            totalCharacters = bodyText.textInfo.characterCount;
            bodyText.maxVisibleCharacters = 0;

            progress = 0f;
            lastBlipIndex = 0;
            typing = true;

            HideChoices();
            if (continueIndicator != null) continueIndicator.SetActive(false);

            if (totalCharacters == 0)
            {
                FinishTyping();
            }
        }

        private void OnEnded()
        {
            typing = false;
            currentNode = null;

            HideChoices();
            if (continueIndicator != null) continueIndicator.SetActive(false);
            if (panel != null) panel.Hide();

            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        private void OnAdvancePerformed(InputAction.CallbackContext ctx)
        {
            if (panel == null || !panel.IsVisible || currentNode == null) return;
            if (Time.frameCount == shownFrame) return;

            if (typing)
            {
                FinishTyping();
                return;
            }

            // มีตัวเลือก: ต้องเลือกด้วยปุ่ม (การคลิกปุ่มไม่นับเป็นการกดข้าม)
            if (currentNode.HasChoices) return;

            runner.Advance();
        }

        private void FinishTyping()
        {
            typing = false;
            bodyText.maxVisibleCharacters = int.MaxValue;

            if (currentNode == null) return;

            if (currentNode.HasChoices)
            {
                ShowChoices(currentNode);
            }
            else if (continueIndicator != null)
            {
                continueIndicator.SetActive(true);
            }
        }

        private void ShowChoices(DialogueNode node)
        {
            int slot = 0;
            Button firstInteractable = null;

            for (int i = 0; i < node.Choices.Length; i++)
            {
                DialogueChoice choice = node.Choices[i];
                if (!runner.IsChoiceVisible(choice)) continue;

                if (slot >= choiceButtons.Length)
                {
                    Debug.LogWarning("DialogueUI: ตัวเลือกที่แสดงได้มากกว่าจำนวนปุ่ม (" + choiceButtons.Length
                        + ") ตัวเลือกส่วนเกินจะไม่แสดง", this);
                    break;
                }

                Button button = choiceButtons[slot];
                choiceMap[slot] = i;

                bool available = runner.IsChoiceAvailable(choice);
                button.gameObject.SetActive(true);
                button.interactable = available;

                TMP_Text label = button.GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    label.text = available || choice.RequiredItem == null
                        ? choice.Text
                        : choice.Text + " (need " + choice.RequiredItem.DisplayName + ")";
                }

                if (available && firstInteractable == null) firstInteractable = button;
                slot++;
            }

            // ปุ่มที่เหลือซ่อนไว้
            for (int i = slot; i < choiceButtons.Length; i++)
            {
                if (choiceButtons[i] != null) choiceButtons[i].gameObject.SetActive(false);
            }

            if (firstInteractable == null)
            {
                Debug.LogWarning("DialogueUI: node นี้ไม่มีตัวเลือกที่เลือกได้เลย ผู้เล่นจะติด ให้เพิ่มตัวเลือกที่ไม่มีเงื่อนไขอย่างน้อย 1 อัน", this);
            }
            else if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(firstInteractable.gameObject);
            }
        }

        private void HideChoices()
        {
            foreach (Button button in choiceButtons)
            {
                if (button != null) button.gameObject.SetActive(false);
            }
        }

        private void OnChoiceClicked(int buttonIndex)
        {
            if (typing || currentNode == null) return;
            if (buttonIndex < 0 || buttonIndex >= choiceMap.Length) return;

            runner.Choose(choiceMap[buttonIndex]);
        }

        private void PlayBlips(int visible)
        {
            if (blipEveryCharacters <= 0 || visible - lastBlipIndex < blipEveryCharacters) return;

            lastBlipIndex = visible;

            int index = Mathf.Min(visible, bodyText.textInfo.characterCount) - 1;
            if (index < 0) return;

            // ข้ามช่องว่าง ไม่ต้องมีเสียง
            if (char.IsWhiteSpace(bodyText.textInfo.characterInfo[index].character)) return;

            GameEvents.RequestEffect(GameEffect.DialogueBlip, Vector3.zero);
        }
    }
}