using PixelDash.Core;

namespace PixelDash.Audio
{
    /// <summary>
    /// 通用敌人被击杀音效（本工程新增）。
    ///
    /// 源项目没有这个管理器 —— 它靠各敌人的专属事件（normal_one_fly_dead 等）分别出声。
    /// 本工程的 EnemyEvent.ENEMY_KILLED 是任何敌人死亡都会发的通用事件，
    /// 单独一个管理器承接，避免在每个专属管理器里重复播放同一条音效。
    /// </summary>
    public class EnemyKilledSoundEffectManager : AudioManagerBase
    {
        private void OnEnable()
        {
            GameEventBus.Instance.Enemy.On(GameEventBus.EnemyEvent.ENEMY_KILLED, OnKilled);
        }

        private void OnDisable()
        {
            GameEventBus.Instance.Enemy.Off(GameEventBus.EnemyEvent.ENEMY_KILLED, OnKilled);
        }

        private void OnKilled(object _) => PlaySfx("enemy_killed");
    }
}
