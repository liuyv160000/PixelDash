using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PixelDash.Inputs
{
    /// <summary>
    /// 虚拟按钮（镜像源项目 virtual_button）：按住时把动作转发给 UnifieldInputManager，
    /// 并切换按下/默认颜色。actionType 取 "UP" / "DOWN" / "ATTACK"。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class VirtualButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Tooltip("按钮绑定的动作：UP / DOWN / ATTACK")]
        public string actionType = "UP";

        [SerializeField] private Color defaultColor = new Color(1f, 1f, 1f, 0.8f);
        [SerializeField] private Color pressColor = new Color(0.59f, 0.59f, 0.59f, 0.8f);

        private Image _image;

        private void Awake()
        {
            _image = GetComponent<Image>();
            if (_image != null) _image.color = defaultColor;
        }

        private void Start()
        {
            var mgr = UnifieldInputManager.Instance;
            if (mgr == null)
            {
                Debug.LogWarning("[VirtualButton] UnifieldInputManager 实例不存在");
                return;
            }
            mgr.RegisterButton(gameObject, ParseAction());
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_image != null) _image.color = pressColor;
            UnifieldInputManager.Instance?.SimulateKeyDown(ParseAction());
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_image != null) _image.color = defaultColor;
            UnifieldInputManager.Instance?.SimulateKeyUp(ParseAction());
        }

        private EInputType ParseAction()
        {
            switch (actionType)
            {
                case "DOWN": return EInputType.Down;
                case "ATTACK": return EInputType.Attack;
                default: return EInputType.Up;
            }
        }

        private void OnDestroy()
        {
            UnifieldInputManager.Instance?.UnregisterButton(gameObject);
        }
    }
}
