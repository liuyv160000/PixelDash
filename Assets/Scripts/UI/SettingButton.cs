using UnityEngine;

namespace PixelDash.UI
{
    /// <summary>
    /// 设置按钮（镜像源项目 setting_button）：切换暂停并开关设置面板。
    /// 用 Time.timeScale 暂停游戏（对应源项目 director.pause()）。
    /// </summary>
    public class SettingButton : MonoBehaviour
    {
        [Tooltip("设置面板根节点（显示/隐藏）")]
        [SerializeField] private GameObject settingPanel;

        /// <summary>true = 游戏进行中；false = 已暂停（面板打开）。</summary>
        [SerializeField] private bool gameState = true;

        private void Start()
        {
            Apply();
        }

        /// <summary>供按钮 onClick 绑定：切换游戏/暂停状态。</summary>
        public void ChangeGameState()
        {
            gameState = !gameState;
            Apply();
        }

        private void Apply()
        {
            if (gameState)
            {
                Time.timeScale = 1f;
                if (settingPanel != null) settingPanel.SetActive(false);
            }
            else
            {
                if (settingPanel != null) settingPanel.SetActive(true);
                Time.timeScale = 0f;
            }
        }

        private void OnDestroy()
        {
            // 避免离开场景后仍处于暂停
            Time.timeScale = 1f;
        }
    }
}
