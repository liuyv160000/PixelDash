using System.Collections.Generic;
using UnityEngine;

namespace PixelDash.Player
{
    /// <summary>
    /// 三连发技能：每轮先立即发射一发，其余几发按 <see cref="burstDelays"/> 依次延迟发射。
    /// 间隔 / 时长 / 延迟序列都在 Inspector 里配置。
    /// </summary>
    public class MultiBulletSkill : BaseSkill
    {
        public override string SkillId => "bullet_multi";

        [Header("三连发")]
        [Tooltip("每轮连发中后续子弹的延迟（秒），按顺序依次发射。例如 0.1 / 0.2 表示首发立即、第二发 0.1s 后、第三发 0.3s 后")]
        [SerializeField] private float[] burstDelays = new float[] { 0.1f, 0.2f };

        /// <summary>待发射的延迟队列（运行时倒计时）。</summary>
        private readonly List<float> _pending = new List<float>();

        protected override void Fire()
        {
            SpawnBullet(0f);                          // 首发立即

            foreach (var d in burstDelays)
                _pending.Add(d);                      // 其余按延迟排队
        }

        protected override void OnActivate() => _pending.Clear();

        protected override void OnDeactivate() => _pending.Clear();

        public override void UpdateSkill(float dt)
        {
            base.UpdateSkill(dt);

            if (!IsActive)
            {
                _pending.Clear();
                return;
            }

            // 倒计时推进：到点就补发一发
            for (int i = _pending.Count - 1; i >= 0; i--)
            {
                _pending[i] -= dt;
                if (_pending[i] <= 0f)
                {
                    SpawnBullet(0f);
                    _pending.RemoveAt(i);
                }
            }
        }

        /// <summary>编辑器里 Add Component 时的默认值。</summary>
        private void Reset()
        {
            fireInterval = 0.5f;
            duration = 20f;
            burstDelays = new float[] { 0.1f, 0.2f };
            bulletSpeed = 800f;
            bulletDamage = 10;
            bulletDistance = 2800f;
        }
    }
}
