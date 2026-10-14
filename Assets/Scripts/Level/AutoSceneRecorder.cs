using UnityEngine;
using UnityEngine.SceneManagement;

namespace PixelDash.Level
{
    /// <summary>
    /// 场景记录器（镜像源项目 AutoSceneRecorder）：启动时记录当前场景名，供"重玩/返回"等按钮读取。
    /// </summary>
    public class AutoSceneRecorder : MonoBehaviour
    {
        /// <summary>最近一次记录的场景名（跨场景保留）。</summary>
        public static string CurrentSceneName { get; private set; }

        private void Awake()
        {
            CurrentSceneName = SceneManager.GetActiveScene().name;
        }

        /// <summary>手动记录指定场景名。</summary>
        public void Record(string sceneName)
        {
            CurrentSceneName = sceneName;
        }
    }
}
