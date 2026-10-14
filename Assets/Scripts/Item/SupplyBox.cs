using UnityEngine;
using PixelDash.Core;
using PixelDash.Enemy;

namespace PixelDash.Item
{
    /// <summary>
    /// 补给箱：继承 EnemyControllerBase —— **基础属性一律用敌人基类的**
    /// （<c>enemyHp</c> 血量、<c>moveSpeed</c> 左移速度、<c>moveDir</c> 方向），
    /// 本类只声明补给箱特有的字段。
    ///
    /// ⚠️ 绝不要在本类重复声明基类已有的字段名。子类与基类同名字段会让 Unity 序列化报
    /// "The same field name is serialized multiple times"，
    /// 且旧场景数据里的重复条目不会自动清理（需重存场景或删组件重建）。
    ///
    /// 行为：
    ///   - 碰撞体统一为 **Trigger**（只做命中检测，不参与物理推开）
    ///   - 移动：向左 + 正弦上下浮动（振幅 / 频率可调）
    ///   - **被玩家子弹击中 → 在原地生成一个技能球，自身销毁**
    /// </summary>
    public class SupplyBox : EnemyControllerBase
    {
        [Header("补给箱特有配置")]
        [SerializeField] private float floatAmplitude = 1f;   // 正弦振幅
        [SerializeField] private float floatFrequency = 1f;   // 正弦频率

        [Header("被击破后掉的技能球")]
        [Tooltip("技能球预制体（BulletSkillOrb / TripleBulletSkillOrb / MultiBulletSkillOrb）")]
        [SerializeField] private GameObject skillOrbPrefab;

        [Tooltip("技能球生成后的飞行速度（0 = 用预制体自身的 speed）")]
        [SerializeField] private float skillOrbSpeed = 0f;

        [Header("出屏回收")]
        [Tooltip("完全飞出主相机左边缘后，再往左这么多就销毁（世界单位）")]
        [SerializeField] private float outMargin = 1f;

        private Rigidbody2D _rb;
        private float _baseY;   // 出生 Y（正弦浮动基准）
        private float _elapsed; // 浮动累计时间

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            if (_rb == null) _rb = gameObject.AddComponent<Rigidbody2D>();
            _rb.gravityScale = 0f; // 补给箱不受重力

            // 碰撞体统一 Trigger：只做伤害/命中检测，不推开玩家
            var col = GetComponent<Collider2D>();
            if (col == null) col = gameObject.AddComponent<BoxCollider2D>();
            foreach (var c in GetComponentsInChildren<Collider2D>(true))
            {
                if (c != null) c.isTrigger = true;
            }

            _baseY = transform.position.y;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float x = transform.position.x - moveSpeed * Time.deltaTime;                                              // 向左（moveSpeed 来自基类）
            float y = _baseY + floatAmplitude * Mathf.Sin(2f * Mathf.PI * floatFrequency * _elapsed);                  // 正弦上下浮
            transform.position = new Vector3(x, y, transform.position.z);
            if (_rb != null) _rb.velocity = Vector2.zero;

            // 出屏回收：完全飞出主相机左边缘（再留 outMargin 余量）即销毁
            float leftLimit = ViewportAnchor.GetViewportLeftX(FallbackOutX) - Mathf.Abs(outMargin);
            if (transform.position.x < leftLimit)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>取不到主相机时的兜底左边界（与 CircleSaw 的约定一致）。</summary>
        private const float FallbackOutX = -800f;

        /// <summary>
        /// 被玩家子弹击中 → 原地生成技能球并销毁自己。
        ///
        /// 由 <c>PlayerBullet.OnTriggerEnter2D</c> 调用（子弹找到 EnemyControllerBase 后扣血）。
        /// 这里不扣血、不等血量归零 —— 碰到即破裂。
        /// 若想恢复"打几下才破"，把内容换成 enemyHp -= damage; if (enemyHp > 0) return; 即可。
        /// </summary>
        public override void GetHurted(int damage)
        {
            SpawnSkillOrb();
            Destroy(gameObject);
        }

        /// <summary>在当前位置生成技能球。</summary>
        private void SpawnSkillOrb()
        {
            if (skillOrbPrefab == null)
            {
                Debug.LogWarning("[SupplyBox] 未配置技能球预制体，被击破后不会掉落。", this);
                return;
            }

            var go = Instantiate(skillOrbPrefab, transform.position, Quaternion.identity);

            // 让技能球按指定速度向左飞（speed 为 0 时沿用预制体自身的设置）
            if (skillOrbSpeed > 0f)
            {
                var orb = go.GetComponent<BulletSkillOrb>();
                if (orb != null) orb.Init(null, skillOrbSpeed);
            }
        }
    }
}