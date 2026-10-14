using UnityEngine;
using PixelDash.Core;

namespace PixelDash.Enemy
{
    /// <summary>
    /// 电锯敌人生成器（对应源项目 circle_saw_maker）。
    /// 生成 circle_saw 并注入 <see cref="CircleSawParams"/> —— **参数以本生成器为准**。
    /// 生成位置：X 与生成器节点对齐，Y 默认**锁定线平台**（plat_line 的世界 Y）。
    ///
    /// 走对象池：电锯出屏即回收，进出场频繁，用池避免反复 Instantiate/Destroy。
    /// </summary>
    public class CircleSawMaker : EnemyMakerBase
    {
        [Header("circle_saw 参数（生成时注入敌人；以这里为准）")]
        [SerializeField] private CircleSawParams circleSawParams = CircleSawParams.Default;

        [Header("生成位置")]
        [Tooltip("勾选：生成 Y 锁定线平台（平台线世界 Y + spawnYOffset 微调）；取消：生成器节点 Y + spawnYOffset")]
        [SerializeField] private bool alignYToPlatLine = true;

        [Header("对象池")]
        [SerializeField] private int poolSize = 5;
        [SerializeField] private int poolMaxSize = 30;

        private ObjectPool _pool;

        protected override void Awake()
        {
            base.Awake();   // 建计时器

            // 专属对象池（不能与同节点上的其他生成器共用）
            _pool = CreateOwnPool(poolSize, poolMaxSize);
        }

        protected override void SpawnOne(int index)
        {
            // 生成位置：本节点局部坐标 (0, spawnYOffset)
            Vector3 local = LocalAnchor(circleSawParams.spawnYOffset);
            Vector3 world = transform.TransformPoint(local);

            // 优先取池，池满则临时新建（临时对象不回池，用完自行销毁）
            GameObject go = TakeFromPoolOrCreate(_pool, enemyPrefab, local, world, out ObjectPool ownerPool);
            go.name = "circle_saw";

            // Y 锁定线平台：平台线世界 Y + spawnYOffset（微调量，默认 0）
            // 此时已是本节点子物体，直接设世界 position，Unity 会自动换算回局部坐标
            if (alignYToPlatLine && ViewportAnchor.TryGetPlatLineY(out float lineY))
            {
                Vector3 p = go.transform.position;
                go.transform.position = new Vector3(p.x, lineY + circleSawParams.spawnYOffset, p.z);
            }

            GetOrAdd<CircleSaw>(go).Setup(circleSawParams, ownerPool);
        }
    }
}
