using UnityEngine;

namespace PixelDash.Player
{
    /// <summary>
    /// 自动射击技能：单发直射。
    /// 间隔 / 时长 / 子弹参数在 Inspector 里配置（时长填 0 = 无限）。
    /// </summary>
    public class BulletAutoSkill : BaseSkill
    {
        public override string SkillId => "bullet_auto";

        protected override void Fire()
        {
            SpawnBullet(0f);
        }

        /// <summary>编辑器里 Add Component 时的默认值（单向无限射击）。</summary>
        private void Reset()
        {
            fireInterval = 0.5f;
            duration = 0f;          // 无限
            bulletSpeed = 800f;
            bulletDamage = 10;
            bulletDistance = 2800f;
        }
    }
}
