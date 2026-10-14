using PixelDash.Core;

namespace PixelDash.Audio
{
    /// <summary>金币拾取音效（镜像源项目 coin_sound_effect_manager）。</summary>
    public class CoinSoundEffectManager : AudioManagerBase
    {
        private void OnEnable()
        {
            GameEventBus.Instance.Collection.On(GameEventBus.CollectionEvent.COIN_COLLECTED, OnCollected);
        }

        private void OnDisable()
        {
            GameEventBus.Instance.Collection.Off(GameEventBus.CollectionEvent.COIN_COLLECTED, OnCollected);
        }

        private void OnCollected(object _) => PlaySfx("coin_collect");
    }
}
