using System;
using System.Collections.Generic;

namespace PixelDash.Core
{
    /// <summary>
    /// 事件总线的一条管道（对应源项目的 player_event / enemy_event / bullet_event / collection_event）。
    ///
    /// 存储层用 <see cref="Delegate"/> 基类承载不同签名，派发时按运行时类型分发：
    ///   ① 强类型订阅 <c>On&lt;T&gt;</c> / 发布 <c>Emit&lt;T&gt;</c>：T 为值类型时**不装箱**（CLR 泛型实例化）；
    ///   ② 旧签名 <c>On(string, Action&lt;object&gt;)</c>：保留，行为与升级前完全一致；
    ///   ③ 无 payload 事件 <c>On(string, Action)</c>：不必再传一个无意义的 null。
    ///
    /// 约束（重要）：**同一个事件名的所有订阅者必须用同一种签名**（<c>Action</c> / <c>Action&lt;object&gt;</c> / <c>Action&lt;T&gt;</c>）。
    /// 混用会被拒绝并上报（<c>Delegate.Combine</c> 要求运行时类型完全一致，否则会抛 ArgumentException）。
    /// 所以迁移要**按事件整体迁移**，不能只改其中一个订阅者。
    ///
    /// 本类不依赖 UnityEngine：错误上报走 <see cref="OnError"/> 钩子，由 GameEventBusBridge 接到 Debug.LogError。
    /// </summary>
    public sealed class Channel
    {
        private readonly Dictionary<string, Delegate> _handlers = new Dictionary<string, Delegate>();

        /// <summary>错误上报钩子（纯 C# 侧留口，Unity 侧由 GameEventBusBridge 注入）。</summary>
        public static Action<string> OnError;

        private static void Report(string msg) => OnError?.Invoke(msg);

        /// <summary>当前登记的事件名数量（调试用）。</summary>
        public int EventCount => _handlers.Count;

        /// <summary>把委托类型打印成可读签名（如 Action&lt;int&gt;）。</summary>
        private static string SigName(Delegate d)
        {
            var t = d.GetType();
            if (!t.IsGenericType) return t.Name;
            var args = t.GetGenericArguments();
            var parts = new string[args.Length];
            for (int i = 0; i < args.Length; i++) parts[i] = args[i].Name;
            return t.Name.Split('`')[0] + "<" + string.Join(", ", parts) + ">";
        }

        // ---------------------------------------------------------------- 订阅
        /// <summary>订阅无 payload 事件。</summary>
        public void On(string name, Action handler) => Add(name, handler);

        /// <summary>订阅旧签名事件（payload 为 object）。</summary>
        public void On(string name, Action<object> handler) => Add(name, handler);

        /// <summary>订阅强类型事件（推荐）：T 为值类型时不装箱。</summary>
        public void On<T>(string name, Action<T> handler) => Add(name, handler);

        private void Add(string name, Delegate handler)
        {
            if (handler == null) return;

            if (_handlers.TryGetValue(name, out var existing))
            {
                if (existing.GetType() != handler.GetType())
                {
                    Report($"[EventBus] \"{name}\" 订阅签名不一致：已有 {SigName(existing)}，" +
                           $"新来 {SigName(handler)} —— 本次订阅被忽略（同一事件的订阅者签名必须统一）");
                    return;
                }
                _handlers[name] = Delegate.Combine(existing, handler);
            }
            else
            {
                _handlers[name] = handler;
            }
        }

        // ---------------------------------------------------------------- 退订
        /// <summary>退订无 payload 事件。</summary>
        public void Off(string name, Action handler) => Remove(name, handler);

        /// <summary>退订旧签名事件。</summary>
        public void Off(string name, Action<object> handler) => Remove(name, handler);

        /// <summary>退订强类型事件。</summary>
        public void Off<T>(string name, Action<T> handler) => Remove(name, handler);

        private void Remove(string name, Delegate handler)
        {
            if (handler == null) return;
            if (!_handlers.TryGetValue(name, out var existing)) return;

            // Delegate.Remove 按 (target, method) 匹配 —— 方法组每次求值都是新委托实例，但相等性成立
            var next = Delegate.Remove(existing, handler);
            if (next == null) _handlers.Remove(name);
            else _handlers[name] = next;
        }

        // ---------------------------------------------------------------- 发布
        /// <summary>发布无 payload 事件。</summary>
        public void Emit(string name)
        {
            if (!_handlers.TryGetValue(name, out var d)) return;

            if (d is Action action) action();
            else if (d is Action<object> boxed) boxed(null);        // 兼容旧签名订阅者（它们不关心 payload）
            else Report($"[EventBus] \"{name}\" 以无参形式发布，但订阅方签名是 {SigName(d)}（需要带 payload 发布）");
        }

        /// <summary>发布事件（payload 为 object，旧 API）。</summary>
        public void Emit(string name, object data)
        {
            if (!_handlers.TryGetValue(name, out var d)) return;

            if (d is Action<object> boxed) boxed(data);
            else if (d is Action action) action();
            else
            {
                // 订阅方是强类型、却以 object 形式发布：反射兜底（罕见路径，正确性优先）
                try { d.DynamicInvoke(data); }
                catch (Exception e) { Report($"[EventBus] \"{name}\" 以 object 发布时派发失败：{e.InnerException?.Message ?? e.Message}"); }
            }
        }

        /// <summary>发布强类型事件（推荐）：T 为值类型时不装箱。</summary>
        public void Emit<T>(string name, T data)
        {
            if (!_handlers.TryGetValue(name, out var d)) return;

            if (d is Action<T> typed) { typed(data); return; }
            if (d is Action<object> boxed) { boxed(data); return; }   // 旧签名订阅者（引用类型 payload 无装箱）
            if (d is Action action) { action(); return; }             // 订阅方只关心"事情发生了"
            Report($"[EventBus] \"{name}\" 发布 payload 类型不匹配：订阅方 {SigName(d)}，发布 {typeof(T).Name}");
        }

        /// <summary>整条管道清空（硬重置：只丢引用，不通知订阅者）。</summary>
        public void Clear() => _handlers.Clear();
    }

    /// <summary>
    /// 事件总线：持有 5 条管道 + 事件名常量。
    /// 纯 C# 类（非 MonoBehaviour）：跨场景存活，故订阅必须在 OnDisable 对称退订。
    /// </summary>
    public sealed class GameEventBus
    {
        public static readonly GameEventBus Instance = new GameEventBus();

        public readonly Channel Player = new Channel();
        public readonly Channel Enemy = new Channel();
        public readonly Channel Bullet = new Channel();
        public readonly Channel Collection = new Channel();
        public readonly Channel Other = new Channel();

        /// <summary>清空所有管道（供 Domain Reload 重置等场景使用）。</summary>
        public void ClearAll()
        {
            Player.Clear();
            Enemy.Clear();
            Bullet.Clear();
            Collection.Clear();
            Other.Clear();
        }

        /// <summary>事件名常量（与源项目 player_event / enemy_event / bullet_event / collection_event 对应）</summary>
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
