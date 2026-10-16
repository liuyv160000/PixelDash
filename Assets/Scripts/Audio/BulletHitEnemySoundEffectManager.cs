using UnityEngine;
using PixelDash.Core;

namespace PixelDash.Audio
{
    /// <summary>玩家子弹命中敌人音效（镜像源项目 bullet_hit_enemy_sound_effect_manager）。</summary>
    public class BulletHitEnemySoundEffectManager : AudioManagerBase
    {
        private void OnEnable()
        {
            GameEventBus.Instance.Bullet.On<GameObject>(GameEventBus.BulletEvent.HIT_ENEMY, OnHit);
        }

        private void OnDisable()
        {
            GameEventBus.Instance.Bullet.Off<GameObject>(GameEventBus.BulletEvent.HIT_ENEMY, OnHit);
        }

        private void OnHit(GameObject _) => PlaySfx("bullet_hit_enemy");
    }
}
