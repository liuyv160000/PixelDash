using UnityEngine;

namespace PixelDash.Core
{
    /// <summary>
    /// 场景组件基类：所有需要"被统一暂停/恢复"的场景元素都继承它。
    ///
    /// 设计意图：
    ///   - 暂停协议集中在这里（幂等保护 + 状态跟踪），子类只管实现自己的 <see cref="OnPause"/> / <see cref="OnResume"/>
    ///   - **ExManager 不再逐个硬引用**（player / background / enemyManager …），
    ///     而是遍历场景里所有 <see cref="SceneComponentBase"/> 统一 Pause/Resume
    ///
    /// 幂等性：重复调用 Pause() 只生效一次，避免"管理器转发 + ExManager 直调"两条路径互相干扰。
    ///
    /// 初始状态：默认**不暂停**（这样没接 ExManager 时组件也能独立工作）；
    /// 需要"初始就暂停、等 ExManager 开跑"的组件覆写 <see cref="StartPaused"/> 返回 true。
    ///
    /// 注意：本类**不定义 Awake/Update**，避免 Unity 因派生类同签名私有方法而屏蔽基类初始化。
    /// </summary>
    public abstract class SceneComponentBase : MonoBehaviour
    {
        /// <summary>显式的暂停状态。null = 还没被设置过，此时取 <see cref="StartPaused"/>。</summary>
        private bool? _paused;

        /// <summary>
        /// 初始是否处于暂停态。子类覆写（例如倒计时、教学文案需要"等游戏开始才动"）。
        /// 默认 false —— 未接入 ExManager 时也能独立运行。
        /// </summary>
        protected virtual bool StartPaused => false;

        /// <summary>当前是否处于暂停态。</summary>
        public bool IsPaused => _paused ?? StartPaused;

        /// <summary>暂停（幂等）。子类覆写 <see cref="OnPause"/>，不要覆写本方法。</summary>
        public virtual void Pause()
        {
            if (IsPaused) return;
            _paused = true;
            OnPause();
        }

        /// <summary>恢复（幂等）。子类覆写 <see cref="OnResume"/>，不要覆写本方法。</summary>
        public virtual void Resume()
        {
            if (!IsPaused) return;
            _paused = false;
            OnResume();
        }

        /// <summary>子类实现：暂停时要做什么（停计时器、停物理、停位移、停生成……）。</summary>
        protected virtual void OnPause() { }

        /// <summary>子类实现：恢复时要做什么。</summary>
        protected virtual void OnResume() { }
    }
}
