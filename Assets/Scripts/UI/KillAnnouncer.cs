using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PixelDash.Core;

namespace PixelDash.UI
{
    /// <summary>
    /// 击杀公告（镜像源项目 announcer）：监听敌人死亡事件，显示「击杀数 + Kill 字样」，
    /// 入场缩放动画（0.5→1.2→1）→ 停留 → 淡出。
    /// 美术资源：UI/Number/0-9.png、UI/Announce/kill.png（在 Inspector 拖入）。
    /// </summary>
    public class KillAnnouncer : MonoBehaviour
    {
        public static KillAnnouncer Instance { get; private set; }

        [Header("美术资源")]
        [Tooltip("数字 0-9 的 Sprite（10 个，按 0~9 顺序）")]
        [SerializeField] private Sprite[] digitSprites = new Sprite[10];
        [SerializeField] private Sprite killSprite;

        [Header("布局")]
        [Tooltip("单个数字时的字间距")]
        [SerializeField] private float digitSpacing = 60f;

        [Tooltip("两位数及以上时的字间距（比单个数字更紧：两位数靠太开会显得散）")]
        [SerializeField] private float multiDigitSpacing = 42f;

        [SerializeField] private float verticalSpacing = -80f;

        [Header("动画")]
        [SerializeField] private float animStartScale = 0.5f;
        [SerializeField] private float animEndScale = 1.2f;
        [SerializeField] private float animDuration = 0.3f;
        [SerializeField] private float displayDuration = 1.5f;
        [SerializeField] private float fadeOutDuration = 0.5f;

        private int _killCount;
        private readonly List<GameObject> _spawned = new List<GameObject>();
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
            GameEventBus.Instance.Enemy.On(GameEventBus.EnemyEvent.ENEMY_KILLED, OnEnemyKilled);
        }

        private void OnDisable()
        {
            GameEventBus.Instance.Enemy.Off(GameEventBus.EnemyEvent.ENEMY_KILLED, OnEnemyKilled);
        }

        private void OnEnemyKilled(object data)
        {
            _killCount++;
            Show(_killCount);
        }

        /// <summary>播报指定击杀数。</summary>
        public void Show(int count)
        {
            _killCount = count;
            ClearDisplay();

            if (_routine != null) StopCoroutine(_routine);
            BuildDisplay(count);
            _routine = StartCoroutine(AnimateRoutine());
        }

        private void BuildDisplay(int count)
        {
            string digits = count.ToString();

            // 位数越多字间距越紧：两位及以上用 multiDigitSpacing，避免两位数字看起来分得太开
            float spacing = digits.Length >= 2 ? multiDigitSpacing : digitSpacing;
            float x0 = -(digits.Length - 1) * spacing * 0.2f;

            for (int i = 0; i < digits.Length; i++)
            {
                int d = digits[i] - '0';
                if (d < 0 || d > 9) continue;
                var go = CreateSprite("digit_" + d,
                    d < digitSprites.Length ? digitSprites[d] : null,
                    new Vector2(x0 + i * spacing, 0f));
                if (go != null) _spawned.Add(go);
            }

            var killGo = CreateSprite("kill_text", killSprite, new Vector2(0f, verticalSpacing));
            if (killGo != null) _spawned.Add(killGo);
        }

        private GameObject CreateSprite(string name, Sprite sprite, Vector2 anchoredPos)
        {
            if (sprite == null) return null;
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(sprite.rect.width, sprite.rect.height);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            go.transform.localScale = Vector3.one * animStartScale;
            return go;
        }

        private IEnumerator AnimateRoutine()
        {
            // 入场：放大到 animEndScale（backOut 感）再回落到 1
            float t = 0f;
            while (t < animDuration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / animDuration);
                float eased = 1f - Mathf.Pow(1f - k, 3f); // 近似 backOut 的收束
                float s = Mathf.Lerp(animStartScale, animEndScale, eased);
                SetScale(s);
                yield return null;
            }
            float t2 = 0f;
            while (t2 < animDuration * 0.5f)
            {
                t2 += Time.deltaTime;
                SetScale(Mathf.Lerp(animEndScale, 1f, Mathf.Clamp01(t2 / (animDuration * 0.5f))));
                yield return null;
            }
            SetScale(1f);

            yield return new WaitForSeconds(displayDuration);

            // 淡出：缩放 0.8 + alpha 0
            float t3 = 0f;
            while (t3 < fadeOutDuration)
            {
                t3 += Time.deltaTime;
                float k = Mathf.Clamp01(t3 / fadeOutDuration);
                SetScale(Mathf.Lerp(1f, 0.8f, k));
                SetAlpha(1f - k);
                yield return null;
            }

            ClearDisplay();
        }

        private void SetScale(float s)
        {
            foreach (var go in _spawned)
                if (go != null) go.transform.localScale = Vector3.one * s;
        }

        private void SetAlpha(float a)
        {
            foreach (var go in _spawned)
            {
                if (go == null) continue;
                var img = go.GetComponent<Image>();
                if (img != null) { var c = img.color; c.a = a; img.color = c; }
            }
        }

        private void ClearDisplay()
        {
            foreach (var go in _spawned)
                if (go != null) Destroy(go);
            _spawned.Clear();
        }

        /// <summary>供外部直接加一次击杀（如测试）。</summary>
        public void AddKill() => Show(_killCount + 1);

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
