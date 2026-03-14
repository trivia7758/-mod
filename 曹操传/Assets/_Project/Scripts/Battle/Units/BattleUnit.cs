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
        BattleUnitAnimator _animator;

        public BattleUnitAnimator Animator => _animator;

        void Awake()
        {
            // Delay visual creation — stats may not be set yet when AddComponent is called.
        }

        void Start()
        {
            EnsureVisual();
            UpdatePosition();
        }

        /// <summary>
        /// Re-create the visual elements after stats/team have been set externally.
        /// </summary>
        public void RefreshVisual()
        {
            if (_sprite != null)
            {
                Destroy(_sprite.gameObject);
                _sprite = null;
            }
            if (_hpLabel != null)
            {
                Destroy(_hpLabel.transform.parent.gameObject);
                _hpLabel = null;
            }
            _animator = null;
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
                _sprite.sortingOrder = 5;
            }

            // Use battle sprite sheet for all units
            BattleSpriteSheet.EnsureLoaded();
            var idleSprite = BattleSpriteSheet.IdleDown;
            if (idleSprite != null)
            {
                _sprite.sprite = idleSprite;
                _sprite.transform.localScale = Vector3.one; // sprites are 48x48, 1 pixel = 1 world unit
            }
            else
            {
                // Fallback: colored 1px square
                var tex = new Texture2D(1, 1);
                tex.SetPixel(0, 0, team == UnitTeam.Player ? new Color(0.2f, 0.6f, 1f) : new Color(0.9f, 0.25f, 0.2f));
                tex.Apply();
                _sprite.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
                _sprite.transform.localScale = new Vector3(tileSize.x * 0.8f, tileSize.y * 0.8f, 1);
            }

            // Set up animator
            EnsureAnimator();

            // Team tint: slightly tint enemy units red
            if (team == UnitTeam.Enemy)
                _sprite.color = new Color(1f, 0.75f, 0.75f, 1f);
            else
                _sprite.color = Color.white;

            EnsureHpLabel();
        }

        void EnsureAnimator()
        {
            if (_animator != null) return;
            _animator = GetComponent<BattleUnitAnimator>();
            if (_animator == null)
                _animator = gameObject.AddComponent<BattleUnitAnimator>();
            _animator.Init(_sprite);
        }

        void EnsureHpLabel()
        {
            _hpLabel = GetComponentInChildren<TMPro.TMP_Text>();
            if (_hpLabel == null)
            {
                var canvasGo = new GameObject("HPCanvas");
                canvasGo.transform.SetParent(transform);
                canvasGo.transform.localPosition = new Vector3(0, tileSize.y * 0.55f, 0);
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
                _hpLabel.color = team == UnitTeam.Player ? new Color(0.4f, 0.9f, 1f) : new Color(1f, 0.6f, 0.5f);
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

        /// <summary>
        /// Teleport to target (no animation).
        /// </summary>
        public void MoveTo(Vector2Int target, bool animate)
        {
            if (!animate)
            {
                cell = target;
                UpdatePosition();
                UpdateHpLabel();
                _animator?.StopWalking();
                OnMoved?.Invoke(this);
                return;
            }
            // Single-cell animated move (used by enemy AI)
            MoveAlongPath(new System.Collections.Generic.List<Vector2Int> { target });
        }

        /// <summary>
        /// Walk along a path cell by cell with proper directional animation.
        /// Path should be ordered: first step → last step (not including current cell).
        /// </summary>
        public void MoveAlongPath(System.Collections.Generic.List<Vector2Int> path)
        {
            if (path == null || path.Count == 0)
            {
                OnMoved?.Invoke(this);
                return;
            }
            StartCoroutine(AnimateAlongPath(path));
        }

        IEnumerator AnimateAlongPath(System.Collections.Generic.List<Vector2Int> path)
        {
            float stepDuration = 0.15f; // time per cell

            for (int i = 0; i < path.Count; i++)
            {
                var from = cell;
                var to = path[i];
                cell = to;

                // Update walk direction for this step
                _animator?.StartWalkingTowards(from, to);

                // Animate position from current to next cell
                Vector3 startPos = transform.position;
                Vector3 endPos = CellToWorld(to);
                float elapsed = 0f;

                while (elapsed < stepDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / stepDuration);
                    transform.position = Vector3.Lerp(startPos, endPos, t);
                    yield return null;
                }
                transform.position = endPos;
            }

            // Stop walk animation, return to idle facing last movement direction
            _animator?.StopWalking();
            UpdateHpLabel();
            OnMoved?.Invoke(this);
        }

        Vector3 CellToWorld(Vector2Int c)
        {
            return new Vector3(
                c.x * tileSize.x + tileSize.x * 0.5f,
                c.y * tileSize.y + tileSize.y * 0.5f,
                0
            );
        }

        public void Attack(BattleUnit target)
        {
            if (target == null) return;

            // Face towards target
            var dir = GetDirection(cell, target.cell);
            _animator?.PlayAttack(dir);

            int dmg = Mathf.Max(1, atk - target.def);
            target.hp -= dmg;
            target.UpdateHpLabel();

            // Target plays hit animation
            target._animator?.PlayHit();

            OnAttacked?.Invoke(this, target);
            if (target.hp <= 0)
            {
                target.hp = 0;
                target.UpdateHpLabel();

                // Play death animation then notify
                if (target._animator != null)
                {
                    target._animator.PlayDeath(() =>
                    {
                        // Hide HP label after death anim
                        var canvas = target.GetComponentInChildren<Canvas>();
                        if (canvas != null) canvas.gameObject.SetActive(false);
                    });
                }

                target.OnDied?.Invoke(target);
            }
        }

        public void Wait()
        {
            acted = true;
            // Dim sprite slightly to show unit has acted
            if (_sprite != null)
            {
                Color c = _sprite.color;
                c.a = 0.6f;
                _sprite.color = c;
            }
        }

        /// <summary>
        /// Reset acted state for new turn.
        /// </summary>
        public void ResetActed()
        {
            acted = false;
            if (_sprite != null)
            {
                Color c = _sprite.color;
                c.a = 1f;
                _sprite.color = c;
            }
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
        }

        static FacingDirection GetDirection(Vector2Int from, Vector2Int to)
        {
            var delta = to - from;
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
                return delta.x >= 0 ? FacingDirection.Right : FacingDirection.Left;
            return delta.y >= 0 ? FacingDirection.Up : FacingDirection.Down;
        }
    }
}
