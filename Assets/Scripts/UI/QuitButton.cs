using UnityEngine;
using UnityEngine.UI;

namespace PixelDash.UI
{
    /// <summary>
    /// 退出游戏按钮：点击后结束应用。
    ///
    /// 与 <see cref="ButtonChangeScene"/> 同一套约定 —— 组件自己接 <c>onClick</c>，
    /// 所以只要把这个组件挂到按钮 GameObject 上即可，不需要在 Inspector 里手动配 OnClick。
    ///
    /// 注意：编辑器里 Play 时点它只会**停止 Play**（`Application.Quit()` 在编辑器下不生效），
    /// 打包后才会真正退出程序 —— 这是最容易误判成"没接上"的地方。
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class QuitButton : MonoBehaviour
    {
        [Tooltip("点击后延迟多少秒退出，留给按钮音效/动画播完；0 = 立即退出")]
        [SerializeField] private float delay = 0.1f;

        private void Awake()
        {
            var btn = GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(QuitGame);
            }
            else
            {
                // RequireComponent 正常会兜住，这里只是不静默失败
                Debug.LogWarning("[QuitButton] 本物体上没有 Button 组件，点击逻辑不会生效。");
            }
        }

        /// <summary>点击入口（也可在 Inspector 的 OnClick 里手动指向它）。</summary>
        public void QuitGame()
        {
            if (delay > 0f)
            {
                Invoke(nameof(DoQuit), delay);
            }
            else
            {
                DoQuit();
            }
        }

        private void DoQuit()
        {
            Debug.Log("[QuitButton] 退出游戏");
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
