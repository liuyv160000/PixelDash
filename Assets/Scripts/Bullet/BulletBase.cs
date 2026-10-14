using UnityEngine;
using PixelDash.Core;

namespace PixelDash.Bullet
{
    /// <summary>
    /// 子弹基类：speed / damage / moveDir / moveDistance + 生命周期计时器。
    /// 生命周期 = moveDistance / speed（秒），到期回收。
    /// move()：位置 += moveDir * speed * deltaTime。
    /// 物理：Rigidbody2D 恒 gravityScale = 0（子弹不受重力）。
    ///
    /// 支持对象池：由生成者注入 <see cref="OwnerPool"/> 后，寿命到期与命中都会
    /// 归还池子而不是销毁（未注入时退化为 Destroy）。
    /// </summary>
    public abstract class BulletBase : MonoBehaviour, IPoolable
    {
        [Header("子弹基础属性")]
        public float speed = 800f;                 // 速度
        public int damage = 10;                    // 伤害
        public Vector2 moveDir = Vector2.right;    // 移动方向
        public float moveDistance = 2800f;         // 最大飞行距离（与速度共同决定寿命）

        /// <summary>生命周期计时器。</summary>
        protected TimerComponent LifeTimer { get; private set; }

        /// <summary>刚体引用（gravityScale 恒为 0）。</summary>
        protected Rigidbody2D Rb { get; private set; }

        /// <summary>归属对象池（由生成者注入；为空时退化为 Destroy）。</summary>
        public ObjectPool OwnerPool { get; set; }

        /// <summary>本发子弹是否已归还/已销毁（供命中回调做幂等判断）。</summary>
        protected bool IsRecycled => _recycled;

        /// <summary>
        /// 已归还守卫。
        ///
        /// 为什么需要：一次物理步里 OnTriggerEnter2D 会**按碰撞体逐个触发** ——
        /// 子弹同时压到两个敌人（或敌人的父子两个碰撞体）时会连着调两次 <see cref="RecycleSelf"/>。
        /// 第一次已经回池、并把 <see cref="OwnerPool"/> 置空，第二次就会走 Destroy 分支，
        /// 把**已经躺在池子里**的对象销毁掉 —— 下次 Get 取到它就是 MissingReferenceException。
        /// 守卫在取出时（OnEnable / Init）复位，所以不会影响正常复用。
        /// </summary>
        private bool _recycled;

        protected virtual void Awake()
        {
            Rb = GetComponent<Rigidbody2D>();
            if (Rb == null) Rb = gameObject.AddComponent<Rigidbody2D>();
            Rb.gravityScale = 0f; // 子弹不受重力

            // 占位碰撞体：无预制体时兜底，保证命中检测能触发
            if (GetComponent<Collider2D>() == null)
                gameObject.AddComponent<CircleCollider2D>();

            // 子弹碰撞体统一走 Trigger（与敌人一致）：只做命中检测，不被物理阻挡，
            // 命中判定用 OnTriggerEnter2D。注意 GetComponentsInChildren 必须放在加完碰撞体之后。
            foreach (var c in GetComponentsInChildren<Collider2D>(true))
            {
                if (c != null) c.isTrigger = true;
            }
        }

        /// <summary>从池里取出（SetActive(true)）或首次实例化时复位守卫。</summary>
        protected virtual void OnEnable()
        {
            _recycled = false;
        }

        protected virtual void Start()
        {
            ApplyLifeTimer();
        }

        /// <summary>初始化子弹参数并（重新）启动生命周期计时。</summary>
        public virtual void Init(float spd, int dmg, Vector2 dir, float distance)
        {
            _recycled = false;      // 再次出膛：复位守卫
            speed = spd;
            damage = dmg;
            moveDir = dir.normalized;
            moveDistance = distance;
            ApplyLifeTimer();
        }

        /// <summary>确保计时器存在、设定时长并重新开始计时。</summary>
        protected void ApplyLifeTimer()
        {
            if (LifeTimer == null) LifeTimer = gameObject.AddComponent<TimerComponent>();
            LifeTimer.SetDuration(speed > 0f ? moveDistance / speed : 0f);
            LifeTimer.Restart();
        }

        protected virtual void Update()
        {
            Move();
            // 中和物理积分：保证 transform 位置不被刚体覆盖
            if (Rb != null) Rb.velocity = Vector2.zero;

            if (LifeTimer != null && LifeTimer.CheckIfEnd())
                OnLifeEnd();
        }

        /// <summary>移动：位置 += moveDir * speed * deltaTime。</summary>
        protected virtual void Move()
        {
            transform.position += (Vector3)moveDir * speed * Time.deltaTime;
        }

        /// <summary>生命周期到期：默认回收（有池回池，无池销毁）。</summary>
        protected virtual void OnLifeEnd()
        {
            RecycleSelf();
        }

        /// <summary>回收自身：有池则归还（复用），否则销毁。**幂等** —— 同一物理步可能被多个碰撞体重复触发。</summary>
        protected virtual void RecycleSelf()
        {
            if (_recycled) return;      // 已经归还/销毁过，忽略第二次
            _recycled = true;

            if (OwnerPool != null) OwnerPool.Recycle(gameObject);
            else Destroy(gameObject);
        }

        /// <summary>归还对象池时重置状态（IPoolable）。参数由生成者下次 Init 时重新注入。</summary>
        public virtual void OnRecycle()
        {
            _recycled = true;           // 兜底：即使是被池 RecycleAll 收回的，也算已回收
            OwnerPool = null;
        }
    }
}