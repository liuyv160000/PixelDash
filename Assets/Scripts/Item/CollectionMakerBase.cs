using UnityEngine;
using PixelDash.Core;

namespace PixelDash.Item
{
    /// <summary>
    /// 收集物生成器基类（与敌人生成器 <c>EnemyMakerBase</c> 对称）。
    ///
    /// 职责边界：
    ///   - **生成器**：自己持有预制体、生成时间表、Y 偏移表与生成逻辑
    ///   - **管理器**（<see cref="CollectionManager"/>）：只持有生成器列表并转发 Pause/Resume
    ///
    /// 骨架（与三个具体生成器完全一致，故上提到基类）：
    ///   计时器 → 每帧比对时间表 → 到点调 <see cref="SpawnOne"/>
    /// </summary>
    public abstract class CollectionMakerBase : SceneComponentBase
    {
        [Header("生成器基础配置")]
        [Tooltip("要生成的收集物预制体。留空则运行时程序化创建占位对象")]
        [SerializeField] protected GameObject prefab;

        [Tooltip("本关卡的生成总时长（秒）")]
        [SerializeField] protected float totalDuration = 90f;

        [Header("生成时间表（秒）")]
        [SerializeField] protected float[] spawnTimes = new float[0];

        [Header("对应 Y 偏移（相对本节点位置）")]
        [SerializeField] protected float[] spawnYOffsets = new float[0];

        /// <summary>本生成器的计时器（Awake 时自动创建）。</summary>
        protected TimerComponent Timer;

        private int _spawnIdx;

        /// <summary>初始暂停 —— 等 ExManager.StartGame() 统一 Resume 后才开始生成。</summary>
        protected override bool StartPaused => true;

        protected virtual void Awake()
        {
            Timer = gameObject.AddComponent<TimerComponent>();
            Timer.SetDuration(totalDuration);
        }

        protected virtual void Start()
        {
            // 启动时做一次配置体检：空时间表 / 缺预制体 / 表长不一致 / 未升序
            // 这些错误会让生成器静默地什么都不做，必须报出来
            SpawnConfigValidator.Validate(this, prefab, spawnTimes, spawnYOffsets);

            Timer.Start();
        }

        protected virtual void Update()
        {
            if (IsPaused) return;

            float t = Timer.GetElapsedTime();
            // 处理同一帧到期的所有时间点
            while (_spawnIdx < spawnTimes.Length && t >= spawnTimes[_spawnIdx])
            {
                SpawnOne(_spawnIdx);
                _spawnIdx++;
            }
        }

        /// <summary>生成第 index 个收集物（子类实现：实例化/取池 + 配置）。</summary>
        protected abstract void SpawnOne(int index);

        /// <summary>
        /// 设置生成计划（时间表 + Y 偏移表）。
        /// 供编辑器装配脚本在生成场景时使用 —— 字段本身保持 protected，封装不破坏。
        /// </summary>
        public void SetSpawnPlan(float[] times, float[] yOffsets)
        {
            spawnTimes = times ?? new float[0];
            spawnYOffsets = yOffsets ?? new float[0];
        }

        // ===== 暂停协议：由 SceneComponentBase 统一管理（幂等）=====

        protected override void OnPause() => Timer?.Stop();

        protected override void OnResume() => Timer?.Start();

        // ===== 子类可用的工具 =====

        /// <summary>
        /// 生成位置（**本节点的局部坐标**）：X 恒为 0（与生成器节点对齐），Y 按传入偏移。
        /// 生成物会成为本节点的子物体，跟随本节点移动。
        /// </summary>
        protected Vector3 LocalAnchor(float yOffset = 0f) => new Vector3(0f, yOffset, 0f);

        /// <summary>第 index 次生成的局部位置（Y 取自 <see cref="spawnYOffsets"/>）。</summary>
        protected Vector3 LocalAnchorAt(int index) => LocalAnchor(Pick(spawnYOffsets, index));

        /// <summary>
        /// 实例化预制体并**挂到本节点下**（局部位置由 <paramref name="localPos"/> 指定）；
        /// 无预制体时创建空对象（由子类自行 AddComponent）。
        /// </summary>
        protected GameObject InstantiateOrCreate(string name, Vector3 localPos)
        {
            GameObject go;

            if (prefab != null)
            {
                go = Instantiate(prefab, transform);   // 直接作为子物体
                go.name = name;
            }
            else
            {
                go = new GameObject(name);
                go.transform.SetParent(transform, false);
            }

            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;
            return go;
        }

        protected static float Pick(float[] arr, int i)
            => arr != null && i >= 0 && i < arr.Length ? arr[i] : 0f;

        protected static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            if (c == null) c = go.AddComponent<T>();
            return c;
        }

        /// <summary>
        /// 创建本生成器**专属**的对象池（挂在同名子节点上）。
        ///
        /// 为什么不直接用 <c>GetComponent&lt;ObjectPool&gt;()</c>：多个生成器常挂在同一个
        /// GameObject 上（如 CollectionPoster 上同时挂金币 / 技能球 / 补给箱生成器）。
        /// 那样第二个生成器会拿到第一个建的池，再用自己的 prefab 覆盖配置 ——
        /// 最后配置的那个赢，结果所有生成器都生成同一种收集物。所以每个池必须独立。
        /// </summary>
        protected ObjectPool CreateOwnPool(int initialSize, int maxSize)
        {
            var holder = new GameObject("pool_" + GetType().Name);
            holder.transform.SetParent(transform, false);

            var pool = holder.AddComponent<ObjectPool>();
            pool.Configure(prefab, initialSize, maxSize);
            return pool;
        }

        // ===== 只读查询（供验证/调试用）=====

        /// <summary>本生成器计划生成的总数量。</summary>
        public int PlannedSpawnCount => spawnTimes != null ? spawnTimes.Length : 0;

        /// <summary>第 i 个生成时间点（越界返回 -1）。</summary>
        public float GetSpawnTime(int i)
            => spawnTimes != null && i >= 0 && i < spawnTimes.Length ? spawnTimes[i] : -1f;
    }
}
