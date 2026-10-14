using System;
using System.Collections.Generic;
using UnityEngine;
using PixelDash.Core;

namespace PixelDash.Audio
{
    /// <summary>
    /// 音频管理器基类（镜像源项目 audio_manager_base）。
    ///
    /// 核心设计：**一个音效 = 一个常驻 AudioSource**（不是 PlayOneShot）。
    /// 用常驻源才能真正 StopSfx（停循环音）与逐源调音量；
    /// PlayOneShot 是"发射后不管"，停了、循环音都做不了。
    ///
    /// 配置方式：在 Inspector 的「音效表」里登记 name + clip，
    /// Awake 时自动为每一项建一个 AudioSource。
    ///
    /// 暂停：继承 <see cref="SceneComponentBase"/>，ExManager 统一遍历时自动 Pause/Resume，
    /// 用 AudioSource.Pause/UnPause 实现"从暂停处继续"。
    /// </summary>
    public abstract class AudioManagerBase : SceneComponentBase
    {
        /// <summary>一条音效登记项。</summary>
        [Serializable]
        public class SfxEntry
        {
            [Tooltip("音效名（供 PlaySfx(name) 查询）")]
            public string name;

            [Tooltip("音频片段")]
            public AudioClip clip;

            [Tooltip("是否循环播放")]
            public bool loop;

            [Tooltip("相对音量（最终音量 = 该值 × 全局音量）")]
            [Range(0f, 1f)] public float volumeScale = 1f;
        }

        [Header("音效表（name + clip；Awake 时自动建 AudioSource）")]
        [SerializeField] protected SfxEntry[] sfx = new SfxEntry[0];

        /// <summary>主音量（由 AlterAudioManager 设置）。static —— 跨场景、跨管理器保持。</summary>
        protected static float MasterVolume = 1f;

        /// <summary>静音因子：0 = 完全静音，1 = 正常。static，理由同上。</summary>
        protected static float SilenceFactor = 1f;

        /// <summary>当前实际生效的音量。</summary>
        protected static float UsedVolume => MasterVolume * SilenceFactor;

        private readonly Dictionary<string, AudioSource> _sources = new Dictionary<string, AudioSource>();

        protected virtual void Awake()
        {
            BuildSources();
        }

        /// <summary>为每条登记项建立 AudioSource，并登记到查找表。</summary>
        private void BuildSources()
        {
            _sources.Clear();
            if (sfx == null) return;

            foreach (var e in sfx)
            {
                if (e == null || string.IsNullOrEmpty(e.name) || e.clip == null) continue;
                if (_sources.ContainsKey(e.name)) continue;   // 重名只取第一条

                var src = gameObject.AddComponent<AudioSource>();
                src.clip = e.clip;
                src.loop = e.loop;
                src.playOnAwake = false;
                src.volume = UsedVolume * Mathf.Clamp01(e.volumeScale);

                _sources.Add(e.name, src);
            }
        }

        /// <summary>取指定音效的 AudioSource（不存在返回 null）。供子类精确操作某个源。</summary>
        protected AudioSource GetSource(string name)
            => _sources.TryGetValue(name, out var src) ? src : null;

        /// <summary>本管理器是否登记了指定名字的音效。</summary>
        public bool HasSfx(string name)
            => !string.IsNullOrEmpty(name) && _sources.ContainsKey(name);

        /// <summary>本管理器登记的所有音效名（供调试 / 全局查找用）。</summary>
        public IEnumerable<string> SfxNames => _sources.Keys;

        /// <summary>播放指定音效。</summary>
        /// <param name="loopOverride">覆盖登记项里的 loop 设置；null 表示沿用登记项。</param>
        public void PlaySfx(string name, bool? loopOverride = null)
        {
            if (!_sources.TryGetValue(name, out var src) || src == null) return;

            if (loopOverride.HasValue) src.loop = loopOverride.Value;
            src.volume = UsedVolume;
            src.Play();
        }

        /// <summary>停止指定音效。</summary>
        public void StopSfx(string name)
        {
            if (_sources.TryGetValue(name, out var src) && src != null) src.Stop();
        }

        /// <summary>暂停全部音效（保持播放位置）。</summary>
        public void PauseAllSfx()
        {
            foreach (var src in _sources.Values)
            {
                if (src != null && src.isPlaying) src.Pause();
            }
        }

        /// <summary>恢复全部音效（从暂停处继续）。</summary>
        public void ResumeAllSfx()
        {
            foreach (var src in _sources.Values)
            {
                if (src != null) src.UnPause();
            }
        }

        /// <summary>停止全部音效（回到开头）。</summary>
        public void StopAllSfx()
        {
            foreach (var src in _sources.Values)
            {
                if (src != null) src.Stop();
            }
        }

        /// <summary>把当前的全局音量重新应用到本管理器的所有音源。</summary>
        public void ApplyVolume()
        {
            foreach (var src in _sources.Values)
            {
                if (src != null) src.volume = UsedVolume;
            }
        }

        // ===== 全局音量（供 AlterAudioManager 调用）=====

        /// <summary>设置全局音量与静音因子。已存在的管理器需再调 <see cref="ApplyVolume"/> 才会立即生效。</summary>
        public static void SetGlobalVolume(float volume, float silenceFactor)
        {
            MasterVolume = Mathf.Clamp01(volume);
            SilenceFactor = Mathf.Clamp01(silenceFactor);
        }

        /// <summary>取主音量（不含静音因子）。</summary>
        public static float GetMasterVolume() => MasterVolume;

        // ===== 暂停协议：ExManager 统一遍历时自动调用 =====

        protected override void OnPause() => PauseAllSfx();

        protected override void OnResume() => ResumeAllSfx();
    }
}
