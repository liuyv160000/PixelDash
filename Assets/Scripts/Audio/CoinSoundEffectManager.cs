using UnityEngine;
using PixelDash.Core;

namespace PixelDash.Audio
{
    /// <summary>金币拾取音效（镜像源项目 coin_sound_effect_manager）。</summary>
    public class CoinSoundEffectManager : AudioManagerBase
    {
        private void OnEnable()
        {
            // 强类型订阅（GameObject）：引用类型 payload，零装箱；发布方 NormalCoin 用 Emit(..., gameObject) 自动推断为 Emit<GameObject>
            GameEventBus.Instance.Collection.On<GameObject>(GameEventBus.CollectionEvent.COIN_COLLECTED, OnCollected);
        }

        private void OnDisable()
        {
            GameEventBus.Instance.Collection.Off<GameObject>(GameEventBus.CollectionEvent.COIN_COLLECTED, OnCollected);
        }

        private void OnCollected(GameObject coin) => PlaySfx("coin_collect");
    }
}
