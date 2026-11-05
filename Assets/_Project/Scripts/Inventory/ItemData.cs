using UnityEngine;

namespace AWildSheepChase.Items
{
    /// <summary>คลาสแม่ของไอเทมทุกชนิด สร้าง asset จากคลาสลูก (Potion, Key)</summary>
    public abstract class ItemData : ScriptableObject
    {
        [Tooltip("รหัสไอเทม ใช้ตอน Save/Load ปล่อยว่างไว้ = ตั้งจากชื่อ asset อัตโนมัติ")]
        [SerializeField] private string id;
        [SerializeField] private string displayName = "Item";
        [SerializeField, TextArea] private string description;
        [Tooltip("ปล่อยว่างได้ HUD จะใช้ช่องสี่เหลี่ยมสีตาม World Color แทน")]
        [SerializeField] private Sprite icon;
        [Tooltip("สีของลูกบอลในโลก และสีไอคอนใน HUD")]
        [SerializeField] private Color worldColor = Color.white;
        [SerializeField, Min(1)] private int maxStack = 9;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public Color WorldColor => worldColor;
        public int MaxStack => maxStack;

        /// <summary>
        /// ใช้ไอเทม คืน true ถ้าใช้สำเร็จ (Inventory จะหักจำนวนให้เอง)
        /// คืน false ถ้าใช้ไม่ได้ในตอนนี้ (เช่น HP เต็ม) จะไม่หักจำนวน
        /// </summary>
        public virtual bool Use(GameObject user) => false;

        protected virtual void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                id = name;
            }
        }
    }
}