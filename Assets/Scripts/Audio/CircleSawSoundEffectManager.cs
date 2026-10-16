using UnityEngine;
using PixelDash.Core;

namespace PixelDash.Audio
{
    /// <summary>
    /// 电锯命中音效（镜像源项目 circle_saw_sound_effect_manager）。
    /// 注意：源项目定义了这个常量却从未 emit（那个管理器是死代码）；
    /// 本工程在 CircleSaw 切到玩家时补上了触发点。
    /// </summary>
    public class CircleSawSoundEffectManager : AudioManagerBase
    {
        private void OnEnable()
        {
            GameEventBus.Instance.Enemy.On<GameObject>(GameEventBus.EnemyEvent.CIRCLE_SAW_HIT, OnHit);
        }

        private void OnDisable()
        {
            GameEventBus.Instance.Enemy.Off<GameObject>(GameEventBus.EnemyEvent.CIRCLE_SAW_HIT, OnHit);
        }

        private void OnHit(GameObject _) => PlaySfx("circle_saw_hit");
    }
}
