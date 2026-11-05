using AWildSheepChase.Vitals;
using UnityEngine;

namespace AWildSheepChase.Items
{
    [CreateAssetMenu(fileName = "Potion", menuName = "AWildSheepChase/Items/Potion")]
    public class PotionItemData : ItemData
    {
        [SerializeField, Min(0f)] private float healAmount = 40f;
        [SerializeField, Min(0f)] private float staminaRestore;

        public float HealAmount => healAmount;
        public float StaminaRestore => staminaRestore;

        public override bool Use(GameObject user)
        {
            if (!user.TryGetComponent(out Health health)) return false;
            if (!health.IsAlive || health.Current >= health.MaxHealth) return false;

            health.Heal(healAmount);

            if (staminaRestore > 0f && user.TryGetComponent(out Stamina stamina))
            {
                stamina.Restore(staminaRestore);
            }

            return true;
        }
    }
}