using UnityEngine;
using PixelDash.Core;

namespace PixelDash.Level
{
    /// <summary>
    /// 相机死区跟随 + 受伤抖动：镜像源项目 camera_controller。
    /// 玩家在死区（BoxCollider2D 定义）内不动相机，超出则按动态速度平滑追到玩家。
    /// </summary>
    public class CameraController : SceneComponentBase
    {
        [Header("目标")]
        [SerializeField] private PixelDash.Player.PlayerController player;
        [SerializeField] private PixelDash.Player.PlayerMovementCtrl movementCtrl;

        [Header("死区")]
        [SerializeField] private BoxCollider2D deadZoneCollider;
        [SerializeField] private float minFollowSpeed = 10f;
        [SerializeField] private float followSpeedMultiplier = 2f;
        [SerializeField] private bool followX = true;
        [SerializeField] private bool followY = true;

        [Header("关卡边界（Y）")]
        [Tooltip("勾选后相机 Y 被限制在背景覆盖范围内，画面不会露出背景上下的空白")]
        [SerializeField] private bool clampYToBackground = true;

        [Tooltip("边界来源；留空则自动查找场景里的 BackgroundController")]
        [SerializeField] private PixelDash.Background.BackgroundController background;

        [Tooltip("在算出的边界上再向内收缩这么多世界单位（给画面留余量）")]
        [SerializeField] private float boundsPadding = 0f;

        [Tooltip("勾选后忽略背景自动边界，改用下面手填的世界 Y 范围")]
        [SerializeField] private bool useManualBoundsY = false;
        [SerializeField] private float manualMinY = 0f;
        [SerializeField] private float manualMaxY = 0f;

        private Vector3 _deadZoneOffset;
        private float _deadZoneWidth;
        private float _deadZoneHeight;
        private Vector3 _currentPos;

        // 关卡边界（已换算成"相机中心"允许的范围 = 世界边界向内扣掉相机半高）
        private bool _hasBoundsY;
        private float _minCamY;
        private float _maxCamY;

        // 抖动
        private float _shakeTimeLeft;
        private float _shakeStrength;
        private float _shakeFrequency = 30f;
        private float _shakeTimer;
        private Vector3 _shakeOffset;

        private void Awake()
        {
            _currentPos = transform.position;
            if (player == null)
                player = FindObjectOfType<PixelDash.Player.PlayerController>();
            if (movementCtrl == null && player != null)
                movementCtrl = player.GetComponent<PixelDash.Player.PlayerMovementCtrl>();
            if (deadZoneCollider == null)
            {
                var dz = GameObject.Find("dead_zone");
                if (dz != null) deadZoneCollider = dz.GetComponent<BoxCollider2D>();
            }

            if (deadZoneCollider != null)
            {
                var size = deadZoneCollider.size;
                var scale = deadZoneCollider.transform.lossyScale;
                _deadZoneWidth = Mathf.Abs(size.x * scale.x);
                _deadZoneHeight = Mathf.Abs(size.y * scale.y);
                _deadZoneOffset = deadZoneCollider.transform.position - transform.position;
            }

            CacheBoundsY();
        }

        /// <summary>
        /// 跟随必须放在 LateUpdate：此时玩家本帧的移动（Update / 物理 / FixedUpdate）已全部结算，
        /// 相机读到的是最终位置，不会因脚本执行顺序拿到上一帧位置而抖动。
        /// 顺序：先算跟随位置，再叠加抖动偏移。
        /// </summary>
        private void LateUpdate()
        {
            if (!IsPaused && player != null)
                UpdateCameraFollow(Time.deltaTime);

            ApplyShake(Time.deltaTime);
        }

        private void UpdateCameraFollow(float dt)
        {
            if (player == null) return;

            Vector3 playerPos = player.transform.position;
            float centerX = _currentPos.x + _deadZoneOffset.x;
            float centerY = _currentPos.y + _deadZoneOffset.y;
            float left = centerX - _deadZoneWidth / 2f;
            float right = centerX + _deadZoneWidth / 2f;
            float bottom = centerY - _deadZoneHeight / 2f;
            float top = centerY + _deadZoneHeight / 2f;

            bool inX = playerPos.x >= left && playerPos.x <= right;
            bool inY = playerPos.y >= bottom && playerPos.y <= top;
            bool inDeadZone = (!followX || inX) && (!followY || inY);
            if (inDeadZone) return;

            float targetX = _currentPos.x;
            float targetY = _currentPos.y;
            if (followX)
            {
                targetX = playerPos.x < left
                    ? playerPos.x - _deadZoneOffset.x + _deadZoneWidth / 2f
                    : playerPos.x - _deadZoneOffset.x - _deadZoneWidth / 2f;
            }
            if (followY)
            {
                targetY = playerPos.y < bottom
                    ? playerPos.y - _deadZoneOffset.y + _deadZoneHeight / 2f
                    : playerPos.y - _deadZoneOffset.y - _deadZoneHeight / 2f;
            }

            float speedX = minFollowSpeed, speedY = minFollowSpeed;
            if (movementCtrl != null)
            {
                speedX = Mathf.Max(minFollowSpeed, Mathf.Abs(movementCtrl.GetXSpeed()) * followSpeedMultiplier);
                speedY = Mathf.Max(minFollowSpeed, Mathf.Abs(movementCtrl.GetYSpeed()) * followSpeedMultiplier);
            }

            float dx = targetX - _currentPos.x;
            float dy = targetY - _currentPos.y;
            _currentPos.x += dx * Mathf.Min(1f, speedX * dt);
            _currentPos.y += dy * Mathf.Min(1f, speedY * dt);

            // 关卡边界：相机中心不得越过背景覆盖范围（边界值已扣掉相机半高）
            if (_hasBoundsY)
                _currentPos.y = Mathf.Clamp(_currentPos.y, _minCamY, _maxCamY);

            transform.position = _currentPos;
        }

        // ================= 关卡边界（Y） =================
        /// <summary>
        /// 计算相机的 Y 活动范围：取 BackgroundController **所有子节点**的 SpriteRenderer
        /// 世界包围盒并集作为关卡的上下边界（子节点即循环滚动的 city 图，Awake 时铺开的范围
        /// 就是关卡的可视范围），再扣掉相机自身可视半高，保证画面上下边缘始终压在背景内。
        ///
        /// X 轴不处理：本工程玩家固定 X、相机不在 X 上移动（前进感由背景 / 平台左移提供）。
        /// 只在 Awake 算一次并缓存：背景运行时会循环平移，每帧重算会让边界跟着漂。
        /// </summary>
        private void CacheBoundsY()
        {
            _hasBoundsY = false;
            if (!clampYToBackground) return;

            if (useManualBoundsY)
            {
                SetBoundsY(manualMinY, manualMaxY);
                return;
            }

            if (background == null)
                background = FindObjectOfType<PixelDash.Background.BackgroundController>();
            if (background == null)
            {
                Debug.LogWarning("[CameraController] 未找到 BackgroundController，Y 边界限制未启用", this);
                return;
            }

            bool any = false;
            float worldMinY = float.MaxValue, worldMaxY = float.MinValue;
            foreach (Transform child in background.transform)
            {
                if (child == null) continue;
                var sr = child.GetComponent<SpriteRenderer>();
                if (sr == null || sr.sprite == null) continue;

                if (sr.bounds.min.y < worldMinY) worldMinY = sr.bounds.min.y;
                if (sr.bounds.max.y > worldMaxY) worldMaxY = sr.bounds.max.y;
                any = true;
            }

            if (!any)
            {
                Debug.LogWarning("[CameraController] BackgroundController 下没有带 Sprite 的子节点，Y 边界限制未启用", this);
                return;
            }

            SetBoundsY(worldMinY, worldMaxY);
        }

        /// <summary>把世界边界换算成"相机中心"的允许范围（向内扣掉相机可视半高）。</summary>
        private void SetBoundsY(float worldMinY, float worldMaxY)
        {
            // 正交相机的可视半高就是 orthographicSize（与屏幕宽高比无关）
            float halfHeight = 0f;
            var cam = GetComponent<Camera>();
            if (cam != null && cam.orthographic)
                halfHeight = cam.orthographicSize;

            float lo = worldMinY + halfHeight + boundsPadding;
            float hi = worldMaxY - halfHeight - boundsPadding;

            // 背景总高不足一屏：退化成居中固定，避免 lo > hi 造成抖动
            if (hi < lo)
            {
                float mid = (lo + hi) * 0.5f;
                lo = hi = mid;
            }

            _minCamY = lo;
            _maxCamY = hi;
            _hasBoundsY = true;
        }

        private void ApplyShake(float dt)
        {
            if (_shakeTimeLeft > 0f)
            {
                _shakeTimeLeft -= dt;
                _shakeTimer += dt;
                if (_shakeTimer >= 1f / _shakeFrequency)
                {
                    _shakeTimer = 0f;
                    _shakeOffset.x = (Random.value * 2f - 1f) * _shakeStrength;
                    _shakeOffset.y = (Random.value * 2f - 1f) * _shakeStrength;
                }
            }
            else
            {
                _shakeOffset = Vector3.zero;
            }

            transform.position = _currentPos + _shakeOffset;
        }

        public void TriggerShake(float strength = 10f, float duration = 0.2f, float frequency = 30f)
        {
            _shakeStrength = Mathf.Max(0f, strength);
            _shakeTimeLeft = Mathf.Max(0f, duration);
            _shakeFrequency = Mathf.Max(1f, frequency);
            _shakeTimer = 0f;
        }

        // ===== 暂停协议：由 SceneComponentBase 统一管理（幂等）=====
        // 原来的 PauseFollow/ResumeFollow 无外部调用，直接并入统一协议
        protected override void OnPause() { }

        protected override void OnResume() { }
    }
}