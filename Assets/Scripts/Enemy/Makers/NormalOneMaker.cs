using UnityEngine;
using PixelDash.Core;

namespace PixelDash.Enemy
{
    /// <summary>
    /// 普通飞行敌人生成器（对应源项目 normal_one_fly_maker）。
    /// 生成 normal_one 并注入 <see cref="NormalOneParams"/> —— **参数以本生成器为准**。
    ///
    /// 走对象池：normal_one 有完整的入场→攻击→离场生命周期，进出场频繁，
    /// 用池避免反复 Instantiate/Destroy。
    /// </summary>
    public class NormalOneMaker : EnemyMakerBase
    {
        [Header("normal_one 参数（生成时注入敌人；以这里为准）")]
        [SerializeField] private NormalOneParams normalOneParams = NormalOneParams.Default;

        [Tooltip("敌人子弹预制体（可空，留空则程序化创建占位弹）")]
        [SerializeField] private GameObject bulletPrefab;

        [Header("各次生成的起始巡逻方向（与生成次数一一对应）")]
        [Tooltip("1 = 向上起步（停靠点作为下边界）；-1 = 向下起步。缺省的位置回退到 normalOneParams.patrolStartDir")]
        [SerializeField] private int[] spawnPatrolStartDirs = new int[0];

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
            Vector3 local = LocalAnchorAt(index);
            Vector3 world = transform.TransformPoint(local);

            // 优先取池，池满则临时新建（临时对象不回池，用完自行销毁）
            GameObject go = TakeFromPoolOrCreate(_pool, enemyPrefab, local, world, out ObjectPool ownerPool);
            go.name = "normal_one";

            // 本次生成的起始巡逻方向：数组里有就按序号取，缺省则用参数里的默认方向
            int startDir = Pick(spawnPatrolStartDirs, index, normalOneParams.patrolStartDir);

            GetOrAdd<NormalOne>(go).Setup(normalOneParams, ownerPool, startDir, bulletPrefab);
        }
    }
}
