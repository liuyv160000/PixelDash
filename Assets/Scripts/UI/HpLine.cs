using UnityEngine;
using UnityEngine.UI;
using PixelDash.Core;

namespace PixelDash.UI
{
    /// <summary>
    /// 血条线条：**监听玩家受伤事件**，按剩余血量比例重绘填充。镜像 hp_line。
    ///
    /// 由「每帧轮询」改为「事件驱动」：血量只在 GetHurted / Die 时变化，
    /// 事件里刷新一次即可，不必每帧做除法与赋值。
    /// </summary>
    public class HpLine : MonoBehaviour
    {
        [Tooltip("玩家控制器（留空则自动查找场景里的 PlayerController）")]
        [SerializeField] private PixelDash.Player.PlayerController playerNode;

        [Tooltip("血条填充图（由 Image.fillAmount 驱动）")]
        [SerializeField] private Image fill;

        [Tooltip("满血值。玩家当前血量以此为分母换算填充比例")]
        [SerializeField] private int maxHealth = 100;

        private void Awake()
        {
            if (playerNode == null)
                playerNode = FindObjectOfType<PixelDash.Player.PlayerController>();

            if (fill == null)
                fill = GetComponent<Image>();

            EnsureFilledType();
        }

        /// <summary>
        /// 强制把 Image 设为 **Filled** 并指定水平填充。
        ///
        /// 必须做这一步：Image.fillAmount 只在 type == Filled 时才会被读取。
        /// 若 Image 是 Simple / Sliced / Tiled，赋值既不报错也不显示 ——
        /// 表现为"刷新函数跑了但血条纹丝不动"，极难排查（本工程就踩过一次）。
        /// </summary>
        private void EnsureFilledType()
        {
            if (fill == null) return;

            if (fill.type != Image.Type.Filled)
                fill.type = Image.Type.Filled;

            if (fill.fillMethod != Image.FillMethod.Horizontal)
                fill.fillMethod = Image.FillMethod.Horizontal;

            // 从左往右填充：血量减少时右侧先消失（血条的常规观感）
            if (fill.fillOrigin != (int)Image.OriginHorizontal.Left)
                fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        }

        // Awake 一定先于 OnEnable，所以这里 fill / playerNode 都已就绪
        private void OnEnable()
        {
            GameEventBus.Instance.Player.On(GameEventBus.PlayerEvent.HURT, OnPlayerHealthChanged);
            GameEventBus.Instance.Player.On(GameEventBus.PlayerEvent.DEAD, OnPlayerHealthChanged);

            Refresh();   // 立刻同步一次，避免开局显示满格（或残留 Inspector 值）
        }

        private void OnDisable()
        {
            GameEventBus.Instance.Player.Off(GameEventBus.PlayerEvent.HURT, OnPlayerHealthChanged);
            GameEventBus.Instance.Player.Off(GameEventBus.PlayerEvent.DEAD, OnPlayerHealthChanged);
        }

        /// <summary>事件回调（参数是受伤值，这里不关心，直接按当前血量重绘）。</summary>
        private void OnPlayerHealthChanged(object data)
        {
            Refresh();
        }

        /// <summary>按当前血量比例重绘血条。</summary>
        public void Refresh()
        {
            if (fill == null || playerNode == null) return;
            print("更新血条");

            // maxHealth 是新加字段，已有场景组件反序列化后为 0 —— 兜底避免除零
            int denominator = maxHealth > 0 ? maxHealth : 100;

            fill.fillAmount = Mathf.Clamp01((float)playerNode.Health / denominator);
        }
    }
}
