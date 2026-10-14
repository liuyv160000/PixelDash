using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PixelDash.Core;

namespace PixelDash.EditorTools
{
    /// <summary>
    /// Play 模式运行时冒烟测试（批处理）：
    ///   Unity.exe -batchmode -projectPath "&lt;工程&gt;" -executeMethod PixelDash.EditorTools.PlaySmokeTest.Run -logFile -
    /// 打开 FreshHand → 进入 Play → 自动开始 → 运行 10s → 输出运行时状态 → 退出。
    /// 内置 100s 硬超时，避免批处理挂死。
    /// </summary>
    public static class PlaySmokeTest
    {
        private static double _deadline;
        private static double _playStart = -1;
        private static bool _gameStarted;
        private static bool _subscribed;
        private static int _coins, _shots, _booms, _errors;

        public static void Run()
        {
            // 关键：关闭「进入播放模式时重载脚本域」。
            // 否则 Domain Reload 会清空本类的静态字段与回调订阅，测试在 ExitingEditMode 后失联。
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;

            Application.logMessageReceived += OnLog;
            EditorSceneManager.OpenScene("Assets/Scenes/FreshHand.unity");
            EditorApplication.playModeStateChanged += OnModeChanged;
            EditorApplication.update += OnUpdate;
            _deadline = EditorApplication.timeSinceStartup + 100.0;
            Debug.Log("[PlaySmoke] 请求进入 Play 模式（已禁用 Domain Reload）");
            EditorApplication.EnterPlaymode();
        }

        private static void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error)
            {
                _errors++;
                Debug.Log("[PlaySmoke][运行时报错] " + condition);
            }
        }

        private static void OnModeChanged(PlayModeStateChange state)
        {
            Debug.Log("[PlaySmoke] playModeState = " + state);
            if (state == PlayModeStateChange.EnteredPlayMode)
                _playStart = EditorApplication.timeSinceStartup;
        }

        private static void OnUpdate()
        {
            if (EditorApplication.timeSinceStartup > _deadline)
            {
                Debug.LogError("[PlaySmoke] 硬超时（100s）—— Play 模式未完成");
                Finish();
                return;
            }
            if (_playStart < 0) return;

            if (!_subscribed)
            {
                _subscribed = true;
                GameEventBus.Instance.Collection.On(GameEventBus.CollectionEvent.COIN_COLLECTED, _ => _coins++);
                GameEventBus.Instance.Enemy.On(GameEventBus.EnemyEvent.NORMAL_ONE_FLY_ATTACK, _ => _shots++);
                GameEventBus.Instance.Enemy.On(GameEventBus.EnemyEvent.MISSILE_BOOM, _ => _booms++);
            }

            double t = EditorApplication.timeSinceStartup - _playStart;

            if (!_gameStarted && t > 1.0)
            {
                _gameStarted = true;
                var ex = Object.FindObjectOfType<PixelDash.Level.ExManager>();
                if (ex != null) ex.StartGame();
                Debug.Log("[PlaySmoke] StartGame 已调用=" + (ex != null));
            }

            if (t < 10.0) return;
            Report();
            Finish();
        }

        private static void Report()
        {
            var player = GameObject.Find("player");
            var pc = player != null ? player.GetComponent<PixelDash.Player.PlayerController>() : null;
            var mgr = Object.FindObjectOfType<PixelDash.Enemy.SceneEnemyManager>();

            Debug.Log("========== Play 模式冒烟结果 ==========");
            Debug.Log("[PlaySmoke] 玩家存在=" + (player != null)
                + " | 血量=" + (pc != null ? pc.Health : -1)
                + " | 状态=" + (pc != null ? pc.CurrentState : "?")
                + " | 坐标=" + (player != null ? player.transform.position.ToString("F1") : "-"));
            Debug.Log("[PlaySmoke] 已生成敌人(当前)=" + Object.FindObjectsOfType<PixelDash.Enemy.EnemyControllerBase>().Length
                + " | 子弹=" + Object.FindObjectsOfType<PixelDash.Bullet.BulletBase>().Length
                + " | 金币=" + Object.FindObjectsOfType<PixelDash.Item.NormalCoin>().Length);
            Debug.Log("[PlaySmoke] 事件计数：金币拾取=" + _coins + " | 敌人开火=" + _shots + " | 导弹命中=" + _booms);
            Debug.Log("[PlaySmoke] Physics2D.gravity=" + Physics2D.gravity);
            Debug.Log("[PlaySmoke] 运行时错误数=" + _errors);
            Debug.Log("=======================================");
        }

        private static void Finish()
        {
            EditorApplication.update -= OnUpdate;
            EditorApplication.playModeStateChanged -= OnModeChanged;
            Application.logMessageReceived -= OnLog;
            EditorApplication.Exit(_errors == 0 ? 0 : 1);
        }
    }
}
