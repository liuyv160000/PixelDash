using System;
using UnityEngine;
using PixelDash.Core;
using PixelDash.Player;

namespace PixelDash.Enemy
{
    /// <summary>
    /// 电锯的可调参数集（镜像源项目 circle_saw 的字段默认值）。
    /// 由生成器（<see cref="CircleSawMaker"/>）在生成时注入 —— 生成器上的值是**唯一来源**。
    /// </summary>
    [Serializable]
    public struct CircleSawParams
    {
        [Tooltip("移动速度（源项目 1000）")]
        public float moveSpeed;

        [Tooltip("撞击玩家造成的伤害（源项目 30）")]
        public int contactDamage;

        [Tooltip("自转速度（度/秒，源项目 360）")]
        public float rotateSpeed;

        [Tooltip("出界判据：取消勾选（默认）= 用主相机视口边缘（离开屏幕即回收，推荐）；勾选 = 用下面两个固定阈值")]
        public bool useFixedBounds;

        [Tooltip("左出界阈值，x < 该值则回收（仅勾选 useFixedBounds 时生效。源项目 -800）")]
        public float outLeft;

        [Tooltip("右出界阈值，x > 该值则回收（仅勾选 useFixedBounds 时生效。源项目 1380）")]
        public float outRight;

        [Tooltip("生成时相对基准的 Y 偏移。勾选\"锁定线平台\"时为平台线之上的微调量；否则为相对生成器节点的偏移")]
        public float spawnYOffset;

        /// <summary>源项目默认值（spawnYOffset 取自源项目投放点 y=-37.175，此处按\"平台线微调量\"语义归零）。</summary>
        public static CircleSawParams Default => new CircleSawParams
        {
            moveSpeed = 1000f,
            contactDamage = 30,
            rotateSpeed = 360f,
            useFixedBounds = false,
            outLeft = -800f,
            outRight = 1380f,
            spawnYOffset = 0f,
        };
    }

    /// <summary>
    /// 电锯（镜像源项目 circle_saw，无状态）：从右向左直线移动 + 按 rotateSpeed 自转。
    /// 碰玩家造成伤害；出界（x &lt; outLeft 或 x &gt; outRight）回收。
    /// 注意：GetHurted 为空实现 —— 电锯是环境危险物，没有 HP、无法被玩家子弹杀死（源项目行为）。
    ///
    /// 所有数值来自 <see cref="CircleSawParams"/> —— 由生成器在 <see cref="Setup"/> 时注入。
    /// </summary>
    public class CircleSaw : EnemyControllerBase, IPoolable
    {
        [Header("参数（生成时由 Maker 注入；此处仅为预制体默认值）")]
        [SerializeField] private CircleSawParams parameters = CircleSawParams.Default;

        /// <summary>当前生效的参数。</summary>
        public CircleSawParams Parameters => parameters;

        /// <summary>归属对象池（由生成器注入；为空时退化为 Destroy）。</summary>
        private ObjectPool _pool;

        /// <summary>由生成器调用：注入参数与对象池。</summary>
        public void Setup(in CircleSawParams p, ObjectPool pool)
        {
            parameters = p;
            _pool = pool;
            ApplyParams();
        }

        private void ApplyParams()
        {
            moveSpeed = parameters.moveSpeed;
            moveDir = Vector2.left;
        }

        private void Awake()
        {
            EnsurePhysics(new Color(0.85f, 0.85f, 0.2f), 1.6f);

            // 所有碰撞体设为 Trigger —— 只做伤害检测，不参与物理推开（不会把玩家撞飞）。
            // 同时覆盖父子各挂一个的情况。
            MakeTriggers();
        }

        private void Update()
        {
            if (isDead) return;
            float dt = Time.deltaTime;

            MoveBySpeed(dt);
            transform.Rotate(0f, 0f, parameters.rotateSpeed * dt);

            // 出界回收：默认按主相机视口判据（离开屏幕即回收）
            float x = transform.position.x;
            if (x < LeftLimit() || x > RightLimit())
                RecycleToPool();
        }

        /// <summary>
        /// 回收：有对象池则归还（复用），否则销毁。
        /// 不用基类的 <c>Recycle()</c> —— 那个是 Destroy，且非 virtual 无法覆写。
        /// </summary>
        private void RecycleToPool()
        {
            if (isDead) return;
            isDead = true;

            if (_pool != null) _pool.Recycle(gameObject);
            else Destroy(gameObject);
        }

        /// <summary>归还对象池时重置状态（IPoolable）。自转会改变 rotation，必须复位。</summary>
        public void OnRecycle()
        {
            isDead = false;
            transform.localRotation = Quaternion.identity;
        }

        /// <summary>
        /// 左回收边界。默认（useFixedBounds = false）取主相机左边缘再往左留 1 单位余量，
        /// 确保完全离开画面才回收；勾选 useFixedBounds 时用固定的 outLeft。
        /// </summary>
        private float LeftLimit()
            => parameters.useFixedBounds
                ? parameters.outLeft
                : ViewportAnchor.GetViewportLeftX(parameters.outLeft) - 1f;

        /// <summary>
        /// 右回收边界。默认取主相机右边缘再往右留 1 单位余量；
        /// 勾选 useFixedBounds 时用固定的 outRight。
        /// </summary>
        private float RightLimit()
            => parameters.useFixedBounds
                ? parameters.outRight
                : ViewportAnchor.GetViewportRightX(parameters.outRight) + 1f;

        /// <summary>不可击杀（源项目行为：get_hurted 里 enemy_hp 不扣）。电锯是环境危险物，无 HP。</summary>
        public override void GetHurted(int damage) { }

        /// <summary>
        /// 碰到玩家 → 输出调试信息 + 造成伤害。
        ///
        /// 用 Trigger 而不是 Collision：敌人只做伤害检测，不参与物理推开
        /// （否则电锯会把玩家顶飞，跑酷手感全毁）。
        /// 回调参数是 <see cref="Collider2D"/>，不是 Collision2D —— 两者不能混用。
        /// </summary>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isDead) return;

            // 玩家的碰撞器可能挂在子物体上（父挂刚体 + 子挂碰撞器架构）
            var p = other.GetComponent<PlayerController>()
                 ?? other.GetComponentInParent<PlayerController>();

            if (p == null) return;

            Debug.Log("[CircleSaw] 碰到玩家了 —— 伤害 " + parameters.contactDamage
                      + "（碰到的是 " + other.name + "）", this);

            // 上报被电锯命中（源项目定义了 CIRCLE_SAW_HIT 常量但从未 emit，
            // 此处补上 —— 否则电锯音效永远不响）
            GameEventBus.Instance.Enemy.Emit(GameEventBus.EnemyEvent.CIRCLE_SAW_HIT, gameObject);

            p.GetHurted(parameters.contactDamage);
        }
    }
}
