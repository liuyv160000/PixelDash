using UnityEngine;
using PixelDash.Core;

namespace PixelDash.Enemy
{
    /// <summary>
    /// 敌人生成器基类（对应源项目 enemy_manager）。
    ///
    /// 职责边界：
    ///   - **生成器**：自己持有预制体、生成时间表、生成参数与生成逻辑（本类及其子类）
    ///   - **场景管理器**（<see cref="SceneEnemyManager"/>）：只持有生成器列表并转发 Pause/Resume
    ///
    /// 每个具体生成器（NormalOneMaker / CircleSawMaker / LaserMaker / TrackingMissileMaker）
    /// 独立挂在一个节点上，由它自己决定"何时、生成什么、参数多少"。
    ///
    /// 生成数据（时间表 / 位置偏移 / 敌人参数）**全部在 Inspector 里配置**，运行时不改写。
    /// </summary>
    public abstract class EnemyMakerBase : SceneComponentBase
    {
        [Header("生成器基础配置")]
        [Tooltip("敌人预制体。留空则运行时程序化创建占位对象（便于先跑通逻辑）")]
        [SerializeField] protected GameObject enemyPrefab;

        [Tooltip("本关卡的生成总时长（秒）。超过这个时间不再生成")]
        [SerializeField] protected float totalDuration = 90f;

        [Header("生成数据")]
        [Tooltip("生成时间点（秒）。按索引与下面的 Y 偏移表一一对应。")]
        [SerializeField] protected float[] spawnTimes = new float[0];

        [Tooltip("各次生成相对本节点的 Y 偏移。长度不足时按 0 处理。")]
        [SerializeField] protected float[] spawnYOffsets = new float[0];

        /// <summary>本生成器的计时器（Awake 时自动创建）。</summary>
        protected TimerComponent Timer;

        private int _spawnIdx;

        protected virtual void Awake()
        {
            Timer = gameObject.AddComponent<TimerComponent>();
            Timer.SetDuration(totalDuration);
        }

        protected virtual void Start()
        {
            // 启动时做一次配置体检：空时间表 / 缺预制体 / 表长不一致 / 未升序
            // 这些错误会让生成器静默地什么都不做，必须报出来
            SpawnConfigValidator.Validate(this, enemyPrefab, spawnTimes, spawnYOffsets);

            Timer.Start();
        }

        protected virtual void Update()
        {
            if (IsPaused) return;

            float t = Timer.GetElapsedTime();
            while (_spawnIdx < spawnTimes.Length && t >= spawnTimes[_spawnIdx])
            {
                SpawnOne(_spawnIdx);
                _spawnIdx++;
            }
        }

        /// <summary>生成第 index 个敌人（子类实现：实例化 + 配置参数）。</summary>
        protected abstract void SpawnOne(int index);

        // ===== 暂停协议：由 SceneComponentBase 统一管理（幂等），这里只实现具体动作 =====
        // 初始为暂停 —— 等 ExManager.StartGame() 统一 Resume 后各生成器才开始出怪
        protected override bool StartPaused => true;

        protected override void OnPause()
        {
            Timer?.Stop();
        }

        protected override void OnResume()
        {
            Timer?.Start();
        }

        // ================= 子类可用的工具 =================

        /// <summary>
        /// 生成位置（**本节点的局部坐标**）：X 恒为 0（与生成器节点对齐），Y 按传入偏移。
        /// 生成物会成为本节点的子物体，跟随本节点移动。
        /// </summary>
        protected Vector3 LocalAnchor(float yOffset = 0f) => new Vector3(0f, yOffset, 0f);

        /// <summary>第 index 次生成的局部位置（Y 取自 <see cref="spawnYOffsets"/>）。</summary>
        protected Vector3 LocalAnchorAt(int index) => LocalAnchor(Pick(spawnYOffsets, index));

        /// <summary>
        /// 实例化预制体并**挂到本节点下**（局部位置由 <paramref name="localPos"/> 指定）；
        /// 无预制体时程序化创建带占位渲染的对象。
        /// </summary>
        protected GameObject InstantiateOrCreate(string name, Vector3 localPos, Color color, float worldSize)
        {
            GameObject go;

            if (enemyPrefab != null)
            {
                go = Instantiate(enemyPrefab, transform);   // 直接作为子物体
                go.name = name;
            }
            else
            {
                go = new GameObject(name);
                go.transform.SetParent(transform, false);
                PlaceholderSprite.EnsureRenderer(go, color, worldSize);
            }

            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;
            return go;
        }

        protected static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            if (c == null) c = go.AddComponent<T>();
            return c;
        }

        /// <summary>
        /// 取一个生成物：**优先从池取；池满（或没有池）就临时新建一个**。
        ///
        /// 临时新建的对象**不入池** —— 它拿到的 <paramref name="ownerPool"/> 是 null，
        /// 生命周期结束时自行 Destroy。这样池的容量上限不会被突发事件悄悄撑破：
        /// 瞬时压力过去后，池里的旧对象继续复用。
        /// </summary>
        /// <param name="pool">目标对象池（可空）。</param>
        /// <param name="prefab">临时新建时用的预制体（可空 = 程序化创建占位对象，渲染由敌人自己的 Awake 补）。</param>
        /// <param name="localPos">本节点局部位置（生成物会成为本节点的子物体）。</param>
        /// <param name="worldPos">从池取出瞬间的初始世界位置（随后被 localPos 覆盖；池取用时需要它来做父级换算）。</param>
        /// <param name="ownerPool">生成物归属的池：正常取用 = pool；临时新建 = <c>null</c>（回收即销毁）。</param>
        protected GameObject TakeFromPoolOrCreate(ObjectPool pool, GameObject prefab, Vector3 localPos, Vector3 worldPos,
                                                 out ObjectPool ownerPool)
        {
            ownerPool = pool;

            GameObject go = pool != null ? pool.Get(worldPos) : null;

            if (go == null)
            {
                // 池满 / 无池 → 临时新建（复用池配置的预制体；连预制体都没有才程序化创建）
                GameObject src = prefab != null ? prefab : (pool != null ? pool.Prefab : null);
                go = src != null ? Instantiate(src) : new GameObject("temp_enemy");

                ownerPool = null;   // ⚠️ 关键：临时对象不回池，用完自行销毁
            }

            go.transform.SetParent(transform, true);   // 归到本节点下，保持世界位置
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;
            return go;
        }

        /// <summary>
        /// 创建本生成器**专属**的对象池（挂在同名子节点上）。
        ///
        /// 为什么不直接用 <c>GetComponent&lt;ObjectPool&gt;()</c>：多个生成器常挂在同一个
        /// GameObject 上（例如 EnemyPoster 上同时挂 NormalOneMaker / CircleSawMaker /
        /// LaserMaker / TrackingMissileMaker）。那样第二个生成器会拿到第一个建的池，
        /// 再用自己的 prefab 覆盖配置 —— 四个生成器抢一个池，最后配置的那个赢，
        /// 结果所有生成器都生成同一种敌人。所以每个池必须独立。
        /// </summary>
        protected ObjectPool CreateOwnPool(int initialSize, int maxSize)
            => CreateOwnPool(enemyPrefab, initialSize, maxSize);

        /// <summary>
        /// 用**指定的预制体**创建专属对象池（给"一次生成本体 + 附属物"的生成器用，
        /// 例如 LaserMaker 的激光本体与警戒区是两个不同预制体，必须各建一个池）。
        /// </summary>
        protected ObjectPool CreateOwnPool(GameObject prefab, int initialSize, int maxSize)
        {
            var holder = new GameObject("pool_" + GetType().Name + (prefab == enemyPrefab ? "" : "_2"));
            holder.transform.SetParent(transform, false);

            var pool = holder.AddComponent<ObjectPool>();
            pool.Configure(prefab, initialSize, maxSize);
            return pool;
        }

        /// <summary>安全取数组第 i 项（越界返回 0）。</summary>
        protected static float Pick(float[] arr, int i)
            => arr != null && i >= 0 && i < arr.Length ? arr[i] : 0f;

        /// <summary>安全取 int 数组第 i 项（越界返回 fallback）。</summary>
        protected static int Pick(int[] arr, int i, int fallback = 1)
            => arr != null && i >= 0 && i < arr.Length ? arr[i] : fallback;

        // ===== 只读查询（供验证/调试用）=====

        /// <summary>本生成器计划生成的总数量。</summary>
        public int PlannedSpawnCount => spawnTimes != null ? spawnTimes.Length : 0;

        /// <summary>第 i 个生成时间点（越界返回 -1）。</summary>
        public float GetSpawnTime(int i)
            => spawnTimes != null && i >= 0 && i < spawnTimes.Length ? spawnTimes[i] : -1f;

        /// <summary>第 i 个位置偏移（越界返回 0）。</summary>
        public float GetSpawnYOffset(int i) => Pick(spawnYOffsets, i);
    }
}
