using UnityEngine;

namespace PixelDash.Audio
{
    /// <summary>
    /// 背景音乐管理器（镜像源项目 music_audio_manager）。
    ///
    /// 与普通音效管理器的区别：
    ///   - 追踪「当前正在播放的那一首」（<see cref="NowPlaying"/>）
    ///   - 提供 Pause / Resume（**从暂停处继续**，而不是重新从头播）
    ///   - 提供 <see cref="PlayMusic"/> 换曲（自动停掉上一首）
    ///
    /// 暂停协议来自基类 —— ExManager 统一 Pause/Resume 时，音乐也跟着冻结。
    /// </summary>
    public class MusicAudioManager : AudioManagerBase
    {
        /// <summary>当前正在播放的 AudioSource（换曲 / 暂停时用）。</summary>
        public AudioSource NowPlaying { get; private set; }

        /// <summary>当前曲目名。</summary>
        public string NowPlayingName { get; private set; }

        /// <summary>
        /// 播放指定曲目（自动停掉上一首）。
        /// 曲目名必须是「音效表」里登记过的 name。
        /// </summary>
        public void PlayMusic(string name, bool loop = true)
        {
            if (NowPlayingName == name && NowPlaying != null && NowPlaying.isPlaying) return;  // 同一首不重启

            StopMusic();

            PlaySfx(name, loop);
            NowPlaying = GetSource(name);      // 基类的查找表里精确取回，不靠 isPlaying 猜
            NowPlayingName = name;
        }

        /// <summary>停止当前曲目（回到开头）。</summary>
        public void StopMusic()
        {
            if (NowPlaying != null) NowPlaying.Stop();
            NowPlaying = null;
            NowPlayingName = null;
        }

        /// <summary>暂停当前曲目（保留播放位置）。</summary>
        public void PauseMusic()
        {
            if (NowPlaying != null && NowPlaying.isPlaying) NowPlaying.Pause();
        }

        /// <summary>从暂停处继续。</summary>
        public void ResumeMusic()
        {
            if (NowPlaying != null) NowPlaying.UnPause();
        }

        // ===== 暂停协议：音乐也用 Pause/UnPause（保留播放位置）=====

        protected override void OnPause() => PauseMusic();

        protected override void OnResume() => ResumeMusic();
    }
}
