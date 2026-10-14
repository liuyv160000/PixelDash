using UnityEngine;
using PixelDash.Core;
using PixelDash.Player;

namespace PixelDash.Bullet
{
    /// <summary>
    /// 敌方普通攻击子弹：speed=1400 damage=10 moveDir=(-1,0) moveDistance=1400。
    /// 碰玩家 → PlayerController.GetHurted(damage) + emit HIT_PLAYER。
    /// 判定：other.name == "player" 或 GetComponent&lt;PlayerController&gt;() != null。
    /// 提供 SetSpeed / SetDamage 供敌人生成器注入参数。
    /// </summary>
    public class NormalAttackBullet : BulletBase
    {
        /// <summary>是否已被生成器注入参数（决定 Start 用注入值还是默认值）。</summary>
        private bool _externalConfig = true;

        protected override void Start()
        {
            // 生成器会先 AddComponent → SetSpeed/SetDamage（同帧），随后 Unity 调用 Start。
            // 若已注入则用注入值初始化，避免默认值 1400 覆盖管理器传入的 bullet_speed。
            if (_externalConfig)
                Init(speed, damage, Vector2.left, 1400f);
            else
                Init(1400f, 10, Vector2.left, 1400f);
        }

        /// <summary>设置速度（同步更新寿命 = moveDistance / speed）。</summary>
        public void SetSpeed(float newSpeed)
        {
            _externalConfig = true;
            speed = newSpeed;
            if (LifeTimer != null)
                LifeTimer.SetDuration(speed > 0f ? moveDistance / speed : 0f);
        }

        /// <summary>设置伤害。</summary>
        public void SetDamage(int newDamage)
        {
            _externalConfig = true;
            damage = newDamage;
        }

        /// <summary>
        /// 命中玩家 → 输出调试信息 + 造成伤害并销毁自身。
        ///
        /// 用 Trigger（与敌人/子弹的碰撞体一致）。判定不再依赖对象名，
        /// 改为按 PlayerController 组件查找，并兼容"父挂刚体 + 子挂碰撞器"的架构。
        /// </summary>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null) return;
            if (IsRecycled) return;   // 同一物理步已命中过 → 不重复结算伤害、不重复回收

            var player = other.GetComponent<PlayerController>()
                      ?? other.GetComponentInParent<PlayerController>();

            if (player == null) return;

            Debug.Log("[NormalAttackBullet] 打到玩家了 —— 伤害 " + damage
                      + "（打到的是 " + other.name + "）", this);

            player.GetHurted(damage);
            GameEventBus.Instance.Bullet.Emit(GameEventBus.BulletEvent.HIT_PLAYER, other.gameObject);
            RecycleSelf();   // 有池回池，无池销毁
        }
    }
}