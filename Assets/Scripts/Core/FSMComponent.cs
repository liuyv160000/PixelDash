using UnityEngine;

namespace PixelDash.Core
{
    /// <summary>
    /// 可在 Inspector 挂载的状态机组件（对 <see cref="FSM"/> 的 MonoBehaviour 包装）。
    ///
    /// 适合场景：
    ///   - 想把这个状态机**挂到某个 GameObject 上**（而不是自己 new 一个纯类）
    ///   - 想在 **Inspector 里实时看到当前状态名**
    ///   - 状态注册仍然在代码里做（状态本来就带宿主逻辑，不适合纯 Inspector 配置）
    ///
    /// 用法：
    ///   var sm = gameObject.AddComponent&lt;FSMComponent&gt;();
    ///   sm.RegisterState("idle", new IdleState(this));
    ///   sm.RegisterState("jump", new JumpState(this));
    ///   sm.ChangeState("idle");          // Update 由本组件自动驱动
    ///
    /// 注意：本组件每帧自动调用 Machine.Update(Time.deltaTime)，
    /// 若宿主已自行驱动，请把 autoUpdate 关掉以免重复调用。
    /// </summary>
    [DisallowMultipleComponent]
    public class FSMComponent : MonoBehaviour
    {
        [Header("状态机")]
        [Tooltip("游戏开始时自动进入的初始状态（留空则不自动进入）")]
        [SerializeField] private string initialState = "";

        [Tooltip("勾选后本组件在 Update 中自动驱动状态机；宿主自己调 Machine.Update 时请取消勾选")]
        [SerializeField] private bool autoUpdate = true;

        [Header("运行时状态（只读显示）")]
        [SerializeField] private string currentStateDebug = "(未进入)";
        [SerializeField] private int registeredCountDebug;

        private readonly FSM _fsm = new FSM();

        /// <summary>底层状态机（需要手动驱动时用这个）。</summary>
        public FSM Machine => _fsm;

        public string CurrentState => _fsm.CurrentName;

        private void Start()
        {
            if (!string.IsNullOrEmpty(initialState))
                ChangeState(initialState);
        }

        private void Update()
        {
            if (autoUpdate)
                _fsm.Update(Time.deltaTime);

            RefreshDebug();
        }

        /// <summary>注册状态（在 Start 之前或之后都可调用）。</summary>
        public void RegisterState(string name, FSMState state)
        {
            _fsm.RegisterState(name, state);
            RefreshDebug();
        }

        /// <summary>切换状态。</summary>
        public void ChangeState(string name)
        {
            _fsm.ChangeState(name);
            RefreshDebug();
        }

        public bool HasState(string name) => _fsm.HasState(name);

        public string[] GetStateNames() => _fsm.GetStateNames();

        private void RefreshDebug()
        {
            currentStateDebug = string.IsNullOrEmpty(_fsm.CurrentName) ? "(未进入)" : _fsm.CurrentName;
            registeredCountDebug = _fsm.StateCount;
        }
    }
}
