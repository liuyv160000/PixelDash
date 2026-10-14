using System.Collections.Generic;
using UnityEngine;

namespace PixelDash.Core
{
    /// <summary>
    /// 有限状态机（纯 C# 类，不是 MonoBehaviour —— 不能 AddComponent/GetComponent）。
    ///
    /// 用法（三步，全部是代码）：
    ///   // 1) 建机器
    ///   private FSM _fsm = new FSM();
    ///   // 2) 注册状态（构造时把宿主传进去，状态里才能访问宿主；第二个参数是该状态的 Animator bool 名）
    ///   _fsm.RegisterState("idle", new IdleState(this, "isIdle"));
    ///   _fsm.RegisterState("jump", new JumpState(this, "isJump"));
    ///   // 3) 需要时切换，并每帧驱动
    ///   _fsm.ChangeState("idle");
    ///   void Update() => _fsm.Update(Time.deltaTime);
    ///
    /// 想在 Inspector 里挂载 / 看当前状态 → 用 <see cref="FSMComponent"/>。
    /// </summary>
    public sealed class FSM
    {
        private readonly Dictionary<string, FSMState> _states = new Dictionary<string, FSMState>();
        private FSMState _current;

        /// <summary>当前状态名（未进入任何状态时为 null）。</summary>
        public string CurrentName { get; private set; }

        /// <summary>是否已进入某个状态。</summary>
        public bool HasCurrent => _current != null;

        /// <summary>已注册的状态数量。</summary>
        public int StateCount => _states.Count;

        /// <summary>注册一个状态。name 为空或 state 为 null 时会报错（而不是静默忽略）。</summary>
        public void RegisterState(string name, FSMState state)
        {
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogError("[FSM] 注册失败：状态名不能为空");
                return;
            }
            if (state == null)
            {
                Debug.LogError($"[FSM] 注册失败：状态 '{name}' 的实例为 null");
                return;
            }
            state.Owner = this;
            state.StateName = name;
            _states[name] = state;
        }

        /// <summary>是否已注册该状态。</summary>
        public bool HasState(string name)
        {
            return !string.IsNullOrEmpty(name) && _states.ContainsKey(name);
        }

        /// <summary>已注册的全部状态名（便于排查拼写错误）。</summary>
        public string[] GetStateNames()
        {
            var arr = new string[_states.Count];
            _states.Keys.CopyTo(arr, 0);
            return arr;
        }

        /// <summary>切换状态。目标未注册时会报错并列出已注册状态（不再静默失败）。</summary>
        public void ChangeState(string name)
        {
            if (!_states.TryGetValue(name, out var next))
            {
                Debug.LogError($"[FSM] 找不到状态 '{name}'。已注册：{string.Join(", ", GetStateNames())}");
                return;
            }
            SwitchTo(next, name);
        }

        /// <summary>直接切换到状态实例（不想用字符串时）。</summary>
        public void ChangeState(FSMState state)
        {
            if (state == null)
            {
                Debug.LogError("[FSM] ChangeState(null)");
                return;
            }
            SwitchTo(state, string.IsNullOrEmpty(state.StateName) ? state.GetType().Name : state.StateName);
        }

        private void SwitchTo(FSMState next, string name)
        {
            if (ReferenceEquals(_current, next)) return; // 同状态不重复进出
            _current?.OnExit();
            _current = next;
            CurrentName = name;
            _current.OnEnter();
        }

        /// <summary>每帧驱动当前状态（宿主 Update 里调用）。</summary>
        public void Update(float deltaTime)
        {
            _current?.OnUpdate(deltaTime);
        }

        /// <summary>退出当前状态并清空（可选）。</summary>
        public void Stop()
        {
            _current?.OnExit();
            _current = null;
            CurrentName = null;
        }

        /// <summary>兼容源项目命名。未进入任何状态时返回可读提示（便于排查"注册了却没进状态"）。</summary>
        public string getCurrentStateName() => CurrentName ?? "(未进入任何状态)";
    }

    /// <summary>
    /// 状态基类。子类在构造函数里接收宿主引用，以及**本状态对应的 Animator bool 参数名**：
    /// <code>public class IdleState : FSMState { public IdleState(PlayerController c, string anim) : base(c, anim) { _ctrl = c; } }</code>
    ///
    /// 动画约定（配合 Animator）：进入状态把该 bool 置 true，退出置 false —— 由基类统一处理，
    /// 所以子类覆写 <see cref="OnEnter"/> / <see cref="OnExit"/> 时**必须调用 base**，否则动画会粘住。
    /// </summary>
    public abstract class FSMState
    {
        /// <summary>所属状态机（RegisterState 时自动注入）。</summary>
        public FSM Owner { get; set; }

        /// <summary>本状态注册时的名字（RegisterState 时自动注入）。</summary>
        public string StateName { get; internal set; }

        /// <summary>宿主 Animator（构造时自动从宿主身上取；宿主没挂 Animator 时为 null）。</summary>
        protected readonly Animator _animator;

        /// <summary>本状态对应的 Animator bool 参数名（构造时传入；留空 = 本状态不驱动动画）。</summary>
        private readonly string _animBoolName;

        /// <summary>不接动画的状态用这个构造（Animator 为 null，置位会被静默跳过）。</summary>
        protected FSMState() { }

        /// <param name="host">宿主（MonoBehaviour）—— 基类从它身上 GetComponent&lt;Animator&gt;()。</param>
        /// <param name="animBoolName">Animator 里的 bool 参数名（大小写敏感，必须与 Animator Controller 里一致）。</param>
        protected FSMState(MonoBehaviour host, string animBoolName)
        {
            // 玩家/敌人的 Animator 挂在子节点上（父挂刚体、子挂渲染+动画），所以往下找
            _animator = host != null ? host.GetComponentInChildren<Animator>(true) : null;
            _animBoolName = animBoolName;
        }

        /// <summary>进入状态：先置动画 bool = true（子类覆写时先调 base.OnEnter()）。</summary>
        public virtual void OnEnter() => SetAnimBool(true);

        /// <summary>离开状态：置动画 bool = false（子类覆写时别忘了调 base.OnExit()）。</summary>
        public virtual void OnExit() => SetAnimBool(false);

        /// <summary>停留在该状态期间每帧调用。</summary>
        public virtual void OnUpdate(float deltaTime) { }

        /// <summary>
        /// 写本状态的 Animator bool。
        /// 没挂 Animator、或构造时没传参数名 → 静默跳过（占位场景没动画也能正常跑）。
        /// </summary>
        protected void SetAnimBool(bool value)
        {
            if (_animator == null || string.IsNullOrEmpty(_animBoolName)) return;
            _animator.SetBool(_animBoolName, value);
        }
    }
}
