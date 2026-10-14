using UnityEngine;
using UnityEngine.SceneManagement;
using PixelDash.Level;

namespace PixelDash.UI
{
    /// <summary>
    /// 结果场景（胜利/失败）控制器：重玩本关 / 返回选关 / 退出。
    ///
    /// 目标场景优先从 <see cref="SceneRecord"/> 读 —— 那是关卡场景里的 SceneRecord 跨场景带过来的记录：
    ///   重开     → SceneRecord.RestartScene     （= 刚刚打完的那一关）
    ///   返回选关 → SceneRecord.LevelSelectScene （= 选关界面）
    /// 没有记录时（例如直接在编辑器里跑胜利场景调试）才退回 Inspector 上的兜底值。
    ///
    /// 跳转统一走 SceneMgr 的**带 Loading 场景加载**，并且先校验目标场景 ——
    /// 否则目标无效时会先切进 Loading 场景再报错，玩家会卡在加载界面。
    /// </summary>
    public class ResultSceneController : MonoBehaviour
    {
        // ===== 兜底场景名（没有 SceneRecord 记录时用）=====
        [Header("兜底场景名（无 SceneRecord 记录时生效）")]
        [Tooltip("重开要回的关卡场景（正常应由 SceneRecord 覆盖）")]
        [SerializeField] private string restartScene = "FreshHandScene";

        [Tooltip("「返回选关」要去的选关场景")]
        [SerializeField] private string levelSelectScene = "EXChoosingScene";

        // ===== 加载 =====
        [Header("加载")]
        [Tooltip("Loading 中转场景名（留空 = 不用中转，直接切）")]
        [SerializeField] private string loadingScene = "LoadingScene";

        /// <summary>重新开始：回刚刚打完的那一关（读 SceneRecord）。</summary>
        public void Restart()
        {
            Load(Resolve(SceneRecord.RestartScene, restartScene), "重开");
        }

        /// <summary>返回选关（读 SceneRecord 里记的选关场景）。</summary>
        public void ReturnToLevelSelect()
        {
            Load(Resolve(SceneRecord.LevelSelectScene, levelSelectScene), "返回选关");
        }

        /// <summary>切换到指定场景（想从 Inspector 直接指定时用）。</summary>
        public void GoToScene(string sceneName)
        {
            Load(sceneName, "指定场景");
        }

        /// <summary>退出应用。</summary>
        public void Quit()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        /// <summary>记录里有的用记录，没有的用 Inspector 兜底值。</summary>
        private static string Resolve(string recorded, string fallback)
        {
            return string.IsNullOrEmpty(recorded) ? fallback : recorded;
        }

        /// <summary>
        /// 统一跳转出口：校验目标场景 → 复位 timeScale → 走 SceneMgr 的带 Loading 场景加载。
        /// 场景名空 / 未加入 Build Settings 都提前拦住并明确报错，不静默失败（也不会卡在 Loading 场景）。
        /// </summary>
        private void Load(string target, string who)
        {
            if (string.IsNullOrEmpty(target))
            {
                Debug.LogError($"[ResultSceneController] {who}：目标场景名为空" +
                               "（SceneRecord 没有记录，且 Inspector 上的兜底场景名也没填）。");
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(target))
            {
                Debug.LogError($"[ResultSceneController] {who}：场景 '{target}' 不在 Build Settings 里，无法加载。" +
                               "请到 File → Build Settings 把场景加进去，或修正场景名。");
                return;
            }

            Time.timeScale = 1f;   // 结果场景可能是从暂停态进来的，切换前恢复（与 ButtonChangeScene 一致）

            if (string.IsNullOrEmpty(loadingScene))
                SceneMgr.Instance.LoadScene(target);
            else
                SceneMgr.Instance.LoadSceneWithLoading(target, loadingScene);
        }
    }
}
