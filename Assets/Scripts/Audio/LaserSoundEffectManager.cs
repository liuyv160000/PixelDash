using UnityEngine;
using PixelDash.Core;

namespace PixelDash.Audio
{
    /// <summary>激光发射音效（镜像源项目 laser_sound_effect_manager）。</summary>
    public class LaserSoundEffectManager : AudioManagerBase
    {
        private void OnEnable()
        {
            GameEventBus.Instance.Enemy.On<GameObject>(GameEventBus.EnemyEvent.LASER_ATTACK, OnAttack);
        }

        private void OnDisable()
        {
            GameEventBus.Instance.Enemy.Off<GameObject>(GameEventBus.EnemyEvent.LASER_ATTACK, OnAttack);
        }

        private void OnAttack(GameObject _) => PlaySfx("laser_attack");
    }
}
