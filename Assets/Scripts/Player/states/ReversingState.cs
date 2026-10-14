using UnityEngine;
using PixelDash.Core;

namespace PixelDash.Player
{
    /// <summary>
    /// 反转状态：穿过平台线翻转到另一侧（重力反转）。
    /// 进入：记起始 y、沿重力方向给穿线速度、关碰撞体、翻转 scale.y；
    /// 位移达标 → air；退出：恢复碰撞体、清 y 速度、反转重力。
    /// </summary>
    public class ReversingState : FSMState
    {
        private readonly PlayerController _ctrl;
        private float _logY;
        private float _halfHeight;

        public ReversingState(PlayerController ctrl, string animBoolName) : base(ctrl, animBoolName)
        {
            _ctrl = ctrl;
        }

        public override void OnEnter()
        {
            base.OnEnter();                 // 置动画 bool = true
            _logY = _ctrl.transform.position.y;
            _halfHeight = _ctrl.GetHeight() * 1.1f;
            // 穿过速度：-height * gravity_reverse * 1.5
            _ctrl.SetYSpeed(-_ctrl.GetHeight() * _ctrl.gravity_reverse * 2.0f);
            _ctrl.SetColliderTrigger(true);
            _ctrl.FlipScaleY();
        }

        public override void OnUpdate(float deltaTime)
        {
            base.OnUpdate(deltaTime);       // 基类暂无逻辑，保持链式调用
            float dy = Mathf.Abs(_ctrl.transform.position.y - _logY);
            if (dy >= _halfHeight)
                _ctrl.ChangeState("air");
        }

        public override void OnExit()
        {
            base.OnExit();                  // 置动画 bool = false
            _ctrl.SetColliderTrigger(false);
            _ctrl.SetYSpeed(0f);
            _ctrl.ReverseGravity();
        }
    }
}
