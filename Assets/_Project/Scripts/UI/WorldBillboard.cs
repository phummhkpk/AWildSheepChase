using UnityEngine;

namespace AWildSheepChase.UI
{
    /// <summary>หัน object (เช่น Canvas แบบ World Space) เข้าหากล้องตลอดเวลา</summary>
    public class WorldBillboard : MonoBehaviour
    {
        private Camera viewCamera;

        private void LateUpdate()
        {
            if (viewCamera == null)
            {
                viewCamera = Camera.main;
                if (viewCamera == null) return;
            }

            transform.rotation = viewCamera.transform.rotation;
        }
    }
}