using System;
using System.Collections.Generic;

namespace PixelDash.Core
{
    /// <summary>
    /// 事件总线：对应源项目 game_event_bus 的 5 管道。
    /// 用法：GameEventBus.Instance.Player.Emit("JUMP");
    ///       GameEventBus.Instance.Enemy.On("HIT_ENEMY", handler);
    /// </summary>
    public sealed class Channel
    {
        private readonly Dictionary<string, Action<object>> _handlers = new Dictionary<string, Action<object>>();

        public void On(string name, Action<object> handler)
        {
            if (handler == null) return;
            if (_handlers.TryGetValue(name, out var existing))
                _handlers[name] = existing + handler;
            else
                _handlers[name] = handler;
        }

        public void Off(string name, Action<object> handler)
        {
            if (handler == null || !_handlers.TryGetValue(name, out var existing)) return;
            var next = existing - handler;
            if (next == null) _handlers.Remove(name);
            else _handlers[name] = next;
        }

        public void Emit(string name, object data = null)
        {
            if (_handlers.TryGetValue(name, out var handler))
                handler?.Invoke(data);
        }

        public void Clear() => _handlers.Clear();
    }

    public sealed class GameEventBus
    {
        public static readonly GameEventBus Instance = new GameEventBus();
        public readonly Channel Player = new Channel();
        public readonly Channel Enemy = new Channel();
        public readonly Channel Bullet = new Channel();
        public readonly Channel Collection = new Channel();
        public readonly Channel Other = new Channel();

        /// <summary>事件名常量（与源项目 player_event/enemy_event/bullet_event/collection_event 对应）</summary>
        public static class PlayerEvent
        {
            public const string JUMP = "JUMP";
            public const string LAND = "LAND";
            public const string HURT = "HURT";
            public const string DEAD = "DEAD";
            public const string FIRE = "FIRE";

            /// <summary>
            /// 受伤音效事件：与 <see cref="HURT"/> 分开 —— HURT 管"数值变了"（血条等），本事件只管"响一声"。
            /// 让撞击方自带专属音效（如导弹爆炸）时能只发 HURT、不发本事件，避免两个音效叠在一起。
            /// </summary>
            public const string HURT_SFX = "HURT_SFX";
        }

        public static class EnemyEvent
        {
            public const string HIT_ENEMY = "HIT_ENEMY";
            public const string ENEMY_KILLED = "ENEMY_KILLED";
            public const string NORMAL_ONE_FLY_DEAD = "NORMAL_ONE_FLY_DEAD";
            public const string NORMAL_ONE_FLY_ATTACK = "NORMAL_ONE_FLY_ATTACK";
            public const string LASER_ATTACK = "LASER_ATTACK";
            public const string MISSILE_BOOM = "MISSILE_BOOM";
            public const string WARNING_ZONE_ENTER = "WARNING_ZONE_ENTER";
            public const string CIRCLE_SAW_HIT = "CIRCLE_SAW_HIT";
        }

        public static class BulletEvent
        {
            public const string HIT_PLAYER = "HIT_PLAYER";
            public const string HIT_ENEMY = "HIT_ENEMY";
        }

        public static class CollectionEvent
        {
            public const string COIN_COLLECTED = "COIN_COLLECTED";
            public const string BULLET_POWERUP_COLLECTED = "BULLET_POWERUP_COLLECTED";
        }
    }
}