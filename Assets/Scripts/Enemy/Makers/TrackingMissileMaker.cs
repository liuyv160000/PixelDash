using UnityEngine;
using PixelDash.Core;

namespace PixelDash.Enemy
{
    /// <summary>
    /// 追踪导弹生成器（对应源项目 tracking_missile_maker）。
    /// 生成 tracking_missile 并注入 <see cref="MissileParams"/> —— **参数以本生成器为准**。
    /// 生成时间表与 Y 偏移在基类的 Inspector 字段里配置。
    /// </summary>
    public class TrackingMissileMaker : EnemyMakerBase
    {
        [Header("tracking_missile 参数（生成时注入敌人；以这里为准）")]
        [SerializeField] private MissileParams missileParams = MissileParams.Default;

        [Header("对象池（对齐源项目 tracking_missile_poster：init 5 / max 20）")]
        [SerializeField] private int poolSize = 5;
        [SerializeField] private int poolMaxSize = 20;

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

            // 优先取池，池满则临时新建（临时对象不回池，用完自行销毁）
            GameObject go = TakeFromPoolOrCreate(_pool, enemyPrefab, local, transform.TransformPoint(local), out ObjectPool ownerPool);
            go.name = "tracking_missile";

            GetOrAdd<TrackingMissile>(go).Setup(missileParams, ownerPool);
        }
    }
}
