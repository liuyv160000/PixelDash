using UnityEngine;
using PixelDash.Core;

namespace PixelDash.Enemy
{
    /// <summary>
    /// 场景敌人管理器（对应源项目 scene_enemy_manager_new）。
    ///
    /// **只负责管理同节点下的敌人生成器**：
    ///   - 持有各生成器引用（留空则自动收集本节点及子节点上的所有 <see cref="EnemyMakerBase"/>）
    ///   - 把 Pause/Resume 转发给各生成器
    ///
    /// 它自己**不写任何生成逻辑** —— 生成时间表、预制体、参数全在各生成器里。
    /// </summary>
    public class SceneEnemyManager : SceneComponentBase
    {
        [Header("敌人生成器")]
        [Tooltip("留空则自动收集本节点及子节点上的所有 EnemyMakerBase")]
        [SerializeField] private EnemyMakerBase[] makers;

        [Header("唤醒定位")]
        [Tooltip("唤醒时把本节点放到主相机视口右边缘之外这么远（世界单位）。生成物即从画面外飞入。")]
        [SerializeField] private float spawnOffsetX = 50f;

        [Tooltip("唤醒时 Y 坐标是否对齐线平台（plat_line）")]
        [SerializeField] private bool alignYToPlatLine = true;

        private void Awake()
        {
            // 唤醒定位：视口右边缘外侧 + offsetX，Y 与线平台一致
            ViewportAnchor.AlignToRightOutsideView(transform, spawnOffsetX, alignYToPlatLine);

            // 自动收集：数组为空，或数组里全是空引用（Inspector 里拖剩的占位槽）
            if (IsMakersEmpty())
                makers = GetComponentsInChildren<EnemyMakerBase>(true);
        }

        private bool IsMakersEmpty()
        {
            if (makers == null || makers.Length == 0) return true;
            foreach (var m in makers)
            {
                if (m != null) return false;
            }
            return true;
        }

        // ===== 暂停协议：转发给同节点下的各生成器 =====
        // 注意：生成器自身也继承 SceneComponentBase，所以 ExManager 统一遍历时它们会被直接调用；
        // 这里的转发是为了支持"只持有管理器引用"的旧调用方式，重复调用由幂等保护消化。

        protected override void OnPause()
        {
            if (makers == null) return;
            foreach (var m in makers)
            {
                if (m != null) m.Pause();
            }
        }

        protected override void OnResume()
        {
            if (makers == null) return;
            foreach (var m in makers)
            {
                if (m != null) m.Resume();
            }
        }
    }
}
