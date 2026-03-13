using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CaoCao.Battle
{
    public class BattleActionMenu : MonoBehaviour
    {
        [SerializeField] Button attackButton;
        [SerializeField] Button waitButton;
        [SerializeField] RectTransform panelRect;

        public event Action OnAttack;
        public event Action OnWait;

        public bool IsShowing => gameObject.activeSelf;

        Canvas _canvas;

        void Awake()
        {
            _canvas = GetComponentInParent<Canvas>();
            if (attackButton != null)
                attackButton.onClick.AddListener(() => OnAttack?.Invoke());
            if (waitButton != null)
                waitButton.onClick.AddListener(() => OnWait?.Invoke());
        }

        public void ShowAt(Vector2 worldPosition)
        {
            gameObject.SetActive(true);
            if (_canvas != null && _canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                var cam = Camera.main;
                if (cam != null)
                {
                    Vector2 screenPos = cam.WorldToScreenPoint(worldPosition);
                    panelRect.position = screenPos;
                }
            }
            else
            {
                transform.position = worldPosition;
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public bool ContainsScreenPoint(Vector2 screenPoint)
        {
            if (panelRect == null) return false;
            return RectTransformUtility.RectangleContainsScreenPoint(panelRect, screenPoint, null);
        }
    }
}
