using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using PixelDash.Core;

namespace PixelDash.UI
{
    /// <summary>
    /// 技能球公告（镜像源项目 background_announcer）：拾取技能球时弹出公告背景，
    /// 入场缩放（0.5→1.2→1）→ 停留 2s → 淡出。
    /// 源实现靠轮询场景中的技能球节点；本工程改为监听 BULLET_POWERUP_COLLECTED 事件（更可靠）。
    /// 美术资源：UI/Announce/（子弹时间.png / 强.png）在 Inspector 拖入。
    /// </summary>
    public class SkillOrbAnnouncer : MonoBehaviour
    {
        public static SkillOrbAnnouncer Instance { get; private set; }

        [Header("美术资源")]
        [SerializeField] private Sprite backgroundSprite;
        [Tooltip("可选：与公告一起弹出的文字（如 子弹时间 / 强）")]
        [SerializeField] private Sprite[] extraSprites;

        [Header("动画")]
        [SerializeField] private float animStartScale = 0.5f;
        [SerializeField] private float animEndScale = 1.2f;
        [SerializeField] private float animDuration = 0.3f;
        [SerializeField] private float displayDuration = 2f;
        [SerializeField] private float fadeOutDuration = 0.5f;

        private GameObject _current;
        private Coroutine _routine;

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
            GameEventBus.Instance.Collection.On<GameObject>(GameEventBus.CollectionEvent.BULLET_POWERUP_COLLECTED, OnPowerup);
        }

        private void OnDisable()
        {
            GameEventBus.Instance.Collection.Off<GameObject>(GameEventBus.CollectionEvent.BULLET_POWERUP_COLLECTED, OnPowerup);
        }

        private void OnPowerup(GameObject orb)
        {
            Show();
        }

        /// <summary>显示公告。</summary>
        public void Show()
        {
            Clear();
            Build();
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(AnimateRoutine());
        }

        private void Build()
        {
            if (backgroundSprite == null && (extraSprites == null || extraSprites.Length == 0))
                return; // 无美术时不显示（占位）

            _current = new GameObject("announce_root", typeof(RectTransform));
            _current.transform.SetParent(transform, false);
            var rootRt = _current.GetComponent<RectTransform>();
            rootRt.anchorMin = rootRt.anchorMax = new Vector2(0.5f, 0.5f);
            rootRt.pivot = new Vector2(0.5f, 0.5f);
            rootRt.anchoredPosition = Vector2.zero;

            if (backgroundSprite != null)
                AddImage(_current.transform, "bg", backgroundSprite, Vector2.zero);

            if (extraSprites != null)
            {
                for (int i = 0; i < extraSprites.Length; i++)
                    AddImage(_current.transform, "extra_" + i, extraSprites[i], new Vector2(0f, -120f * (i + 1)));
            }

            _current.transform.localScale = Vector3.one * animStartScale;
        }

        private static void AddImage(Transform parent, string name, Sprite sprite, Vector2 pos)
        {
            if (sprite == null) return;
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(sprite.rect.width, sprite.rect.height);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
        }

        private IEnumerator AnimateRoutine()
        {
            if (_current == null) yield break;

            float t = 0f;
            while (t < animDuration)
            {
                t += Time.deltaTime;
                float k = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / animDuration), 3f);
                _current.transform.localScale = Vector3.one * Mathf.Lerp(animStartScale, animEndScale, k);
                yield return null;
            }
            float t2 = 0f;
            while (t2 < animDuration * 0.5f)
            {
                t2 += Time.deltaTime;
                _current.transform.localScale = Vector3.one *
                    Mathf.Lerp(animEndScale, 1f, Mathf.Clamp01(t2 / (animDuration * 0.5f)));
                yield return null;
            }
            _current.transform.localScale = Vector3.one;

            yield return new WaitForSeconds(displayDuration);

            float t3 = 0f;
            while (t3 < fadeOutDuration)
            {
                t3 += Time.deltaTime;
                float k = Mathf.Clamp01(t3 / fadeOutDuration);
                _current.transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.8f, k);
                SetAlpha(_current, 1f - k);
                yield return null;
            }

            Clear();
        }

        private static void SetAlpha(GameObject root, float a)
        {
            foreach (var img in root.GetComponentsInChildren<Image>())
            {
                var c = img.color;
                c.a = a;
                img.color = c;
            }
        }

        public void HideImmediately() => Clear();

        public void TriggerFadeOut()
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(FadeOutNow());
        }

        private IEnumerator FadeOutNow()
        {
            if (_current == null) yield break;
            float t = 0f;
            while (t < fadeOutDuration)
            {
                t += Time.deltaTime;
                SetAlpha(_current, 1f - Mathf.Clamp01(t / fadeOutDuration));
                yield return null;
            }
            Clear();
        }

        private void Clear()
        {
            if (_current != null) Destroy(_current);
            _current = null;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
