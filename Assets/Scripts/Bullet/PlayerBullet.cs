using UnityEngine;
using PixelDash.Core;
using PixelDash.Enemy;

namespace PixelDash.Bullet
{
    /// <summary>
    /// 玩家子弹：碰敌人 → EnemyControllerBase.GetHurted(damage) + emit HIT_ENEMY。
    ///
    /// **自身不做初始化** —— 速度、伤害、方向、射程全部由生成者（技能系统）在
    /// <see cref="Init"/> 里注入。这里不能再写 Start() 去调 Init，否则外部在
    /// Instantiate 之后、Start 之前设的方向会被无条件覆盖。
    /// </summary>
    public class PlayerBullet : BulletBase
    {
        /// <summary>碰撞规则是否已应用（IgnoreLayerCollision 是全局设置，只需一次）。</summary>
        private static bool _collisionRulesApplied;

        protected override void Awake()
        {
            base.Awake();
            ApplyCollisionRules();
        }

        /// <summary>
        /// 设置 Bullet 层的碰撞对象：**只与 Player / Enemy 碰撞**。
        /// 做法是先关掉 Bullet 与全部 32 层的碰撞，再单独打开这两层。
        ///
        /// 为什么是这两层：Bullet 层同时装着玩家子弹和敌人子弹，
        ///   玩家子弹需要碰 Enemy（打敌人）
        ///   敌人子弹需要碰 Player（打玩家）
        /// 两层都打开即可，无需拆成两个 Layer ——
        /// 子弹命中后各自按组件判定（PlayerBullet 找 EnemyControllerBase、
        /// NormalAttackBullet 找 PlayerController），碰到不是目标的层会直接 return，无副作用。
        ///
        /// Trap 层（CircleSaw / Laser）不参与碰撞：那两种是环境危险物，没有 HP、不可击杀。
        ///
        /// 为什么写在代码里而不是只配 Layer Collision Matrix：矩阵是工程全局设置，
        /// 换机器、重建工程或点一下 Reset 都会丢；写在代码里规则跟着源码走。
        /// </summary>
        private static void ApplyCollisionRules()
        {
            if (_collisionRulesApplied) return;
            _collisionRulesApplied = true;

            int bullet = LayerMask.NameToLayer("Bullet");
            if (bullet < 0)
            {
                Debug.LogWarning("[PlayerBullet] 未找到 Bullet 图层，碰撞规则未设置。", null);
                return;
            }

            // 先逐层关闭（i = 32 个内置 + 自定义层的编号）
            for (int i = 0; i < 32; i++)
                Physics2D.IgnoreLayerCollision(bullet, i, true);

            // 再逐一打开需要碰撞的层
            EnableWith(bullet, "Player");
            EnableWith(bullet, "Enemy");
        }

        /// <summary>打开两个层之间的碰撞（任一层名不存在时跳过并给出提示）。</summary>
        private static void EnableWith(int layerA, string layerName)
        {
            int layerB = LayerMask.NameToLayer(layerName);
            if (layerB < 0)
            {
                Debug.LogWarning("[PlayerBullet] 未找到图层 " + layerName + "，该层碰撞未开启。", null);
                return;
            }

            Physics2D.IgnoreLayerCollision(layerA, layerB, false);
        }

        /// <summary>旋转移动方向（角度制，绕 Z 轴，相对当前方向旋转）。</summary>
        public void ChangeAngle(float angleDegrees)
        {
            moveDir = Quaternion.Euler(0f, 0f, angleDegrees) * moveDir;
            moveDir = moveDir.normalized;
        }

        /// <summary>
        /// 命中敌人 → 输出调试信息 + 造成伤害并销毁自身。
        ///
        /// 用 Trigger：敌人和子弹的碰撞体都是 Trigger（只做命中检测，不参与物理推开）。
        /// 敌人的碰撞器可能挂在子物体上，所以先找自身再往上找父级。
        /// </summary>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null) return;
            if (IsRecycled) return;   // 同一物理步已命中过（子弹同时压到两个碰撞体）→ 不重复结算伤害、不重复回收

            var enemy = other.GetComponent<EnemyControllerBase>()
                     ?? other.GetComponentInParent<EnemyControllerBase>();

            if (enemy == null) return;

            Debug.Log("[PlayerBullet] 打到敌人了 —— 伤害 " + damage
                      + "（打到的是 " + other.name + "）", this);

            enemy.GetHurted(damage);
            GameEventBus.Instance.Bullet.Emit(GameEventBus.BulletEvent.HIT_ENEMY, other.gameObject);
            RecycleSelf();   // 有池回池，无池销毁
        }
    }
}