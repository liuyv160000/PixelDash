using PixelDash.Core;

namespace PixelDash.Audio
{
    /// <summary>
    /// 玩家音效（镜像源项目 player_audio_manager）。
    /// 源项目只监听 JUMP / LAND / FIRE；本工程额外接受伤 / 死亡，
    /// 受伤音效走 <see cref="GameEventBus.PlayerEvent.HURT_SFX"/>（不是 HURT）——
    /// 撞击方自带专属音效时只发 HURT 不发 HURT_SFX，免得两个音叠在一起。
    /// </summary>
    public class PlayerAudioManager : AudioManagerBase
    {
        private void OnEnable()
        {
            var bus = GameEventBus.Instance.Player;
            bus.On(GameEventBus.PlayerEvent.JUMP, OnJump);
            bus.On(GameEventBus.PlayerEvent.LAND, OnLand);
            bus.On(GameEventBus.PlayerEvent.FIRE, OnFire);
            bus.On(GameEventBus.PlayerEvent.HURT_SFX, OnHurt);
            bus.On(GameEventBus.PlayerEvent.DEAD, OnDead);
        }

        private void OnDisable()
        {
            var bus = GameEventBus.Instance.Player;
            bus.Off(GameEventBus.PlayerEvent.JUMP, OnJump);
            bus.Off(GameEventBus.PlayerEvent.LAND, OnLand);
            bus.Off(GameEventBus.PlayerEvent.FIRE, OnFire);
            bus.Off(GameEventBus.PlayerEvent.HURT_SFX, OnHurt);
            bus.Off(GameEventBus.PlayerEvent.DEAD, OnDead);
        }

        private void OnJump(object _) => PlaySfx("player_jump");
        private void OnLand(object _) => PlaySfx("player_land");
        private void OnFire(object _) => PlaySfx("player_fire");
        private void OnHurt(object _) => PlaySfx("player_hurt");
        private void OnDead(object _) => PlaySfx("player_dead");
    }
}
