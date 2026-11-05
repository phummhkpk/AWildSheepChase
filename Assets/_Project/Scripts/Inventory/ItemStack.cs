using UnityEngine;

namespace AWildSheepChase.Items
{
    [System.Serializable]
    public class ItemStack
    {
        [SerializeField] private ItemData item;
        [SerializeField, Min(1)] private int amount = 1;

        public ItemData Item => item;
        public int Amount => amount;

        public ItemStack()
        {
        }

        public ItemStack(ItemData item, int amount)
        {
            this.item = item;
            this.amount = amount;
        }
    }
}