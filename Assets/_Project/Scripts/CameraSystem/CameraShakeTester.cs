using AWildSheepChase.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AWildSheepChase.CameraSystem
{
    /// <summary>สคริปต์ทดสอบชั่วคราว: กด T = shake เบา, กด Y = shake แรง ลบทิ้งได้หลังทดสอบ</summary>
    public class CameraShakeTester : MonoBehaviour
    {
        [SerializeField] private CameraShakeProfile lightShake;
        [SerializeField] private CameraShakeProfile heavyShake;

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.tKey.wasPressedThisFrame) GameEvents.RequestCameraShake(lightShake);
            if (keyboard.yKey.wasPressedThisFrame) GameEvents.RequestCameraShake(heavyShake);
        }
    }
}