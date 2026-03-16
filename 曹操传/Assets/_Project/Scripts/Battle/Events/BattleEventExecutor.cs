using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CaoCao.Story;

namespace CaoCao.Battle.Events
{
    /// <summary>
    /// Executes a sequence of BattleActions as coroutines.
    /// Handles dialogue, spawning, movement, duels, damage, buffs, etc.
    /// </summary>
    public class BattleEventExecutor
    {
        readonly BattleController _ctrl;
        readonly BattleEventContext _ctx;
        readonly BattleEventScript _script;
        DialogueBox _dialogueBox;

        // Talking icon
        Sprite _talkingIconSprite;
        GameObject _currentTalkingIcon;

        public BattleEventExecutor(BattleController ctrl, BattleEventContext ctx, BattleEventScript script)
        {
            _ctrl = ctrl;
            _ctx = ctx;
            _script = script;
        }

        /// <summary>Execute a list of actions sequentially.</summary>
        public IEnumerator RunActions(List<BattleAction> actions)
        {
            foreach (var action in actions)
            {
                yield return RunAction(action);
            }
        }

        IEnumerator RunAction(BattleAction action)
        {
            switch (action.Type)
            {
                case BattleActionType.Talk:
                    yield return DoTalk(action);
                    break;
                case BattleActionType.HideDialogue:
                    HideDialogue();
                    break;
                case BattleActionType.Wait:
                    yield return new WaitForSeconds(action.Seconds);
                    break;
                case BattleActionType.CameraFocus:
                    yield return DoCameraFocus(action);
                    break;
                case BattleActionType.Spawn:
                    yield return DoSpawn(action);
                    break;
                case BattleActionType.Move:
                    yield return DoMove(action);
                    break;
                case BattleActionType.Face:
                    DoFace(action);
                    break;
                case BattleActionType.SetPos:
                    DoSetPos(action);
                    break;
                case BattleActionType.Damage:
                    DoDamage(action);
                    break;
                case BattleActionType.Heal:
                    DoHeal(action);
                    break;
                case BattleActionType.Kill:
                    yield return DoKill(action);
                    break;
                case BattleActionType.Buff:
                    DoBuff(action);
                    break;
                case BattleActionType.BuffTeam:
                    DoBuffTeam(action);
                    break;
                case BattleActionType.ChangeTeam:
                    DoChangeTeam(action);
                    break;
                case BattleActionType.SetFlag:
                    _ctx.SetFlag(action.FlagName);
                    break;
                case BattleActionType.ClearFlag:
                    _ctx.ClearFlag(action.FlagName);
                    break;
                case BattleActionType.Attack:
                    yield return DoDuelAttack(action);
                    break;
                case BattleActionType.DuelBegin:
                    yield return DoDuel(action);
                    break;
            }
        }

        // ────────────────────────────────────────
        //  Talk / Dialogue
        // ────────────────────────────────────────

        IEnumerator DoTalk(BattleAction action)
        {
            EnsureDialogueBox();
            if (_dialogueBox == null) yield break;

            string speaker = action.UnitName;
            Sprite portrait = null;
            DialogueSide side = action.TalkSide;
            BattleUnit speakerUnit = null;

            // Try to get portrait from unit
            if (speaker != "narrator")
            {
                speakerUnit = _ctx.FindUnit(speaker);
                if (speakerUnit != null)
                    portrait = speakerUnit.portrait;

                // Try loading from Resources if unit has no portrait
                if (portrait == null)
                    portrait = BattleUnit.LoadPortraitFromResources(speaker);
            }
            else
            {
                side = DialogueSide.Center;
            }

            // Show talking icon above speaker's head
            ShowTalkingIcon(speakerUnit);

            yield return _dialogueBox.ShowLine(action.Text, speaker, portrait, side);

            // Hide talking icon after this line
            HideTalkingIcon();
        }

        void HideDialogue()
        {
            HideTalkingIcon();
            if (_dialogueBox != null)
                _dialogueBox.gameObject.SetActive(false);
        }

        // ────────────────────────────────────────
        //  Talking Icon (显示在说话武将头上)
        // ────────────────────────────────────────

        void ShowTalkingIcon(BattleUnit unit)
        {
            HideTalkingIcon(); // remove previous icon

            if (unit == null || unit.hp <= 0) return;

            // Lazy load sprite
            if (_talkingIconSprite == null)
            {
                _talkingIconSprite = Resources.Load<Sprite>("Sprites/Battle/talking");
                if (_talkingIconSprite == null)
                {
                    // Try as Texture2D
                    var tex = Resources.Load<Texture2D>("Sprites/Battle/talking");
                    if (tex != null)
                        _talkingIconSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 1);
                }
            }
            if (_talkingIconSprite == null) return;

            _currentTalkingIcon = new GameObject("TalkingIcon");
            _currentTalkingIcon.transform.SetParent(unit.transform);
            // Position above the unit sprite (above HP bar)
            float yOffset = unit.tileSize.y * 0.75f;
            _currentTalkingIcon.transform.localPosition = new Vector3(0, yOffset, 0);

            var sr = _currentTalkingIcon.AddComponent<SpriteRenderer>();
            sr.sprite = _talkingIconSprite;
            sr.sortingOrder = 15;

            // Scale icon to reasonable size (24x24 pixels)
            float targetSize = 24f;
            float spriteW = _talkingIconSprite.bounds.size.x;
            if (spriteW > 0)
            {
                float scale = targetSize / spriteW;
                _currentTalkingIcon.transform.localScale = new Vector3(scale, scale, 1);
            }
        }

        void HideTalkingIcon()
        {
            if (_currentTalkingIcon != null)
            {
                Object.Destroy(_currentTalkingIcon);
                _currentTalkingIcon = null;
            }
        }

        void EnsureDialogueBox()
        {
            if (_dialogueBox != null) return;

            // Try to find existing DialogueBox in scene
            _dialogueBox = Object.FindAnyObjectByType<DialogueBox>();
            if (_dialogueBox != null) return;

            // Create one — same structure as story mode
            var canvasGo = new GameObject("BattleDialogueCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1280, 720);
            canvasGo.AddComponent<GraphicRaycaster>();

            // Dialogue panel at bottom
            var panelGo = new GameObject("DialogueBox");
            panelGo.transform.SetParent(canvasGo.transform, false);
            var panelRt = panelGo.AddComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0, 0);
            panelRt.anchorMax = new Vector2(1, 0);
            panelRt.offsetMin = new Vector2(20, 10);
            panelRt.offsetMax = new Vector2(-20, 160);

            // Background
            var bg = panelGo.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.85f);

            // CanvasGroup for fade
            panelGo.AddComponent<CanvasGroup>();

            // Name bar
            var nameBarGo = new GameObject("NameBar");
            nameBarGo.transform.SetParent(panelGo.transform, false);
            var nameBarRt = nameBarGo.AddComponent<RectTransform>();
            nameBarRt.anchorMin = new Vector2(0, 1);
            nameBarRt.anchorMax = new Vector2(0, 1);
            nameBarRt.anchoredPosition = new Vector2(80, 20);
            nameBarRt.sizeDelta = new Vector2(140, 32);
            var nameBarBg = nameBarGo.AddComponent<Image>();
            nameBarBg.color = new Color(0.6f, 0.4f, 0.1f, 0.9f);

            var nameTextGo = new GameObject("NameText");
            nameTextGo.transform.SetParent(nameBarGo.transform, false);
            var nameText = nameTextGo.AddComponent<TextMeshProUGUI>();
            nameText.fontSize = 18;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.color = Color.white;
            var nameTextRt = nameText.GetComponent<RectTransform>();
            nameTextRt.anchorMin = Vector2.zero;
            nameTextRt.anchorMax = Vector2.one;
            nameTextRt.offsetMin = Vector2.zero;
            nameTextRt.offsetMax = Vector2.zero;

            // Main text
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(panelGo.transform, false);
            var textTmp = textGo.AddComponent<TextMeshProUGUI>();
            textTmp.fontSize = 22;
            textTmp.color = Color.white;
            textTmp.alignment = TextAlignmentOptions.TopLeft;
            var textRt = textTmp.GetComponent<RectTransform>();
            textRt.anchorMin = new Vector2(0, 0);
            textRt.anchorMax = new Vector2(1, 1);
            textRt.offsetMin = new Vector2(20, 10);
            textRt.offsetMax = new Vector2(-20, -10);

            // Portrait left
            var pLeftGo = new GameObject("PortraitLeft");
            pLeftGo.transform.SetParent(panelGo.transform, false);
            var pLeftRt = pLeftGo.AddComponent<RectTransform>();
            pLeftRt.anchorMin = new Vector2(0, 0);
            pLeftRt.anchorMax = new Vector2(0, 1);
            pLeftRt.anchoredPosition = new Vector2(-60, 0);
            pLeftRt.sizeDelta = new Vector2(120, 0);
            var pLeftImgGo = new GameObject("PortraitLeftImage");
            pLeftImgGo.transform.SetParent(pLeftGo.transform, false);
            var pLeftImg = pLeftImgGo.AddComponent<Image>();
            pLeftImg.preserveAspect = true;
            var pLeftImgRt = pLeftImg.GetComponent<RectTransform>();
            pLeftImgRt.anchorMin = Vector2.zero;
            pLeftImgRt.anchorMax = Vector2.one;
            pLeftImgRt.offsetMin = Vector2.zero;
            pLeftImgRt.offsetMax = Vector2.zero;
            pLeftGo.SetActive(false);

            // Portrait right
            var pRightGo = new GameObject("PortraitRight");
            pRightGo.transform.SetParent(panelGo.transform, false);
            var pRightRt = pRightGo.AddComponent<RectTransform>();
            pRightRt.anchorMin = new Vector2(1, 0);
            pRightRt.anchorMax = new Vector2(1, 1);
            pRightRt.anchoredPosition = new Vector2(60, 0);
            pRightRt.sizeDelta = new Vector2(120, 0);
            var pRightImgGo = new GameObject("PortraitRightImage");
            pRightImgGo.transform.SetParent(pRightGo.transform, false);
            var pRightImg = pRightImgGo.AddComponent<Image>();
            pRightImg.preserveAspect = true;
            var pRightImgRt = pRightImg.GetComponent<RectTransform>();
            pRightImgRt.anchorMin = Vector2.zero;
            pRightImgRt.anchorMax = Vector2.one;
            pRightImgRt.offsetMin = Vector2.zero;
            pRightImgRt.offsetMax = Vector2.zero;
            pRightGo.SetActive(false);

            // Add DialogueBox component and wire references via serialized fields
            _dialogueBox = panelGo.AddComponent<DialogueBox>();
            // Use reflection to set serialized fields (they're private)
            SetPrivateField(_dialogueBox, "textLabel", textTmp);
            SetPrivateField(_dialogueBox, "nameLabel", nameText);
            SetPrivateField(_dialogueBox, "nameBar", nameBarGo);
            SetPrivateField(_dialogueBox, "portraitLeftFrame", pLeftGo);
            SetPrivateField(_dialogueBox, "portraitLeftImage", pLeftImg);
            SetPrivateField(_dialogueBox, "portraitRightFrame", pRightGo);
            SetPrivateField(_dialogueBox, "portraitRightImage", pRightImg);
            SetPrivateField(_dialogueBox, "dialogueRect", panelRt);

            panelGo.SetActive(false);

            Debug.Log("[BattleEventExecutor] Created DialogueBox for battle events");
        }

        static void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
            field?.SetValue(obj, value);
        }

        // ────────────────────────────────────────
        //  Camera
        // ────────────────────────────────────────

        IEnumerator DoCameraFocus(BattleAction action)
        {
            if (!string.IsNullOrEmpty(action.UnitName))
            {
                var unit = _ctx.FindUnit(action.UnitName);
                if (unit != null)
                {
                    _ctrl.FocusCameraOn(_ctrl.GridMap.CellToWorld(unit.cell));
                    yield return new WaitForSeconds(0.3f);
                }
            }
            else
            {
                var cell = new Vector2Int(action.X, action.Y);
                _ctrl.FocusCameraOn(_ctrl.GridMap.CellToWorld(cell));
                yield return new WaitForSeconds(0.3f);
            }
        }

        // ────────────────────────────────────────
        //  Spawn
        // ────────────────────────────────────────

        IEnumerator DoSpawn(BattleAction action)
        {
            var cell = new Vector2Int(action.X, action.Y);
            var team = action.Team == "player" ? UnitTeam.Player : UnitTeam.Enemy;

            // Focus camera on spawn point
            _ctrl.FocusCameraOn(_ctrl.GridMap.CellToWorld(cell));
            yield return new WaitForSeconds(0.2f);

            _ctrl.SpawnEventUnit(action.UnitName, team, cell,
                action.SpawnHp, action.SpawnAtk, action.SpawnDef, action.SpawnMov);

            // Brief flash/pause to show the spawn
            yield return new WaitForSeconds(0.3f);
        }

        // ────────────────────────────────────────
        //  Movement (scripted, uses pathfinding)
        // ────────────────────────────────────────

        IEnumerator DoMove(BattleAction action)
        {
            var unit = _ctx.FindUnit(action.UnitName);
            if (unit == null) yield break;

            var target = new Vector2Int(action.X, action.Y);
            if (unit.cell == target) yield break;

            // Simple direct path (cell-by-cell towards target)
            var path = BuildSimplePath(unit.cell, target);
            if (path.Count == 0) yield break;

            // Camera follows during scripted movement
            _ctrl.StartCameraFollow(unit.transform);

            bool moved = false;
            unit.OnMoved += _ => moved = true;
            unit.MoveAlongPath(path);
            while (!moved) yield return null;

            _ctrl.StopCameraFollow();
        }

        /// <summary>Build a simple path from start to end (no pathfinding, just step-by-step).</summary>
        List<Vector2Int> BuildSimplePath(Vector2Int from, Vector2Int to)
        {
            var path = new List<Vector2Int>();
            var current = from;
            int maxSteps = 50; // safety limit

            while (current != to && maxSteps-- > 0)
            {
                int dx = to.x - current.x;
                int dy = to.y - current.y;

                // Move in the larger delta direction first
                if (Mathf.Abs(dx) >= Mathf.Abs(dy))
                    current += new Vector2Int(dx > 0 ? 1 : -1, 0);
                else
                    current += new Vector2Int(0, dy > 0 ? 1 : -1);

                path.Add(current);
            }
            return path;
        }

        // ────────────────────────────────────────
        //  Face / SetPos
        // ────────────────────────────────────────

        void DoFace(BattleAction action)
        {
            var unit = _ctx.FindUnit(action.UnitName);
            if (unit == null) return;

            var dir = action.Direction?.ToLower() switch
            {
                "up" => FacingDirection.Up,
                "down" => FacingDirection.Down,
                "left" => FacingDirection.Left,
                "right" => FacingDirection.Right,
                _ => FacingDirection.Down
            };

            unit.GetComponentInChildren<BattleUnitAnimator>()?.SetIdle(dir);
        }

        void DoSetPos(BattleAction action)
        {
            var unit = _ctx.FindUnit(action.UnitName);
            if (unit == null) return;

            var cell = new Vector2Int(action.X, action.Y);
            unit.MoveTo(cell, false); // instant teleport
        }

        // ────────────────────────────────────────
        //  Damage / Heal / Kill
        // ────────────────────────────────────────

        void DoDamage(BattleAction action)
        {
            var unit = _ctx.FindUnit(action.UnitName);
            if (unit == null || unit.hp <= 0) return;

            unit.TakeDamage(action.Amount);
            Debug.Log($"[BattleEvent] {action.UnitName} 受到 {action.Amount} 伤害, HP={unit.hp}");
        }

        void DoHeal(BattleAction action)
        {
            var unit = _ctx.FindUnit(action.UnitName);
            if (unit == null || unit.hp <= 0) return;

            unit.hp = Mathf.Min(unit.maxHp, unit.hp + action.Amount);
            unit.UpdateHpBar();

            Debug.Log($"[BattleEvent] {action.UnitName} 回复 {action.Amount} HP, HP={unit.hp}");
        }

        IEnumerator DoKill(BattleAction action)
        {
            var unit = _ctx.FindUnit(action.UnitName);
            if (unit == null || unit.hp <= 0) yield break;

            // ForceKill handles: set hp=0, hide HP bar, play death anim, fire OnDied
            unit.ForceKill();
            Debug.Log($"[BattleEvent] {action.UnitName} 被击杀");

            // Wait for death animation to finish
            yield return new WaitForSeconds(1.5f);
        }

        // ────────────────────────────────────────
        //  Buff / ChangeTeam
        // ────────────────────────────────────────

        void DoBuff(BattleAction action)
        {
            var unit = _ctx.FindUnit(action.UnitName);
            if (unit == null) return;
            ApplyBuff(unit, action.Stat, action.Amount);
        }

        void DoBuffTeam(BattleAction action)
        {
            var team = action.Team == "player" ? UnitTeam.Player : UnitTeam.Enemy;
            var units = _ctrl.GetUnitsByTeam(team);
            foreach (var u in units)
                ApplyBuff(u, action.Stat, action.Amount);

            Debug.Log($"[BattleEvent] {action.Team} 全体 {action.Stat}+{action.Amount}");
        }

        void ApplyBuff(BattleUnit unit, string stat, int amount)
        {
            switch (stat?.ToLower())
            {
                case "atk": unit.atk += amount; break;
                case "def": unit.def += amount; break;
                case "mov": unit.mov += amount; break;
                case "hp": unit.maxHp += amount; unit.hp = Mathf.Min(unit.hp, unit.maxHp); break;
                case "mp": unit.maxMp += amount; unit.mp = Mathf.Min(unit.mp, unit.maxMp); break;
            }
            Debug.Log($"[BattleEvent] {unit.displayName} {stat}+{amount}");
        }

        void DoChangeTeam(BattleAction action)
        {
            var unit = _ctx.FindUnit(action.UnitName);
            if (unit == null) return;

            var newTeam = action.Team == "player" ? UnitTeam.Player : UnitTeam.Enemy;
            unit.team = newTeam;

            // Update sprite color to reflect new team
            var sr = unit.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = Color.white; // reset any tint
            }

            Debug.Log($"[BattleEvent] {action.UnitName} 转变阵营 → {newTeam}");
        }

        // ────────────────────────────────────────
        //  Duel (scripted animation sequence)
        // ────────────────────────────────────────

        IEnumerator DoDuel(BattleAction action)
        {
            if (action.DuelActions == null || action.DuelActions.Count == 0)
                yield break;

            var unitA = _ctx.FindUnit(action.UnitName);
            var unitB = _ctx.FindUnit(action.TargetUnit);

            if (unitA != null && unitB != null)
            {
                // Focus camera on midpoint of the two duelists
                Vector2 midpoint = (_ctrl.GridMap.CellToWorld(unitA.cell) +
                                    _ctrl.GridMap.CellToWorld(unitB.cell)) * 0.5f;
                _ctrl.FocusCameraOn(midpoint);
                yield return new WaitForSeconds(0.3f);
            }

            // Execute duel actions
            yield return RunActions(action.DuelActions);
        }

        IEnumerator DoDuelAttack(BattleAction action)
        {
            var attacker = _ctx.FindUnit(action.UnitName);
            var target = _ctx.FindUnit(action.TargetUnit);
            if (attacker == null || target == null) yield break;

            // Face each other
            var dir = GetDirection(attacker.cell, target.cell);
            var attackerAnim = attacker.GetComponentInChildren<BattleUnitAnimator>();
            var targetAnim = target.GetComponentInChildren<BattleUnitAnimator>();

            attackerAnim?.PlayAttack(dir);
            targetAnim?.PlayHit();

            yield return new WaitForSeconds(0.4f);
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
