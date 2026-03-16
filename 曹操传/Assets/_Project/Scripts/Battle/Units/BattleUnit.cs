using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CaoCao.Common;
using CaoCao.Data;

namespace CaoCao.Battle
{
    public class BattleUnit : MonoBehaviour
    {
        [Header("Identity")]
        public string displayName = "";
        public string unitTypeName = "";   // 兵种名 e.g. "轻步兵"
        public int level = 1;
        public int exp = 0;
        public Sprite portrait;            // 头像

        [Header("Stats")]
        public int maxHp = 20;
        public int hp = 20;
        public int maxMp = 0;
        public int mp = 0;
        public int atk = 6;
        public int def = 2;
        public int mov = 5;
        public int atkRange = 1;       // 攻击距离 (1=近战, 2+=远程弓兵/炮车)

        [Header("Movement")]
        public MovementType movementType = MovementType.Infantry;
        public UnitClass unitClass = UnitClass.Infantry;

        [Header("Skills & Items")]
        public List<SkillDefinition> skills = new();
        public List<ItemStack> items = new();   // battle-local item copies

        [Header("State")]
        public UnitTeam team = UnitTeam.Player;
        public Vector2Int cell;
        public Vector2Int tileSize = new(48, 48);
        public bool acted;

        public event Action<BattleUnit> OnMoved;
        public event Action<BattleUnit, BattleUnit> OnAttacked;
        public event Action<BattleUnit> OnDied;

        SpriteRenderer _sprite;
        SpriteRenderer _hpBarBg;
        SpriteRenderer _hpBarFill;
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
            if (_hpBarBg != null)
            {
                Destroy(_hpBarBg.gameObject);
                _hpBarBg = null;
                _hpBarFill = null;
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

            EnsureHpBar();
        }

        void EnsureAnimator()
        {
            if (_animator != null) return;
            _animator = GetComponent<BattleUnitAnimator>();
            if (_animator == null)
                _animator = gameObject.AddComponent<BattleUnitAnimator>();
            _animator.Init(_sprite);
        }

        public void EnsureHpBar()
        {
            if (_hpBarBg != null) return;

            // Create a 1x1 white pixel texture shared by bg and fill
            var whiteTex = new Texture2D(1, 1);
            whiteTex.SetPixel(0, 0, Color.white);
            whiteTex.Apply();
            var whiteSprite = Sprite.Create(whiteTex, new Rect(0, 0, 1, 1), new Vector2(0, 0.5f), 1);

            float barW = tileSize.x * 0.7f;  // ~34px wide
            float barH = 4f;                   // 4px tall
            float yOffset = tileSize.y * 0.55f;

            // Background (dark)
            var bgGo = new GameObject("HpBarBg");
            bgGo.transform.SetParent(transform);
            bgGo.transform.localPosition = new Vector3(-barW * 0.5f, yOffset, 0);
            _hpBarBg = bgGo.AddComponent<SpriteRenderer>();
            _hpBarBg.sprite = whiteSprite;
            _hpBarBg.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);
            _hpBarBg.sortingOrder = 10;
            bgGo.transform.localScale = new Vector3(barW, barH, 1);

            // Fill (green)
            var fillGo = new GameObject("HpBarFill");
            fillGo.transform.SetParent(transform);
            fillGo.transform.localPosition = new Vector3(-barW * 0.5f, yOffset, 0);
            _hpBarFill = fillGo.AddComponent<SpriteRenderer>();
            _hpBarFill.sprite = whiteSprite;
            _hpBarFill.color = new Color(0.1f, 0.85f, 0.2f);
            _hpBarFill.sortingOrder = 11;
            fillGo.transform.localScale = new Vector3(barW, barH, 1);

            UpdateHpBar();
        }

        public void UpdatePosition()
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
                UpdateHpBar();
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
            UpdateHpBar();
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
            target.UpdateHpBar();

            // Target plays hit animation
            target._animator?.PlayHit();

            OnAttacked?.Invoke(this, target);
            if (target.hp <= 0)
            {
                target.hp = 0;
                target.UpdateHpBar();

                // Play death animation then notify
                if (target._animator != null)
                {
                    target._animator.PlayDeath(() =>
                    {
                        // Hide HP bar after death anim
                        if (target._hpBarBg != null) target._hpBarBg.gameObject.SetActive(false);
                        if (target._hpBarFill != null) target._hpBarFill.gameObject.SetActive(false);
                    });
                }

                target.OnDied?.Invoke(target);
            }
        }

        /// <summary>Use a consumable item (HP/MP recovery).</summary>
        public bool UseItem(ItemDefinition itemDef)
        {
            if (itemDef == null) return false;

            // Find the item in inventory and consume 1
            var stack = items.Find(s => s.itemId == itemDef.id && s.count > 0);
            if (stack == null) return false;

            if (itemDef.healAmount > 0)
            {
                int oldHp = hp;
                hp = Mathf.Min(maxHp, hp + itemDef.healAmount);
                Debug.Log($"[BattleUnit] {displayName} 使用 {itemDef.displayName}: HP {oldHp} → {hp} (+{hp - oldHp})");
            }

            // MP recovery items (use mpBonus as MP heal for consumables)
            if (itemDef.mpBonus > 0)
            {
                int oldMp = mp;
                mp = Mathf.Min(maxMp, mp + itemDef.mpBonus);
                Debug.Log($"[BattleUnit] {displayName} 使用 {itemDef.displayName}: MP {oldMp} → {mp} (+{mp - oldMp})");
            }

            stack.count--;
            if (stack.count <= 0) items.Remove(stack);
            UpdateHpBar();
            return true;
        }

        /// <summary>Use a skill on a target unit.</summary>
        public bool UseSkill(SkillDefinition skillDef, BattleUnit target)
        {
            if (skillDef == null || mp < skillDef.mpCost) return false;
            mp -= skillDef.mpCost;

            switch (skillDef.effectType)
            {
                case SkillEffectType.Damage:
                    int dmg = Mathf.Max(1, skillDef.power + atk / 2 - target.def / 2);
                    target.hp -= dmg;
                    target.UpdateHpBar();
                    target._animator?.PlayHit();
                    Debug.Log($"[BattleUnit] {displayName} 施展 {skillDef.displayName} → {target.displayName}: -{dmg} HP");
                    if (target.hp <= 0)
                    {
                        target.hp = 0;
                        target.UpdateHpBar();
                        target._animator?.PlayDeath(() =>
                        {
                            if (target._hpBarBg != null) target._hpBarBg.gameObject.SetActive(false);
                            if (target._hpBarFill != null) target._hpBarFill.gameObject.SetActive(false);
                        });
                        target.OnDied?.Invoke(target);
                    }
                    break;

                case SkillEffectType.Heal:
                    int heal = skillDef.power;
                    int oldHp = target.hp;
                    target.hp = Mathf.Min(target.maxHp, target.hp + heal);
                    target.UpdateHpBar();
                    Debug.Log($"[BattleUnit] {displayName} 施展 {skillDef.displayName} → {target.displayName}: HP +{target.hp - oldHp}");
                    break;

                case SkillEffectType.Buff:
                    // TODO: implement buff system
                    Debug.Log($"[BattleUnit] {displayName} 施展 {skillDef.displayName} → {target.displayName} (Buff - 暂未实装)");
                    break;

                case SkillEffectType.Debuff:
                    // TODO: implement debuff system
                    Debug.Log($"[BattleUnit] {displayName} 施展 {skillDef.displayName} → {target.displayName} (Debuff - 暂未实装)");
                    break;
            }

            // Face towards target
            var dir = GetDirection(cell, target.cell);
            _animator?.PlayAttack(dir);

            OnAttacked?.Invoke(this, target);
            return true;
        }

        /// <summary>Force-kill this unit (for scripted events). Fires OnDied.</summary>
        public void ForceKill()
        {
            hp = 0;
            UpdateHpBar();

            // Hide HP bar
            if (_hpBarBg != null) _hpBarBg.gameObject.SetActive(false);
            if (_hpBarFill != null) _hpBarFill.gameObject.SetActive(false);

            // Play death animation (fade out sprite)
            if (_animator != null)
            {
                _animator.PlayDeath(() =>
                {
                    // HP bar already hidden above
                });
            }

            OnDied?.Invoke(this);
        }

        /// <summary>Apply scripted damage. Fires OnDied if lethal.</summary>
        public void TakeDamage(int amount)
        {
            hp = Mathf.Max(0, hp - amount);
            UpdateHpBar();
            _animator?.PlayHit();
            if (hp <= 0)
            {
                if (_hpBarBg != null) _hpBarBg.gameObject.SetActive(false);
                if (_hpBarFill != null) _hpBarFill.gameObject.SetActive(false);
                _animator?.PlayDeath();
                OnDied?.Invoke(this);
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

        public void UpdateHpBar()
        {
            if (_hpBarFill == null) return;
            float ratio = maxHp > 0 ? (float)hp / maxHp : 0;
            float barW = tileSize.x * 0.7f;
            var s = _hpBarFill.transform.localScale;
            s.x = barW * ratio;
            _hpBarFill.transform.localScale = s;

            // Color: green > yellow > red based on HP ratio
            _hpBarFill.color = ratio > 0.5f ? new Color(0.1f, 0.85f, 0.2f)
                             : ratio > 0.25f ? new Color(0.9f, 0.8f, 0.1f)
                             : new Color(0.9f, 0.15f, 0.1f);
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

            // Identity
            displayName = heroDef.displayName;
            portrait = heroDef.portrait;
            level = runtime.level;
            exp = runtime.exp;
            if (heroDef.defaultUnitType != null)
            {
                unitTypeName = heroDef.defaultUnitType.displayName;
                movementType = heroDef.defaultUnitType.movementType;
                unitClass = heroDef.defaultUnitType.unitClass;
            }

            // Try to load portrait from Resources if not assigned in definition
            if (portrait == null)
                portrait = LoadPortraitFromResources(heroDef.id);

            // Stats
            maxHp = runtime.maxHp;
            hp = runtime.currentHp;
            maxMp = runtime.maxMp;
            mp = runtime.currentMp;
            atk = runtime.atk;
            this.def = runtime.def;
            mov = runtime.mov;
            team = UnitTeam.Player;

            // Copy learnable skills (up to current level)
            skills.Clear();
            if (heroDef.learnableSkills != null)
            {
                foreach (var sk in heroDef.learnableSkills)
                {
                    if (sk != null && sk.learnLevel <= level)
                        skills.Add(sk);
                }
            }
        }

        /// <summary>
        /// Try to load a portrait sprite from Resources/Portraits/ folder.
        /// Tries heroId first, then known aliases.
        /// </summary>
        public static Sprite LoadPortraitFromResources(string heroId)
        {
            // Try direct hero ID match
            var sprite = Resources.Load<Sprite>($"Portraits/{heroId}");
            if (sprite != null) return sprite;

            // Known hero → portrait filename mapping
            string alias = heroId switch
            {
                "cao_cao" => "nanzhu",      // 男主 = 曹操
                _ => null
            };
            if (alias != null)
            {
                sprite = Resources.Load<Sprite>($"Portraits/{alias}");
                if (sprite != null) return sprite;
            }

            // Try loading as Texture2D and create sprite (for .jpg files)
            var tex = Resources.Load<Texture2D>($"Portraits/{heroId}");
            if (tex == null && alias != null)
                tex = Resources.Load<Texture2D>($"Portraits/{alias}");
            if (tex != null)
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));

            return null;
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
