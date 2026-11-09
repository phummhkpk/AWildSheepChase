using System;
using AWildSheepChase.Items;
using UnityEngine;

namespace AWildSheepChase.Dialogue
{
    public enum NodeFlow
    {
        /// <summary>ไป node ถัดไปในลิสต์ (ถ้าเป็น node สุดท้ายจะจบบทสนทนา)</summary>
        Next,
        /// <summary>กระโดดไป node ที่ระบุใน Jump Target</summary>
        Jump,
        /// <summary>จบบทสนทนา</summary>
        End
    }

    public enum UnmetBehavior
    {
        /// <summary>ซ่อนตัวเลือกถ้าเงื่อนไขไม่ครบ</summary>
        Hide,
        /// <summary>แสดงเป็นสีเทากดไม่ได้ พร้อมบอกว่าต้องใช้อะไร</summary>
        Disable
    }

    [Serializable]
    public class DialogueChoice
    {
        [SerializeField] private string text = "Choice";
        [Tooltip("ลำดับ node ที่จะไป (เริ่มนับที่ 0) ใส่ -1 = จบบทสนทนา")]
        [SerializeField, Min(-1)] private int targetNode = -1;

        [Header("Condition (ไม่บังคับ)")]
        [Tooltip("ต้องมีไอเทมนี้ในกระเป๋าถึงเลือกได้ (ว่าง = ไม่มีเงื่อนไข)")]
        [SerializeField] private ItemData requiredItem;
        [SerializeField, Min(1)] private int requiredAmount = 1;
        [Tooltip("ติ๊ก = หักไอเทมออกจากกระเป๋าเมื่อเลือก")]
        [SerializeField] private bool takeItem;
        [SerializeField] private UnmetBehavior whenUnmet = UnmetBehavior.Hide;

        public string Text => text;
        public int TargetNode => targetNode;
        public ItemData RequiredItem => requiredItem;
        public int RequiredAmount => requiredAmount;
        public bool TakeItem => takeItem;
        public UnmetBehavior WhenUnmet => whenUnmet;
    }   

    [Serializable]
    public class DialogueNode
    {
        [SerializeField] private string speaker = "NPC";
        [SerializeField, TextArea(2, 5)] private string text;

        [Header("Flow (ไม่ใช้ถ้ามี Choices)")]
        [SerializeField] private NodeFlow flow = NodeFlow.Next;
        [Tooltip("ใช้เมื่อ Flow = Jump (เริ่มนับที่ 0)")]
        [SerializeField, Min(0)] private int jumpTarget;

        [Header("Choices (ถ้ามี จะต้องเลือกก่อนไปต่อ)")]
        [SerializeField] private DialogueChoice[] choices = new DialogueChoice[0];

        [Header("Effects")]
        [Tooltip("แจกไอเทมตอน node นี้ขึ้น (ไม่บังคับ)")]
        [SerializeField] private ItemData giveItem;
        [SerializeField, Min(1)] private int giveAmount = 1;
        [Tooltip("เมื่อผู้เล่นมาถึง node นี้ ถือว่าบทสนทนานี้ 'จบแล้ว' (NPC จะเปลี่ยนไปใช้บทคุยซ้ำ)")]
        [SerializeField] private bool completesDialogue;
        [Header("Show Condition (ไม่บังคับ)")]
        [Tooltip("node นี้แสดงเมื่อมีไอเทมนี้ (ว่าง = แสดงเสมอ) ถ้าไม่ครบจะข้าม node นี้")]
        [SerializeField] private ItemData conditionItem;
        [SerializeField, Min(1)] private int conditionAmount = 1;
        [Tooltip("ติ๊ก = สลับเงื่อนไข: แสดงเมื่อ 'ไม่มี' ไอเทมนี้")]
        [SerializeField] private bool invertCondition;

        public string Speaker => speaker;
        public string Text => text;
        public NodeFlow Flow => flow;
        public int JumpTarget => jumpTarget;
        public DialogueChoice[] Choices => choices;
        public bool HasChoices => choices != null && choices.Length > 0;
        public ItemData GiveItem => giveItem;
        public int GiveAmount => giveAmount;
        public bool CompletesDialogue => completesDialogue;
        public ItemData ConditionItem => conditionItem;
        public int ConditionAmount => conditionAmount;
        public bool InvertCondition => invertCondition;
    }

    [CreateAssetMenu(fileName = "Dialogue", menuName = "AWildSheepChase/Dialogue/Dialogue Data")]
    public class DialogueData : ScriptableObject
    {
        [SerializeField] private DialogueNode[] nodes = new DialogueNode[0];

        public int NodeCount => nodes != null ? nodes.Length : 0;

        public bool IsValidIndex(int index) => index >= 0 && index < NodeCount;

        public DialogueNode GetNode(int index) => nodes[index];
    }
}