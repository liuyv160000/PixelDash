using System;
using UnityEngine;
using PixelDash.Core;
using PixelDash.Player;

namespace PixelDash.Enemy
{
    /// <summary>
    /// 追踪导弹的可调参数集。由生成器（<see cref="TrackingMissileMaker"/>）在生成时注入，
    /// 生成器上的值是**唯一来源** —— 敌人自身的 Inspector 值只作为预制体默认值。
    /// </summary>
    [Serializable]
    public struct MissileParams
    {
        [Header("阶段时长")]
        [Tooltip("入场持续时间（秒）")]
        public float onInTime;

        [Tooltip("瞄准（锁定）持续时间（秒）")]
        public float aimingTime;

        [Tooltip("攻击持续时间（秒），到时自毁")]
        public float atkTime;

        [Header("移动")]
        [Tooltip("入场速度（on_in 阶段，向左直线飞行）")]
        public float onInSpeed;

        [Tooltip("攻击阶段起始速度")]
        public float startSpeed;

        [Tooltip("攻击阶段加速度（单位/秒²）")]
        public float accel;

        [Tooltip("攻击阶段最高速度")]
        public float maxSpeed;

        [Header("战斗数值")]
        [Tooltip("撞击玩家造成的伤害")]
        public int contactDamage;

        [Tooltip("生命值（被子弹击中扣除，≤0 死亡）")]
        public int hp;

        [Header("入场保护 / 朝向")]
        [Tooltip("入场后多久启用碰撞体（秒）。源项目 0.75 —— 这段时间内导弹不可被击中")]
        public float protectTime;

        [Tooltip("贴图朝向补偿（度）。源项目 180（导弹贴图朝左，需加 180 才对准飞行方向）")]
        public float rotateOffset;

        /// <summary>源项目默认值。</summary>
        public static MissileParams Default => new MissileParams
        {
            onInTime = 2f,
            aimingTime = 1f,
            atkTime = 3f,

            onInSpeed = 400f,
            startSpeed = 900f,
            accel = 2200f,
            maxSpeed = 1200f,

            contactDamage = 10,
            hp = 1,

            protectTime = 0.75f,
            rotateOffset = 180f,
        };
    }

    /// <summary>
    /// 追踪导弹（镜像源项目 tracking_missile，4 状态）。
    ///
    /// 行为链：
    ///   **on_in（入场）** —— 向左匀速飞入，碰撞体在 protectTime 后启用
    ///   **aiming（锁定）** —— 悬停刹车，进入本状态的瞬间**锁定一次**玩家方向
    ///   **atk（发射）** —— 沿已锁定方向冲刺：startSpeed 起步，按 accel 加速到 maxSpeed
    ///   **dead** —— 自毁/被击毁
    ///
    /// 方向只在 <see cref="St.Aiming"/> 入口锁定一次，之后不再追踪
    /// （所以是"瞄准后直线冲刺"，不是持续咬人的追踪弹）。
    ///
    /// 所有数值来自 <see cref="MissileParams"/> —— 由生成器在 <see cref="Setup"/> 时注入。
    /// </summary>
    public class TrackingMissile : EnemyControllerBase, IPoolable
    {
        private enum St { OnIn, Aiming, Atk, Dead }

        [Header("参数（生成时由 Maker 注入；此处仅为预制体默认值）")]
        [SerializeField] private MissileParams parameters = MissileParams.Default;

        /// <summary>当前生效的参数。</summary>
        public MissileParams Parameters => parameters;

        /// <summary>当前状态名（调试用）。</summary>
        public string CurrentPhase => _st.ToString();

        private St _st;
        private float _stateTimer;
        private float _atkSpeed;
        private float _protectTimer;
        private bool _protected = true;
        public Animator _anim;

        /// <summary>归属对象池（由生成器注入；为空时退化为 Destroy）。</summary>
        private ObjectPool _pool;

        /// <summary>是否已归还对象池（回收幂等，与 isDead 分开 —— 死亡延时期 isDead 已为 true）。</summary>
        private bool _recycled;

        /// <summary>由生成器调用：注入参数与对象池并立即生效。</summary>
        public void Setup(in MissileParams p, ObjectPool pool = null)
        {
            parameters = p;
            _pool = pool;
            ApplyParams();
        }

        /// <summary>把参数应用到运行时状态。</summary>
        private void ApplyParams()
        {
            enemyHp = parameters.hp;
            moveSpeed = parameters.onInSpeed;
            moveDir = Vector2.left;
            _atkSpeed = parameters.startSpeed;

            _protectTimer = parameters.protectTime;
            _protected = true;
            SetCollidersEnabled(false);   // 入场保护期间不可碰撞
        }

        private void Awake()
        {
            EnsurePhysics(new Color(1f, 0.65f, 0.15f), 0.9f);
            _anim = GetComponent<Animator>();

            // 所有碰撞体设为 Trigger —— 只做伤害检测，不参与物理推开（导弹不会把玩家顶飞）
            MakeTriggers();

            ChangeState(St.OnIn);
            ApplyParams();
        }

        private void Update()
        {
            if (isDead) return;
            float dt = Time.deltaTime;

            // 入场保护：到时启用碰撞体
            if (_protected)
            {
                _protectTimer -= dt;
                if (_protectTimer <= 0f)
                {
                    _protected = false;
                    SetCollidersEnabled(true);
                }
            }

            _stateTimer += dt;

            switch (_st)
            {
                case St.OnIn:
                    moveSpeed = parameters.onInSpeed;
                    moveDir = Vector2.left;
                    MoveBySpeed(dt);
                    if (_stateTimer >= parameters.onInTime) ChangeState(St.Aiming);
                    break;

                case St.Aiming:
                    // 锁定阶段：悬停刹车，**每帧**持续锁定玩家位置并跟随旋转
                    moveSpeed = 0f;
                    AimAtPlayer();
                    if (_stateTimer >= parameters.aimingTime) ChangeState(St.Atk);
                    break;

                case St.Atk:
                    // 发射：沿退出 aiming 时锁定的方向冲刺（此后不再调整方向）
                    _atkSpeed = Mathf.Min(parameters.maxSpeed, _atkSpeed + parameters.accel * dt);
                    moveSpeed = _atkSpeed;
                    MoveBySpeed(dt);
                    // 攻击超时 → 静默回收（不是死亡：不发爆炸音效）
                    if (_stateTimer >= parameters.atkTime) RecycleSilently();
                    break;
            }
        }

        /// <summary>
        /// 统一切换状态（只改状态与计时，不在这里做回收）。
        ///
        /// 回收时机由两条路径各自决定：
        ///   · 死亡 → <see cref="Die"/>（延迟 <c>deathRecycleDelay</c> 秒，留给死亡动画）
        ///   · 攻击超时 → <see cref="RecycleSilently"/>（立即，不发声）
        ///
        /// ⚠️ 回收动作里**不要**先写 isDead = true：基类 Recycle() 的幂等检查就是
        /// if (isDead) return，提前置位会让回收被静默跳过、对象永远留在场上。
        /// </summary>
        private void ChangeState(St next)
        {
            _st = next;
            _stateTimer = 0f;
        }

        /// <summary>
        /// 攻击超时回收：这不是死亡，只是把到期对象收回去 —— **不发爆炸音效、也没有死亡延时**。
        /// （源项目里攻击状态没有超时，本工程加的；音效只跟"死亡"绑定。）
        /// </summary>
        private void RecycleSilently()
        {
            ChangeState(St.Dead);
            RecycleToPool();      // 有池回池，无池销毁
        }

        /// <summary>
        /// 回收：有对象池则归还（复用），否则销毁。
        /// 用独立的 <c>_recycled</c> 做幂等 —— **不能用 isDead**：死亡延时期 isDead 已经是 true 了。
        /// </summary>
        private void RecycleToPool()
        {
            if (_recycled) return;
            _recycled = true;
            isDead = true;

            if (_pool != null) _pool.Recycle(gameObject);
            else Destroy(gameObject);
        }

        /// <summary>归还对象池时重置状态（IPoolable）。旋转、阶段、保护计时与动画参数都要复位。</summary>
        public void OnRecycle()
        {
            isDead = false;
            _recycled = false;

            ChangeState(St.OnIn);
            _atkSpeed = parameters.startSpeed;
            _protectTimer = parameters.protectTime;
            _protected = true;
            transform.localRotation = Quaternion.identity;

            // 动画参数复位（Bool 不会自己复位；对象池复用时最怕带着上一轮的 Boom 出来）
            if (_anim != null) _anim.SetBool("Boom", false);
        }

        /// <summary>朝向玩家：更新移动方向与旋转（旋转含贴图朝向补偿）。</summary>
        private void AimAtPlayer()
        {
            var pt = PlayerTransform;
            if (pt == null) return;

            Vector2 dir = (Vector2)pt.position - (Vector2)transform.position;
            if (dir.sqrMagnitude < 0.0001f) return;

            moveDir = dir.normalized;
            float ang = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, parameters.rotateOffset + ang);
        }

        /// <summary>
        /// hp ≤ 0 → 上报击杀（ENEMY_KILLED）→ 走统一的死亡流程 <see cref="Die"/>。
        /// 只有"被玩家子弹打死"这条路径算玩家击杀，击杀播报与击杀音效才会计数。
        /// </summary>
        protected override void DieCheck()
        {
            if (isDead || enemyHp > 0) return;

            GameEventBus.Instance.Enemy.Emit(GameEventBus.EnemyEvent.ENEMY_KILLED, gameObject);
            Die();
        }

        /// <summary>
        /// 死亡：切 Dead 状态 + 发爆炸事件 → 交给基类延迟回收（<c>deathRecycleDelay</c> 秒，留给死亡动画）。
        ///
        /// **爆炸音效只在死亡逻辑里发** —— 攻击超时回收走 <see cref="RecycleSilently"/>，不发声也不延时。
        /// </summary>
        protected override void Die()
        {
            if (_st == St.Dead) return;      // 已在死亡流程里，避免重复发爆炸
            _anim.SetBool("Boom", true);
            GameEventBus.Instance.Enemy.Emit(GameEventBus.EnemyEvent.MISSILE_BOOM, gameObject);
            ChangeState(St.Dead);
            base.Die();                      // 置 isDead + 延迟 deathRecycleDelay 秒后 → OnDeathRecycle()
        }

        /// <summary>死亡延时期满 → 归还对象池（基类默认是 Destroy，这里改成回池）。</summary>
        protected override void OnDeathRecycle()
        {
            RecycleToPool();
        }

        /// <summary>
        /// 碰到玩家 → 造成伤害，随后自身直接死亡。
        ///
        /// 两处刻意"不做"：
        ///   · 不算玩家击杀 —— 不发 ENEMY_KILLED；
        ///   · 不响玩家的受伤音效 —— withHurtSfx: false，撞击声只由导弹的爆炸事件负责，
        ///     否则"角色被击中"和"爆炸"会两个音叠在一起。
        /// </summary>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isDead) return;

            // 玩家的碰撞器可能挂在子物体上（父挂刚体 + 子挂碰撞器的架构），
            // 所以先找自身，再往上找父级。
            var p = other.GetComponent<PlayerController>()
                 ?? other.GetComponentInParent<PlayerController>();

            if (p == null) return;

            Debug.Log("[TrackingMissile] 碰到玩家了 —— 伤害 " + parameters.contactDamage
                      + "（碰到的是 " + other.name + "）", this);

            p.GetHurted(parameters.contactDamage, withHurtSfx: false);

            // 撞玩家 → 直接 Die()，**不走 DieCheck()**：这不是玩家击杀，不发 ENEMY_KILLED。
            // 爆炸事件由 Die() 发出（只有死亡逻辑才发声）。
            Die();
        }
    }
}
