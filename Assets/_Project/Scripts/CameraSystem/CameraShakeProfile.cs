using UnityEngine;

namespace AWildSheepChase.CameraSystem
{
    [CreateAssetMenu(fileName = "CameraShakeProfile", menuName = "AWildSheepChase/Camera/Shake Profile")]
    public class CameraShakeProfile : ScriptableObject
    {
        [SerializeField, Min(0f)] private float intensity = 0.5f;

        public float Intensity => intensity;
    }
}