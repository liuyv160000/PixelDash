using UnityEngine;

namespace PixelDash.Core
{
    /// <summary>
    /// 生成器配置体检，供 <c>EnemyMakerBase</c> / <c>CollectionMakerBase</c> 在启动时调用。
    ///
    /// 解决的问题：生成时间表填错（空表 / 长度不匹配 / 未排序 / 缺预制体）时，
    /// 生成器**完全静默**地什么都不做 —— 没有报错也没有警告。这类问题只能靠人工
    /// 翻场景文件才能定位，代价很高。这里把常见配置错误一次性报出来。
    /// </summary>
    public static class SpawnConfigValidator
    {
        /// <summary>检查一个生成器的生成配置，发现问题即输出警告（不抛异常，不影响运行）。</summary>
        /// <param name="owner">调用方组件（用于警告定位）</param>
        /// <param name="prefab">生成用的预制体</param>
        /// <param name="spawnTimes">生成时间表</param>
        /// <param name="spawnYOffsets">对应的 Y 偏移表</param>
        public static void Validate(
            MonoBehaviour owner,
            GameObject prefab,
            float[] spawnTimes,
            float[] spawnYOffsets)
        {
            if (owner == null) return;

            string who = owner.GetType().Name;

            // ① 空时间表 —— 最致命的一种：生成器全程不工作，且毫无提示
            if (spawnTimes == null || spawnTimes.Length == 0)
            {
                Debug.LogWarning(
                    "[" + who + "] 生成时间表 Spawn Times 为空 —— 本生成器不会生成任何东西。" +
                    "请在 Inspector 里填写生成时间点。", owner);
                return;
            }

            // ② 预制体缺失 —— 会退化成无贴图的占位对象
            if (prefab == null)
            {
                Debug.LogWarning(
                    "[" + who + "] 未设置预制体 —— 将生成无贴图的占位对象。", owner);
            }

            // ③ 两张表长度不一致 —— 多出的偏移按 0 处理（可能不是本意）
            int offsetLen = spawnYOffsets != null ? spawnYOffsets.Length : 0;
            if (offsetLen != spawnTimes.Length)
            {
                Debug.LogWarning(
                    "[" + who + "] 时间表与 Y 偏移表长度不一致（" + spawnTimes.Length +
                    " vs " + offsetLen + "）—— 缺少的偏移按 0 处理。", owner);
            }

            // ④ 时间表未升序 —— 倒序时第一帧会把后面的生成点全部触发
            for (int i = 1; i < spawnTimes.Length; i++)
            {
                if (spawnTimes[i] < spawnTimes[i - 1])
                {
                    Debug.LogWarning(
                        "[" + who + "] 生成时间表未按升序排列（第 " + i + " 项 " +
                        spawnTimes[i] + " 小于前一项 " + spawnTimes[i - 1] +
                        "）—— 这些生成点会在同一帧全部触发。", owner);
                    break;
                }
            }
        }
    }
}
