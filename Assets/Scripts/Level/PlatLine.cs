using System.Collections.Generic;
using UnityEngine;

namespace PixelDash.Level
{
    /// <summary>
    /// 平台线（镜像源项目 plat_line）：作为地面/平台标记，记录当前站在其上的实体，
    /// 供外部查询（玩家落地与重力反转判定依赖节点的 "plat_line" / "plat_ver" 命名约定）。
    /// </summary>
    public class PlatLine : MonoBehaviour
    {
        private readonly HashSet<Transform> _nodesOnPlatform = new HashSet<Transform>();
        private Collider2D _collider;

        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
            EnsureStaticBody();
        }

        /// <summary>静态刚体（不受重力、不移动，正确参与碰撞）。</summary>
        private void EnsureStaticBody()
        {
            var rb = GetComponent<Rigidbody2D>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;
            rb.gravityScale = 0f;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision == null) return;
            var t = collision.collider.transform;
            if (_nodesOnPlatform.Add(t))
                OnEntityEnterPlatform(t);
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            if (collision == null) return;
            var t = collision.collider.transform;
            if (_nodesOnPlatform.Remove(t))
                OnEntityExitPlatform(t);
        }

        /// <summary>实体进入平台（可在此扩展逻辑）。</summary>
        protected virtual void OnEntityEnterPlatform(Transform node) { }

        /// <summary>实体离开平台（可在此扩展逻辑）。</summary>
        protected virtual void OnEntityExitPlatform(Transform node) { }

        public int GetNodeCountOnPlatform() => _nodesOnPlatform.Count;

        public List<Transform> GetNodesOnPlatform() => new List<Transform>(_nodesOnPlatform);

        public bool IsNodeOnPlatform(Transform node) => _nodesOnPlatform.Contains(node);

        public void ClearPlatformNodes() => _nodesOnPlatform.Clear();
    }
}
