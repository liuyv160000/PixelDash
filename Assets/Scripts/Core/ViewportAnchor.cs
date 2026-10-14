using UnityEngine;

namespace PixelDash.Core
{
    /// <summary>
    /// 视口定位工具：把节点摆到主相机视口之外。
    ///
    /// 用途：敌人生成器 / 收集物生成器在 Awake 时把自己移到屏幕右侧外，
    /// 这样生成出来的敌人、金币、技能球天然从画面外飞入，而不是凭空出现在屏幕里。
    /// </summary>
    public static class ViewportAnchor
    {
        /// <summary>取主相机（优先 Camera.main，回退到场景里任意一台）。</summary>
        public static Camera GetMainCamera()
        {
            var cam = Camera.main;
            if (cam != null) return cam;
            return Object.FindObjectOfType<Camera>();
        }

        /// <summary>主相机视口右边缘的世界 X 坐标。相机不存在时返回 <paramref name="fallback"/>。</summary>
        public static float GetViewportRightX(float fallback = 0f)
        {
            var cam = GetMainCamera();
            if (cam == null) return fallback;

            if (cam.orthographic)
            {
                // 正交相机：可见半宽 = orthographicSize × aspect
                float halfWidth = cam.orthographicSize * cam.aspect;
                return cam.transform.position.x + halfWidth;
            }

            // 透视相机：把视口右上角投影到近裁剪面
            Vector3 p = cam.ViewportToWorldPoint(new Vector3(1f, 0.5f, cam.nearClipPlane));
            return p.x;
        }

        /// <summary>主相机视口左边缘的世界 X 坐标。</summary>
        public static float GetViewportLeftX(float fallback = 0f)
        {
            var cam = GetMainCamera();
            if (cam == null) return fallback;

            if (cam.orthographic)
            {
                float halfWidth = cam.orthographicSize * cam.aspect;
                return cam.transform.position.x - halfWidth;
            }

            Vector3 p = cam.ViewportToWorldPoint(new Vector3(0f, 0.5f, cam.nearClipPlane));
            return p.x;
        }

        /// <summary>
        /// 把节点定位到主相机视口**右侧外** <paramref name="offsetX"/> 处。
        /// </summary>
        /// <param name="target">要移动的节点（通常是一个生成器容器）</param>
        /// <param name="offsetX">超出视口右边缘的距离（世界单位，正数）</param>
        /// <param name="alignYToPlatLine">true = Y 坐标对齐线平台（plat_line）；false = 保持原 Y</param>
        public static void AlignToRightOutsideView(Transform target, float offsetX, bool alignYToPlatLine)
        {
            if (target == null) return;

            float x = GetViewportRightX(target.position.x) + offsetX;
            float y = target.position.y;

            if (alignYToPlatLine)
            {
                float lineY;
                if (TryGetPlatLineY(out lineY)) y = lineY;
            }

            target.position = new Vector3(x, y, target.position.z);
        }

        /// <summary>取线平台（plat_line）的世界 Y 坐标。找不到返回 false。</summary>
        public static bool TryGetPlatLineY(out float y)
        {
            y = 0f;

            // 场景里通常有 PlatLine 组件，直接按组件找最稳
            // 注意：这里必须写全限定名 —— 本类在 PixelDash.Core 下，
            // 简写 Level.PlatLine 会被解析成 PixelDash.Core.Level.PlatLine（不存在）
            var platLine = Object.FindObjectOfType<PixelDash.Level.PlatLine>();
            if (platLine != null)
            {
                y = platLine.transform.position.y;
                return true;
            }

            // 回退：按命名约定找（plat_ver/plat_line 或根节点的 plat_line）
            var go = GameObject.Find("plat_line");
            if (go != null)
            {
                y = go.transform.position.y;
                return true;
            }

            return false;
        }
    }
}
