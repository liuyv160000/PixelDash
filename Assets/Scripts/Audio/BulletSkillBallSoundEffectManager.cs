using PixelDash.Core;

namespace PixelDash.Audio
{
    /// <summary>技能球拾取音效（镜像源项目 bullet_skill_ball_sound_effect_manager）。</summary>
    public class BulletSkillBallSoundEffectManager : AudioManagerBase
    {
        private void OnEnable()
        {
            GameEventBus.Instance.Collection.On(GameEventBus.CollectionEvent.BULLET_POWERUP_COLLECTED, OnCollected);
        }

        private void OnDisable()
        {
            GameEventBus.Instance.Collection.Off(GameEventBus.CollectionEvent.BULLET_POWERUP_COLLECTED, OnCollected);
        }

        private void OnCollected(object _) => PlaySfx("bullet_powerup_collect");
    }
}
