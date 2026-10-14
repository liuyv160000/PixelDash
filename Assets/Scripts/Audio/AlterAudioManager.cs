using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using PixelDash.Core;

namespace PixelDash.Audio
{
    /// <summary>
    /// 音量总控 + 全局音频入口（镜像源项目 alter_audio_manager）。
    ///
    /// 职责：
    ///   ① 统一设置所有音频管理器的音量（支持静音/恢复、UI Slider 联动）
    ///   ② **跨场景常驻** —— 勾上 Persist Across Scenes 后切成 DontDestroyOnLoad，
    ///      成为全局唯一的音频接口；切换场景时新场景自带的重复实例会自动销毁
    ///   ③ 对外提供便捷查询（按名字找音效、直接播某个音效，不用先拿到具体管理器的引用）
    ///   ④ **自动接管音量滑块** —— 初次加载与每次场景切换时，
    ///      按对象名找到音量节点并取它上面的 Slider，无需手动拖引用
    ///
    /// 音量存在 <see cref="AudioManagerBase"/> 的 static 字段里，所以跨场景、跨管理器天然一致。
    /// </summary>
    public class AlterAudioManager : SceneComponentBase
    {
        /// <summary>全局唯一实例（只在 Persist Across Scenes 勾选时有效）。</summary>
        public static AlterAudioManager Instance { get; private set; }

        [Header("跨场景常驻")]
        [Tooltip("勾选后本对象切换场景不销毁，成为全局音量总控。\n" +
                 "注意：该节点上不要挂其它需要随场景销毁的组件。")]
        [SerializeField] private bool persistAcrossScenes = true;

        [Header("音量")]
        [Range(0f, 1f)]
        [Tooltip("初始主音量")]
        [SerializeField] private float initialVolume = 1f;

        [Header("UI 联动")]
        [Tooltip("音量滑块；留空则运行时按下面的节点名自动查找")]
        [SerializeField] private Slider volumeSlider;

        [Tooltip("音量滑块所在节点的名字。初次加载与每次场景切换时都会按这个名字重新查找并接管。")]
        [SerializeField] private string volumeNodeName = "VolumeCtrl";

        /// <summary>
        /// 静音因子：0 = 完全静音，1 = 正常。与 MasterVolume 分开保存，恢复时能还原原音量。
        /// **static** —— 跨场景、跨实例保持（否则切场景后静音会自己解除）。
        /// </summary>
        private static float _silenceFactor = 1f;

        /// <summary>
        /// 音量是否已被初始化过。**static** —— 用来区分"首次进入游戏"与"切换场景"：
        ///   首次：采用 Inspector 配置的 initialVolume
        ///   切场景：沿用玩家已调整过的音量，不被 Inspector 的旧值覆盖
        /// </summary>
        private static bool _volumeInitialized;

        /// <summary>本实例是否因单例去重而被销毁（其 Start 不应再动音量）。</summary>
        private bool _isDuplicate;

        // ================= 生命周期 =================

        private void Awake()
        {
            if (!persistAcrossScenes) return;

            // 单例去重：切场景时新场景若也带了本组件，销毁新的、保留常驻的那个
            if (Instance != null && Instance != this)
            {
                _isDuplicate = true;      // 标记后 Start 会直接返回，避免误改全局音量
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            // 常驻对象切场景时不会重跑 Start，必须靠事件通知才能重新接管新场景的滑块
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            if (_isDuplicate) return;   // 已被去重销毁，不要碰全局音量

            if (_volumeInitialized)
            {
                // 切换场景：沿用玩家已调整过的音量（AudioManagerBase 的 static 字段跨场景保留）
                SetVolume(AudioManagerBase.GetMasterVolume());
            }
            else
            {
                // 首次进入游戏：采用 Inspector 配置的初始音量
                SetVolume(initialVolume);
                _volumeInitialized = true;
            }

            ResolveVolumeSlider();     // 初次加载时接管
        }

        /// <summary>每次场景加载完成后重新查找并接管音量滑块。</summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ResolveVolumeSlider();
        }

        // ================= 音量滑块接管 =================

        /// <summary>
        /// 查找并接管音量滑块。
        ///
        /// 每次都重新查的原因：旧场景的 Slider 已随场景销毁，手里那个引用是悬空的
        /// （Unity 里表现为 "MissingReferenceException" 或静默失效）。
        /// </summary>
        private void ResolveVolumeSlider()
        {
            // 先摘掉挂在旧滑块上的监听，避免重复叠加
            if (volumeSlider != null)
                volumeSlider.onValueChanged.RemoveListener(OnSliderChanged);

            Slider found = FindVolumeSlider(volumeNodeName);

            if (found == null)
            {
                volumeSlider = null;
                return;
            }

            volumeSlider = found;

            // 让滑块初始位置与当前音量对齐（先设值再加监听，避免设值时触发回调）
            volumeSlider.value = AudioManagerBase.GetMasterVolume();
            volumeSlider.onValueChanged.AddListener(OnSliderChanged);
        }

        /// <summary>
        /// 按对象名查找音量滑块。
        ///
        /// Slider 可能就挂在同名节点上，也可能挂在其子物体上，两种都覆盖。
        /// 用 FindObjectsOfType 而不是 GameObject.Find：前者能带上未激活的对象，
        /// 且不会因为名字里有层级路径而找不到。
        /// </summary>
        private static Slider FindVolumeSlider(string nodeName)
        {
            if (string.IsNullOrEmpty(nodeName)) return null;

            foreach (var s in FindObjectsOfType<Slider>(true))
            {
                if (s == null) continue;

                if (s.gameObject.name == nodeName) return s;

                var parent = s.transform.parent;
                if (parent != null && parent.name == nodeName) return s;
            }

            return null;
        }

        // ================= 音量 =================

        /// <summary>Slider 值变化回调。</summary>
        public void OnSliderChanged(float value) => SetVolume(value);

        /// <summary>设置主音量，并立即应用到当前场景内所有音频管理器。</summary>
        public void SetVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);

            // 回写本字段：
            //   ① 让它始终代表"当前音量"，切场景时新实例在 Inspector 里看到的也是实际值
            //   ② 首次初始化后即成为后续场景的"初始音量"
            initialVolume = volume;

            AudioManagerBase.SetGlobalVolume(volume, _silenceFactor);

            foreach (var m in FindObjectsOfType<AudioManagerBase>())
            {
                if (m != null) m.ApplyVolume();
            }
        }

        /// <summary>取当前主音量。</summary>
        public float GetVolume() => AudioManagerBase.GetMasterVolume();

        /// <summary>静音（保留原音量值，<see cref="ResumeVolume"/> 可还原）。</summary>
        public void SilenceVolume()
        {
            _silenceFactor = 0f;
            SetVolume(AudioManagerBase.GetMasterVolume());
        }

        /// <summary>取消静音。</summary>
        public void ResumeVolume()
        {
            _silenceFactor = 1f;
            SetVolume(AudioManagerBase.GetMasterVolume());
        }

        /// <summary>当前是否处于静音状态。</summary>
        public bool IsSilenced => _silenceFactor <= 0f;

        /// <summary>当前接管的滑块（可能为 null —— 该场景没有音量节点）。</summary>
        public Slider VolumeSlider => volumeSlider;

        // ===== 全局音频入口：不必先拿到具体管理器的引用 =====

        /// <summary>取当前场景里所有音效管理器（每次都实时查，切场景后返回新场景的）。</summary>
        public AudioManagerBase[] GetAllManagers() => FindObjectsOfType<AudioManagerBase>();

        /// <summary>
        /// 在所有管理器里播放指定的音效（按名字匹配第一个登记了它的管理器）。
        /// 便于 UI 按钮之类不知道具体归属的场景调用。
        /// </summary>
        /// <returns>是否找到并播放。</returns>
        public bool PlaySfxAnywhere(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;

            foreach (var m in FindObjectsOfType<AudioManagerBase>())
            {
                if (m != null && m.HasSfx(name))
                {
                    m.PlaySfx(name);
                    return true;
                }
            }

            Debug.LogWarning("[AlterAudioManager] 没有管理器登记音效：" + name, this);
            return false;
        }

        /// <summary>在所有管理器里停止指定的音效。</summary>
        public bool StopSfxAnywhere(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;

            foreach (var m in FindObjectsOfType<AudioManagerBase>())
            {
                if (m != null && m.HasSfx(name))
                {
                    m.StopSfx(name);
                    return true;
                }
            }
            return false;
        }
    }
}
