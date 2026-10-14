using UnityEngine;

namespace PixelDash.Player
{
    /// <summary>
    /// 玩家移动控制：负责 Rigidbody2D 的速度 / 重力相关操作。
    /// 对应源项目 player 节点的移动控制组件（跳跃 / 快落 / 重力反转 / 速度读写）。
    /// </summary>
    public class PlayerMovementCtrl : MonoBehaviour
    {
        // 移动控制量。
        // 源项目（Cocos）原值：jump_force = 30、quick_down_force = 90（引擎物理单位）。
        // Unity 使用像素级世界坐标（1 unit = 1 px，Physics2D.gravity = -320），
        // 故换算为等效手感值：跳跃高度 ≈ 450²/(2×320×1.5) ≈ 202 px，与源项目一致。
        public float jump_force = 8f;
        public float quick_down_force = 20f;
        // 重力方向：1 = 重力向下（正常）；-1 = 重力向上（反转）
        public float gravity_reverse = 1f;

        /// <summary>基础重力缩放（与源项目 gravityScale 一致）。</summary>
        public const float GravityScale = 1.5f;

        private Rigidbody2D _rb;

        /// <summary>由 PlayerController 在初始化时注入 Rigidbody2D。</summary>
        public void Init(Rigidbody2D rb)
        {
            _rb = rb;
            ApplyGravity();
        }

        /// <summary>应用重力：bodyType=Dynamic、锁旋转、gravityScale = 2.5 * gravity_reverse。</summary>
        private void ApplyGravity()
        {
            if (_rb == null) return;
            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.freezeRotation = true;
            _rb.gravityScale = GravityScale * gravity_reverse;
        }

        /// <summary>反转重力：gravity_reverse 取反并重新应用到刚体。</summary>
        public void Reverse_the_gravity()
        {
            gravity_reverse = -gravity_reverse;
            ApplyGravity();
        }

        /// <summary>跳跃：沿重力反向给冲量 (0, gravity_reverse * jump_force)。</summary>
        public void Jump()
        {
            if (_rb == null) return;
            _rb.velocity = new Vector2(_rb.velocity.x, gravity_reverse * jump_force);
        }

        /// <summary>快速下落：(0, -gravity_reverse * quick_down_force)。</summary>
        public void quickDown()
        {
            if (_rb == null) return;
            _rb.velocity = new Vector2(_rb.velocity.x, -gravity_reverse * quick_down_force);
        }

        /// <summary>设置 Y 轴速度（保留当前 X 速度）。</summary>
        public void SetYSpeed(float y)
        {
            if (_rb == null) return;
            _rb.velocity = new Vector2(_rb.velocity.x, y);
        }

        /// <summary>读取 Y 轴速度。</summary>
        public float GetYSpeed()
        {
            return _rb != null ? _rb.velocity.y : 0f;
        }

        /// <summary>读取 X 轴速度（供相机跟随的速度同步使用）。</summary>
        public float GetXSpeed()
        {
            return _rb != null ? _rb.velocity.x : 0f;
        }

        /// <summary>读取当前重力方向（1 = 正常，-1 = 反转）。</summary>
        public float GetGravityReverse() => gravity_reverse;
    }
}