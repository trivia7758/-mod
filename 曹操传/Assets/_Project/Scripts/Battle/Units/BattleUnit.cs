using System;
using System.Collections;
using UnityEngine;
using CaoCao.Data;

namespace CaoCao.Battle
{
    public class BattleUnit : MonoBehaviour
    {
        [Header("Stats")]
        public int maxHp = 20;
        public int hp = 20;
        public int atk = 6;
        public int def = 2;
        public int mov = 5;

        [Header("State")]
        public UnitTeam team = UnitTeam.Player;
        public Vector2Int cell;
        public Vector2Int tileSize = new(48, 48);
        public bool acted;

        public event Action<BattleUnit> OnMoved;
        public event Action<BattleUnit, BattleUnit> OnAttacked;
        public event Action<BattleUnit> OnDied;

        SpriteRenderer _sprite;
        TMPro.TMP_Text _hpLabel;
        Sprite _overrideSprite;  // Set by InitFromHero before Start()

        void Awake()
        {
            // Delay visual creation — stats may not be set yet when AddComponent is called.
            // Visual is created on first Start() or when RefreshVisual() is called explicitly.
        }

        void Start()
        {
            EnsureVisual();
            UpdatePosition();
        }

        /// <summary>
        /// Re-create the visual elements after stats/team have been set externally.
        /// Called automatically in Start(), but can be called manually if needed.
        /// </summary>
        public void RefreshVisual()
        {
            // Destroy old visual if it exists
            if (_sprite != null)
            {
                Destroy(_sprite.gameObject);
                _sprite = null;
            }
            if (_hpLabel != null)
            {
                Destroy(_hpLabel.transform.parent.gameObject); // destroy the canvas parent
                _hpLabel = null;
            }
            EnsureVisual();
            UpdatePosition();
        }

        void EnsureVisual()
        {
            _sprite = GetComponentInChildren<SpriteRenderer>();
            if (_sprite == null)
            {
                var sprGo = new GameObject("Sprite");
                sprGo.transform.SetParent(transform);
                sprGo.transform.localPosition = Vector3.zero;
                _sprite = sprGo.AddComponent<SpriteRenderer>();

                // Create 1px colored sprite
                var tex = new Texture2D(1, 1);
                tex.SetPixel(0, 0, team == UnitTeam.Player ? new Color(0.2f, 0.6f, 1f) : new Color(0.9f, 0.25f, 0.2f));
                tex.Apply();
                _sprite.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
                _sprite.transform.localScale = new Vector3(tileSize.x * 0.8f, tileSize.y * 0.8f, 1);
            }

            // Apply override sprite from InitFromHero if set
            if (_overrideSprite != null && _sprite != null)
            {
                _sprite.sprite = _overrideSprite;
                _sprite.transform.localScale = Vector3.one;
            }

            EnsureHpLabel();
        }

        void EnsureHpLabel()
        {
            _hpLabel = GetComponentInChildren<TMPro.TMP_Text>();
            if (_hpLabel == null)
            {
                // Create a world-space canvas for HP label
                var canvasGo = new GameObject("HPCanvas");
                canvasGo.transform.SetParent(transform);
                canvasGo.transform.localPosition = new Vector3(0, tileSize.y * 0.6f, 0);
                var canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.sortingOrder = 10;
                var rt = canvasGo.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(80, 20);
                rt.localScale = new Vector3(0.5f, 0.5f, 1f);

                var textGo = new GameObject("HpText");
                textGo.transform.SetParent(canvasGo.transform);
                _hpLabel = textGo.AddComponent<TMPro.TextMeshProUGUI>();
                _hpLabel.alignment = TMPro.TextAlignmentOptions.Center;
                _hpLabel.fontSize = 14;
                var textRt = textGo.GetComponent<RectTransform>();
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.offsetMin = Vector2.zero;
                textRt.offsetMax = Vector2.zero;
            }
            UpdateHpLabel();
        }

        void UpdatePosition()
        {
            transform.position = new Vector3(
                cell.x * tileSize.x + tileSize.x * 0.5f,
                cell.y * tileSize.y + tileSize.y * 0.5f,
                0
            );
        }

        public void MoveTo(Vector2Int target, bool animate)
        {
            cell = target;
            if (!animate)
            {
                UpdatePosition();
                UpdateHpLabel();
                OnMoved?.Invoke(this);
                return;
            }
            StartCoroutine(AnimateMove(target));
        }

        IEnumerator AnimateMove(Vector2Int target)
        {
            Vector3 startPos = transform.position;
            Vector3 endPos = new(
                target.x * tileSize.x + tileSize.x * 0.5f,
                target.y * tileSize.y + tileSize.y * 0.5f,
                0
            );
            float duration = 0.2f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(startPos, endPos, elapsed / duration);
                yield return null;
            }
            transform.position = endPos;
            OnMoved?.Invoke(this);
        }

        public void Attack(BattleUnit target)
        {
            if (target == null) return;
            int dmg = Mathf.Max(1, atk - target.def);
            target.hp -= dmg;
            target.UpdateHpLabel();
            OnAttacked?.Invoke(this, target);
            if (target.hp <= 0)
            {
                target.hp = 0;
                target.UpdateHpLabel();
                target.OnDied?.Invoke(target);
            }
        }

        public void Wait()
        {
            acted = true;
        }

        void UpdateHpLabel()
        {
            if (_hpLabel != null)
                _hpLabel.text = $"HP {hp}/{maxHp}";
        }

        public void InitFromDefinition(BattleUnitDefinition def)
        {
            if (def == null) return;
            maxHp = def.maxHp;
            hp = def.maxHp;
            atk = def.atk;
            this.def = def.def;
            mov = def.mov;
            team = def.team;
        }

        /// <summary>
        /// Initialize this battle unit from hero runtime data + definition.
        /// Used by deployment system when spawning player heroes.
        /// </summary>
        public void InitFromHero(HeroRuntimeData runtime, HeroDefinition heroDef)
        {
            if (runtime == null || heroDef == null) return;

            maxHp = runtime.maxHp;
            hp = runtime.currentHp;
            atk = runtime.atk;
            this.def = runtime.def;
            mov = runtime.mov;
            team = UnitTeam.Player;

            // Store sprite to be applied in Start() when visual is created
            if (heroDef.unitSprite != null)
                _overrideSprite = heroDef.unitSprite;
        }
    }
}
