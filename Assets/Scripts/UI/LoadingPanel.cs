using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Loading 场景的进度面板。
///
/// 显示逻辑：SceneMgr 广播真实进度 → 本脚本记为目标值 → 显示值以固定速度平滑追赶目标值。
/// 这样即使场景加载极快（进度瞬间从 0 跳到 1），进度条也会有一段可见的推进动画，
/// 而不是"唰"地一下闪满。
///
/// 配合 SceneMgr 的 minLoadingTime（最短显示时长）使用，效果最佳。
/// </summary>
public class LoadingPanel : MonoBehaviour
{
    [Header("引用（Inspector 拖拽）")]
    [Tooltip("0~1 的 Slider，Min=0 / Max=1")]
    public Slider progressBar;

    [Tooltip("Legacy Text（UI → Legacy → Text），显示百分比")]
    public Text progressText;

    [Header("显示调校")]
    [Tooltip("显示值追赶真实进度的速度（每秒涨多少，1 = 每秒 100%）。调小更慢更从容，调大更跟手。")]
    [SerializeField] private float catchUpSpeed = 2.5f;

    // 真实进度（SceneMgr 广播）、当前显示进度
    private float _target;
    private float _display;

    private void OnEnable()
    {
        // 每次进入 Loading 场景都从 0 开始，避免残留上一次的值
        _target = 0f;
        _display = 0f;
        Apply();
        SceneMgr.OnProgress += OnProgress;
    }

    private void OnDisable()
    {
        SceneMgr.OnProgress -= OnProgress;
    }

    /// <summary>收到真实进度：只前进不后退（防止进度条闪回）。</summary>
    private void OnProgress(float p)
    {
        if (p > _target) _target = p;
    }

    private void Update()
    {
        if (_display >= _target) return;

        // 用 unscaledDeltaTime：即使 timeScale 为 0（例如从暂停态切场景）也能正常推进
        _display = Mathf.MoveTowards(_display, _target, catchUpSpeed * Time.unscaledDeltaTime);
        Apply();
    }

    private void Apply()
    {
        if (progressBar) progressBar.value = _display;
        if (progressText) progressText.text = (_display * 100f).ToString("F0") + "%";
    }
}
