using UnityEngine;
using PixelDash.Core;

namespace PixelDash.Audio
{
    /// <summary>玩家被子弹命中音效（镜像源项目 bullet_hit_player_sound_effect_manager）。</summary>
    public class BulletHitPlayerSoundEffectManager : AudioManagerBase
    {
        private void OnEnable()
        {
            GameEventBus.Instance.Bullet.On<GameObject>(GameEventBus.BulletEvent.HIT_PLAYER, OnHit);
        }

        private void OnDisable()
        {
            GameEventBus.Instance.Bullet.Off<GameObject>(GameEventBus.BulletEvent.HIT_PLAYER, OnHit);
        }

        private void OnHit(GameObject _) => PlaySfx("bullet_hit_player");
    }
}
