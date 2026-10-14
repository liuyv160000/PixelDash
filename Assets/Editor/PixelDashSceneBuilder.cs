using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using PixelDash.Core;
using PixelDash.Player;
using PixelDash.Enemy;
using PixelDash.Item;
using PixelDash.Level;
using PixelDash.Background;
using PixelDash.UI;

namespace PixelDash.EditorTools
{
    /// <summary>
    /// 场景构建器：一键生成两关卡场景 + 胜利/失败场景，并写入 Build Settings。
    /// 命令行用法：
    ///   Unity.exe -batchmode -quit -projectPath "&lt;工程&gt;" -executeMethod PixelDash.EditorTools.PixelDashSceneBuilder.BuildAll -logFile -
    /// 编辑器内用法：菜单 PixelDash → ① 构建全部场景。
    /// </summary>
    public static class PixelDashSceneBuilder
    {
        private const float DesignW = 1280f;
        private const float DesignH = 720f;
        private const string SceneDir = "Assets/Scenes";

        private static readonly Color PlayerColor = new Color(0.35f, 0.75f, 1f);
        private static readonly Color CityColor = new Color(0.10f, 0.14f, 0.26f);
        private static readonly Color PlatformColor = new Color(0.55f, 0.62f, 0.72f);
        private static readonly Color NearSiteColor = new Color(0.18f, 0.21f, 0.34f);

        [MenuItem("PixelDash/① 构建全部场景")]
        public static void BuildAll()
        {
            EnsureSceneDir();
            BuildLevelScene("FreshHandScene", 0, true);
            BuildLevelScene("EditingScene", 1, false);
            BuildResultScene("VictoryScene", "胜利！", new Color(0.07f, 0.15f, 0.28f), true);
            BuildResultScene("FailScene", "失败…", new Color(0.26f, 0.09f, 0.12f), false);
            SetupBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PixelDash] 场景构建完成：FreshHandScene / EditingScene / VictoryScene / FailScene");
        }

        private static void EnsureSceneDir()
        {
            if (!AssetDatabase.IsValidFolder(SceneDir))
                AssetDatabase.CreateFolder("Assets", "Scenes");
        }

        // ============================ 关卡场景 ============================
        private static void BuildLevelScene(string sceneName, int levelIndex, bool withTutorial)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ---- 相机 + 死区 ----
            var camGo = new GameObject("Main Camera");
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = DesignH * 0.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.06f, 0.11f);
            camGo.transform.position = new Vector3(0f, 15.9f, -10f);
            camGo.tag = "MainCamera";
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<CameraController>();

            var deadZone = new GameObject("dead_zone");
            deadZone.transform.SetParent(camGo.transform);
            deadZone.transform.localPosition = new Vector3(-351.4f, 11f, 0f);
            deadZone.AddComponent<BoxCollider2D>().size = new Vector2(400f, 400f);

            // ---- GameLayer ----
            var gameLayer = new GameObject("GameLayer");

            // 背景（3 张 city 循环滚动）
            var bg = new GameObject("background_controler");
            bg.transform.SetParent(gameLayer.transform);
            bg.transform.localPosition = new Vector3(-201.5f, 0f, 0f);
            bg.AddComponent<BackgroundController>();
            for (int i = 0; i < 3; i++)
            {
                var city = new GameObject("city_" + (i + 1));
                city.transform.SetParent(bg.transform);
                city.transform.localPosition = new Vector3(-2435.8f + i * 3840f, 17.6f, 0f);
                city.transform.localScale = new Vector3(3840f, 1400f, 1f);
                PlaceholderSprite.EnsureRenderer(city, CityColor, 1f);
            }

            // 前景（大楼 / 广告牌）
            var nearSite = new GameObject("near_site");
            nearSite.transform.SetParent(gameLayer.transform);
            nearSite.AddComponent<NearSiteMove>();
            var b1 = new GameObject("大楼_1");
            b1.transform.SetParent(nearSite.transform);
            b1.transform.localPosition = new Vector3(3340.4f, 543f, 0f);
            b1.transform.localScale = new Vector3(600f, 900f, 1f);
            PlaceholderSprite.EnsureRenderer(b1, NearSiteColor, 1f);
            var b2 = new GameObject("广告牌_1");
            b2.transform.SetParent(nearSite.transform);
            b2.transform.localPosition = new Vector3(7496.9f, -156.6f, 0f);
            b2.transform.localScale = new Vector3(700f, 320f, 1f);
            PlaceholderSprite.EnsureRenderer(b2, NearSiteColor, 1f);

            // 平台线（plat_ver → plat_line）：根用 localScale 缩放，碰撞体用归一化尺寸
            var platVer = new GameObject("plat_ver");
            platVer.transform.SetParent(gameLayer.transform);
            var platLine = new GameObject("plat_line");
            platLine.transform.SetParent(platVer.transform);
            platLine.transform.localPosition = new Vector3(-13.4f, -37f, 0f);
            platLine.transform.localScale = new Vector3(700f, 16f, 1f);
            platLine.AddComponent<BoxCollider2D>().size = Vector2.one; // × scale = 700×16
            PlaceholderSprite.EnsureRenderer(platLine, PlatformColor, 1f);

            // 玩家
            var player = new GameObject("player");
            player.transform.SetParent(gameLayer.transform);
            player.transform.localPosition = new Vector3(-335.4f, -19.4f, 0f);
            var prb = player.AddComponent<Rigidbody2D>();
            prb.bodyType = RigidbodyType2D.Dynamic;
            prb.gravityScale = 2.5f;
            prb.freezeRotation = true;
            prb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            player.AddComponent<CircleCollider2D>().radius = 10f;
            PlaceholderSprite.EnsureRenderer(player, PlayerColor, 22f);
            player.AddComponent<PlayerController>(); // 自动挂 PlayerMovementCtrl + SkillSystem

            // 道具生成器（管理器 + 子生成器，与敌人数对称）
            var collection = new GameObject("collection_poster");
            collection.transform.SetParent(gameLayer.transform);
            collection.transform.localPosition = new Vector3(levelIndex == 0 ? 636.5f : 586.8f, -36.4f, 0f);
            collection.AddComponent<CollectionManager>();
            BuildItemMakers(collection.transform, levelIndex);

            // 敌人生成器
            var enemyPoster = new GameObject("enemy_poster");
            enemyPoster.transform.SetParent(gameLayer.transform);
            enemyPoster.transform.localPosition = new Vector3(1265.7f, -43.8f, 0f);
            var mgr = enemyPoster.AddComponent<SceneEnemyManager>();

            // ---- 关卡驱动器 ----
            new GameObject("ex_manager").AddComponent<ExManager>();

            // ---- 场景记录 ----
            // 单独一个根节点：它跨场景常驻（DontDestroyOnLoad），进胜利/失败场景后供
            // ResultSceneController 读取"重开回哪一关 / 返回选关去哪"。
            // levelScene 留空 = 运行时自动记录当前场景名；levelSelectScene 默认 EXChoosingScene。
            new GameObject("[SceneRecord]").AddComponent<SceneRecord>();

            // ---- UI ----
            var canvasGo = BuildCanvas();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var timerGo = MakeText(canvasGo.transform, "timer", "90", 56, TextAnchor.UpperCenter,
                new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(400f, 90f), font);
            timerGo.AddComponent<TimeShower>();

            var tipsGo = MakeText(canvasGo.transform, "tips", "", 40, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(1000f, 140f), font);
            if (withTutorial)
                tipsGo.AddComponent<AllController>();

            var hpGo = new GameObject("hp_line");
            hpGo.transform.SetParent(canvasGo.transform, false);
            var hpRt = hpGo.AddComponent<RectTransform>();
            hpRt.anchorMin = hpRt.anchorMax = new Vector2(0f, 1f);
            hpRt.pivot = new Vector2(0f, 1f);
            hpRt.anchoredPosition = new Vector2(40f, -40f);
            hpRt.sizeDelta = new Vector2(300f, 24f);
            var hpImg = hpGo.AddComponent<Image>();
            hpImg.color = new Color(0.9f, 0.25f, 0.3f);
            hpImg.type = Image.Type.Filled;
            hpImg.fillMethod = Image.FillMethod.Horizontal;
            hpImg.fillAmount = 1f;
            hpGo.AddComponent<HpLine>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, SceneDir + "/" + sceneName + ".unity");
        }

        // ============================ 道具生成器 ============================
        private static void BuildItemMakers(Transform parent, int levelIndex)
        {
            if (levelIndex == 0)
            {
                AddCoinMaker(parent, "coin_maker_A", new[] { 23f, 23.5f }, new[] { 150f, 10f });
                AddCoinMaker(parent, "coin_maker_B", new[] { 3f, 32f, 52f }, new[] { 10f, -10f, 10f });
                AddCoinMaker(parent, "coin_maker_C",
                    new[] { 11f, 11.1f, 11.2f, 11.3f, 11.4f, 11.5f, 42.1f, 42.2f, 42.3f, 42.4f, 42.5f, 47f, 47.5f },
                    new[] { 150f, -150f, 150f, -150f, 150f, -150f, 150f, -150f, 150f, -150f, 150f, -150f, -10f });
                AddSkillOrbMaker(parent, "skillball_maker", new[] { 72f }, new[] { 10f });
                AddSupplyMaker(parent, "supply_maker", new float[0], new float[0]); // fresh_hand 无补给箱
            }
            else
            {
                AddCoinMaker(parent, "coin_maker_A",
                    new[] { 1.5f, 1.7f, 1.9f, 2.1f, 7f, 7.1f, 7.2f, 7.3f, 7.4f },
                    new[] { 5f, 5f, 5f, 5f, -20f, -20f, -20f, -20f, -20f });
                AddCoinMaker(parent, "coin_maker_B",
                    new[] { 14.3f, 14.4f, 14.5f, 14.6f, 14.7f, 14.8f, 14.9f, 15f, 15.1f, 15.2f, 15.3f },
                    new[] { -10f, -60f, -100f, -150f, -170f, -184.9f, -200f, -180f, -170f, -150f, -100f });
                AddCoinMaker(parent, "coin_maker_C", new[] { 0f, 0.1f, 0.5f, 1f }, new[] { 0f, 10f, -50f, 20f });
                AddSkillOrbMaker(parent, "skillball_maker_A", new[] { 0f }, new[] { 10f });
                AddSkillOrbMaker(parent, "skillball_maker_B", new[] { 5f }, new[] { 20f });
                AddSupplyMaker(parent, "supply_maker_A", new[] { 10f }, new[] { 50f });
                AddSupplyMaker(parent, "supply_maker_B", new[] { 20f, 25f, 55f }, new[] { 40f, -40f, 100f });
                AddSupplyMaker(parent, "supply_maker_C", new[] { 35f, 45f, 55f }, new[] { 0f, -10f, 10f });
            }
        }

        private static void AddCoinMaker(Transform parent, string name, float[] times, float[] offsets)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<CoinMaker>().SetSpawnPlan(times, offsets);
        }

        private static void AddSkillOrbMaker(Transform parent, string name, float[] times, float[] offsets)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<BulletSkillballMaker>().SetSpawnPlan(times, offsets);
        }

        private static void AddSupplyMaker(Transform parent, string name, float[] times, float[] offsets)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<BoxSupplyMaker>().SetSpawnPlan(times, offsets);
        }

        // ============================ 结果场景 ============================
        private static void BuildResultScene(string sceneName, string title, Color bg, bool isVictory)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = DesignH * 0.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = bg;
            camGo.transform.position = new Vector3(0f, 0f, -10f);
            camGo.tag = "MainCamera";
            camGo.AddComponent<AudioListener>();

            var canvasGo = BuildCanvas();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            MakeText(canvasGo.transform, "title", title, 96, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(900f, 180f), font);

            var ctrl = new GameObject("result_controller").AddComponent<ResultSceneController>();

            var restart = MakeButton(canvasGo.transform, "btn_restart", "重新开始", font, new Vector2(0f, -60f));
            UnityEventTools.AddPersistentListener(restart.GetComponent<Button>().onClick, ctrl.Restart);

            if (isVictory)
            {
                var back = MakeButton(canvasGo.transform, "btn_back_to_select", "返回选关", font, new Vector2(0f, -170f));
                UnityEventTools.AddPersistentListener(back.GetComponent<Button>().onClick, ctrl.ReturnToLevelSelect);
            }
            else
            {
                var quit = MakeButton(canvasGo.transform, "btn_quit", "退出", font, new Vector2(0f, -170f));
                UnityEventTools.AddPersistentListener(quit.GetComponent<Button>().onClick, ctrl.Quit);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, SceneDir + "/" + sceneName + ".unity");
        }

        // ============================ UI 辅助 ============================
        private static GameObject BuildCanvas()
        {
            var canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(DesignW, DesignH);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            return canvasGo;
        }

        private static GameObject MakeText(Transform parent, string name, string content, int size,
            TextAnchor anchor, Vector2 anchorPoint, Vector2 anchoredPos, Vector2 sizeDelta, Font font)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchorPoint;
            rt.pivot = anchorPoint;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
            var t = go.AddComponent<Text>();
            t.text = content;
            t.font = font;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = Color.white;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return go;
        }

        private static GameObject MakeButton(Transform parent, string name, string label, Font font, Vector2 anchoredPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(360f, 90f);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.2f, 0.45f, 0.8f);
            go.AddComponent<Button>();
            MakeText(go.transform, "label", label, 40, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360f, 90f), font);
            return go;
        }

        // ============================ Build Settings ============================
        private static void SetupBuildSettings()
        {
            // 已知场景按固定顺序打头（顺序与工程现状一致）。
            // 注意：EXChoosingScene / LoadingScene 不是本 builder 生成的（选关界面、加载中转场景是手工做的），
            // 但必须留在 Build Settings 里 —— 否则按名字加载它们会失败，选关和 Loading 中转整条链都断。
            var ordered = new[]
            {
                "VictoryScene", "FailScene", "EditingScene",
                "EXChoosingScene", "LoadingScene", "FreshHandScene"
            };

            var list = new List<EditorBuildSettingsScene>();
            var names = new List<string>();
            var missing = new List<string>();

            foreach (var n in ordered)
            {
                var path = SceneDir + "/" + n + ".unity";
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null)
                {
                    list.Add(new EditorBuildSettingsScene(path, true));
                    names.Add(n);
                }
                else
                {
                    missing.Add(n);
                }
            }

            // 其余已在 Build Settings 里的场景原样保留（放在后面）——
            // 免得以后新加关卡场景时，被这里"只写已知清单"悄悄踢掉。
            var known = new HashSet<string>();
            foreach (var s in list) known.Add(s.path);
            foreach (var s in EditorBuildSettings.scenes)
            {
                if (known.Contains(s.path)) continue;
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(s.path) == null) continue;   // 文件已不存在 → 丢弃脏条目
                list.Add(s);
                names.Add(System.IO.Path.GetFileNameWithoutExtension(s.path));
            }

            if (missing.Count > 0)
            {
                Debug.LogWarning($"[PixelDash] 这些场景文件还不存在，未写入 Build Settings：{string.Join(" / ", missing)}。" +
                                 "运行期按名字加载它们会失败，需要先用菜单把场景建出来（选关界面的按钮就是按场景名加载的）。");
            }

            EditorBuildSettings.scenes = list.ToArray();
            Debug.Log($"[PixelDash] Build Settings 已写入 {list.Count} 个场景：" + string.Join(" / ", names));
        }
    }
}
