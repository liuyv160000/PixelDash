using UnityEngine;
using UnityEngine.SceneManagement;

namespace PixelDash.Level
{
    /// <summary>
    /// 场景记录：把关卡"我是哪一关、返回选关该回哪"记下来，跨场景带进胜利/失败场景，
    /// 供 result_controller 上的 ResultSceneController 在结算后读取（重开要加载哪一关、返回选关去哪）。
    ///
    /// 生命周期（三条，对应需求）：
    ///   ① 关卡场景 Awake 时记录 + 常驻（DontDestroyOnLoad）
    ///   ② 关卡结算 → 切进胜利/失败场景：**不销毁**，数据仍可读
    ///   ③ 从胜利/失败场景再加载任何别的场景（重开 / 返回选关 / 退出 / 下一关）→ **立即释放**
    ///
    /// 挂载位置：**关卡场景**（FreshHandScene、EditingScene…）里的节点上。
    ///   Unity 的 DontDestroyOnLoad 只对根物体生效，所以挂在子节点上时会自动脱离父节点（见 Awake），
    ///   否则"进结算场景不消失"这条会静默失效。建议单独挂在一个不带子物体的节点上。
    ///
    /// 读取方式（结算场景里怎么写）：
    ///   <code>
    ///   if (SceneRecord.HasRecord) SceneManager.LoadScene(SceneRecord.RestartScene);
    ///   else SceneManager.LoadScene(restartScene);   // 没记录（例如直接在编辑器里跑胜利场景）用兜底值
    ///   </code>
    /// </summary>
    [DisallowMultipleComponent]
    public class SceneRecord : MonoBehaviour
    {
        /// <summary>当前有效的记录（没有记录时为 null，例如直接在编辑器里从胜利场景启动）。</summary>
        public static SceneRecord Instance { get; private set; }

        // ===== 记录内容 =====
        [Header("记录内容")]
        [Tooltip("本关场景名。留空 = Awake 时自动记录当前场景名（推荐留空）")]
        [SerializeField] private string levelScene;

        [Tooltip("「返回选关」要去的场景名（选关界面）")]
        [SerializeField] private string levelSelectScene = "EXChoosingScene";

        // ===== 生命周期 =====
        [Header("生命周期")]
        [Tooltip("结算场景名单：进这些场景时记录保留，从这些场景切走时释放")]
        [SerializeField] private string[] resultScenes = { "VictoryScene", "FailScene" };

        [Tooltip("打印记录 / 释放日志，方便确认时机")]
        [SerializeField] private bool logLifecycle = true;

        // ===== 静态读取口（ResultSceneController / 按钮用） =====
        /// <summary>是否有可用记录。</summary>
        public static bool HasRecord => Instance != null;

        /// <summary>重开要加载的场景名（无记录时返回空串，调用方用自己的兜底值）。</summary>
        public static string RestartScene => Instance != null ? Instance.levelScene : string.Empty;

        /// <summary>返回选关要加载的场景名（无记录时返回空串）。</summary>
        public static string LevelSelectScene => Instance != null ? Instance.levelSelectScene : string.Empty;

        // ===== 实例读取口 =====
        /// <summary>本关场景名（= 重开目标）。</summary>
        public string LevelScene => levelScene;

        /// <summary>返回选关的场景名。</summary>
        public string LevelSelectSceneName => levelSelectScene;

        /// <summary>是否已经从关卡走进过结算场景（区分"关口没走结算就切走"和"走完结算再走"）。</summary>
        private bool _enteredResult;

        private void Awake()
        {
            string activeScene = SceneManager.GetActiveScene().name;
            bool bornInResultScene = IsResultScene(activeScene);

            // 只保留一份，且**取更可靠的那一条**：
            //   - 已有记录是"结算场景里误挂的空记录"（关卡名为空），而本次是正常关卡 → 本次接管（否则重开会跳回错的场景）
            //   - 反过来本次是误挂的 → 本次让位（不能把关卡带进来的真实记录顶掉）
            if (Instance != null && Instance != this)
            {
                bool oldIsUseful = !string.IsNullOrEmpty(Instance.levelScene);
                bool newIsUseful = !bornInResultScene;

                if (!newIsUseful && oldIsUseful)
                {
                    Destroy(gameObject);
                    return;
                }

                Destroy(Instance.gameObject);   // 旧的让位（它的 OnDestroy 会因为 Instance 已换人而只销毁自己）
            }
            Instance = this;

            if (string.IsNullOrEmpty(levelScene))
            {
                if (bornInResultScene)
                {
                    // 挂错场景了：结算场景里记录不到关卡名。不销毁（免得手滑挂上去直接崩），
                    // 但明确报出来 —— 否则后面"重开回到了胜利场景"会很难查。
                    Debug.LogWarning($"[SceneRecord] 被挂在了结算场景「{activeScene}」里，记录不到关卡名。" +
                                     "请把它挂到关卡场景（FreshHandScene / EditingScene）的节点上。");
                }
                else
                {
                    levelScene = activeScene;
                }
            }

            if (string.IsNullOrEmpty(levelSelectScene))
                Debug.LogWarning("[SceneRecord] 未配置「返回选关」场景名，返回选关按钮会拿不到目标场景。");

            // Unity 的 DontDestroyOnLoad 只对根物体生效：挂在子节点上会静默失效（记录进不了结算场景）。
            // 这里自动脱父，比之后在结算场景里读不到数据再去排查省事。
            if (transform.parent != null)
            {
                Debug.LogWarning($"[SceneRecord] 「{name}」挂在子节点上，已自动脱离父节点以满足常驻要求；" +
                                 "建议直接挂在根物体的独立节点上。");
                transform.SetParent(null, true);
            }

            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;

            if (logLifecycle)
                Debug.Log($"[SceneRecord] 记录中：本关（重开目标）={levelScene}，返回选关={levelSelectScene}");
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            // 静态引用不留脏数据；被新记录顶掉时不能把新的清掉
            if (Instance == this) Instance = null;

            if (logLifecycle) Debug.Log("[SceneRecord] 已释放");
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (IsResultScene(scene.name))
            {
                _enteredResult = true;   // 进结算场景：数据还要给 result_controller 读，留着
                return;
            }

            if (_enteredResult)
            {
                Destroy(gameObject);     // 从结算场景走到了别的场景 → 本次记录用完，释放
                return;
            }

            // 没经过结算就切到了别的场景（例如关卡里点返回）→ 这条记录已经过时
            if (scene.name != levelScene)
                Destroy(gameObject);
        }

        private bool IsResultScene(string sceneName)
        {
            if (resultScenes == null) return false;
            for (int i = 0; i < resultScenes.Length; i++)
                if (resultScenes[i] == sceneName) return true;
            return false;
        }
    }
}
