using UnityEngine;
using PixelDash.Core;
using PixelDash.Player;

namespace PixelDash.Item
{
    /// <summary>
    /// 技能球基类（对应 bullet_auto 单发技能球）：从右向左移动（默认 speed=500，moveDir=(-1,0)，不受重力）。
    /// 碰玩家 → SkillSystem.EnableSkill(SkillId) + emit BULLET_POWERUP_COLLECTED，随后回收（有池则回池，否则销毁）。
    /// 派生类覆写 SkillId：bullet_triple / bullet_multi。
    /// 实现 IPoolable 以便归还对象池时重置状态。
    /// </summary>
    public class BulletSkillOrb : MonoBehaviour, IPoolable
    {
        [Header("移动参数")]
        public float speed = 500f;
        public Vector2 moveDir = Vector2.left;

        [Header("播报")]
        [Tooltip("拾取本技能球时是否触发全屏播报。\n" +
                 "只有勾选的球会让 BackgroundAnnouncer 播放背景动画（默认不播）。")]
        [SerializeField] private bool triggersAnnouncement = false;

        /// <summary>出屏回收边界。</summary>
        protected const float RecyclableX = -800f;

        /// <summary>归属对象池（由 maker 注入；为空时走 Destroy）。</summary>
        public ObjectPool OwnerPool { get; set; }

        /// <summary>启用的技能 ID（派生类覆写）。</summary>
        protected virtual string SkillId => "bullet_auto";

        private Rigidbody2D _rb;

        protected virtual void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            if (_rb == null) _rb = gameObject.AddComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;

            if (GetComponent<Collider2D>() == null)
                gameObject.AddComponent<CircleCollider2D>();

            // 碰撞体统一走 Trigger：技能球只做拾取判定，不参与物理。
            // 注意必须放在加完碰撞体之后。
            foreach (var c in GetComponentsInChildren<Collider2D>(true))
            {
                if (c != null) c.isTrigger = true;
            }
        }

        /// <summary>拾取本球时是否触发全屏播报（预制体上配置，生成器可覆盖）。</summary>
        public bool TriggersAnnouncement => triggersAnnouncement;

        /// <summary>
        /// 由生成器注入：归属对象池 + 飞行速度 + （可选）是否触发播报。
        /// </summary>
        /// <param name="ownerPool">归属池；传 null 表示不池化</param>
        /// <param name="flightSpeed">飞行速度</param>
        /// <param name="announcement">
        /// 覆盖"是否触发播报"；**传 null 表示沿用预制体自己的设置**
        /// </param>
        public void Init(ObjectPool ownerPool, float flightSpeed, bool? announcement = null)
        {
            OwnerPool = ownerPool;
            speed = flightSpeed;
            moveDir = Vector2.left;

            if (announcement.HasValue)
                triggersAnnouncement = announcement.Value;
        }

        protected virtual void Update()
        {
            transform.position += (Vector3)moveDir * speed * Time.deltaTime;
            if (_rb != null) _rb.velocity = Vector2.zero;

            if (transform.position.x < RecyclableX)
                RecycleOrDestroy();
        }

        /// <summary>
        /// 被玩家碰到 → 输出调试信息 + 启用技能 + 回收。
        /// 用 Trigger（与敌人/子弹一致），判定按 PlayerController 组件查找，
        /// 兼容"父挂刚体 + 子挂碰撞器"的架构，不再依赖对象名。
        /// </summary>
        protected virtual void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null) return;

            var player = other.GetComponent<PlayerController>()
                      ?? other.GetComponentInParent<PlayerController>();

            if (player == null) return;

            Debug.Log("[BulletSkillOrb] 碰到玩家了 —— 拾取技能 " + SkillId
                      + "（碰到的是 " + other.name + "）", this);

            var skill = player.GetComponent<SkillSystem>();
            skill?.EnableSkill(SkillId);

            GameEventBus.Instance.Collection.Emit(GameEventBus.CollectionEvent.BULLET_POWERUP_COLLECTED, gameObject);
            RecycleOrDestroy();
        }

        /// <summary>回收：有对象池则回池，否则销毁。</summary>
        protected void RecycleOrDestroy()
        {
            if (OwnerPool != null) OwnerPool.Recycle(gameObject);
            else Destroy(gameObject);
        }

        /// <summary>归还对象池时重置状态（IPoolable）。</summary>
        public virtual void OnRecycle()
        {
            OwnerPool = null;
            moveDir = Vector2.left;
            speed = 500f;
        }
    }
}