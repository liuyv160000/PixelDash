using UnityEngine;

namespace PixelDash.Player
{
    /// <summary>
    /// 三发散射自动射击：每次同时发射 -spreadAngle / 0 / +spreadAngle 三发。
    /// 间隔 / 时长 / 散射角都在 Inspector 里配置。
    /// </summary>
    public class TripleBulletAutoSkill : BaseSkill
    {
        public override string SkillId => "bullet_triple";

        [Header("三发散射")]
        [Tooltip("散射角（度）。三发分别取 -该值 / 0 / +该值")]
        [SerializeField] private float spreadAngle = 15f;

        protected override void Fire()
        {
            SpawnBullet(-spreadAngle);
            SpawnBullet(0f);
            SpawnBullet(spreadAngle);
        }

        /// <summary>编辑器里 Add Component 时的默认值。</summary>
        private void Reset()
        {
            fireInterval = 0.5f;
            duration = 20f;
            spreadAngle = 15f;
            bulletSpeed = 800f;
            bulletDamage = 10;
            bulletDistance = 2800f;
        }
    }
}
