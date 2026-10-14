using UnityEngine;
using PixelDash.Core;

namespace PixelDash.Item
{
    /// <summary>
    /// 技能球生成器。骨架由 <see cref="CollectionMakerBase"/> 提供，
    /// 本类负责"从对象池取一个并用 BulletSkillOrb 初始化"。
    /// </summary>
    public class BulletSkillballMaker : CollectionMakerBase
    {
        [Header("对象池")]
        [SerializeField] private int poolSize = 3;
        [SerializeField] private int poolMaxSize = 10;

        [Header("飞行速度")]
        [SerializeField] private float flightSpeed = 5f;

        [Header("播报")]
        [Tooltip("本生成器产出的技能球是否触发全屏播报。\n" +
                 "勾选后注入给技能球，覆盖预制体上的同名设置 —— 本生成器的值是唯一来源。")]
        [SerializeField] private bool triggersAnnouncement = false;

        private ObjectPool _pool;

        protected override void Awake()
        {
            base.Awake();   // 建计时器

            // 专属对象池（不能与同节点上的其他生成器共用）
            _pool = CreateOwnPool(poolSize, poolMaxSize);
        }

        protected override void SpawnOne(int index)
        {
            // 从池取（池对象由 ObjectPool 管理，取用时已设好世界位置；
            // 这里用本节点的世界坐标换算，确保落点与"子物体投放"一致）
            Vector3 worldPos = transform.TransformPoint(LocalAnchorAt(index));

            GameObject go = _pool.Get(worldPos);
            if (go == null) return;   // 池满，跳过本次

            go.transform.SetParent(transform, true);   // 归到本节点下，保持世界位置
            go.transform.localPosition = LocalAnchorAt(index);

            var orb = go.GetComponent<BulletSkillOrb>();
            if (orb == null) orb = go.AddComponent<BulletSkillOrb>();
            orb.Init(_pool, flightSpeed, triggersAnnouncement);
        }
    }
}
