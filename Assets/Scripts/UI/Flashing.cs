using UnityEngine;
using UnityEngine.UI;

namespace PixelDash.UI
{
    /// <summary>
    /// 文字闪烁（镜像源项目 flashing）：用正弦波平滑改变透明度。
    /// </summary>
    public class Flashing : MonoBehaviour
    {
        [Tooltip("闪烁速度（次/秒）")]
        [SerializeField] private float flashSpeed = 2f;

        [Tooltip("最小透明度（0-1）")]
        [SerializeField] private float minOpacity = 50f / 255f;

        [Tooltip("最大透明度（0-1）")]
        [SerializeField] private float maxOpacity = 1f;

        [SerializeField] private bool autoStart = true;

        private Graphic _graphic;
        private float _elapsed;
        private bool _flashing;

        private void Awake()
        {
            _graphic = GetComponent<Graphic>();
            if (_graphic == null)
                Debug.LogWarning("[Flashing] 未找到 Graphic（Text/Image）组件");
        }

        private void Start()
        {
            if (autoStart) StartFlashing();
        }

        private void Update()
        {
            if (!_flashing || _graphic == null) return;

            _elapsed += Time.deltaTime;
            float normalized = (Mathf.Sin(_elapsed * flashSpeed * 2f * Mathf.PI) + 1f) * 0.5f;
            float a = Mathf.Lerp(minOpacity, maxOpacity, normalized);

            var c = _graphic.color;
            c.a = a;
            _graphic.color = c;
        }

        public void StartFlashing()
        {
            if (_graphic == null) return;
            _flashing = true;
            _elapsed = 0f;
        }

        public void StopFlashing()
        {
            _flashing = false;
            if (_graphic != null)
            {
                var c = _graphic.color;
                c.a = 1f;
                _graphic.color = c;
            }
        }

        public void PauseFlashing() => _flashing = false;

        public void ResumeFlashing()
        {
            if (_graphic != null) _flashing = true;
        }

        public void SetFlashSpeed(float speed) => flashSpeed = Mathf.Max(0.1f, speed);

        public void SetOpacityRange(float min, float max)
        {
            minOpacity = Mathf.Clamp01(min);
            maxOpacity = Mathf.Clamp01(max);
        }
    }
}
