using UnityEngine;

namespace PixelDash.Item
{
    /// <summary>
    /// 金币生成器。骨架（计时器 / 时间表 / 暂停）由 <see cref="CollectionMakerBase"/> 提供，
    /// 本类负责"生成一个 NormalCoin 并注入移动参数"。
    /// </summary>
    public class CoinMaker : CollectionMakerBase
    {
        [Header("coin 参数（生成时注入金币；以这里为准）")]
        [SerializeField] private CoinParams coinParams = CoinParams.Default;

        protected override void SpawnOne(int index)
        {
            // 作为本节点的子物体投放：X 与节点对齐，Y 按偏移表
            GameObject go = InstantiateOrCreate("normal_coin", LocalAnchorAt(index));

            GetOrAdd<NormalCoin>(go).Setup(coinParams);
        }
    }
}
