using System;
using UnityEngine;
using PixelDash.Bullet;
using PixelDash.Core;
using PixelDash.Player;

namespace PixelDash.Enemy
{
    /// <summary>
    /// 普通飞行敌人的可调参数集。由生成器（<see cref="NormalOneMaker"/>）在生成时注入 ——
    /// 生成器上的值是**唯一来源**。
    /// </summary>
    [Serializable]
    public struct NormalOneParams
    {
        // ================= ① 入场 =================

        [Tooltip("入场阶段向左的移动速度")]
        public float onInSpeed;

        [Tooltip("停靠点距屏幕左边缘的距离（世界单位）。0 = 停在屏幕左边缘，正值 = 停在屏幕内更靠右处")]
        public float stopOffsetFromLeft;

        [Tooltip("入场超时保护（秒）。即使没飞到停靠点，超过这么久也强制进入攻击状态（防止视口取不到时卡死）")]
        public float onInTimeout;

        // ================= ② 攻击 =================

        [Tooltip("上下往复巡逻的速度")]
        public float patrolSpeed;

        [Tooltip("巡逻半高：从起始巡逻点（停靠点）起，单向巡逻的幅度")]
        public float patrolHalfHeight;

        [Tooltip("起始巡逻方向：1 = 向上（停靠点作为下边界，向上走满半高再折返）；-1 = 向下（停靠点作为上边界）")]
        public int patrolStartDir;

        [Tooltip("攻击状态持续时长（秒），到点转入离场状态")]
        public float attackDuration;

        [Tooltip("开火间隔（秒）")]
        public float fireInterval;

        [Tooltip("射线检测距离（世界单位）。用于判断玩家是否在开火点的正左方向上")]
        public float rayDistance;

        // ================= ③ 离场 =================

        [Tooltip("离场阶段向左的移动速度")]
        public float leaveSpeed;

        [Tooltip("离场回收边界（世界 X）。填 0 = 自动取主相机左边缘")]
        public float leaveBoundary;

        // ================= 战斗数值 =================

        [Tooltip("撞击玩家造成的伤害")]
        public int contactDamage;

        [Tooltip("生命值（被子弹击中扣除，≤0 进入回收状态）")]
        public int hp;

        [Tooltip("子弹速度")]
        public float bulletSpeed;

        [Tooltip("子弹伤害")]
        public int bulletDamage;

        [Tooltip("【已废弃】入场保护时长。现在入场即可被攻击，该值不再影响碰撞")]
        public float protectedTime;

        /// <summary>默认值（可作为调参起点）。</summary>
        public static NormalOneParams Default => new NormalOneParams
        {
            onInSpeed = 400f,
            stopOffsetFromLeft = 200f,
            onInTimeout = 6f,

            patrolSpeed = 120f,
            patrolHalfHeight = 180f,
            patrolStartDir = 1,
            attackDuration = 10f,
            fireInterval = 0.8f,
            rayDistance = 2000f,

            leaveSpeed = 600f,
            leaveBoundary = 0f,

            contactDamage = 20,
            hp = 50,
            bulletSpeed = 1400f,
            bulletDamage = 10,
            protectedTime = 0.5f,
        };
    }

    /// <summary>
    /// 普通飞行敌人 —— 四段式生命周期（对应用户设计，不是源项目的 idle/run_away 循环）：
    ///
    ///   **① OnIn 入场** —— 从主相机屏幕外**向左**飞入，不攻击；
    ///                        飞到"距屏幕左边缘 stopOffsetFromLeft"处停下 → 转入攻击。
    ///   **② Attack 攻击** —— **停止左右移动**，只做上下往复巡逻；
    ///                        用射线检测玩家是否处在开火点的**正左方向**直线上，是则开火；
    ///                        持续 attackDuration 秒后 → 转入离场。
    ///   **③ Leave 离场** —— 停止攻击、停止上下移动，**继续向左**飞；
    ///                        飞出屏幕左边缘（或中途死亡）→ 转入回收。
    ///   **④ Recycle 回收** —— 回对象池（<see cref="ObjectPool"/>）。
    ///
    /// 任何状态下 hp ≤ 0 都直接跳到 ④。
    ///
    /// 所有数值来自 <see cref="NormalOneParams"/> —— 由生成器在 <see cref="Setup"/> 时注入。
    /// </summary>
    public class NormalOne : EnemyControllerBase, IPoolable
    {
        private enum St { OnIn, Attack, Leave, Recycle }

        [Header("参数（生成时由 Maker 注入；此处仅为预制体默认值）")]
        [SerializeField] private NormalOneParams parameters = NormalOneParams.Default;

        /// <summary>当前生效的参数。</summary>
        public NormalOneParams Parameters => parameters;

        /// <summary>当前状态名（调试用，Play 时可在 Inspector 观察）。</summary>
        public string CurrentPhase => _st.ToString();

        private St _st;
        private float _stateTimer;
        private float _fireTimer;

        /// <summary>起始巡逻点 Y（进入攻击状态时的停靠点）。</summary>
        private float _patrolStartY;

        /// <summary>巡逻上边界（进入攻击状态时按起始方向算好并缓存）。</summary>
        private float _patrolTop;

        /// <summary>巡逻下边界（进入攻击状态时按起始方向算好并缓存）。</summary>
        private float _patrolBottom;

        /// <summary>巡逻方向：+1 向上，-1 向下。</summary>
        private int _patrolDir = 1;

        /// <summary>本次生成的起始巡逻方向（+1 向上 / -1 向下）。由生成器按生成序号注入。</summary>
        private int _startDir = 1;

        private ObjectPool _pool;
        private GameObject _bulletPrefab;
        private Collider2D _col;
        private Collider2D _bodyCol;
        private SpriteRenderer _sr;
        private Animator _anim;

        // ===== 动画参数名 =====
        // 必须与 Art/Animation/EnemyAnimation/NormalOneAnimation/NormalOneAC.controller 里的参数逐字一致。
        // 对应三个状态：NormalOneFlying（默认）/ NormalOneShot（一次性）/ NormalOneBoom。
        private const string AnimFlying = "Flying";   // Bool：处于飞行状态（常态播放 NormalOneFlying）
        private const string AnimDead = "Dead";       // Bool：死亡 → 播 NormalOneBoom
        private const string AnimShot = "Shot";       // Trigger：每次开火触发一次 NormalOneShot

        /// <summary>是否已归还对象池（回收幂等，与 isDead 分开 —— 死亡延时期 isDead 已为 true）。</summary>
        private bool _recycled;

        /// <summary>射线命中缓冲（复用，避免每帧分配）。</summary>
        private readonly RaycastHit2D[] _rayHits = new RaycastHit2D[8];

        // ================= 初始化 =================

        /// <summary>
        /// 由生成器调用：注入参数、对象池、子弹预制体与**本次的起始巡逻方向**。
        /// </summary>
        /// <param name="startDir">起始巡逻方向：1 = 向上（停靠点作为下边界）；-1 = 向下。
        /// 传 0 则回退到 <see cref="NormalOneParams.patrolStartDir"/>。</param>
        public void Setup(in NormalOneParams p, ObjectPool pool, int startDir, GameObject bulletPrefab = null)
        {
            parameters = p;
            _pool = pool;
            _bulletPrefab = bulletPrefab;

            // 每次生成单独指定；传 0 时退回参数里的默认方向
            if (startDir != 0) _startDir = startDir >= 0 ? 1 : -1;
            else _startDir = p.patrolStartDir >= 0 ? 1 : -1;

            ApplyParams();
        }

        /// <summary>把参数应用到运行时状态并从头开始生命周期。</summary>
        private void ApplyParams()
        {
            enemyHp = parameters.hp;

            // 入场即可被攻击：不再禁用碰撞体。
            // （源项目入场时 collider.enabled = false、protectedTime 秒后才启用，
            //  这里改为全程可被打。protectedTime 参数保留但不再影响碰撞。）
            // 显式启用是必要的 —— 池里复用的对象可能残留上一轮的禁用状态。
            SetCollidersEnabled(true);

            _fireTimer = parameters.fireInterval;   // 进入攻击状态即可开火，不必再等一个间隔
            _patrolDir = _startDir;

            // 动画状态复位：池里复用的对象会残留上一轮的参数值，Trigger 更是会一直挂着不消失
            SetAnimBool(AnimFlying, true);
            SetAnimBool(AnimDead, false);
            ResetAnimTrigger(AnimShot);

            ChangeState(St.OnIn);
        }

        private void Awake()
        {
            EnsurePhysics(new Color(1f, 0.4f, 0.4f), 1.2f);

            // 碰撞体可能挂在子物体上（父挂刚体 + 子挂碰撞器架构）
            _col = GetComponent<Collider2D>() ?? GetComponentInChildren<Collider2D>();
            _bodyCol = _col;
            _sr = GetComponent<SpriteRenderer>();
            _anim = GetComponent<Animator>();

            MakeTriggers();      // 只做伤害检测，不参与物理推开
        }

        // ================= 主循环 =================

        private void Update()
        {
            if (isDead) return;
            float dt = Time.deltaTime;

            _stateTimer += dt;
            _fireTimer += dt;

            switch (_st)
            {
                case St.OnIn:    DoOnIn(dt);    break;
                case St.Attack:  DoAttack(dt);  break;
                case St.Leave:   DoLeave(dt);   break;
                case St.Recycle: DoRecycle();   break;
            }
        }

        // ================= ① 入场 =================

        private void DoOnIn(float dt)
        {
            moveDir = Vector2.left;
            moveSpeed = parameters.onInSpeed;
            MoveBySpeed(dt);

            // 飞到"距屏幕左边缘 stopOffsetFromLeft"处即停下（超时则强制进入攻击）
            if (transform.position.x <= StopX() || _stateTimer >= parameters.onInTimeout)
                ChangeState(St.Attack);
        }

        /// <summary>停靠点 X = 主相机左边缘 + stopOffsetFromLeft。</summary>
        private float StopX()
            => ViewportAnchor.GetViewportLeftX(-800f) + parameters.stopOffsetFromLeft;

        // ================= ② 攻击 =================

        private void DoAttack(float dt)
        {
            PatrolVertical(dt);              // 只上下动，不左右移动

            if (PlayerInSight()) TryFire();

            if (_stateTimer >= parameters.attackDuration)
                ChangeState(St.Leave);
        }

        /// <summary>
        /// 上下往复巡逻：起始巡逻点（停靠点）在范围的一端，从它出发走满半高后折返。
        ///
        /// 边界在进入攻击状态时按起始方向算好并缓存 —— 不能每帧按当前 _patrolDir 重算，
        /// 否则方向一翻转边界就跟着跳变。
        /// </summary>
        private void PatrolVertical(float dt)
        {
            float y = transform.position.y + _patrolDir * parameters.patrolSpeed * dt;

            if (y >= _patrolTop) { y = _patrolTop; _patrolDir = -1; }
            else if (y <= _patrolBottom) { y = _patrolBottom; _patrolDir = 1; }

            Vector3 p = transform.position;
            transform.position = new Vector3(p.x, y, p.z);
        }

        /// <summary>
        /// 射线检测：玩家是否处在开火点的**正左方向**直线上。
        ///
        /// 从碰撞体左边缘起射线（避免起点落在自己体内），只认 PlayerController。
        /// 命中其他敌人的碰撞体不会中断查找 —— 继续往左看，直到找到玩家或超出射程。
        /// </summary>
        private bool PlayerInSight()
        {
            Vector3 muzzle = MuzzlePosition();
            int n = Physics2D.RaycastNonAlloc(muzzle, Vector2.left, _rayHits, parameters.rayDistance);

            for (int i = 0; i < n; i++)
            {
                var col = _rayHits[i].collider;
                if (col == null) continue;
                if (col.transform.IsChildOf(transform)) continue;   // 跳过自己
                if (col.GetComponentInParent<PlayerController>() != null) return true;
            }
            return false;
        }

        private void TryFire()
        {
            if (_fireTimer < parameters.fireInterval) return;
            _fireTimer = 0f;
            Fire();
        }

        /// <summary>开火点：自身位置 X + 高度中点 Y（子弹从这里射出，与射线同一条基准线）。</summary>
        private Vector3 MuzzlePosition()
        {
            float x = _bodyCol != null ? _bodyCol.bounds.min.x - 0.01f : transform.position.x;
            return new Vector3(x, AttackAnchorY(), transform.position.z);
        }

        /// <summary>
        /// 开火点的高度：自身高度的中点。
        /// 优先取精灵的世界包围盒中心，回退到碰撞体包围盒中心。
        /// </summary>
        private float AttackAnchorY()
        {
            if (_sr != null) return _sr.bounds.center.y;
            if (_bodyCol != null) return _bodyCol.bounds.center.y;
            return transform.position.y;
        }

        private void Fire()
        {
            Vector3 muzzle = MuzzlePosition();

            GameObject go;
            if (_bulletPrefab != null)
            {
                go = Instantiate(_bulletPrefab, muzzle, Quaternion.identity, transform.parent);
            }
            else
            {
                go = new GameObject("normal_attack_bullet");
                if (transform.parent != null) go.transform.SetParent(transform.parent, false);
                go.transform.position = muzzle;
                PlaceholderSprite.EnsureRenderer(go, new Color(1f, 0.15f, 0.15f), 0.4f);
            }

            var b = go.GetComponent<NormalAttackBullet>();
            if (b == null) b = go.AddComponent<NormalAttackBullet>();
            b.SetSpeed(parameters.bulletSpeed);
            b.SetDamage(parameters.bulletDamage);

            GameEventBus.Instance.Enemy.Emit(GameEventBus.EnemyEvent.NORMAL_ONE_FLY_ATTACK, gameObject);

            // 开火动画：Trigger 每次开火都能重新触发（枪口火光这种"一次一下"的效果只能用 Trigger）
            SetAnimTrigger(AnimShot);

            // 开火后还要回到 Flying 动画，所以 Flying 必须保持 true ——
            // 回程 transition 的条件就是靠它（Flying 一直是 true，播完 Shot 自然切回）
            SetAnimBool(AnimFlying, true);
        }

        // ================= ③ 离场 =================

        private void DoLeave(float dt)
        {
            moveDir = Vector2.left;
            moveSpeed = parameters.leaveSpeed;
            MoveBySpeed(dt);

            if (transform.position.x <= LeaveBoundary())
                ChangeState(St.Recycle);
        }

        /// <summary>离场回收边界：参数为 0 时自动取主相机左边缘（再往左留 100 的余量）。</summary>
        private float LeaveBoundary()
            => Mathf.Approximately(parameters.leaveBoundary, 0f)
                ? ViewportAnchor.GetViewportLeftX(-800f) - 100f
                : parameters.leaveBoundary;

        // ================= ④ 回收 =================

        private void DoRecycle()
        {
            // 离场飞出屏幕：只回收，不发死亡音效、也没有死亡延时（与 Die() 的区别就在这里）
            ChangeState(St.Recycle);
            ReleaseToPool();
        }

        private void ChangeState(St next)
        {
            _st = next;
            _stateTimer = 0f;

            // 进入攻击状态：记录起始巡逻点（停靠点），并按起始方向确定巡逻范围
            if (next == St.Attack)
            {
                _patrolStartY = transform.position.y;
                _patrolDir = _startDir;   // 本次生成指定的起始方向

                if (_patrolDir > 0)
                {
                    // 向上起步 —— 停靠点就是下边界
                    _patrolTop = _patrolStartY + parameters.patrolHalfHeight;
                    _patrolBottom = _patrolStartY;
                }
                else
                {
                    // 向下起步 —— 停靠点就是上边界
                    _patrolTop = _patrolStartY;
                    _patrolBottom = _patrolStartY - parameters.patrolHalfHeight;
                }
            }
        }

        // ================= 死亡 / 池化 =================

        /// <summary>
        /// hp ≤ 0 → 上报击杀（ENEMY_KILLED）→ 走统一的死亡流程 <see cref="Die"/>。
        /// 只有这条路径算"被玩家击杀" —— 击杀播报与击杀音效才会计数。
        /// 死亡音效（NORMAL_ONE_FLY_DEAD）不在这里发，见 <see cref="Die"/>。
        /// </summary>
        protected override void DieCheck()
        {
            if (isDead || enemyHp > 0) return;

            GameEventBus.Instance.Enemy.Emit(GameEventBus.EnemyEvent.ENEMY_KILLED, gameObject);
            Die();
        }

        /// <summary>
        /// 死亡：切到回收状态 + 发专属死亡音效 → 交给基类走**延迟回收**（<c>deathRecycleDelay</c> 秒，留给死亡动画）。
        ///
        /// 死亡音效在这里发（不在 <see cref="DieCheck"/>）—— 因为**"被玩家击杀"和"撞到玩家"两条路都要响**；
        /// 而"算不算玩家击杀"的上报（ENEMY_KILLED）仍然只在 DieCheck 里发。
        /// </summary>
        protected override void Die()
        {
            if (isDead) return;

            // 专属死亡音效：源项目 normal_one_fly.ts 的 normal_one_fly_dead，两条死亡路径共用
            GameEventBus.Instance.Enemy.Emit(GameEventBus.EnemyEvent.NORMAL_ONE_FLY_DEAD, gameObject);

            // 死亡动画参数：**只留 Dead = true，其余全部清空**
            // （Bool 不会自己复位、Trigger 会一直挂到被消费，池复用时会串味）
            SetAnimBool(AnimDead, true);
            SetAnimBool(AnimFlying, false);
            ResetAnimTrigger(AnimShot);

            ChangeState(St.Recycle);   // 状态标记（死后 Update 不再执行，Inspector 里仍能看到 CurrentPhase）
            base.Die();                // 置 isDead + 延迟 deathRecycleDelay 秒 → OnDeathRecycle()
        }

        // ================= 动画驱动（判空：没挂 Animator 时不报错，只静默跳过）=================

        /// <summary>写 Animator Bool 参数。</summary>
        private void SetAnimBool(string name, bool value)
        {
            if (_anim == null || string.IsNullOrEmpty(name)) return;
            _anim.SetBool(name, value);
        }

        /// <summary>触发 Animator Trigger（一次性动画，如开火）。</summary>
        private void SetAnimTrigger(string name)
        {
            if (_anim == null || string.IsNullOrEmpty(name)) return;
            _anim.SetTrigger(name);
        }

        /// <summary>清掉尚未被消费的 Trigger（池复用时防止上一轮残留的触发被"补播"一次）。</summary>
        private void ResetAnimTrigger(string name)
        {
            if (_anim == null || string.IsNullOrEmpty(name)) return;
            _anim.ResetTrigger(name);
        }

        /// <summary>死亡延时期满 → 归还对象池（基类默认是 Destroy，这里改成回池）。</summary>
        protected override void OnDeathRecycle()
        {
            ReleaseToPool();
        }

        /// <summary>
        /// 立刻归还对象池（无池则销毁）。
        /// 用独立的 <c>_recycled</c> 做幂等，**不能用 isDead** —— 死亡路径进入延时期时 isDead 已经是 true 了。
        /// </summary>
        private void ReleaseToPool()
        {
            if (_recycled) return;
            _recycled = true;
            isDead = true;                        // 防止回收后这一帧还有回调进来

            if (_pool != null) _pool.Recycle(gameObject);
            else Destroy(gameObject);
        }

        /// <summary>归还对象池时重置状态（IPoolable）。</summary>
        public void OnRecycle()
        {
            isDead = false;
            _recycled = false;
            _patrolDir = 1;
            _stateTimer = 0f;
            _fireTimer = 0f;
            transform.localRotation = Quaternion.identity;
        }

        // ================= 碰撞 =================

        /// <summary>
        /// 碰到玩家 → 对玩家造成伤害，随后自身直接死亡。
        ///
        /// 三处刻意"不做"：
        ///   · 不算玩家击杀 —— 不发 ENEMY_KILLED（不计击杀、不触发击杀播报）；
        ///   · 不响玩家的受伤音效 —— withHurtSfx: false，撞击声只留本敌人自己的死亡音效；
        ///   · 不在这里发死亡音效 —— 统一由 <see cref="Die"/> 发（两条死亡路径共用）。
        /// </summary>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isDead) return;

            // 玩家的碰撞器可能挂在子物体上（父挂刚体 + 子挂碰撞器架构）
            var p = other.GetComponent<PlayerController>()
                 ?? other.GetComponentInParent<PlayerController>();

            if (p == null) return;

            Debug.Log("[NormalOne] 碰到玩家了 —— 伤害 " + parameters.contactDamage
                      + "（碰到的是 " + other.name + "）", this);

            p.GetHurted(parameters.contactDamage, withHurtSfx: false);

            // 撞玩家 → 直接 Die()，**不走 DieCheck()**：这不是玩家击杀，不发 ENEMY_KILLED。
            // 死亡音效（NORMAL_ONE_FLY_DEAD）由 Die() 发出，所以撞玩家也会有。
            Die();
        }
    }
}
