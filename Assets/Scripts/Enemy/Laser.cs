using System;
using UnityEngine;
using PixelDash.Core;
using PixelDash.Player;

namespace PixelDash.Enemy
{
    /// <summary>
    /// 激光的可调参数集（镜像源项目 laser 的字段默认值）。
    /// 由生成器（<see cref="LaserMaker"/>）在生成时注入 —— 生成器上的值是**唯一来源**。
    /// </summary>
    [Serializable]
    public struct LaserParams
    {
        [Tooltip("碰到玩家造成的伤害（源项目 30）")]
        public int contactDamage;

        [Tooltip("激光总存在时长（秒，源项目 atk_time = 0.5），到时回收")]
        public float lifeTime;

        [Tooltip("预警阶段占总时长的比例（0~1）。源项目把生命期前段作为预警，此处默认 0.5")]
        public float warningRatio;

        [Tooltip("预警阶段的外观透明度")]
        public float warningAlpha;

        [Tooltip("攻击阶段的外观透明度")]
        public float attackAlpha;

        /// <summary>源项目默认值。</summary>
        public static LaserParams Default => new LaserParams
        {
            contactDamage = 30,
            lifeTime = 0.5f,
            warningRatio = 0.5f,
            warningAlpha = 0.35f,
            attackAlpha = 1f,
        };
    }

    /// <summary>
    /// 激光（镜像源项目 laser，2 状态）：warning(预警) → attack(攻击)，本体不移动。
    /// 生命期前 warningRatio 为预警态（半透明），之后转攻击态（高亮），到期回收；碰玩家造成伤害。
    ///
    /// 和电锯一样属于**环境危险物**：没有 HP 参数、不参与玩家子弹的碰撞（子弹只碰 Player / Enemy 层）、
    /// 且显式覆写了 <see cref="GetHurted"/> 为空实现 —— 即便将来碰撞真的发生也不会掉血。
    ///
    /// 所有数值来自 <see cref="LaserParams"/> —— 由生成器在 <see cref="Setup"/> 时注入。
    /// </summary>
    public class Laser : EnemyControllerBase, IPoolable
    {
        private enum St { Warning, Attack }

        [Header("参数（生成时由 Maker 注入；此处仅为预制体默认值）")]
        [SerializeField] private LaserParams parameters = LaserParams.Default;

        /// <summary>当前生效的参数。</summary>
        public LaserParams Parameters => parameters;

        /// <summary>当前阶段名（调试用）。</summary>
        public string CurrentPhase => _st.ToString();

        private St _st;
        private float _lifeTimer;
        private float _attackAtElapsed;   // 经过多少秒后由预警转攻击
        private SpriteRenderer _sr;

        // ===== 攻击态闪烁（不走 Animator：激光是程序化的"闪"，没有美术动画）=====
        /// <summary>闪烁两端颜色：白 (255,255,255,255)。</summary>
        private static readonly Color FlickerColorA = new Color(1f, 1f, 1f, 1f);

        /// <summary>闪烁另一端颜色：橙红 (255,55,0,255)。</summary>
        private static readonly Color FlickerColorB = new Color(1f, 55f / 255f, 0f, 1f);

        [Tooltip("闪烁速度：每秒完成多少个「亮→暗→亮」的完整往返")]
        [SerializeField] private float flickerSpeed = 2f;

        /// <summary>归属对象池（由生成器注入；为空时退化为 Destroy）。</summary>
        private ObjectPool _pool;

        /// <summary>由生成器调用：注入参数与对象池。</summary>
        public void Setup(in LaserParams p, ObjectPool pool = null)
        {
            parameters = p;
            _pool = pool;
            _lifeTimer = parameters.lifeTime;
            // 预警持续 lifeTime * warningRatio 秒，之后转攻击态
            _attackAtElapsed = parameters.lifeTime * Mathf.Clamp01(parameters.warningRatio);
            ChangeState(St.Warning);
        }

        private void Awake()
        {
            EnsurePhysics(new Color(1f, 0.25f, 0.85f), 1f);
            _sr = GetComponent<SpriteRenderer>();

            // 所有碰撞体设为 Trigger —— 只做伤害检测，不参与物理推开
            MakeTriggers();
        }

        private void Update()
        {
            if (isDead) return;
            float dt = Time.deltaTime;

            _lifeTimer -= dt;
            if (_lifeTimer <= 0f)
            {
                RecycleToPool();
                return;
            }

            if (_st == St.Warning && parameters.lifeTime - _lifeTimer >= _attackAtElapsed)
                ChangeState(St.Attack);

            // 攻击态每帧闪烁（预警态不闪，保持半透明静态）
            if (_st == St.Attack)
                VisualEffect();
        }

        /// <summary>
        /// 攻击态视觉效果：让颜色在 (255,255,255,255) 与 (255,55,0,255) 之间**线性往返循环**，
        /// 模拟激光的闪烁。
        ///
        /// 实现：Repeat + 取镜像得到 0 → 1 → 0 → 1 … 的三角波（一个完整往返 = 1/flickerSpeed 秒），
        /// 再用 Color.Lerp 线性插值 —— 匀速变化，不是缓入缓出。
        /// 透明度沿用参数里的 attackAlpha（这里只动 RGB）。
        /// 不用 Animator —— 只是改 SpriteRenderer.color，零额外分配。
        /// </summary>
        private void VisualEffect()
        {
            if (_sr == null) return;

            // 相位 0→1 线性循环（一个周期 = 1/flickerSpeed 秒），再镜像成 0→1→0 的三角波
            float cycle = Mathf.Repeat(Time.time * flickerSpeed, 1f);
            float t = cycle < 0.5f ? cycle * 2f : (1f - cycle) * 2f;

            Color c = Color.Lerp(FlickerColorA, FlickerColorB, t);
            c.a = parameters.attackAlpha;      // 透明度仍由状态参数决定，这里只动 RGB
            _sr.color = c;
        }

        private void ChangeState(St next)
        {
            bool enteringAttack = next == St.Attack && _st != St.Attack;

            _st = next;
            if (_sr != null)
            {
                var c = _sr.color;
                c.a = next == St.Attack ? parameters.attackAlpha : parameters.warningAlpha;
                _sr.color = c;
            }

            // 仅在「预警 → 攻击」那一次上报（源项目 laser.ts 转攻击态时 emit）
            if (enteringAttack)
                GameEventBus.Instance.Enemy.Emit(GameEventBus.EnemyEvent.LASER_ATTACK, gameObject);
        }

        /// <summary>
        /// 回收：有对象池则归还（复用），否则销毁。
        /// 不用基类的 <c>Recycle()</c> —— 那个是 Destroy，且非 virtual 无法覆写。
        /// </summary>
        private void RecycleToPool()
        {
            if (isDead) return;
            isDead = true;

            if (_pool != null) _pool.Recycle(gameObject);
            else Destroy(gameObject);
        }

        /// <summary>
        /// 归还对象池时重置状态（IPoolable）。
        /// 颜色一定要复位：上一轮攻击态闪到的橙红会留到下一轮的预警态上。
        /// </summary>
        public void OnRecycle()
        {
            isDead = false;
            _st = St.Warning;
            _lifeTimer = 0f;

            if (_sr != null) _sr.color = FlickerColorA;   // RGB 回白、alpha 回 1（Setup 会再按预警参数设 alpha）
        }

        /// <summary>
        /// 不可击杀（环境危险物，与电锯一致）。
        /// 源项目 laser 没覆写 get_hurted，会走基类扣血 —— 但那是"子弹碰不到它"的副作用，
        /// 不是有意设计。这里显式空实现，杜绝以后打开 Trap 层碰撞时它突然变成可击杀。
        /// </summary>
        public override void GetHurted(int damage) { }

        /// <summary>碰到玩家 → 输出调试信息 + 造成伤害。</summary>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isDead) return;

            // 玩家的碰撞器可能挂在子物体上（父挂刚体 + 子挂碰撞器架构）
            var p = other.GetComponent<PlayerController>()
                 ?? other.GetComponentInParent<PlayerController>();

            if (p == null) return;

            Debug.Log("[Laser] 碰到玩家了 —— 伤害 " + parameters.contactDamage
                      + "（碰到的是 " + other.name + "）", this);

            p.GetHurted(parameters.contactDamage);
        }
    }
}
