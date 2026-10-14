using System;
using UnityEngine;
using PixelDash.Core;
using PixelDash.Player;

namespace PixelDash.Item
{
    /// <summary>
    /// 金币的可调参数集。由生成器（<see cref="CoinMaker"/>）在生成时注入 ——
    /// 生成器上的值是**唯一来源**。
    /// </summary>
    [Serializable]
    public struct CoinParams
    {
        [Tooltip("移动速度（世界单位/秒）")]
        public float speed;

        [Tooltip("移动方向（源项目为向左 (-1, 0)）")]
        public Vector2 moveDir;

        /// <summary>默认值。</summary>
        public static CoinParams Default => new CoinParams
        {
            speed = 500f,
            moveDir = Vector2.left,
        };
    }

    /// <summary>
    /// 金币：按注入的参数从右向左移动（不受重力）。
    /// 碰玩家 → emit COIN_COLLECTED（data=本金币 GameObject），随后销毁。
    ///
    /// 移动数值来自 <see cref="CoinParams"/> —— 由生成器在 <see cref="Setup"/> 时注入。
    /// </summary>
    public class NormalCoin : MonoBehaviour
    {
        [Header("参数（生成时由 Maker 注入；此处仅为预制体默认值）")]
        [SerializeField] private CoinParams parameters = CoinParams.Default;

        /// <summary>当前生效的参数。</summary>
        public CoinParams Parameters => parameters;

        /// <summary>出屏回收边界（x 小于该值即出屏左侧）。</summary>
        private const float RecyclableX = -800f;

        private Rigidbody2D _rb;

        /// <summary>
        /// 由生成器调用：注入移动参数。
        ///
        /// 兜底：CoinParams 是后加的字段，已有的场景组件反序列化后会是 default（speed=0、
        /// moveDir=0），此时回退到默认参数 —— 否则金币会原地不动。
        /// </summary>
        public void Setup(in CoinParams p)
        {
            parameters = p;

            if (Mathf.Approximately(parameters.speed, 0f) && parameters.moveDir == Vector2.zero)
                parameters = CoinParams.Default;
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            if (_rb == null) _rb = gameObject.AddComponent<Rigidbody2D>();
            _rb.gravityScale = 0f; // 道具不受重力

            if (GetComponent<Collider2D>() == null)
                gameObject.AddComponent<CircleCollider2D>();

            // 碰撞体统一走 Trigger：金币只做拾取判定，不参与物理（不会把玩家撞开）。
            // 注意必须放在加完碰撞体之后。
            foreach (var c in GetComponentsInChildren<Collider2D>(true))
            {
                if (c != null) c.isTrigger = true;
            }
        }

        private void Update()
        {
            transform.position += (Vector3)parameters.moveDir * parameters.speed * Time.deltaTime;
            if (_rb != null) _rb.velocity = Vector2.zero;

            if (transform.position.x < RecyclableX)
                Destroy(gameObject);
        }

        /// <summary>
        /// 被玩家碰到 → 输出调试信息 + 拾取。
        /// 用 Trigger（与敌人/子弹一致），判定按 PlayerController 组件查找，
        /// 兼容"父挂刚体 + 子挂碰撞器"的架构，不再依赖对象名。
        /// </summary>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null) return;

            var player = other.GetComponent<PlayerController>()
                      ?? other.GetComponentInParent<PlayerController>();

            if (player == null) return;

            Debug.Log("[NormalCoin] 碰到玩家了 —— 金币被拾取（碰到的是 " + other.name + "）", this);

            GameEventBus.Instance.Collection.Emit(GameEventBus.CollectionEvent.COIN_COLLECTED, gameObject);
            Destroy(gameObject);
        }
    }
}
