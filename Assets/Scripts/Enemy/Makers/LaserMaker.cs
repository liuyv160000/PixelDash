using UnityEngine;
using PixelDash.Core;

namespace PixelDash.Enemy
{
    /// <summary>
    /// 激光敌人生成器（对应源项目 laser_maker）。
    /// 同一次生成分两步：先出**警戒区**（提前 warningCount 秒），到点再出**激光本体**。
    /// 参数以本生成器为准，生成时注入 <see cref="LaserParams"/>。
    /// 生成时间表与 Y 偏移在基类的 Inspector 字段里配置。
    /// </summary>
    public class LaserMaker : EnemyMakerBase
    {
        [Header("laser 参数（生成时注入敌人；以这里为准）")]
        [SerializeField] private LaserParams laserParams = LaserParams.Default;

        [Tooltip("警戒区提前量（秒）")]
        public float warningCount = 1f;

        [Header("生成位置")]
        [Tooltip("相对锚点的 X 偏移（源项目 Cocos 局部坐标为 -1150；Unity 世界坐标下取 0）")]
        [SerializeField] private float spawnOffsetX = 0f;

        [Tooltip("警戒区预制体（可空，留空则程序化创建占位）")]
        [SerializeField] private GameObject warningZonePrefab;

        [Header("对象池（对齐源项目 laser_maker：本体与警戒区各一个池，init 4 / max 15）")]
        [SerializeField] private int poolSize = 4;
        [SerializeField] private int poolMaxSize = 15;
        [SerializeField] private int warningPoolSize = 4;
        [SerializeField] private int warningPoolMaxSize = 15;

        /// <summary>激光本体与警戒区**各自**的对象池（两者预制体不同，不能共用）。</summary>
        private ObjectPool _pool;
        private ObjectPool _warningPool;

        /// <summary>警戒区独立指针（本体指针在基类里走）。</summary>
        private int _warnIdx;

        protected override void Awake()
        {
            base.Awake();   // 建计时器

            // 专属对象池（不能与同节点上的其他生成器共用）
            _pool = CreateOwnPool(enemyPrefab, poolSize, poolMaxSize);
            _warningPool = CreateOwnPool(warningZonePrefab, warningPoolSize, warningPoolMaxSize);
        }

        protected override void Update()
        {
            if (IsPaused) return;

            float t = Timer.GetElapsedTime();

            // 预警区：提前 warningCount 秒投放，位置与激光完全一致（同一个 AnchorFor）。
            //
            // 时序上两者刚好首尾相接：
            //   预警区生成于 t = spawnTimes[i] - warningCount
            //   预警区渐隐时长 = warningCount，所以在 t = spawnTimes[i] 淡完
            //   激光本体生成于 t = spawnTimes[i]  ← 正好接上
            while (_warnIdx < spawnTimes.Length && t >= spawnTimes[_warnIdx] - warningCount)
            {
                SpawnWarningZone(_warnIdx);
                _warnIdx++;
            }

            base.Update();   // 激光本体（到点才出）
        }

        protected override void SpawnOne(int index)
        {
            Vector3 local = LocalAnchorAt(index) + new Vector3(spawnOffsetX, 0f, 0f);
            Vector3 worldAnchor = AnchorFor(index);

            // 优先取池，池满则临时新建（临时对象不回池，用完自行销毁）
            GameObject go = TakeFromPoolOrCreate(_pool, enemyPrefab, local, worldAnchor, out ObjectPool ownerPool);
            go.name = "laser";

            // 先确保组件（首次生成时 Laser.Awake 会补碰撞体），再做对齐 —— 对齐依赖碰撞体尺寸
            var laser = GetOrAdd<Laser>(go);
            AlignRightCenterTo(go, worldAnchor);
            laser.Setup(laserParams, ownerPool);
        }

        private void SpawnWarningZone(int index)
        {
            Vector3 local = LocalAnchorAt(index) + new Vector3(spawnOffsetX, 0f, 0f);
            Vector3 worldAnchor = AnchorFor(index);

            GameObject go = TakeFromPoolOrCreate(_warningPool, warningZonePrefab, local, worldAnchor, out ObjectPool ownerPool);
            go.name = "warning_zone";

            // 位置与激光本体一致：同一个锚点 + 同一套右边缘中点对齐
            var zone = GetOrAdd<BoxWarningZone>(go);
            AlignRightCenterTo(go, worldAnchor);
            zone.Setup(warningCount, ownerPool);
        }

        /// <summary>
        /// 本次生成的投放锚点（世界坐标）。
        /// 激光本体与预警区**共用同一个锚点**，配合同一套 <see cref="AlignRightCenterTo"/>，
        /// 两者的右边缘中点就会落在同一处 —— 位置天然一致，不需要各自另算。
        /// </summary>
        private Vector3 AnchorFor(int index)
            => transform.TransformPoint(LocalAnchorAt(index) + new Vector3(spawnOffsetX, 0f, 0f));

        /// <summary>
        /// 把生成物的**右边缘中点**对齐到指定世界坐标。
        ///
        /// 激光体是矩形物体，投放基准是"矩形右侧那条竖边的中点"，不是物体中心。
        ///
        /// 判据优先级：BoxCollider2D（主用）→ 其它 Collider2D → SpriteRenderer。
        /// **BoxCollider2D 走的是手动计算**，显式乘上 Transform 的缩放 ——
        /// 因为 size 是局部尺寸，而对象可能是子物体、父节点还带缩放；
        /// 用 Collider2D.bounds 虽然也是世界空间，但依赖物理系统的刷新时机，
        /// 刚 Instantiate 出来的当帧可能还取不到新值。
        /// </summary>
        private static void AlignRightCenterTo(GameObject go, Vector3 worldAnchor)
        {
            Vector3 rightCenter;
            if (!TryGetRightCenter(go, out rightCenter)) return;
            go.transform.position += worldAnchor - rightCenter;
        }

        /// <summary>取"右边缘中点"的世界坐标。找不到任何尺寸来源时返回 false。</summary>
        private static bool TryGetRightCenter(GameObject go, out Vector3 rightCenter)
        {
            rightCenter = Vector3.zero;
            float z = go.transform.position.z;

            // ① BoxCollider2D：手动算，显式乘 Transform 缩放
            var box = go.GetComponent<BoxCollider2D>();
            if (box != null)
            {
                Vector3 worldCenter = go.transform.TransformPoint(box.offset);   // 世界中心（含父级缩放）
                Vector3 s = go.transform.lossyScale;                            // 含所有父级的累计缩放

                float halfW = box.size.x * Mathf.Abs(s.x) * 0.5f;

                rightCenter = new Vector3(worldCenter.x + halfW, worldCenter.y, z);
                return true;
            }

            // ② 其它碰撞体：bounds 已是世界空间，自动含缩放
            var col = go.GetComponent<Collider2D>();
            if (col != null)
            {
                Bounds b = col.bounds;
                rightCenter = new Vector3(b.max.x, b.center.y, z);
                return true;
            }

            // ③ 精灵兜底：同样是世界空间包围盒
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Bounds b = sr.bounds;
                rightCenter = new Vector3(b.max.x, b.center.y, z);
                return true;
            }

            return false;
        }
    }
}
