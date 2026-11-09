using UnityEngine;

namespace AWildSheepChase.Core
{
    /// <summary>สิ่งที่ผู้เล่นกดโต้ตอบได้ ต้องมี Collider (ไม่ใช่ Trigger) บน Layer ที่ PlayerInteractor สแกน</summary>
    public interface IInteractable
    {
        /// <summary>ข้อความบน prompt เช่น "Talk"</summary>
        string PromptText { get; }

        bool CanInteract { get; }

        void Interact(GameObject interactor);
    }
}