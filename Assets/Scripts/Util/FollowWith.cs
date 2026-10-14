using UnityEngine;

namespace PixelDash.Util
{
    /// <summary>
    /// 平滑跟随（镜像源项目 follow_with）：带偏移地跟随目标，支持分轴开关与平滑/瞬时模式。
    /// </summary>
    public class FollowWith : MonoBehaviour
    {
        [Header("跟随目标")]
        public Transform target;

        [Tooltip("平滑速度（≤0 = 瞬时跟随）")]
        public float smoothSpeed = 5f;

        public bool followX = true;
        public bool followY = true;
        public bool followZ = true;

        private Vector3 _offset;
        private Vector3 _currentPos;
        private Vector3 _targetPos;
        private bool _hasInit;

        private void Start()
        {
            if (target != null) RefreshOffset();
        }

        private void LateUpdate()
        {
            if (target == null) return;

            if (!_hasInit)
            {
                RefreshOffset();
                return;
            }

            UpdateTargetPosition();

            if (smoothSpeed <= 0f || Time.deltaTime <= 0f)
            {
                transform.position = _targetPos;
                _currentPos = _targetPos;
            }
            else
            {
                // 帧率无关的指数平滑
                float k = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);
                _currentPos = Vector3.Lerp(_currentPos, _targetPos, k);
                transform.position = _currentPos;
            }
        }

        /// <summary>设置跟随目标（偏移自动计算）。</summary>
        public void Follow(Transform t)
        {
            target = t;
            _hasInit = false;
        }

        /// <summary>设置目标并指定偏移。</summary>
        public void FollowWithOffset(Transform t, Vector3 offset)
        {
            target = t;
            _offset = offset;
            _hasInit = true;
            _currentPos = transform.position;
            UpdateTargetPosition();
            transform.position = _targetPos;
        }

        public void SetOffset(Vector3 offset) => _offset = offset;

        public void StopFollow()
        {
            target = null;
            _hasInit = false;
        }

        /// <summary>按当前相对位置重算偏移。</summary>
        public void RefreshOffset()
        {
            if (target == null) return;
            _offset = transform.position - target.position;
            _hasInit = true;
            _currentPos = transform.position;
        }

        private void UpdateTargetPosition()
        {
            if (target == null) return;
            Vector3 p = target.position;
            _targetPos = new Vector3(
                followX ? p.x + _offset.x : transform.position.x,
                followY ? p.y + _offset.y : transform.position.y,
                followZ ? p.z + _offset.z : transform.position.z);
        }
    }
}
