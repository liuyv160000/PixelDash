using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PixelDash.Core;

namespace PixelDash.EditorTools
{
    /// <summary>
    /// 运行时冒烟测试（批处理模式）：
    ///   Unity.exe -batchmode -projectPath "&lt;工程&gt;" -executeMethod PixelDash.EditorTools.SmokeTestRunner.RunFreshHand -logFile -
    /// 打开 FreshHand 场景 → 进入 Play → 自动开始游戏 → 运行 10s → 输出关键状态 → 退出。
    /// 用于验证"关卡能真正跑起来"（玩家存活、生成器工作、事件流有数据）。
    /// </summary>
    public static class SmokeTestRunner
    {
        private static double _start;
        private static bool _entered;
        private static bool _started;
        private static int _coinEvents;
        private static int _enemyEvents;
        private static int _shotEvents;

        public static void RunFreshHand()
        {
            Application.logMessageReceived += OnLog;
            EditorSceneManager.OpenScene("Assets/Scenes/FreshHand.unity");
            EditorApplication.update += OnUpdate;
            EditorApplication.isPlaying = true;
        }

        private static void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error)
                Debug.Log("[SmokeTest][捕获] " + type + ": " + condition);
        }

        private static void OnUpdate()
        {
            if (!EditorApplication.isPlaying) return;

            if (!_entered)
            {
                _entered = true;
                _start = EditorApplication.timeSinceStartup;
                GameEventBus.Instance.Collection.On(GameEventBus.CollectionEvent.COIN_COLLECTED, _ => _coinEvents++);
                GameEventBus.Instance.Enemy.On(GameEventBus.EnemyEvent.NORMAL_ONE_FLY_ATTACK, _ => _shotEvents++);
                GameEventBus.Instance.Enemy.On(GameEventBus.EnemyEvent.MISSILE_BOOM, _ => _enemyEvents++);
                Debug.Log("[SmokeTest] 已进入 Play 模式");
                return;
            }

            double elapsed = EditorApplication.timeSinceStartup - _start;

            // 1s 后自动开始游戏（跳过"任意键开始"）
            if (!_started && elapsed >= 1.0)
            {
                _started = true;
                var ex = Object.FindObjectOfType<PixelDash.Level.ExManager>();
                if (ex != null)
                {
                    ex.StartGame();
                    Debug.Log("[SmokeTest] 已调用 StartGame()");
                }
                else
                {
                    Debug.LogError("[SmokeTest] 未找到 ExManager");
                }
            }

            if (elapsed < 12.0) return;

            EditorApplication.update -= OnUpdate;
            Report();
            EditorApplication.isPlaying = false;
            EditorApplication.Exit(0);
        }

        private static void Report()
        {
            var player = GameObject.Find("player");
            var pc = player != null ? player.GetComponent<PixelDash.Player.PlayerController>() : null;
            var mgr = Object.FindObjectOfType<PixelDash.Enemy.SceneEnemyManager>();
            var enemies = Object.FindObjectsOfType<PixelDash.Enemy.EnemyControllerBase>();
            var bullets = Object.FindObjectsOfType<PixelDash.Bullet.BulletBase>();
            var coins = Object.FindObjectsOfType<PixelDash.Item.NormalCoin>();

            Debug.Log("========== 冒烟测试结果 ==========");
            Debug.Log("[SmokeTest] player 存在=" + (player != null)
                + " | PlayerController=" + (pc != null)
                + " | 血量=" + (pc != null ? pc.Health : -1)
                + " | 状态=" + (pc != null ? pc.CurrentState : "?")
                + " | 坐标=" + (player != null ? player.transform.position.ToString() : "-"));
            Debug.Log("[SmokeTest] ExManager=" + (Object.FindObjectOfType<PixelDash.Level.ExManager>() != null)
                + " | SceneEnemyManager=" + (mgr != null));
            Debug.Log("[SmokeTest] 当前敌人=" + enemies.Length + " | 子弹=" + bullets.Length + " | 金币=" + coins.Length);
            Debug.Log("[SmokeTest] 事件计数：金币拾取=" + _coinEvents + " | 敌人开火=" + _shotEvents + " | 导弹命中=" + _enemyEvents);
            Debug.Log("[SmokeTest] 物理重力=" + Physics2D.gravity);
            Debug.Log("=================================");
        }
    }
}
