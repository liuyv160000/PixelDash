using PixelDash.Core;

namespace PixelDash.Player
{
    /// <summary>待机状态：播 idle 动画占位。</summary>
    public class IdleState : FSMState
    {
        private readonly PlayerController _ctrl;

        public IdleState(PlayerController ctrl, string animBoolName) : base(ctrl, animBoolName)
        {
            _ctrl = ctrl;
        }

        public override void OnEnter()
        {
            base.OnEnter();                 // 置动画 bool "Idle" = true（基类已按注册名置位，这里不用再调 PlayAnim）
        }

        public override void OnExit()
        {
             base.OnExit();                 // 置动画 bool = false
        }
    }
}