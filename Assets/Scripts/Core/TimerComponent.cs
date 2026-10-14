using UnityEngine;

namespace PixelDash.Core
{
    /// <summary>
    /// 计时器：镜像 Cocos 项目的 Timer 语义。
    /// - SetDuration(d)：设置时长（秒）
    /// - Start()：开始计时（running = true）
    /// - Stop()：暂停计时（running = false）
    /// - Reset()：清零 elapsed（保持 running 状态不变）
    /// - Restart()：清零并开始（对应源项目 reStart）
    /// - CheckIfEnd()：running 且 elapsed >= duration（duration>0 才生效）
    /// - GetElapsedTime()：已计时长
    /// </summary>
    public class TimerComponent : MonoBehaviour
    {
        public float Duration { get; private set; }
        public float Elapsed { get; private set; }
        public bool Running { get; private set; }

        public void SetDuration(float duration) => Duration = Mathf.Max(0f, duration);
        public void Start() => Running = true;
        public void Stop() => Running = false;
        public void Reset() => Elapsed = 0f;
        public void Restart() { Elapsed = 0f; Running = true; }

        public bool CheckIfEnd() => Running && Duration > 0f && Elapsed >= Duration;
        public float GetElapsedTime() => Elapsed;

        private void Update()
        {
            if (Running) Elapsed += Time.deltaTime;
        }
    }
}