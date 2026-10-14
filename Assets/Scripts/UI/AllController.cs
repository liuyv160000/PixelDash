using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PixelDash.Core;

namespace PixelDash.UI
{
    /// <summary>单条教学文案。</summary>
    [Serializable]
    public class TipData
    {
        [Tooltip("文案内容")]
        public string text;

        [Tooltip("出现时刻（关卡绝对时间，秒）")]
        public float appearTime;

        [Tooltip("完全显示后保持的时长（秒），之后开始淡出")]
        public float duration = 2f;
    }

    /// <summary>
    /// 教学文案控制器：按时间表显示文案，alpha 淡入淡出。镜像 all_ctrler。
    ///
    /// 单条文案的生命周期：
    ///   到点 → FadeIn(fadeInTime) → Hold(duration) → FadeOut(fadeOutTime) → 清空
    ///
    /// 若下一条的 appearTime 在上一条播完之前到达，直接打断并切换（后来的覆盖先来的）。
    /// 初始暂停 —— 等 ExManager.StartGame() 才开始播。
    /// </summary>
    public class AllController : SceneComponentBase
    {
        private enum Phase { Idle, FadeIn, Hold, FadeOut }

        [SerializeField] private Text tipLabel;
        [SerializeField] private List<TipData> textTips = new List<TipData>();

        [Tooltip("淡入时长（秒）")]
        [SerializeField] private float fadeInTime = 0.3f;

        [Tooltip("淡出时长（秒）")]
        [SerializeField] private float fadeOutTime = 0.3f;

        /// <summary>关卡累计时间（**不归零**，与 appearTime 同为绝对时间轴）。</summary>
        private float _elapsed;

        /// <summary>下一条待播文案的索引。</summary>
        private int _index;

        // ---- 当前文案的状态机 ----
        private Phase _phase = Phase.Idle;
        private float _phaseTimer;
        private float _holdDuration;

        /// <summary>label 的原始颜色（只借用它的 RGB，alpha 由状态机驱动）。</summary>
        private Color _baseColor = Color.white;

        /// <summary>初始暂停 —— 等 ExManager.StartGame() 才开始播教学文案。</summary>
        protected override bool StartPaused => true;

        private void Awake()
        {
            if (tipLabel == null) tipLabel = GetComponent<Text>();

            if (tipLabel != null)
            {
                _baseColor = tipLabel.color;
                tipLabel.text = "";
                SetAlpha(0f);          // 起点透明，避免残留 Inspector 里的 alpha
            }

            // 未在 Inspector 配置时，使用内置的 fresh_hand 教学文案（16 条）
            if (textTips == null || textTips.Count == 0)
                textTips = BuildDefaultTips();

            textTips.Sort((a, b) => a.appearTime.CompareTo(b.appearTime));
        }

        /// <summary>fresh_hand 的 16 条教学文案（与源项目 all_ctrler 一致）。</summary>
        private static List<TipData> BuildDefaultTips()
        {
            return new List<TipData>
            {
                new TipData { text = "欢迎来到像素冲刺", appearTime = 2f, duration = 3f },
                new TipData { text = "按下 W 或者 ↑ 进行跳跃", appearTime = 7f, duration = 3f },
                new TipData { text = "接下来试着跳起来吃吃币吧~", appearTime = 10f, duration = 4f },
                new TipData { text = "好吃😋", appearTime = 16f, duration = 3f },
                new TipData { text = "跳起来在空中按 S 或 ⬇️ 可以快速MAN", appearTime = 20f, duration = 4f },
                new TipData { text = "MAN!!", appearTime = 25f, duration = 3f },
                new TipData { text = "位于线平台上半部分按 S 或 ⬇️ 可以翻转到另一侧", appearTime = 30f, duration = 4f },
                new TipData { text = "翻转后，对应的操作也会翻转", appearTime = 35f, duration = 4f },
                new TipData { text = "在下半侧，按 S 或 ⬇️ 跳跃", appearTime = 40f, duration = 4f },
                new TipData { text = "在空中控制按 W 或 ↑ 进行MAN", appearTime = 46f, duration = 4f },
                new TipData { text = "位于平台上时，按 W 或 ↑ 翻转回上半侧", appearTime = 51f, duration = 4f },
                new TipData { text = "躲避朝你袭来的敌人和子弹，避免被肘击", appearTime = 56f, duration = 5f },
                new TipData { text = "有时候敌人可能会过于？！强强！？", appearTime = 65f, duration = 4f },
                new TipData { text = "不过拾取技能球，你也能获得？！强强？！的力量", appearTime = 71f, duration = 4f },
                new TipData { text = "用？！强强！？的力量击败敌人罢", appearTime = 75f, duration = 5f },
                new TipData { text = "时间结束，你过↑关———", appearTime = 85f, duration = 5f },
            };
        }

        private void Update()
        {
            if (IsPaused) return;

            float dt = Time.deltaTime;

            // ① 推进关卡时间轴（只有这一个计时器，不再归零）
            _elapsed += dt;

            // ② 到点的文案全部取出来播（同一帧到期的只保留最后一条）
            while (_index < textTips.Count && _elapsed >= textTips[_index].appearTime)
            {
                ShowTip(textTips[_index]);
                _index++;
            }

            // ③ 推进当前文案的淡入/保持/淡出
            AdvancePhase(dt);
        }

        /// <summary>开始显示一条文案：从 alpha 0 淡入。</summary>
        private void ShowTip(TipData tip)
        {
            if (tipLabel != null) tipLabel.text = tip.text;

            _holdDuration = Mathf.Max(0f, tip.duration);
            _phase = Phase.FadeIn;
            _phaseTimer = 0f;
            SetAlpha(0f);
        }

        /// <summary>推进淡入 → 保持 → 淡出的状态机。全部用 dt 驱动，因此可被暂停冻结。</summary>
        private void AdvancePhase(float dt)
        {
            if (_phase == Phase.Idle) return;

            _phaseTimer += dt;

            switch (_phase)
            {
                case Phase.FadeIn:
                    if (fadeInTime <= 0f || _phaseTimer >= fadeInTime)
                    {
                        SetAlpha(1f);
                        _phase = Phase.Hold;
                        _phaseTimer = 0f;
                    }
                    else
                    {
                        SetAlpha(_phaseTimer / fadeInTime);
                    }
                    break;

                case Phase.Hold:
                    if (_phaseTimer >= _holdDuration)
                    {
                        _phase = Phase.FadeOut;
                        _phaseTimer = 0f;
                    }
                    break;

                case Phase.FadeOut:
                    if (fadeOutTime <= 0f || _phaseTimer >= fadeOutTime)
                    {
                        ClearLabel();
                    }
                    else
                    {
                        SetAlpha(1f - _phaseTimer / fadeOutTime);
                    }
                    break;
            }
        }

        /// <summary>结束当前文案：清空文本并回到待机（alpha 归 0）。</summary>
        private void ClearLabel()
        {
            if (tipLabel != null) tipLabel.text = "";
            _phase = Phase.Idle;
            _phaseTimer = 0f;
            SetAlpha(0f);
        }

        /// <summary>只改 alpha，保留 Inspector 里配的 RGB。</summary>
        private void SetAlpha(float a)
        {
            if (tipLabel == null) return;

            var c = _baseColor;
            c.a = Mathf.Clamp01(a);
            tipLabel.color = c;
        }

        // 暂停/恢复只需翻转 IsPaused（基类负责），Update 里已按 IsPaused 早退
    }
}
