using UnityEngine;
using UnityEngine.UI;
using PixelDash.Core;

namespace PixelDash.UI
{
    /// <summary>
    /// 计时显示：从 0 开始**正计时**，格式 MM:SS（镜像源项目 time_showe，改为正计时）。
    /// 初始暂停 —— 等 ExManager.StartGame() 才开始计时；暂停时冻结。
    /// </summary>
    public class TimeShower : SceneComponentBase
    {
        [SerializeField] private Text label;

        private float _elapsed;

        /// <summary>初始暂停 —— 等 ExManager.StartGame() 才开始计时。</summary>
        protected override bool StartPaused => true;

        private void Awake()
        {
            if (label == null) label = GetComponent<Text>();
            _elapsed = 0f;
            UpdateLabel();
        }

        private void Update()
        {
            if (IsPaused) return;

            _elapsed += Time.deltaTime;
            UpdateLabel();
        }

        /// <summary>已计时长（秒）。</summary>
        public float ElapsedTime => _elapsed;

        /// <summary>把计时归零（重开一局 / 切关卡时用）。</summary>
        public void ResetTimer()
        {
            _elapsed = 0f;
            UpdateLabel();
        }

        /// <summary>
        /// 刷新文本为 MM:SS。
        /// 秒向下取整（不足 1 秒仍显示 00）；分钟超过 99 时不截断，按实际位数显示（如 120:05）。
        /// </summary>
        private void UpdateLabel()
        {
            if (label == null) return;

            int totalSeconds = Mathf.FloorToInt(_elapsed);
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;

            label.text = minutes.ToString("00") + ":" + seconds.ToString("00");
        }

        // 暂停/恢复只需翻转 IsPaused（基类负责），Update 里已按 IsPaused 早退
    }
}
