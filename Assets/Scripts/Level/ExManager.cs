using UnityEngine;
using UnityEngine.SceneManagement;
using PixelDash.Core;

namespace PixelDash.Level
{
    /// <summary>
    /// 关卡驱动器：镜像源项目 ex_manager。
    /// 启动全暂停 → 任意键 startGame → 倒计时 → 胜利切场景；死亡由玩家 Dead 状态切 FailScene。
    /// </summary>
    public class ExManager : MonoBehaviour
    {
        [Header("关卡配置")]
        [SerializeField] private float totalTime = 90f;
        [SerializeField] private string victorySceneName = "VictoryScene";
        [SerializeField] private float victoryDelay = 0.6f;

        [Header("子系统引用")]
        [SerializeField] private PixelDash.Player.PlayerController player;
        [SerializeField] private Background.BackgroundController background;
        [SerializeField] private PixelDash.Enemy.SceneEnemyManager enemyManager;
        [SerializeField] private UnityEngine.UI.Text StaTips;
        [SerializeField] private UI.TimeShower TimerTips;
        [SerializeField] private UI.AllController allTipsCtrl;

        private TimerComponent _timer;
        private bool _isPlaying;

        /// <summary>
        /// 场景里所有可暂停组件（继承自 SceneComponentBase）。
        /// Awake 时统一收集一次，之后 Pause/Resume 只需遍历这个数组，
        /// 不必再为每个子系统单独写字段和调用。
        /// </summary>
        private SceneComponentBase[] _subsystems;

        private void Awake()
        {

            _timer = gameObject.AddComponent<TimerComponent>();
            _timer.SetDuration(totalTime);

            // 引用兜底：场景由脚本生成，无需在 Inspector 手工连线
            if (player == null) player = FindObjectOfType<PixelDash.Player.PlayerController>();
            if (background == null) background = FindObjectOfType<Background.BackgroundController>();
            if (enemyManager == null) enemyManager = FindObjectOfType<PixelDash.Enemy.SceneEnemyManager>();
            if (allTipsCtrl == null) allTipsCtrl = FindObjectOfType<UI.AllController>();
            if (TimerTips == null) TimerTips = FindObjectOfType<UI.TimeShower>();
            if (StaTips == null)
            {
                var all = FindObjectOfType<UI.AllController>();
                if (all != null) StaTips = all.GetComponent<UnityEngine.UI.Text>();
            }
            // 统一收集所有可暂停组件：之后 Pause/Resume 只需遍历它
            // （收集物由 CollectionManager 统一管理，无需在这里单独持有引用）
            _subsystems = FindObjectsOfType<SceneComponentBase>(true);
        }

        private void Start()
        {
            AllPause();
        }

        private void Update()
        {
            // 任意键/触摸开始游戏
            if (!_isPlaying && Input.anyKeyDown)
            {
                StartGame();
            }

            if (_isPlaying && _timer.CheckIfEnd())
            {
                EndGame();
            }
        }

        /// <summary>开始游戏（任意键触发；也供 UI 按钮 / 自动化测试调用）。</summary>
        public void StartGame()
        {
            if (_isPlaying) return;
            if (StaTips != null) StaTips.text = "";
            _isPlaying = true;
            SetUp();
        }

        private void AllPause()
        {
            // 统一暂停：所有继承 SceneComponentBase 的子系统
            // （玩家 / 背景 / 前景 / 相机 / 敌人生成器 / 收集物生成器 / 倒计时 / 教学文案…）
            PauseAllSubsystems(true);

            _timer.Stop();
        }

        private void SetUp()
        {
            PauseAllSubsystems(false);

            _timer.Restart();
        }

        /// <summary>遍历所有场景组件统一暂停/恢复。</summary>
        private void PauseAllSubsystems(bool pause)
        {
            if (_subsystems == null) return;

            foreach (var s in _subsystems)
            {
                if (s == null) continue;
                if (pause) s.Pause();
                else s.Resume();
            }
        }

        private void EndGame()
        {
            _timer.Stop();
            if (player != null)
            {
                // 胜利收尾：EndOut 内部会置无敌、1s 后发射 GAME_END 并销毁玩家自身
                player.EndOut();
            }
            Invoke(nameof(LoadVictoryScene), victoryDelay);
        }

        private void LoadVictoryScene()
        {
            SceneManager.LoadScene(victorySceneName);
        }
    }
}