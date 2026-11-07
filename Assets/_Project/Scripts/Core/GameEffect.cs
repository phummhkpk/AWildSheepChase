namespace AWildSheepChase.Core
{
    /// <summary>รายชื่อเอฟเฟกต์ที่ระบบเกมขอได้ AudioManager และ VfxManager เป็นคนตัดสินว่าจะเล่นเสียง/ภาพอะไร</summary>
    public enum GameEffect
    {
        SwordSwing,
        SwordHit,
        RangedShot,
        ProjectileImpact,
        PlayerJump,
        PlayerDash,
        PlayerHurt,
        PlayerDeath,
        EnemyHit,
        EnemyWindup,
        EnemyDeath,
        PickupCollect,
        PotionUse,
        DoorOpen,
        CheckpointActivate,
        BossRoar,
        BossDefeated
    }
}