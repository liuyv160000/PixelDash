using System.Collections;
using UnityEngine;
using PixelDash.Core;
using PixelDash.Item;

namespace PixelDash.Announcement
{
    /// <summary>
    /// 背景播报器（镜像源项目 background_announcer）：
    /// **玩家拾取技能球时，在屏幕上播放一次全屏背景动画**（缩放冲入 → 停留 → 淡出）。
    ///
    /// 与源项目的关键差异：
    ///   ① 源项目用 update 轮询"技能球节点是否失效"来判断被拾取；
    ///      本工程直接订阅 <c>CollectionEvent.BULLET_POWERUP_COLLECTED</c> 事件 ——
    ///      更准、更省，且不依赖节点名（源项目那个字符串是硬编码的 "bullet_skill_ball"）。
    ///   ② Cocos 的 tween / backOut / sineIn 在 Unity 没有对应物，改用协程 + 手写缓动。
    ///   ③ 透明度同时兼容 UI（CanvasGroup）与 Sprite（SpriteRenderer）两种背景。
    ///
    /// 暂停：继承 <see cref="SceneComponentBase"/>，但动画用 <c>WaitForSeconds</c> 而非
    /// <c>WaitForSecondsRealtime</c>，所以 timeScale = 0 时会自然冻结，无需额外处理。
    /// </summary>
    public class BackgroundAnnouncer : SceneComponentBase
    {
        /// <summary>全局唯一实例。</summary>
        public static BackgroundAnnouncer Instance { get; private set; }

        [Header("背景预制体")]
        [Tooltip("播报时实例化的全屏背景预制体")]
        [SerializeField] private GameObject backgroundPrefab;

        [Tooltip("生成后挂到这个节点下。留空则挂到本节点的父节点（与源项目一致）")]
        [SerializeField] private Transform spawnParent;

        [Header("动画参数")]
        [Tooltip("入场动画起始缩放")]
        [SerializeField] private float animStartScale = 0.5f;

        [Tooltip("入场动画冲到的目标缩放（会略微过冲，之后再收回 1）")]
        [SerializeField] private float animEndScale = 1.2f;

        [Tooltip("入场动画持续时间（秒）")]
        [SerializeField] private float animDuration = 0.3f;

        [Tooltip("停留时间（秒）")]
        [SerializeField] private float displayDuration = 2.0f;

        [Tooltip("淡出动画持续时间（秒）")]
        [SerializeField] private float fadeOutDuration = 0.5f;

        [Tooltip("淡出时的目标缩放")]
        [SerializeField] private float fadeOutEndScale = 0.8f;

        [Header("触发时机")]
        [Tooltip("拾取技能球时自动播放（整个场景只触发一次）")]
        [SerializeField] private bool triggerOnSkillOrb = true;

        [Tooltip("是否只触发一次。取消勾选则每次拾取技能球都播")]
        [SerializeField] private bool triggerOnlyOnce = true;

        // ---- 运行时状态 ----
        private GameObject _current;
        private Coroutine _routine;
        private bool _hasTriggered;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            if (!triggerOnSkillOrb) return;
            GameEventBus.Instance.Collection.On<GameObject>(
                GameEventBus.CollectionEvent.BULLET_POWERUP_COLLECTED, OnSkillOrbCollected);
        }

        private void OnDisable()
        {
            GameEventBus.Instance.Collection.Off<GameObject>(
                GameEventBus.CollectionEvent.BULLET_POWERUP_COLLECTED, OnSkillOrbCollected);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// 技能球被拾取 → 判断该球是否要求播报，是则播放背景动画。
        ///
        /// 事件数据是技能球的 GameObject（<c>BulletSkillOrb</c> 里 Emit 时传的 gameObject），
        /// 因此可以直接读它的 <see cref="BulletSkillOrb.TriggersAnnouncement"/> ——
        /// **只有勾了"触发播报"的球才会播放**，普通技能球静默通过。
        /// </summary>
        private void OnSkillOrbCollected(GameObject go)
        {
            if (go == null)
            {
                Debug.LogWarning("[BackgroundAnnouncer] 事件数据为空，无法判断是否播报。", this);
                return;
            }

            // 组件可能挂在子物体上，所以也往上找一层
            var orb = go.GetComponent<BulletSkillOrb>() ?? go.GetComponentInParent<BulletSkillOrb>();
            if (orb == null || !orb.TriggersAnnouncement) return;   // 普通技能球：不播报

            if (triggerOnlyOnce && _hasTriggered) return;

            _hasTriggered = true;
            ShowBackground();
        }

        // ================= 对外接口 =================

        /// <summary>播放一次背景动画（会先清掉上一轮）。</summary>
        public void ShowBackground()
        {
            ClearCurrent();
            _routine = StartCoroutine(PlayRoutine());
        }

        /// <summary>立即隐藏并清除背景。</summary>
        public void HideImmediately()
        {
            ClearCurrent();
        }

        /// <summary>手动触发淡出（提前结束停留）。</summary>
        public void TriggerFadeOut()
        {
            if (_current == null) return;

            // 停掉主流程，改走"直接淡出"这条捷径
            var routine = _routine;
            _routine = null;
            if (routine != null) StopCoroutine(routine);

            _routine = StartCoroutine(FadeOutRoutine());
        }

        /// <summary>复位"只触发一次"的状态（重开一局时用）。</summary>
        public void ResetTrigger()
        {
            _hasTriggered = false;
        }

        /// <summary>是否已经触发过。</summary>
        public bool HasTriggered => _hasTriggered;

        // ================= 动画主流程 =================

        private IEnumerator PlayRoutine()
        {
            if (backgroundPrefab == null)
            {
                Debug.LogWarning("[BackgroundAnnouncer] 未设置背景预制体，跳过播放。", this);
                yield break;
            }

            // 与源项目一致：挂到父节点下，位置归零
            Transform parent = spawnParent != null ? spawnParent : transform.parent;
            _current = Instantiate(backgroundPrefab, parent);
            _current.transform.localPosition = Vector3.zero;

            var view = new BackgroundView(_current);

            // ① 入场：animStartScale → animEndScale（backOut，略微过冲）
            yield return ScaleRoutine(view, animStartScale, animEndScale, animDuration, EaseKind.BackOut);

            // ② 收拢：animEndScale → 1（sineOut，时长减半）
            yield return ScaleRoutine(view, animEndScale, 1f, animDuration * 0.5f, EaseKind.SineOut);

            // ③ 停留
            yield return WaitScaled(displayDuration);

            // ④ 淡出
            yield return FadeOutRoutine();

            _routine = null;
        }

        /// <summary>缩放 + 透明度同时归零，然后清除节点。</summary>
        private IEnumerator FadeOutRoutine()
        {
            if (_current == null) { ClearCurrent(); yield break; }

            var view = new BackgroundView(_current);
            float t = 0f;
            float fromScale = _current.transform.localScale.x;
            float fromAlpha = view.Alpha;

            while (t < fadeOutDuration)
            {
                t += Time.deltaTime;
                float k = Ease.SineIn(Mathf.Clamp01(t / fadeOutDuration));

                float s = Mathf.Lerp(fromScale, fadeOutEndScale, k);
                _current.transform.localScale = new Vector3(s, s, 1f);
                view.Alpha = Mathf.Lerp(fromAlpha, 0f, k);

                yield return null;
            }

            ClearCurrent();
            _routine = null;
        }

        /// <summary>从 from 缩放到 to 的通用过程（可选缓动）。</summary>
        private IEnumerator ScaleRoutine(BackgroundView view, float from, float to, float duration, EaseKind ease)
        {
            if (duration <= 0f || _current == null)
            {
                if (_current != null) SetScale(_current.transform, to);
                yield break;
            }

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Ease.Apply(ease, Mathf.Clamp01(t / duration));
                SetScale(_current.transform, Mathf.Lerp(from, to, k));
                yield return null;
            }

            if (_current != null) SetScale(_current.transform, to);
        }

        /// <summary>
        /// 按缩放后的时间等待。
        /// 用 WaitForSeconds 而不是 Realtime —— timeScale = 0（游戏暂停）时动画一并冻结，
        /// 和场景里其它对象保持一致。
        /// </summary>
        private static IEnumerator WaitScaled(float seconds)
        {
            if (seconds <= 0f) yield break;
            yield return new WaitForSeconds(seconds);
        }

        private static void SetScale(Transform tr, float s) => tr.localScale = new Vector3(s, s, 1f);

        /// <summary>清掉当前背景节点与正在跑的过程。</summary>
        private void ClearCurrent()
        {
            // 先取出引用再置空 —— 否则 StopCoroutine 有可能停在调用者自己身上
            var routine = _routine;
            _routine = null;
            if (routine != null) StopCoroutine(routine);

            if (_current != null) Destroy(_current);
            _current = null;
        }

        // ================= 背景视图：兼容 UI 与 Sprite =================

        /// <summary>
        /// 背景透明度访问器。
        ///
        /// 背景既可能是 UGUI（CanvasGroup / Graphic），也可能是 SpriteRenderer，
        /// 这里自动挑一个能用的来读写，调用方不必关心。
        ///
        /// 用 class 而不是 struct：它持有 Unity 对象引用，而 Unity 重载了 == 做"已销毁"判断，
        /// 值类型复制会让这套语义变得难以预测。
        /// </summary>
        private class BackgroundView
        {
            private readonly CanvasGroup _group;
            private readonly UnityEngine.UI.Graphic _graphic;
            private readonly SpriteRenderer _sprite;

            public BackgroundView(GameObject go)
            {
                // 优先 CanvasGroup：它能连同子物体一起淡出
                _group = go.GetComponent<CanvasGroup>();
                if (_group == null && go.GetComponent<UnityEngine.UI.Graphic>() != null)
                    _group = go.AddComponent<CanvasGroup>();

                _graphic = go.GetComponent<UnityEngine.UI.Graphic>();
                _sprite = go.GetComponent<SpriteRenderer>();
            }

            public float Alpha
            {
                get
                {
                    if (_group != null) return _group.alpha;
                    if (_graphic != null) return _graphic.color.a;
                    if (_sprite != null) return _sprite.color.a;
                    return 1f;
                }
                set
                {
                    value = Mathf.Clamp01(value);

                    if (_group != null) _group.alpha = value;
                    if (_graphic != null)
                    {
                        var c = _graphic.color; c.a = value; _graphic.color = c;
                    }
                    if (_sprite != null)
                    {
                        var c = _sprite.color; c.a = value; _sprite.color = c;
                    }
                }
            }
        }

        // ================= 缓动（对应 Cocos 的 easing 名）=================

        private enum EaseKind { Linear, BackOut, SineIn, SineOut }

        private static class Ease
        {
            /// <summary>backOut 的过冲系数，与 Cocos / CSS 的 back 系列一致。</summary>
            private const float BackC1 = 1.70158f;

            public static float Apply(EaseKind kind, float t)
            {
                switch (kind)
                {
                    case EaseKind.BackOut: return BackOut(t);
                    case EaseKind.SineIn: return SineIn(t);
                    case EaseKind.SineOut: return SineOut(t);
                    default: return t;
                }
            }

            /// <summary>先冲过目标再回弹（入场用）。</summary>
            public static float BackOut(float t)
            {
                float c3 = BackC1 + 1f;
                float p = t - 1f;
                return 1f + c3 * p * p * p + BackC1 * p * p;
            }

            /// <summary>慢起快收（淡出用）。</summary>
            public static float SineIn(float t) => 1f - Mathf.Cos(t * Mathf.PI * 0.5f);

            /// <summary>快起慢收（收拢用）。</summary>
            public static float SineOut(float t) => Mathf.Sin(t * Mathf.PI * 0.5f);
        }
    }
}
