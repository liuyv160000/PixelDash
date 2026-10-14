using PixelDash.Core;

namespace PixelDash.Player
{
    /// <summary>
    /// 跳跃状态：进入时给冲量并置空中标志；当相对重力方向开始下落时切换 air。
    /// </summary>
    public class JumpState : FSMState
    {
        private readonly PlayerController _ctrl;

        public JumpState(PlayerController ctrl, string animBoolName) : base(ctrl, animBoolName)
        {
            _ctrl = ctrl;
        }

        public override void OnEnter()
        {
            base.OnEnter();                 // 置动画 bool "Jump" = true（基类已按注册名置位）
            // 给冲量 + ifAir=true, ifGround=false
            _ctrl.Jump();
        }

        public override void OnUpdate(float deltaTime)
        {
            base.OnUpdate(deltaTime);       // 基类暂无逻辑，保持链式调用
            // y 速度开始相对重力反向（下落）→ 切 air
            if (_ctrl.IsFallingRelativeToGravity())
                _ctrl.ChangeState("air");
        }

        public override void OnExit()
        {
            base.OnExit();                 // 置动画 bool = false
        }
    }
}