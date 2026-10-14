using UnityEngine;
using PixelDash.Core;

namespace PixelDash.Enemy
{
    /// <summary>
    /// 警戒区（镜像源项目 box_warning_zone）。
    ///
    /// 生成时完全不透明（alpha = 1），在自己的存在时间内按计时器**线性**降到 0，
    /// 淡完即回收 —— 这一刻正好是激光本体的生成时刻（提前量 = 本时长）。
    /// 不可被击杀；由 <see cref="LaserMaker"/> 在激光时间点之前 warningCount 秒生成。
    /// </summary>
    public class BoxWarningZone : EnemyControllerBase, IPoolable
    {
        /// <summary>渐隐时长（秒）。由生成器按提前量 warningCount 注入。</summary>
        private float _duration = 1f;

        /// <summary>已存在时长（秒）。</summary>
        private float _timer;

        private SpriteRenderer _sr;

        /// <summary>归属对象池（由生成器注入；为空时退化为 Destroy）。</summary>
        private ObjectPool _pool;

        /// <summary>
        /// 由生成器调用：设定渐隐时长，并立即从完全不透明开始。
        /// </summary>
        /// <param name="duration">渐隐总时长，取生成器上的提前量（warningCount）。</param>
        /// <param name="pool">归属对象池（可空）。</param>
        public void Setup(float duration, ObjectPool pool = null)
        {
            _duration = duration > 0f ? duration : 1f;
            _timer = 0f;
            _pool = pool;

            EnsureRenderer();
            ApplyAlpha(1f);   // 起点固定为 1，不受预制体自身 alpha 影响

            // 预警区投放即上报（源项目 box_warning_zone.ts 在 onLoad/初始化时 emit，
            // 携带预警时长供音效层使用）
            GameEventBus.Instance.Enemy.Emit(GameEventBus.EnemyEvent.WARNING_ZONE_ENTER, _duration);
        }

        private void Awake()
        {
            EnsurePhysics(new Color(1f, 0.1f, 0.1f, 1f), 1.5f);
            EnsureRenderer();
        }

        /// <summary>解析渲染器。用 GetComponentInChildren 兜底 —— 预制体可能把渲染器放在子物体上。</summary>
        private void EnsureRenderer()
        {
            if (_sr == null)
                _sr = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
        }

        private void Update()
        {
            _timer += Time.deltaTime;

            // ratio：1（刚生成，完全不透明）→ 0（到期，完全透明）
            float ratio = 1f - Mathf.Clamp01(_timer / _duration);
            ApplyAlpha(ratio);

            if (_timer >= _duration)
                RecycleToPool();
        }

        /// <summary>回收：有对象池则归还（复用），否则销毁。</summary>
        private void RecycleToPool()
        {
            if (isDead) return;
            isDead = true;

            if (_pool != null) _pool.Recycle(gameObject);
            else Destroy(gameObject);
        }

        /// <summary>归还对象池时重置状态（IPoolable）。透明度回到起点，避免下一轮从半透明开始淡。</summary>
        public void OnRecycle()
        {
            isDead = false;
            _timer = 0f;
            ApplyAlpha(1f);
        }

        /// <summary>写入透明度：1 = 完全不透明，0 = 完全透明。只改 alpha，保留 RGB。</summary>
        private void ApplyAlpha(float ratio)
        {
            if (_sr == null) return;

            var c = _sr.color;
            c.a = Mathf.Clamp01(ratio);
            _sr.color = c;
        }

        public override void GetHurted(int damage) { }
    }
}
