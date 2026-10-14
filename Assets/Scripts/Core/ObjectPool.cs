using System.Collections.Generic;
using UnityEngine;

namespace PixelDash.Core
{
    /// <summary>
    /// GameObject 对象池：初始 initialSize 个，动态伸缩（使用率≥80% 总量×1.5，≤20% 总量×0.5，上限 maxSize）。
    /// 挂到生成器上；可复用项需实现 IPoolable 以便归还时重置。
    /// </summary>
    public class ObjectPool : MonoBehaviour
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private int initialSize = 5;
        [SerializeField] private int maxSize = 20;

        /// <summary>扩容倍率：使用率过高时按当前总数 × 该倍率一次性扩充。</summary>
        private const float GrowFactor = 1.5f;

        /// <summary>缩容倍率：使用率过低时按当前总数 × 该倍率一次性收缩。</summary>
        private const float ShrinkFactor = 0.5f;

        /// <summary>扩容触发线：使用中的对象占比达到该值即扩容。</summary>
        private const float GrowThreshold = 0.8f;

        /// <summary>缩容触发线：使用中的对象占比低于该值即缩容。</summary>
        private const float ShrinkThreshold = 0.2f;

        private readonly Queue<GameObject> _free = new Queue<GameObject>();
        private readonly List<GameObject> _active = new List<GameObject>();

        public GameObject Prefab => prefab;
        public int ActiveCount => _active.Count;
        public int TotalCount => _free.Count + _active.Count;

        private void Awake()
        {
            for (int i = 0; i < initialSize; i++)
                CreateOne();
        }

        private GameObject CreateOne()
        {
            var go = prefab != null ? Instantiate(prefab) : new GameObject("pool_item");
            go.SetActive(false);
            _free.Enqueue(go);
            return go;
        }

        /// <summary>
        /// 运行时配置池（供生成器在 Awake 中动态指定 prefab 与容量）：
        /// 清空现有对象并重建。initialSize 即 poolSize。
        /// </summary>
        public void Configure(GameObject newPrefab, int newInitialSize, int newMaxSize)
        {
            while (_free.Count > 0)
            {
                var g = _free.Dequeue();
                if (g != null) Destroy(g);
            }
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (_active[i] != null) Destroy(_active[i]);
            }
            _active.Clear();

            prefab = newPrefab;
            initialSize = Mathf.Max(1, newInitialSize);
            maxSize = Mathf.Max(initialSize, newMaxSize);

            for (int i = 0; i < initialSize; i++)
                CreateOne();
        }

        /// <summary>取出一个对象并放到指定位置，激活。</summary>
        public GameObject Get(Vector3 position)
        {
            // 跳过已被销毁的脏条目（Unity 里 destroyed 对象的 == null 判为 true）。
            // 这类条目本不该出现（Recycle 已做幂等），但一旦出现就是 MissingReferenceException
            // 炸在下面第 88 行，所以这里就地丢弃、让池自愈，而不是把异常抛给调用方。
            GameObject go = null;
            while (_free.Count > 0 && go == null)
                go = _free.Dequeue();

            if (go == null)
            {
                if (TotalCount >= maxSize) return null; // 池满
                go = CreateOne();
            }

            go.transform.position = position;
            go.SetActive(true);
            _active.Add(go);

            // 扩容：使用率 ≥ 80% → 按 1.5 倍一次性扩充（maxSize 封顶）
            if (TotalCount < maxSize && (float)_active.Count / TotalCount >= GrowThreshold)
                Grow();

            return go;
        }

        /// <summary>按 <see cref="GrowFactor"/> 扩充池容量（至少 +1，不超过 maxSize）。</summary>
        private void Grow()
        {
            int target = Mathf.CeilToInt(TotalCount * GrowFactor);
            target = Mathf.Max(target, TotalCount + 1);   // 至少多一个，避免小池四舍五入后不增长
            target = Mathf.Min(target, maxSize);

            while (TotalCount < target) CreateOne();
        }

        /// <summary>按 <see cref="ShrinkFactor"/> 收缩池容量（不低于 initialSize）。</summary>
        private void Shrink()
        {
            int target = Mathf.FloorToInt(TotalCount * ShrinkFactor);
            target = Mathf.Max(target, initialSize);

            while (TotalCount > target && _free.Count > 0)
            {
                var toDestroy = _free.Dequeue();
                if (toDestroy != null) Destroy(toDestroy);
            }
        }

        /// <summary>归还对象：运行时先让对象自身重置（IPoolable.OnRecycle）。**对同一对象的重复归还是幂等的**。</summary>
        public void Recycle(GameObject go)
        {
            if (go == null) return;

            // 幂等：同一对象被重复归还时忽略第二次。
            // 否则它会在 _free 里出现两次 → 两次 Get 取到同一个实例（位置与状态互相覆盖）。
            if (_free.Contains(go)) return;

            if (_active.Contains(go)) _active.Remove(go);

            var poolable = go.GetComponent<IPoolable>();
            poolable?.OnRecycle();

            go.SetActive(false);
            _free.Enqueue(go);

            // 缩容：使用率 ≤ 20% → 按 0.5 倍一次性收缩（不低于 initialSize）
            if (TotalCount > initialSize && _free.Count > 0 && (float)_active.Count / TotalCount <= ShrinkThreshold)
                Shrink();
        }

        public void RecycleAll()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
                Recycle(_active[i]);
            _active.Clear();
        }
    }

    /// <summary>对象池可复用项接口：归还时调用 OnRecycle 重置状态。</summary>
    public interface IPoolable
    {
        void OnRecycle();
    }
}