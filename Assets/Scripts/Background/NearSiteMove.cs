using UnityEngine;
using PixelDash.Core;

namespace PixelDash.Background
{
    /// <summary>前景（大楼/广告牌）独立横向移动：镜像 near_site_move。</summary>
    public class NearSiteMove : SceneComponentBase
    {
        [SerializeField] private float moveSpeed = 8f;
        [SerializeField] private int moveDirection = -1;

        private void Update()
        {
            if (IsPaused) return;
            transform.position += new Vector3(moveDirection * moveSpeed * Time.deltaTime, 0f, 0f);
        }

        // 暂停/恢复只需翻转 IsPaused（基类负责），Update 里已按 IsPaused 早退
    }
}
