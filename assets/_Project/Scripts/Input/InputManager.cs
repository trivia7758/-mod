using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace CaoCao.Input
{
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        public bool ClickDown { get; private set; }
        public bool CancelDown { get; private set; }
        public bool ConfirmDown { get; private set; }
        public Vector2 ScreenPosition { get; private set; }

        Mouse _mouse;
        Keyboard _keyboard;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            EnhancedTouchSupport.Enable();
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                EnhancedTouchSupport.Disable();
            }
        }

        void Update()
        {
            _mouse = Mouse.current;
            _keyboard = Keyboard.current;

            bool mouseClick = _mouse != null && _mouse.leftButton.wasPressedThisFrame;
            bool mouseRight = _mouse != null && _mouse.rightButton.wasPressedThisFrame;
            bool touchBegan = IsTouchBegan();

            ClickDown = mouseClick || touchBegan;

            CancelDown = mouseRight
                      || (_keyboard != null && _keyboard.escapeKey.wasPressedThisFrame);

            ConfirmDown = (_keyboard != null && _keyboard.enterKey.wasPressedThisFrame)
                       || (_keyboard != null && _keyboard.numpadEnterKey.wasPressedThisFrame)
                       || (_keyboard != null && _keyboard.spaceKey.wasPressedThisFrame)
                       || mouseClick
                       || touchBegan;

            if (_mouse != null)
                ScreenPosition = _mouse.position.ReadValue();

            if (Touch.activeFingers.Count > 0)
                ScreenPosition = Touch.activeFingers[0].screenPosition;
        }

        bool IsTouchBegan()
        {
            if (Touch.activeFingers.Count > 0)
            {
                var touch = Touch.activeFingers[0].currentTouch;
                return touch.phase == UnityEngine.InputSystem.TouchPhase.Began;
            }
            return false;
        }

        public Vector2 GetWorldPosition(Camera cam = null)
        {
            if (cam == null) cam = Camera.main;
            if (cam == null) return Vector2.zero;
            return cam.ScreenToWorldPoint(ScreenPosition);
        }

        public Vector2Int GetCellPosition(Vector2 origin, Vector2Int tileSize, Camera cam = null)
        {
            Vector2 world = GetWorldPosition(cam);
            Vector2 local = world - origin;
            return new Vector2Int(
                Mathf.FloorToInt(local.x / tileSize.x),
                Mathf.FloorToInt(local.y / tileSize.y)
            );
        }
    }
}
