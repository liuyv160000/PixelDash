using PixelDash.Core;

namespace PixelDash.Audio
{
    /// <summary>普通飞行敌人音效（镜像源项目 normal_one_fly_sound_effect_manager）：开火 + 死亡。</summary>
    public class NormalOneFlySoundEffectManager : AudioManagerBase
    {
        private void OnEnable()
        {
            var bus = GameEventBus.Instance.Enemy;
            bus.On(GameEventBus.EnemyEvent.NORMAL_ONE_FLY_ATTACK, OnAttack);
            bus.On(GameEventBus.EnemyEvent.NORMAL_ONE_FLY_DEAD, OnDead);
        }

        private void OnDisable()
        {
            var bus = GameEventBus.Instance.Enemy;
            bus.Off(GameEventBus.EnemyEvent.NORMAL_ONE_FLY_ATTACK, OnAttack);
            bus.Off(GameEventBus.EnemyEvent.NORMAL_ONE_FLY_DEAD, OnDead);
        }

        private void OnAttack(object _) => PlaySfx("normal_one_fly_attack");
        private void OnDead(object _) => PlaySfx("normal_one_fly_dead");
    }
}
