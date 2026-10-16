using UnityEngine;
using UnityEngine.SceneManagement;
using PixelDash.Core;


namespace PixelDash.Player
{
    /// <summary>
    /// 玩家主控类（对应源项目 PlayerController）。
    /// 统一输入 + 5 状态 FSM + 重力反转 + 固定 X + 无敌 1s + 碰撞落地 + 技能驱动。
    /// </summary>
    public class PlayerController : SceneComponentBase
    {
        // ===== 公开属性（跨系统接口，其它系统会调用）=====
        public int Health = 100;
        public int FaceDir = 1;   // 1 朝右 / -1 朝左

        // ===== 状态标志（FSM 各状态读写）=====
        public bool ifAir = false;
        public bool ifGround = true;
        public bool on_line = false;         // 站在平台线上（plat_line）
        public bool if_can_reverse = true;  // 是否允许重力反转

        public bool if_reverseing = false;     // 是否正在反转中（Reversing 状态）

        private const float InvincibleDuration = 1.0f; // 无敌时长 0.5s

        // ===== 各状态对应的 Animator bool 参数名 =====
        // 原来是散在状态机注册处的字面量，现在收成常量：要改名只改这五行。
        // 必须与 Animator Controller 里的参数名逐字一致（大小写敏感），否则 Unity 会刷 "Parameter does not exist" 警告。
        // 状态机进入状态置 true、退出置 false（在 FSMState 基类里统一处理）。
        private const string AnimIsIdle = "Idle";
        private const string AnimIsJump = "Jump";
        private const string AnimIsAir = "Air";
        private const string AnimIsReversing = "Reversing";   // 原注册处误传了 "Air"（与 air 状态撞名），这里更正
        private const string AnimIsDead = "Dead";

        /// <summary>受击动画的 bool 名（不属于任何状态，由无敌计时结束时收回）。</summary>
        private const string AnimHurt = "invincle_hurt";

        // 组件引用
        private Rigidbody2D _rb;
        private Collider2D[] _colliders;
        private PlayerMovementCtrl _movement;
        private TimerComponent _invincibleTimer;
        public Animator _animator;
        private SkillSystem _skillSystem;
        private FSM _fsm;
        private SpriteRenderer _spriteRenderer;

        private bool _invincible = false;  // 当前是否无敌
        private bool _ended = false;       // 是否已进入结束流程

        // ===== 供 FSM 状态使用的便捷封装 =====
        public float gravity_reverse => _movement != null ? _movement.gravity_reverse : 1f;

        private void Awake()
        {
            // Rigidbody2D
            _rb = GetComponent<Rigidbody2D>();
            if (_rb == null) _rb = gameObject.AddComponent<Rigidbody2D>();

            // 碰撞体（至少保证有一个）
            if (GetComponent<Collider2D>() == null)
                gameObject.AddComponent<CircleCollider2D>();
            _colliders = GetComponents<Collider2D>();

            // 移动控制
            _movement = GetComponent<PlayerMovementCtrl>();
            if (_movement == null) _movement = gameObject.AddComponent<PlayerMovementCtrl>();
            _movement.Init(_rb);

            // 无敌计时器
            _invincibleTimer = GetComponent<TimerComponent>();
            if (_invincibleTimer == null) _invincibleTimer = gameObject.AddComponent<TimerComponent>();
            _invincibleTimer.SetDuration(InvincibleDuration);

            // 动画（占位，可空）
            _animator = GetComponentInChildren<Animator>();

            // 状态机注册
            _fsm = new FSM();
            _fsm.RegisterState("idle", new IdleState(this, AnimIsIdle));
            _fsm.RegisterState("jump", new JumpState(this, AnimIsJump));
            _fsm.RegisterState("air", new AirState(this, AnimIsAir));
            _fsm.RegisterState("reversing", new ReversingState(this, AnimIsReversing));
            _fsm.RegisterState("dead", new DeadState(this, AnimIsDead));

            // 技能系统
            _skillSystem = GetComponent<SkillSystem>();
            if (_skillSystem == null) _skillSystem = gameObject.AddComponent<SkillSystem>();
            _skillSystem.Init(this);

            _fsm.ChangeState("idle");

            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        }

        private void Start()
        {

        }

        private void Update()
        {
            if (IsPaused) return;

            

            // 无敌计时到期：受击动画的 bool 在这里收回（Bool 驱动没有"自动结束"，得自己关）
            if (_invincible && _invincibleTimer.CheckIfEnd())
            {
                _invincible = false;
                _spriteRenderer.color = new Vector4(_spriteRenderer.color.r, _spriteRenderer.color.g, _spriteRenderer.color.b, 1.0f);
            }

            if(_invincible)
            {
                HurtedEffect();
            }

            HandleInput();
            _fsm.Update(Time.deltaTime);
            _skillSystem.UpdateSkills(Time.deltaTime);
        }

        // ================= 统一输入 =================
        private void HandleInput()
        {
            var im = PixelDash.Inputs.UnifieldInputManager.Instance;

            bool up, down;

            if (im != null)
            {
                // 优先走统一输入管理器（键盘 + 触摸虚拟按钮）
                up = im.IsActionDown(PixelDash.Inputs.EInputType.Up);
                down = im.IsActionDown(PixelDash.Inputs.EInputType.Down);

                float ax = im.GetAxis(PixelDash.Inputs.EInputType.MoveHorizontal);
                if (ax < -0.5f) FaceDir = -1;
                else if (ax > 0.5f) FaceDir = 1;
            }
            else
            {
                // 回退：直接读键盘（无 UnifieldInputManager 时仍可玩）
                up = Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
                down = Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow);
                if (Input.GetKeyDown(KeyCode.A)) FaceDir = -1;
                if (Input.GetKeyDown(KeyCode.D)) FaceDir = 1;
            }

            if (gravity_reverse == 1f)
            {
                // 重力正常：UP = Jump；DOWN = 在地面 Reverse / 在空中 quickDown
                if (up && ifGround) ChangeState("jump");
                if (down)
                {
                    if (ifGround) Reverse();
                    else quickDown();
                }
            }
            else
            {
                // 重力反转：UP = 在地面 Reverse / 在空中 quickDown；DOWN = Jump
                if (up)
                {
                    if (ifGround) Reverse();
                    else quickDown();
                }
                if (down && ifGround) ChangeState("jump");
            }
        }


        // ================= 动作（供输入 / 状态调用）=================
        /// <summary>跳跃动作：给冲量并置空中标志。状态切换由输入触发。</summary>
        public void Jump()
        {
            _movement.Jump(); ChangeState("jump");

            ifAir = true;
            ifGround = false;
            GameEventBus.Instance.Player.Emit(GameEventBus.PlayerEvent.JUMP);
        }

        /// <summary>落地：清空中标志并切回 idle。</summary>
        public void Land()
        {
            bool wasAir = ifAir;
            ifGround = true;
            ifAir = false;

            if (_fsm.CurrentName != "idle" && _fsm.CurrentName != "dead")
                _fsm.ChangeState("idle");

            // 只在"确实从空中落下来"时上报，避免贴着地面反复触发
            if (wasAir)
                GameEventBus.Instance.Player.Emit(GameEventBus.PlayerEvent.LAND);
        }

        /// <summary>空中快速下落。</summary>
        public void quickDown()
        {
            _movement.quickDown();
        }

        /// <summary>重力反转：仅站在平台线且落地时可触发。</summary>
        public void Reverse()
        {
            if (!(on_line && ifGround && if_can_reverse && !ifAir)) return;
            _fsm.ChangeState("reversing");
        }

        // ================= 受伤 / 死亡 =================

        /// <summary>受伤（默认会响受伤音效）。</summary>
        public void GetHurted(int damage) => GetHurted(damage, true);

        /// <summary>
        /// 受伤：扣血 + 播受伤动画 + 发事件。
        ///
        /// 事件拆成两个，用途不同：
        ///   · <see cref="GameEventBus.PlayerEvent.HURT"/> —— "数值变了"（血条等 UI 用），**总是发**；
        ///   · <see cref="GameEventBus.PlayerEvent.HURT_SFX"/> —— "响一声受伤音效"，由 <paramref name="withHurtSfx"/> 控制。
        ///
        /// </summary>
        /// <param name="withHurtSfx">
        /// false = 不响受伤音效。撞击方自带专属音效时用（如追踪导弹撞玩家只放爆炸声，不叠受伤声）。
        /// </param>
        public void GetHurted(int damage, bool withHurtSfx)
        {
            if (_invincible) return;
            _invincible = true;
            _invincibleTimer.Restart();
            Health -= damage;
            
            GameEventBus.Instance.Player.Emit(GameEventBus.PlayerEvent.HURT, damage);
            if (withHurtSfx)
                GameEventBus.Instance.Player.Emit(GameEventBus.PlayerEvent.HURT_SFX);
            if (Health <= 0) Die();
        }

        private void HurtedEffect()
        {
            float a = Mathf.Sin(_invincibleTimer.GetElapsedTime() * 10 * Mathf.PI);
            _spriteRenderer.color = new Vector4(_spriteRenderer.color.r, _spriteRenderer.color.g, _spriteRenderer.color.b, a);
        }

        public void Die()
        {
            if (Health > 0) Health = 0;
            GameEventBus.Instance.Player.Emit(GameEventBus.PlayerEvent.DEAD);
            _fsm.ChangeState("dead");
        }

        /// <summary>死亡结束：0.5s 后加载失败场景。</summary>
        public void StartDeadEnd()
        {
            Invoke(nameof(DeadEnd), 0.5f);
        }

        private void DeadEnd()
        {
            SceneManager.LoadScene("FailScene");
        }

        // ================= 关卡胜利 / 暂停恢复 =================
        /// <summary>关卡胜利结束流程：无敌 + 1s 后发射 GAME_END 并销毁。</summary>
        public void EndOut()
        {
            if (_ended) return;
            _ended = true;
            _invincible = true;
            _invincibleTimer.Restart();
            Invoke(nameof(EndOutCompleted), 1f);
        }

        private void EndOutCompleted()
        {
            GameEventBus.Instance.Other.Emit(GameEventBus.OtherEvent.GAME_END);
            Destroy(gameObject);
        }

        // ===== 暂停协议：由 SceneComponentBase 统一管理（幂等）=====

        protected override void OnPause()
        {
            if (_rb != null) _rb.simulated = false;
            if (_invincibleTimer != null) _invincibleTimer.Stop();
        }

        protected override void OnResume()
        {
            if (_rb != null) _rb.simulated = true;
            if (_invincible && _invincibleTimer != null) _invincibleTimer.Start();
        }

        // ================= 状态机辅助 =================
        public void ChangeState(string name) => _fsm.ChangeState(name);
        public string CurrentState => _fsm.CurrentName;

        /// <summary>相对重力方向是否已开始下落（y 速度方向与重力方向一致）。</summary>
        public bool IsFallingRelativeToGravity()
        {
            return _movement.GetYSpeed() * gravity_reverse < 0f;
        }

        // ================= 尺寸 / 速度 / 碰撞体 / 翻转 =================
        public float GetSizeX() => GetSize().x;
        public float GetHeight() => GetSize().y;

        private Vector2 GetSize()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
                return sr.sprite.bounds.size;
            var bc = GetComponent<BoxCollider2D>();
            if (bc != null)
                return new Vector2(bc.size.x * transform.lossyScale.x, bc.size.y * transform.lossyScale.y);
            var cc = GetComponent<CircleCollider2D>();
            if (cc != null)
            {
                float d = cc.radius * 2f * transform.lossyScale.x;
                return new Vector2(d, d);
            }
            return Vector2.one;
        }

        public void SetYSpeed(float y) => _movement.SetYSpeed(y);
        public float GetYSpeed() => _movement.GetYSpeed();

        public void ReverseGravity() => _movement.Reverse_the_gravity();

        public void SetColliderTrigger(bool enabled)
        {
            foreach (var c in _colliders)
            {
                if (c != null) c.isTrigger = enabled;
            }
        }


        public void FlipScaleY()
        {
            var s = transform.localScale;
            s.y = -s.y;
            transform.localScale = s;
        }

        /// <summary>
        /// 驱动动画：把该 Bool 参数置 true（本工程统一用 Bool 驱动，不用 Trigger）。
        ///
        /// 状态类一般**不需要**调它 —— 状态机进入状态时基类已经按注册的 bool 名置位了。
        /// 它留给"不属于任何状态的临时动画"（如受击），配 <see cref="StopAnim"/> 收尾。
        /// </summary>
        public void PlayAnim(string boolName) => SetAnimBool(boolName, true);

        /// <summary>把该 Bool 参数置 false（临时动画播完/条件结束时调用）。</summary>
        public void StopAnim(string boolName) => SetAnimBool(boolName, false);

        /// <summary>写任意 Animator Bool 参数。没挂 Animator 或参数名为空时静默跳过。</summary>
        public void SetAnimBool(string boolName, bool value)
        {
            if (_animator == null || string.IsNullOrEmpty(boolName)) return;
            _animator.SetBool(boolName, value);
        }

        // ================= 碰撞（落地判定）=================
        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision == null || collision.collider == null) return;
            var other = collision.collider.transform;
            bool isPlatform = other.parent != null && other.parent.name == "PlatVer";
            if (!isPlatform) return;

            // 是否站在平台线（plat_line）上 → 允许反转
            on_line = HasPlatLine(other);
            if_can_reverse = on_line;
            Land();
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            if (collision == null || collision.collider == null) return;
            var other = collision.collider.transform;
            bool isPlatform = other.parent != null && other.parent.name == "PlatVer";
            if (!isPlatform) return;

            ifGround = false;
            ifAir = true;
            on_line = false;
        }

        /// <summary>检测节点是否为平台线（plat_ver 下的 plat_line 节点或带 plat_line 组件）。</summary>
        private bool HasPlatLine(Transform t)
        {
            if (t.name == "plat_line") return true;
            if (t.parent != null && t.transform.name == "PlatLine" ) return true;
            return t.GetComponent("PlatLine") != null;
        }
    }
}