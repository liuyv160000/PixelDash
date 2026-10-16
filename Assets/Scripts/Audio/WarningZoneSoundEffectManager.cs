using UnityEngine;
using PixelDash.Core;

namespace PixelDash.Audio
{
    /// <summary>预警区出现音效（镜像源项目 warning_zone_sound_effect_manager）。</summary>
    public class WarningZoneSoundEffectManager : AudioManagerBase
    {
        private void OnEnable()
        {
            GameEventBus.Instance.Enemy.On<float>(GameEventBus.EnemyEvent.WARNING_ZONE_ENTER, OnEnter);
        }

        private void OnDisable()
        {
            GameEventBus.Instance.Enemy.Off<float>(GameEventBus.EnemyEvent.WARNING_ZONE_ENTER, OnEnter);
        }

        private void OnEnter(float duration) => PlaySfx("warning_zone_enter");
    }
}
