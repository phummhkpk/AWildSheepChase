using AWildSheepChase.Combat;
using UnityEngine;

namespace AWildSheepChase.Enemies
{
    [RequireComponent(typeof(ProjectileLauncher))]
    public class EnemyRangedAttack : EnemyAttack
    {
        private ProjectileLauncher launcher;

        private void Awake()
        {
            launcher = GetComponent<ProjectileLauncher>();
        }

        public override void Execute(Transform target, Vector3 facing)
        {
            launcher.TryFire(facing, gameObject);
        }
    }
}