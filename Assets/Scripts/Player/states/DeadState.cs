using PixelDash.Core;

namespace PixelDash.Player
{
    /// <summary>死亡状态：播死亡动画占位；0.5s 后加载失败场景。</summary>
    public class DeadState : FSMState
    {
        private readonly PlayerController _ctrl;

        public DeadState(PlayerController ctrl, string animBoolName) : base(ctrl, animBoolName)
        {
            _ctrl = ctrl;
        }

        /// <summary>不接动画时的构造（Animator bool 名留空，置位会被跳过）。</summary>
        public DeadState(PlayerController ctrl) : base(ctrl, null)
        {
            _ctrl = ctrl;
        }

        public override void OnEnter()
        {
            base.OnEnter();                 // 置动画 bool "Dead" = true（基类已按注册名置位）
            _ctrl.StartDeadEnd(); // 0.5s 后 LoadScene("FailScene")
        }
    }
}