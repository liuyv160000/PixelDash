using System.Collections.Generic;
using UnityEngine;

namespace PixelDash.Inputs
{
    /// <summary>
    /// 统一输入管理器（镜像源项目 UnifieldInputManager，单例）。
    /// 键盘（W/↑=Up，S/↓=Down，Space/J=Attack，A/←、D/→=水平轴）+ 触摸按钮，
    /// 汇成统一的 Down / Hold / Up 三态与轴值，供玩家等系统读取。
    /// </summary>
    public class UnifieldInputManager : MonoBehaviour
    {
        public static UnifieldInputManager Instance { get; private set; }

        // 键盘映射：Unity 无 KeyCode→动作表，直接在 Update 中判定
        private readonly Dictionary<GameObject, EInputType> _touchActionMap = new Dictionary<GameObject, EInputType>();
        private readonly HashSet<EInputType> _buttonsDown = new HashSet<EInputType>();
        private readonly HashSet<EInputType> _buttonsHold = new HashSet<EInputType>();
        private readonly HashSet<EInputType> _buttonsUp = new HashSet<EInputType>();
        private readonly Dictionary<EInputType, float> _axes = new Dictionary<EInputType, float>();

        private bool _axisLeft, _axisRight, _axisUp, _axisDown;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            _axes[EInputType.MoveHorizontal] = 0f;
            _axes[EInputType.MoveVertical] = 0f;
        }

        private void Update()
        {
            UpdateKeyboardButtons();
            UpdateKeyboardAxis();
        }

        private void LateUpdate()
        {
            // 一次性事件在帧末清空（与源项目 lateUpdate 一致）
            _buttonsDown.Clear();
            _buttonsUp.Clear();
        }

        private void UpdateKeyboardButtons()
        {
            SetButtonState(EInputType.Up,
                Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow),
                Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow),
                Input.GetKeyUp(KeyCode.W) || Input.GetKeyUp(KeyCode.UpArrow));

            SetButtonState(EInputType.Down,
                Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow),
                Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow),
                Input.GetKeyUp(KeyCode.S) || Input.GetKeyUp(KeyCode.DownArrow));

            SetButtonState(EInputType.Attack,
                Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.J),
                Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.J),
                Input.GetKeyUp(KeyCode.Space) || Input.GetKeyUp(KeyCode.J));
        }

        private void SetButtonState(EInputType action, bool down, bool hold, bool up)
        {
            if (down) _buttonsDown.Add(action);
            if (up) _buttonsUp.Add(action);

            if (hold) _buttonsHold.Add(action);
            else if (up || (!down && !hold)) _buttonsHold.Remove(action);
        }

        private void UpdateKeyboardAxis()
        {
            _axisLeft = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
            _axisRight = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
            _axisUp = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
            _axisDown = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);

            float x = (_axisLeft ? -1f : 0f) + (_axisRight ? 1f : 0f);
            float y = (_axisDown ? -1f : 0f) + (_axisUp ? 1f : 0f);

            float len = Mathf.Sqrt(x * x + y * y);
            if (len > 1f) { x /= len; y /= len; }

            _axes[EInputType.MoveHorizontal] = x;
            _axes[EInputType.MoveVertical] = y;
        }

        // ================= 触摸按钮注册 =================
        public void RegisterButton(GameObject node, EInputType action)
        {
            if (node == null) return;
            _touchActionMap[node] = action;
        }

        public void UnregisterButton(GameObject node)
        {
            if (node == null) return;
            _touchActionMap.Remove(node);
        }

        // ================= 模拟输入（供虚拟按钮调用） =================
        public void SimulateKeyDown(EInputType action)
        {
            if (!_buttonsHold.Contains(action)) _buttonsDown.Add(action);
            _buttonsHold.Add(action);
        }

        public void SimulateKeyUp(EInputType action)
        {
            _buttonsHold.Remove(action);
            _buttonsUp.Add(action);
        }

        // ================= 公共查询 =================
        public float GetAxis(EInputType action) => _axes.TryGetValue(action, out var v) ? v : 0f;

        public Vector2 GetMoveDirection() =>
            new Vector2(GetAxis(EInputType.MoveHorizontal), GetAxis(EInputType.MoveVertical));

        public bool IsActionDown(EInputType action) => _buttonsDown.Contains(action);
        public bool IsActionHeld(EInputType action) => _buttonsHold.Contains(action);
        public bool IsActionUp(EInputType action) => _buttonsUp.Contains(action);

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
