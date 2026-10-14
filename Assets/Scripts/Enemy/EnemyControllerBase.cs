using System;
using System.Collections;
using UnityEngine;
using PixelDash.Core;
using PixelDash.Player;

namespace PixelDash.Enemy
{
    /// <summary>
    /// 敌人基类：镜像源项目 enemy_controler_base。
    /// enemy_hp=100、move_speed=100、move_dir；update 时按 move_dir*move_speed 位移；
    /// get_hurted(damage) → hp 减扣 → die_check()：hp ≤ 0 时发 ENEMY_KILLED（算玩家击杀）→ die() 统一死亡与回收。
    /// 非击杀死亡（碰撞玩家等）直接调 die()，不发击杀事件。
    ///
    /// 注意：本类**不定义 Awake/Update**，避免 Unity 因派生类同签名私有方法而屏蔽基类初始化。
    /// 子类需在自己的 Awake 里调用 EnsurePhysics()，在自己的 Update 里调用 MoveBySpeed(dt)。
    /// </summary>
    public abstract class EnemyControllerBase : MonoBehaviour
    {
        [Header("敌人基础属性")]
        public int enemyHp = 100;
        public float moveSpeed = 100f;
        public Vector2 moveDir = Vector2.left;

        [Tooltip("死亡后延迟多久回收（秒），留给死亡动画；0 = 立即回收。源项目 scheduleOnce(recycle, 0.5)")]
        public float deathRecycleDelay = 0.5f;

        /// <summary>死亡 / 回收标志。置位后不再接受伤害与碰撞检测（Update 与各碰撞回调开头都检查它）。</summary>
        protected bool isDead;

        private bool _physicsReady;

        /// <summary>重复组件体检是否已做过（每个实例一次）。</summary>
        private bool _dupWarned;

        // ================= 玩家引用 =================

        /// <summary>全局玩家 Transform 缓存（Unity 对象销毁后自动变 null，无需手动失效）。</summary>
        private static Transform _cachedPlayer;

        /// <summary>
        /// 解析玩家 Transform（带缓存）。
        ///
        /// 查找顺序：
        ///   ① <see cref="PlayerController"/> 组件 —— **不依赖对象名**，最可靠
        ///   ② 名为 "player" 的任意 Transform —— **大小写不敏感**（兼容 Player / PLAYER / player）
        ///
        /// 源项目用 node.parent.getChildByName("player")，Unity 没有等价 API，
        /// 所以改为「组件优先」策略；只用名字查在改名后必然静默失效。
        /// </summary>
        public static Transform ResolvePlayerTransform()
        {
            if (_cachedPlayer != null) return _cachedPlayer;

            // ① 组件优先：场景里挂了 PlayerController 的就是玩家
            var pc = FindObjectOfType<PlayerController>();
            if (pc != null)
            {
                _cachedPlayer = pc.transform;
                return _cachedPlayer;
            }

            // ② 回退：按名字找（大小写不敏感）
            foreach (var t in FindObjectsOfType<Transform>())
            {
                if (string.Equals(t.name, "player", StringComparison.OrdinalIgnoreCase))
                {
                    _cachedPlayer = t;
                    return _cachedPlayer;
                }
            }

            return null;
        }

        /// <summary>清空玩家缓存（切场景时可主动调用）。</summary>
        public static void ClearPlayerCache() => _cachedPlayer = null;

        /// <summary>玩家 Transform（懒查找 + 缓存）。</summary>
        public Transform PlayerTransform => ResolvePlayerTransform();

        // ================= 碰撞体（Trigger）=================

        /// <summary>
        /// 把自身及子物体上的**所有**碰撞体设为 Trigger，并返回它们。
        ///
        /// 敌人只做伤害检测、不参与物理推开，所以碰撞体统一走 Trigger。
        /// Unity 2D 规则：只要一方是 Trigger，事件就走 OnTrigger*，OnCollision* 完全不再触发 ——
        /// 所以**检测方（子弹、收集物）也必须用 OnTriggerEnter2D**。
        /// </summary>
        protected Collider2D[] MakeTriggers()
        {
            var cols = GetComponentsInChildren<Collider2D>(true);
            foreach (var c in cols)
            {
                if (c != null) c.isTrigger = true;
            }
            return cols;
        }

        /// <summary>启用/禁用自身及子物体上的全部碰撞体（入场保护期间用）。</summary>
        protected void SetCollidersEnabled(bool on)
        {
            foreach (var c in GetComponentsInChildren<Collider2D>(true))
            {
                if (c != null) c.enabled = on;
            }
        }

        /// <summary>确保 2D 物理与占位渲染存在（子类 Awake 中调用）。</summary>
        protected void EnsurePhysics(Color? placeholderColor = null, float worldSize = 1f)
        {
            // 防呆只做一次（复用时 EnsurePhysics 可能被再次调用）
            if (!_dupWarned)
            {
                _dupWarned = true;
                WarnIfDuplicateSelf();
            }

            if (!_physicsReady)
            {
                var rb = GetComponent<Rigidbody2D>();
                if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.gravityScale = 0f;
                rb.freezeRotation = true;

                if (GetComponent<Collider2D>() == null)
                    gameObject.AddComponent<BoxCollider2D>();

                _physicsReady = true;
            }

            if (GetComponent<Renderer>() == null)
                PlaceholderSprite.EnsureRenderer(gameObject, placeholderColor ?? new Color(1f, 0.35f, 0.35f), worldSize);
        }

        /// <summary>
        /// 防呆：同一物体上挂了两份**同类型**的敌人组件时，必须显式报出来。
        ///
        /// 为什么：生成器用 <c>GetOrAdd&lt;T&gt;()</c> 只会拿到**第一份**并给它注入参数（Setup），
        /// 另一份会用自己的默认值继续跑它自己的 Update ——
        /// 例如预警区物体上挂着两份 <see cref="BoxWarningZone"/> 时，第二份的 `_timer` 没人重置，
        /// 到期后走「无池则 Destroy」把整个物体销毁；配合对象池反复复用同一物体，
        /// 表现成"预警区越到后面越短、只闪几帧"。这类错静默且时好时坏，只能靠显式报警抓出来。
        /// </summary>
        protected void WarnIfDuplicateSelf()
        {
            var all = GetComponents<MonoBehaviour>();
            int same = 0;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == null) continue;
                if (all[i].GetType() == GetType()) same++;
            }

            if (same > 1)
            {
                Debug.LogWarning($"[{GetType().Name}] 本物体「{name}」上挂了 {same} 份同类型组件，请删掉多余的：" +
                                 "只有第一份会收到生成器注入的参数，其余的会用默认值运行（可能提前销毁物体）。", this);
            }
        }

        /// <summary>按 moveDir * moveSpeed 位移（子类 Update 中调用）。</summary>
        protected void MoveBySpeed(float dt)
        {
            transform.position += (Vector3)moveDir * (moveSpeed * dt);
        }

        /// <summary>受伤：扣血 → 死亡检查（可覆写，如电锯不可击杀、补给箱只继承接口）。</summary>
        public virtual void GetHurted(int damage)
        {
            enemyHp -= damage;
            DieCheck();
        }

        /// <summary>
        /// 死亡检测：**只在"挨打掉血"这条路径上调用**。
        /// 判定成立（hp ≤ 0）就先上报 ENEMY_KILLED（击杀播报 / 击杀音效），再交给 <see cref="Die"/> 走统一的死亡流程。
        ///
        /// 与 <see cref="Die"/> 的分工：
        ///   · 本方法 = **判定"算不算玩家击杀"并上报**；
        ///   · <see cref="Die"/> = **怎么死、怎么回收**。
        ///
        /// 因此非击杀死亡（碰撞玩家、出屏、自爆）请**直接调 <see cref="Die"/>**，
        /// 走这里会误报击杀（击杀数被多算、击杀音效误响）。
        /// </summary>
        protected virtual void DieCheck()
        {
            if (isDead || enemyHp > 0) return;
            GameEventBus.Instance.Enemy.Emit(GameEventBus.EnemyEvent.ENEMY_KILLED, gameObject);
            Die();
        }

        /// <summary>
        /// 死亡：进入死亡状态 → 执行死亡逻辑 → **延迟 <see cref="deathRecycleDelay"/> 秒后回收**（留给死亡动画）。
        ///
        /// 两条路径共用本方法：
        ///   ① <see cref="DieCheck"/> —— hp ≤ 0，算玩家击杀（事件在 DieCheck 里已发）；
        ///   ② 碰撞玩家 / 自爆 —— 直接调用，**不发任何击杀上报**。
        ///
        /// 延迟期间 <c>isDead = true</c>：敌人原地不动、不再开火、碰撞回调全部忽略，
        /// 但对象还活着（动画能播）。到期后走 <see cref="OnDeathRecycle"/> 真正回收。
        /// </summary>
        protected virtual void Die()
        {
            if (isDead) return;
            isDead = true;                       // 立刻停行为、停碰撞响应

            if (deathRecycleDelay <= 0f)
            {
                OnDeathRecycle();
                return;
            }
            StartCoroutine(DeathRecycleRoutine(deathRecycleDelay));
        }

        /// <summary>
        /// 死亡延时期满后的回收动作。基类 = 销毁；接入对象池的子类覆写为"归还对象池"。
        /// ⚠️ 这里**不要再判 isDead** —— 走到这一行时 isDead 已经是 true（判了就永远不回收）。
        /// </summary>
        protected virtual void OnDeathRecycle() => Destroy(gameObject);

        /// <summary>死亡延时：等够时间再回收（用协程而不是 Destroy(go, t)/Invoke，回池后不会留下幽灵回调）。</summary>
        private IEnumerator DeathRecycleRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (!isDead) yield break;            // 期间已被别的路径回收（回池会清 isDead）→ 不再重复回收
            OnDeathRecycle();
        }

        /// <summary>立即销毁（不走上报、无死亡延时）—— 用于出屏等非死亡回收，别用于"死亡"。</summary>
        protected void Recycle()
        {
            if (isDead) return;
            isDead = true;
            Destroy(gameObject);
        }
    }
}
