using UnityEngine;
using PixelDash.Core;

namespace PixelDash.Audio
{
    /// <summary>
    /// 音乐播放控制（镜像源项目 music_play_ctrl）。
    ///
    /// 职责很窄：给外部（UI 按钮、场景流程、暂停系统）提供 pause / resume / stop 三个入口，
    /// 具体播放交给 <see cref="MusicAudioManager"/>。
    ///
    /// 与源项目的差异：源项目的 resume_music 用 play() —— 那是**从头重播**。
    /// 这里改用 Pause() / UnPause()，实现"从暂停处继续"。
    /// </summary>
    public class MusicPlayCtrl : SceneComponentBase
    {
        [Tooltip("目标音乐管理器（留空则自动查找场景里的 MusicAudioManager）")]
        [SerializeField] private MusicAudioManager musicManager;

        [Header("场景启动时自动播放")]
        [Tooltip("勾选后，Start 时自动播放下面这首曲目")]
        [SerializeField] private bool playOnStart = true;

        [Tooltip("要播放的曲目名（必须是音乐管理器「音效表」里登记的 name）")]
        [SerializeField] private string musicName;

        protected override bool StartPaused => true;

        private void Awake()
        {
            if (musicManager == null)
                musicManager = FindObjectOfType<MusicAudioManager>();
        }

        private void Start()
        {
            if (playOnStart && !string.IsNullOrEmpty(musicName))
                PlayMusic(musicName);
        }

        /// <summary>播放（换曲）。</summary>
        public void PlayMusic(string name)
        {
            if (musicManager == null || string.IsNullOrEmpty(name)) return;
            musicManager.PlayMusic(name);
        }

        /// <summary>暂停（保留播放位置）。</summary>
        public void PauseMusic()
        {
            if (musicManager != null) musicManager.PauseMusic();
        }

        /// <summary>从暂停处继续。</summary>
        public void ResumeMusic()
        {
            if (musicManager != null) musicManager.ResumeMusic();
        }

        /// <summary>停止（回到开头，下次播放会重头开始）。</summary>
        public void StopMusic()
        {
            if (musicManager != null) musicManager.StopMusic();
        }

        // ===== 暂停协议：让音乐跟着游戏一起冻结 / 恢复 =====

        protected override void OnPause() => PauseMusic();

        protected override void OnResume() => ResumeMusic();
    }
}
