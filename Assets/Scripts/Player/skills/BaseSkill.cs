using UnityEngine;
using PixelDash.Bullet;
using PixelDash.Core;

namespace PixelDash.Player
{
    /// <summary>
    /// 技能基类。只提供通用骨架：启用状态、计时、开火间隔与时长、子弹发射。
    ///
    /// **所有可调数值都是 [SerializeField]** —— 在 Inspector 里配置。
    /// 注意：技能组件需要**手动挂在 Player 节点上**才会在 Inspector 里出现；
    /// 若靠 <see cref="SkillSystem"/> 运行时 AddComponent，改的值不会保留。
    ///
    /// 子类只需实现 <see cref="SkillId"/> 与 <see cref="Fire"/>。
    /// </summary>
    public abstract class BaseSkill : MonoBehaviour
    {
        /// <summary>技能 ID（与技能球的 SkillId 一一对应）。</summary>
        public abstract string SkillId { get; }

        // ================= 子弹 =================

        [Header("子弹")]
        [Tooltip("玩家子弹预制体在 Resources 下的路径（不含扩展名）")]
        [SerializeField] protected string bulletPrefabPath = "Prefabs/Bullet/PlayerBullet/NormalPlayerBullet";

        [Tooltip("子弹速度（世界单位/秒）")]
        [SerializeField] protected float bulletSpeed = 800f;

        [Tooltip("子弹伤害")]
        [SerializeField] protected int bulletDamage = 10;

        [Tooltip("子弹最大飞行距离（与速度共同决定寿命）")]
        [SerializeField] protected float bulletDistance = 2800f;

        [Tooltip("出膛点的额外前移量（在\"玩家半宽\"之外再往前推这么多；0 = 正好贴着身前）")]
        [SerializeField] protected float spawnForwardOffset = 0f;

        // ================= 技能时序 =================

        [Header("技能时序")]
        [Tooltip("开火间隔（秒）")]
        [SerializeField] protected float fireInterval = 0.5f;

        [Tooltip("技能持续时间（秒）；0 = 无限持续，直到被显式关闭")]
        [SerializeField] protected float duration = 0f;

        // ================= 对象池 =================

        [Header("对象池")]
        [Tooltip("子弹对象池初始容量（进入游戏时预先创建这么多）")]
        [SerializeField] protected int poolInitialSize = 10;

        [Tooltip("子弹对象池上限（超出后池不再扩充，超发的子弹会改为直接销毁）")]
        [SerializeField] protected int poolMaxSize = 60;

        // ================= 运行时状态 =================

        protected PlayerController _owner;

        private GameObject _bulletPrefab;
        private ObjectPool _bulletPool;
        private bool _active;
        private float _fireTimer;
        private float _elapsed;

        /// <summary>当前是否处于启用状态。</summary>
        public bool IsActive => _active;

        /// <summary>由 <see cref="SkillSystem"/> 调用：绑定玩家、加载子弹预制体并建好专属对象池。</summary>
        public virtual void Init(PlayerController owner)
        {
            _owner = owner;

            // 玩家子弹按约定从 Resources 加载（路径可在 Inspector 覆盖）
            _bulletPrefab = string.IsNullOrEmpty(bulletPrefabPath)
                ? null
                : Resources.Load<GameObject>(bulletPrefabPath);

            if (_bulletPrefab == null)
            {
                Debug.LogWarning("[BaseSkill] 子弹预制体加载失败，路径=" + bulletPrefabPath, this);
                return;
            }

            // 专属池：在玩家节点下建一个 pool_<技能类名> 子节点，
            // 避免多个技能挂同一节点时共用池互相覆盖（与敌人 Maker 的 CreateOwnPool 同一思路）
            var holder = new GameObject("pool_" + GetType().Name);
            holder.transform.SetParent(owner.transform, false);

            _bulletPool = holder.AddComponent<ObjectPool>();
            _bulletPool.Configure(_bulletPrefab, poolInitialSize, poolMaxSize);
        }

        public virtual void EnableSkill()
        {
            _active = true;
            _fireTimer = 0f;
            _elapsed = 0f;
            OnActivate();
        }

        public virtual void DisableSkill()
        {
            if (!_active) return;
            _active = false;
            OnDeactivate();
        }

        public virtual void UpdateSkill(float dt)
        {
            if (!_active) return;

            _elapsed += dt;
            if (duration > 0f && _elapsed >= duration)
            {
                DisableSkill();
                return;
            }

            if (fireInterval <= 0f) return;

            _fireTimer += dt;
            while (_fireTimer >= fireInterval)
            {
                _fireTimer -= fireInterval;
                Fire();
            }
        }

        /// <summary>启用时的回调（可选覆写）。</summary>
        protected virtual void OnActivate() { }

        /// <summary>关闭时的回调（可选覆写）。</summary>
        protected virtual void OnDeactivate() { }

        /// <summary>一次开火。子类实现具体的发射方式。</summary>
        protected abstract void Fire();

        /// <summary>
        /// 朝玩家面朝方向发射一发子弹。
        ///
        /// 全部参数在这里**显式注入**给子弹 —— PlayerBullet 不再自己初始化，
        /// 否则它 Start() 里的硬编码会覆盖掉这里设的方向与速度。
        /// </summary>
        /// <param name="relativeAngleDeg">相对面朝方向的偏离角（度）。0 = 正前方。</param>
        protected void SpawnBullet(float relativeAngleDeg)
        {
            if (_bulletPrefab == null || _owner == null) return;

            Vector3 muzzle = MuzzlePosition();

            GameObject go;
            if (_bulletPool != null)
            {
                go = _bulletPool.Get(muzzle);
                if (go == null) return;                 // 池已达上限，本发跳过

                // 子弹是独立飞行物，不能挂在玩家下面（否则会跟着玩家跑）
                go.transform.SetParent(null, true);
                go.transform.rotation = Quaternion.identity;
            }
            else
            {
                go = Instantiate(_bulletPrefab, muzzle, Quaternion.identity);
            }

            var bullet = go.GetComponent<PlayerBullet>();
            if (bullet == null) bullet = go.AddComponent<PlayerBullet>();

            bullet.OwnerPool = _bulletPool;             // 命中/寿命到期时归还到这个池

            // 面朝右 = 0°，面朝左 = 180°，再叠加相对角
            float baseAngle = _owner.FaceDir == 1 ? 0f : 180f;
            float rad = (baseAngle + relativeAngleDeg) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            bullet.Init(bulletSpeed, bulletDamage, dir, bulletDistance);

            // 上报开火（源项目在技能里 emit player_event.FIRE，携带刚发射的子弹）
            GameEventBus.Instance.Player.Emit(GameEventBus.PlayerEvent.FIRE, go);
        }

        /// <summary>
        /// 出膛点：玩家**身前** —— 面朝方向 × (玩家半宽 + spawnForwardOffset)。
        ///
        /// 半宽取玩家碰撞体的世界包围盒（碰撞体可能挂在子物体上，所以用 GetComponentInChildren 兜底）。
        /// 取不到碰撞体时退化为玩家中心，不会报错。
        /// </summary>
        private Vector3 MuzzlePosition()
        {
            float halfWidth = 0f;
            var col = _owner.GetComponent<Collider2D>() ?? _owner.GetComponentInChildren<Collider2D>();
            if (col != null) halfWidth = col.bounds.extents.x;

            float forward = halfWidth + Mathf.Abs(spawnForwardOffset);

            return _owner.transform.position + Vector3.right * (_owner.FaceDir * forward);
        }
    }
}
