using AWildSheepChase.Core;
using Unity.Cinemachine;
using UnityEngine;

namespace AWildSheepChase.CameraSystem
{
    [RequireComponent(typeof(CinemachineImpulseSource))]
    public class CameraShaker : MonoBehaviour
    {
        private CinemachineImpulseSource impulseSource;

        private void Awake()
        {
            impulseSource = GetComponent<CinemachineImpulseSource>();
        }

        private void OnEnable()
        {
            GameEvents.CameraShakeRequested += Shake;
        }

        private void OnDisable()
        {
            GameEvents.CameraShakeRequested -= Shake;
        }

        public void Shake(CameraShakeProfile profile)
        {
            if (profile == null) return;

            // สุ่มทิศทางสั่นทุกครั้ง ให้ความรู้สึกไม่ซ้ำเดิม
            Vector3 direction = Random.onUnitSphere;
            impulseSource.GenerateImpulseWithVelocity(direction * profile.Intensity);
        }
    }
}