using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PixelDash.UI
{
    /// <summary>
    /// 切换场景按钮：仅作为事件触发器，把加载逻辑交给 SceneMgr。
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ButtonChangeScene : MonoBehaviour
    {
        [Tooltip("目标场景名（需已加入 Build Settings）")]
        [SerializeField] private string targetScene;

        [Tooltip("是否使用 Loading 场景中转")]
        [SerializeField] private bool useLoadingScene = true;

        [Tooltip("Loading 场景名（useLoadingScene 为 true 时生效）")]
        [SerializeField] private string loadingScene = "LoadingScene";

        private void Awake()
        {
            var btn = GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(OnLoadScene);
        }

        public void OnLoadScene()
        {
            if (string.IsNullOrEmpty(targetScene))
            {
                Debug.LogWarning("[ButtonChangeScene] targetScene 未配置");
                return;
            }

            Time.timeScale = 1f; // 防止从暂停态切场景

            if (useLoadingScene)
            {
                // 一句话触发全流程，具体加载由 SceneMgr 负责
                SceneMgr.Instance.LoadSceneWithLoading(targetScene, loadingScene);
            }
            else
            {
                SceneMgr.Instance.LoadScene(targetScene);
            }
        }

        public void SetTargetScene(string sceneName) => targetScene = sceneName;
    }
}