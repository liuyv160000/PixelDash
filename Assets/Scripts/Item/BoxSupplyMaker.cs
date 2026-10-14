using UnityEngine;

namespace PixelDash.Item
{
    /// <summary>
    /// 补给箱生成器。骨架由 <see cref="CollectionMakerBase"/> 提供，
    /// 本类只负责"生成一个 SupplyBox"。
    /// </summary>
    public class BoxSupplyMaker : CollectionMakerBase
    {
        protected override void SpawnOne(int index)
        {
            // 作为本节点的子物体投放：X 与节点对齐，Y 按偏移表
            GameObject go = InstantiateOrCreate("supply_box", LocalAnchorAt(index));
            if (go.GetComponent<SupplyBox>() == null) go.AddComponent<SupplyBox>();
        }
    }
}
