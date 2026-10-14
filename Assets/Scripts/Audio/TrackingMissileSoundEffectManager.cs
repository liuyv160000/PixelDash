using PixelDash.Core;

namespace PixelDash.Audio
{
    /// <summary>追踪导弹爆炸音效（镜像源项目 tracking_missile_sound_effect_manager）。</summary>
    public class TrackingMissileSoundEffectManager : AudioManagerBase
    {
        private void OnEnable()
        {
            GameEventBus.Instance.Enemy.On(GameEventBus.EnemyEvent.MISSILE_BOOM, OnBoom);
        }

        private void OnDisable()
        {
            GameEventBus.Instance.Enemy.Off(GameEventBus.EnemyEvent.MISSILE_BOOM, OnBoom);
        }

        private void OnBoom(object _) => PlaySfx("missile_boom");
    }
}
