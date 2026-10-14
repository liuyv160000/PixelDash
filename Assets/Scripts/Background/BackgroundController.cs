using System.Collections.Generic;
using UnityEngine;
using PixelDash.Core;

namespace PixelDash.Background
{
    /// <summary>
    /// 背景循环滚动：镜像源项目 background_controler（三张 city 图循环）。
    /// 以中间子图为基准整体 -X 移动，最左图超出半幅宽度移到最右。
    /// </summary>
    public class BackgroundController : SceneComponentBase
    {
        [SerializeField] private float speed = 50f;

        private readonly List<Transform> _children = new List<Transform>();
        private float _segmentWidth;

        private void Awake()
        {
            foreach (Transform child in transform)
                _children.Add(child);

            if (_children.Count > 0)
            {
                var sr = _children[0].GetComponent<SpriteRenderer>();
                _segmentWidth = sr != null ? sr.bounds.size.x : 10f;
            }
        }

        private void Update()
        {
            if (IsPaused) return;

            foreach (var child in _children)
                child.position += Vector3.left * (speed * Time.deltaTime);

            // 循环：最左子图超出 -segmentWidth/2 移到最右
            if (_children.Count == 0) return;
            float minX = float.MaxValue, maxX = float.MinValue;
            foreach (var c in _children)
            {
                if (c.position.x < minX) minX = c.position.x;
                if (c.position.x > maxX) maxX = c.position.x;
            }

            float centerX = transform.position.x;
            float threshold = centerX - _segmentWidth * 0.5f;
            foreach (var c in _children)
            {
                if (c.position.x < threshold && maxX < centerX + _segmentWidth * 1.5f)
                {
                    c.position = new Vector3(maxX + _segmentWidth, c.position.y, c.position.z);
                    maxX = c.position.x;
                }
            }
        }

        // 暂停/恢复只需翻转 IsPaused（基类负责），Update 里已按 IsPaused 早退
    }
}