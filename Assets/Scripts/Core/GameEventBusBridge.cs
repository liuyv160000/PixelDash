using UnityEngine;

namespace PixelDash.Core
{
    /// <summary>
    /// 把纯 C# 的事件总线接到 Unity：
    ///   ① 错误上报 → <c>Debug.LogError</c>（总线本身不依赖 UnityEngine，留的是 OnError 钩子）；
    ///   ② 关闭 Domain Reload 时清理静态订阅（静态字段不会随 Play 重置，否则会残留上一次的订阅）。
    /// </summary>
    public static class GameEventBusBridge
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            Channel.OnError = msg => Debug.LogError(msg, null);

            // 编辑器里勾掉 Enter Play Mode Options → Domain Reload 时，静态状态不会被重置
            GameEventBus.Instance.ClearAll();
        }
    }
}
