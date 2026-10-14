using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PixelDash.EditorTools
{
    /// <summary>
    /// 批量设置 Sprite 的导入参数（PPU / Filter Mode / Compression）。
    ///
    /// 手动改 77 张图太累，这里一键搞定。菜单在顶部：PixelDash → Sprite
    /// </summary>
    public static class SpriteImportTool
    {
        /// <summary>只处理这个目录下的图片（留空则处理整个 Assets）。</summary>
        private const string TargetFolder = "Assets/Art";

        [MenuItem("PixelDash/Sprite/① 批量设为像素风（PPU=1, Point, 无压缩）", false, 100)]
        public static void SetPixelArt()
        {
            Apply(1f, FilterMode.Point, TextureImporterCompression.Uncompressed, "像素风");
        }

        [MenuItem("PixelDash/Sprite/② 批量恢复 Unity 默认（PPU=100, Bilinear, 压缩）", false, 101)]
        public static void SetDefault()
        {
            Apply(100f, FilterMode.Bilinear, TextureImporterCompression.Compressed, "Unity 默认");
        }

        /// <summary>查看当前 Art 目录下的 PPU 分布（不改动任何东西，只报告）。</summary>
        [MenuItem("PixelDash/Sprite/③ 检查当前导入设置（只读）", false, 102)]
        public static void Inspect()
        {
            var report = new Dictionary<string, int>();
            var wrongFilter = new List<string>();
            var wrongComp = new List<string>();

            foreach (string path in EnumerateSprites())
            {
                var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                if (imp == null) continue;

                string key = $"PPU={imp.spritePixelsPerUnit}";
                report[key] = report.TryGetValue(key, out int c) ? c + 1 : 1;

                if (imp.filterMode != FilterMode.Point) wrongFilter.Add(path);
                if (imp.textureCompression != TextureImporterCompression.Uncompressed) wrongComp.Add(path);
            }

            Debug.Log("========== Sprite 导入设置报告 ==========");
            foreach (var kv in report)
                Debug.Log($"  {kv.Key}  —— {kv.Value} 张");
            Debug.Log($"  Filter Mode 不是 Point 的：{wrongFilter.Count} 张");
            Debug.Log($"  Compression 不是 None 的：{wrongComp.Count} 张");
            Debug.Log("=========================================");
        }

        private static void Apply(float ppu, FilterMode filter, TextureImporterCompression comp, string label)
        {
            var paths = new List<string>(EnumerateSprites());
            if (paths.Count == 0)
            {
                EditorUtility.DisplayDialog("Sprite 导入设置", $"在 {TargetFolder} 下没找到图片。", "好");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "Sprite 导入设置",
                    $"将把 {paths.Count} 张图设为「{label}」：\n\n" +
                    $"  • Pixels Per Unit = {ppu}\n" +
                    $"  • Filter Mode = {filter}\n" +
                    $"  • Compression = {comp}\n\n" +
                    "（重新导入需要一点时间）",
                    "执行", "取消"))
                return;

            int changed = 0;
            try
            {
                AssetDatabase.StartAssetEditing();   // 批量修改期间暂停导入，提速
                foreach (string path in paths)
                {
                    var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (imp == null) continue;

                    bool dirty = false;

                    if (imp.textureType != TextureImporterType.Sprite)
                    {
                        imp.textureType = TextureImporterType.Sprite;
                        dirty = true;
                    }
                    if (!Mathf.Approximately(imp.spritePixelsPerUnit, ppu))
                    {
                        imp.spritePixelsPerUnit = ppu;
                        dirty = true;
                    }
                    if (imp.filterMode != filter)
                    {
                        imp.filterMode = filter;
                        dirty = true;
                    }
                    if (imp.textureCompression != comp)
                    {
                        imp.textureCompression = comp;
                        dirty = true;
                    }

                    if (dirty)
                    {
                        EditorUtility.SetDirty(imp);
                        changed++;
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();    // 结束批量，Unity 统一重新导入
                AssetDatabase.Refresh();
            }

            Debug.Log($"[SpriteImportTool] 已更新 {changed} / {paths.Count} 张图为「{label}」。");
            Debug.Log("[SpriteImportTool] ⚠️ 改完 PPU 后记得同步：Main Camera 的 Size、场景里碰撞体的尺寸。");
        }

        private static IEnumerable<string> EnumerateSprites()
        {
            string folder = string.IsNullOrEmpty(TargetFolder) ? "Assets" : TargetFolder;
            if (!AssetDatabase.IsValidFolder(folder))
                yield break;

            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(path))
                    yield return path;
            }
        }
    }
}
