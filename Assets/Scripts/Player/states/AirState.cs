using PixelDash.Core;

namespace PixelDash.Player
{
    /// <summary>空中状态：置空中标志；落地由碰撞回调触发 Land() → idle。</summary>
    public class AirState : FSMState
    {
        private readonly PlayerController _ctrl;

        public AirState(PlayerController ctrl, string animBoolName) : base(ctrl, animBoolName)
        {
            _ctrl = ctrl;
        }

        public override void OnEnter()
        {
            base.OnEnter();                 // 置动画 bool = true
            _ctrl.ifAir = true;
            _ctrl.ifGround = false;
        }
        
        public override void OnUpdate(float deltaTime)
        {
            base.OnUpdate(deltaTime);       // 基类暂无逻辑，保持链式调用
        }

        public override void OnExit()
        {
            base.OnExit();                 // 置动画 bool = false
            _ctrl.ifAir = false;
            _ctrl.ifGround = true;
        }

    }
}