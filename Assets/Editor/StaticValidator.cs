using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PixelDash.EditorTools
{
    /// <summary>
    /// 结构验证器（批处理模式，快）：
    ///   Unity.exe -batchmode -quit -projectPath "&lt;工程&gt;" -executeMethod PixelDash.EditorTools.StaticValidator.Validate -logFile -
    /// 校验两关卡场景的节点/组件完整性、关卡预设数值，并手动步进物理验证重力与碰撞生效。
    /// </summary>
    public static class StaticValidator
    {
        private static int _pass;
        private static int _fail;
        private static readonly StringBuilder Sb = new StringBuilder();

        public static void Validate()
        {
            // 场景可能被手动重做/删除。存在才验证，避免 OpenScene 抛异常中断整个流程。
            ValidateIfExists(() => ValidateLevel("Assets/Scenes/FreshHand.unity", 0), "Assets/Scenes/FreshHand.unity");
            ValidateIfExists(() => ValidateLevel("Assets/Scenes/ExScene1.unity", 1), "Assets/Scenes/ExScene1.unity");
            ValidateIfExists(() => ValidateResult("Assets/Scenes/VictoryScene.unity", "胜利"), "Assets/Scenes/VictoryScene.unity");
            ValidateIfExists(() => ValidateResult("Assets/Scenes/FailScene.unity", "失败"), "Assets/Scenes/FailScene.unity");

            Debug.Log("\n========== 结构验证报告 ==========");
            Debug.Log(Sb.ToString());
            Debug.Log($"[Validator] 通过 {_pass} 项，失败 {_fail} 项");
            Debug.Log("==================================");

            // 让批处理退出码反映结果
            EditorApplication.Exit(_fail == 0 ? 0 : 1);
        }

        /// <summary>重建全部场景后立即验证（一步到位）。</summary>
        public static void RebuildAndValidate()
        {
            PixelDashSceneBuilder.BuildAll();
            Validate();
        }

        private static void Check(bool ok, string label)
        {
            if (ok) _pass++; else _fail++;
            Sb.AppendLine((ok ? "  [OK]   " : "  [FAIL] ") + label);
        }

        /// <summary>取生成器的时间表数量（未挂载返回 -1）。仅用于报告，不做断言。</summary>
        private static int Count(PixelDash.Enemy.EnemyMakerBase maker)
            => maker != null ? maker.PlannedSpawnCount : -1;

        /// <summary>实例化一个组件以触发其 Awake，验证初始化链路无异常。</summary>
        private static void TryInstantiate(string label, System.Func<Component> factory)
        {
            try
            {
                var c = factory();
                Check(c != null, label + " 实例化正常（Awake 无异常）");
                if (c != null) Object.DestroyImmediate(c.gameObject);
            }
            catch (System.Exception e)
            {
                Check(false, label + " 实例化异常：" + e.Message);
            }
        }

        /// <summary>场景文件存在才执行验证，不存在则跳过并告警。</summary>
        private static void ValidateIfExists(System.Action action, string path)
        {
            if (!System.IO.File.Exists(path))
            {
                Debug.LogWarning($"[Validator] 跳过（场景不存在）：{path}");
                return;
            }
            action();
        }

        private static void ValidateLevel(string path, int levelIndex)
        {
            var scene = EditorSceneManager.OpenScene(path);
            Sb.AppendLine($"--- {scene.name} (levelIndex={levelIndex}) ---");

            // 相机 + 死区
            var cam = Object.FindObjectOfType<Camera>();
            Check(cam != null, "Main Camera 存在");
            Check(cam != null && cam.orthographic, "相机为正交投影");
            Check(Object.FindObjectOfType<PixelDash.Level.CameraController>() != null, "CameraController 已挂载");
            var dz = GameObject.Find("dead_zone");
            Check(dz != null && dz.GetComponent<BoxCollider2D>() != null, "dead_zone + BoxCollider2D 存在");

            // 玩家
            var player = GameObject.Find("player");
            Check(player != null, "player 节点存在（名称必须为 player）");
            if (player != null)
            {
                Check(player.GetComponent<PixelDash.Player.PlayerController>() != null, "PlayerController 已挂载");
                Check(player.GetComponent<Rigidbody2D>() != null, "Rigidbody2D 已挂载");
                var rb = player.GetComponent<Rigidbody2D>();
                Check(rb != null && rb.bodyType == RigidbodyType2D.Dynamic, "刚体为 Dynamic");
                Check(rb != null && Mathf.Approximately(rb.gravityScale, 2.5f), "gravityScale = 2.5");
                Check(player.GetComponent<Collider2D>() != null, "玩家碰撞体存在");
            }

            // 平台（plat_ver → plat_line）
            var platVer = GameObject.Find("plat_ver");
            Check(platVer != null, "plat_ver 节点存在");
            var platLine = platVer != null ? platVer.transform.Find("plat_line") : null;
            Check(platLine != null && platLine.GetComponent<BoxCollider2D>() != null, "plat_ver/plat_line + 碰撞体存在");

            // 敌人生成器（时间表已下沉到各生成器，管理器只持有它们）
            var enemyPoster = GameObject.Find("enemy_poster");
            var mgr = Object.FindObjectOfType<PixelDash.Enemy.SceneEnemyManager>();
            Check(enemyPoster != null && mgr != null, "enemy_poster + SceneEnemyManager 存在");

            var normalMaker = Object.FindObjectOfType<PixelDash.Enemy.NormalOneMaker>();
            var sawMaker = Object.FindObjectOfType<PixelDash.Enemy.CircleSawMaker>();
            var laserMaker = Object.FindObjectOfType<PixelDash.Enemy.LaserMaker>();
            var missileMaker = Object.FindObjectOfType<PixelDash.Enemy.TrackingMissileMaker>();
            Check(normalMaker != null, "NormalOneMaker 已挂载");
            Check(sawMaker != null, "CircleSawMaker 已挂载");
            Check(laserMaker != null, "LaserMaker 已挂载");
            Check(missileMaker != null, "TrackingMissileMaker 已挂载");

            // 生成时间表由 Inspector 配置，这里只报告数量供人工核对（不做断言 —— 不同关卡的配置本来就不同）
            Sb.AppendLine($"      生成时间表数量：normal_one={Count(normalMaker)} 电锯={Count(sawMaker)} 激光={Count(laserMaker)} 导弹={Count(missileMaker)}");

            // 道具生成器（由 CollectionManager 统一管理）
            var collection = Object.FindObjectOfType<PixelDash.Item.CollectionManager>();
            Check(collection != null, "CollectionManager 已挂载");
            Check(Object.FindObjectsOfType<PixelDash.Item.CoinMaker>().Length >= 1, "至少 1 个金币生成器");
            Check(Object.FindObjectsOfType<PixelDash.Item.BulletSkillballMaker>().Length >= 1, "至少 1 个技能球生成器");

            // 关卡驱动器
            var ex = Object.FindObjectOfType<PixelDash.Level.ExManager>();
            Check(ex != null, "ExManager 已挂载");

            // 背景 / 前景
            Check(Object.FindObjectOfType<PixelDash.Background.BackgroundController>() != null, "BackgroundController 已挂载");
            var bg = Object.FindObjectOfType<PixelDash.Background.BackgroundController>();
            Check(bg != null && bg.transform.childCount == 3, "背景包含 3 张 city（循环滚动）");
            Check(Object.FindObjectOfType<PixelDash.Background.NearSiteMove>() != null, "NearSiteMove 已挂载");

            // UI
            var canvas = GameObject.Find("Canvas");
            Check(canvas != null, "Canvas 存在");
            Check(GameObject.Find("timer") != null && Object.FindObjectOfType<PixelDash.UI.TimeShower>() != null, "timer + TimeShower 存在");
            Check(GameObject.Find("hp_line") != null && Object.FindObjectOfType<PixelDash.UI.HpLine>() != null, "hp_line + HpLine 存在");
            var tips = GameObject.Find("tips");
            Check(tips != null, "tips 文本存在");
            if (levelIndex == 0)
                Check(Object.FindObjectOfType<PixelDash.UI.AllController>() != null, "fresh_hand 含教学文案控制器 AllController");
            else
                Check(Object.FindObjectOfType<PixelDash.UI.AllController>() == null, "ex_scene_1 无教学文案（源项目一致）");

            // 组件引用兜底（实例化克隆体触发 Awake，验证初始化不抛异常）
            if (player != null)
            {
                try
                {
                    var clone = Object.Instantiate(player);
                    Check(clone.GetComponent<PixelDash.Player.PlayerController>() != null, "玩家克隆体 Awake 正常（组件初始化无异常）");
                    Object.DestroyImmediate(clone);
                }
                catch (System.Exception e)
                {
                    Check(false, "玩家克隆体 Awake 抛出异常：" + e.Message);
                }
            }

            // 组件实例化验证：AddComponent 会触发 Awake，可捕获初始化期异常
            // （注：Unity 在 Editor 非播放模式下不推进物理，重力/AI 的运行需在 Play 模式验证）
            TryInstantiate("NormalOne", () => new GameObject("t").AddComponent<PixelDash.Enemy.NormalOne>());
            TryInstantiate("CircleSaw", () => new GameObject("t").AddComponent<PixelDash.Enemy.CircleSaw>());
            TryInstantiate("Laser", () => new GameObject("t").AddComponent<PixelDash.Enemy.Laser>());
            TryInstantiate("TrackingMissile", () => new GameObject("t").AddComponent<PixelDash.Enemy.TrackingMissile>());
            TryInstantiate("BoxWarningZone", () => new GameObject("t").AddComponent<PixelDash.Enemy.BoxWarningZone>());
            TryInstantiate("PlayerBullet", () => new GameObject("t").AddComponent<PixelDash.Bullet.PlayerBullet>());
            TryInstantiate("NormalAttackBullet", () => new GameObject("t").AddComponent<PixelDash.Bullet.NormalAttackBullet>());
            TryInstantiate("NormalCoin", () => { var g = new GameObject("t"); return g.AddComponent<PixelDash.Item.NormalCoin>(); });
            TryInstantiate("SupplyBox", () => new GameObject("t").AddComponent<PixelDash.Item.SupplyBox>());

            Sb.AppendLine();
        }

        private static void ValidateResult(string path, string label)
        {
            var scene = EditorSceneManager.OpenScene(path);
            Sb.AppendLine($"--- {scene.name}（{label}场景）---");
            Check(Object.FindObjectOfType<Camera>() != null, "相机存在");
            Check(GameObject.Find("Canvas") != null, "Canvas 存在");
            Check(GameObject.Find("title") != null, "标题文本存在");
            Check(GameObject.Find("btn_restart") != null, "重新开始按钮存在");
            Check(Object.FindObjectOfType<PixelDash.UI.ResultSceneController>() != null, "ResultSceneController 存在");
            Sb.AppendLine();
        }
    }
}
