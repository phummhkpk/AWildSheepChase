using UnityEngine;

namespace AWildSheepChase.Enemies
{
    /// <summary>คลาสแม่ของการโจมตีศัตรู EnemyController จัดการจังหวะเวลา คลาสลูกจัดการผลของการโจมตี</summary>
    public abstract class EnemyAttack : MonoBehaviour
    {
        protected EnemyData Data { get; private set; }

        public void Initialize(EnemyData data)
        {
            Data = data;
        }

        /// <summary>เรียกครั้งเดียวเมื่อหมดช่วง windup facing คือทิศแนวราบที่ล็อกไว้ (normalized)</summary>
        public abstract void Execute(Transform target, Vector3 facing);
    }
}