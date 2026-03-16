using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using CaoCao.Story;

namespace CaoCao.Battle.Events
{
    /// <summary>
    /// Parses .battle.dsl.txt files into BattleEventScript.
    /// Format follows the same tokenization rules as StoryDSLParser
    /// (space-separated, quoted strings, # // ; comments).
    /// </summary>
    public class BattleEventDSLParser
    {
        static readonly string[] CommentPrefixes = { "#", "//", ";" };

        BattleEventScript _script;
        BattleEventDef _currentEvent;
        List<BattleAction> _duelActions;
        bool _inDuel;

        public BattleEventScript Parse(string text)
        {
            _script = new BattleEventScript();
            _currentEvent = null;
            _duelActions = null;
            _inDuel = false;

            var lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string raw = lines[i].Trim();
                if (string.IsNullOrEmpty(raw) || IsComment(raw))
                    continue;

                var tokens = Tokenize(raw);
                if (tokens.Count == 0) continue;

                string cmd = tokens[0];

                // ── Top-level commands (outside event blocks) ──
                if (_currentEvent == null)
                {
                    switch (cmd)
                    {
                        case "actor":
                            ParseActor(tokens);
                            break;
                        case "event":
                            if (tokens.Count >= 2)
                            {
                                _currentEvent = new BattleEventDef { Id = tokens[1] };
                            }
                            break;
                        default:
                            Debug.LogWarning($"[BattleEventDSL] Unknown top-level command: {cmd} (line {i + 1})");
                            break;
                    }
                    continue;
                }

                // ── Inside event block ──
                if (cmd == "end_event")
                {
                    _script.Events.Add(_currentEvent);
                    _currentEvent = null;
                    continue;
                }

                if (cmd == "trigger")
                {
                    _currentEvent.Trigger = ParseTrigger(tokens);
                    continue;
                }

                if (cmd == "condition")
                {
                    var cond = ParseCondition(tokens);
                    if (cond != null)
                        _currentEvent.Conditions.Add(cond);
                    continue;
                }

                // ── Duel block ──
                if (cmd == "duel_begin")
                {
                    _inDuel = true;
                    _duelActions = new List<BattleAction>();
                    // Parse duel_begin <unitA> <unitB>
                    var duelAction = new BattleAction
                    {
                        Type = BattleActionType.DuelBegin,
                        UnitName = tokens.Count > 1 ? tokens[1] : "",
                        TargetUnit = tokens.Count > 2 ? tokens[2] : "",
                        DuelActions = _duelActions
                    };
                    _currentEvent.Actions.Add(duelAction);
                    continue;
                }

                if (cmd == "duel_end")
                {
                    _inDuel = false;
                    _duelActions = null;
                    continue;
                }

                // ── Action commands ──
                var action = ParseAction(cmd, tokens);
                if (action != null)
                {
                    if (_inDuel && _duelActions != null)
                        _duelActions.Add(action);
                    else
                        _currentEvent.Actions.Add(action);
                }
            }

            // Warn if event was not closed
            if (_currentEvent != null)
            {
                Debug.LogWarning($"[BattleEventDSL] Event '{_currentEvent.Id}' was not closed with end_event");
                _script.Events.Add(_currentEvent);
            }

            return _script;
        }

        // ──────────────────────────────────────────────
        //  Parsing helpers
        // ──────────────────────────────────────────────

        void ParseActor(List<string> tokens)
        {
            // actor <name> <left|right|center>
            if (tokens.Count < 3) return;
            var decl = new BattleActorDecl
            {
                Name = tokens[1],
                Side = ParseSide(tokens[2])
            };
            _script.Actors.Add(decl);
        }

        BattleEventTrigger ParseTrigger(List<string> tokens)
        {
            // trigger <type> [args...]
            if (tokens.Count < 2) return null;
            var trigger = new BattleEventTrigger();
            string type = tokens[1];

            switch (type)
            {
                case "battle_start":
                    trigger.Type = BattleTriggerType.BattleStart;
                    break;

                case "turn_start":
                    trigger.Type = BattleTriggerType.TurnStart;
                    trigger.TurnNumber = tokens.Count > 2 ? ParseInt(tokens[2]) : 1;
                    break;

                case "proximity":
                    trigger.Type = BattleTriggerType.Proximity;
                    trigger.UnitA = tokens.Count > 2 ? tokens[2] : "";
                    trigger.UnitB = tokens.Count > 3 ? tokens[3] : "";
                    break;

                case "unit_died":
                    trigger.Type = BattleTriggerType.UnitDied;
                    trigger.UnitName = tokens.Count > 2 ? tokens[2] : "";
                    break;

                case "hp_below":
                    trigger.Type = BattleTriggerType.HpBelow;
                    trigger.UnitName = tokens.Count > 2 ? tokens[2] : "";
                    trigger.HpPercent = tokens.Count > 3 ? ParseInt(tokens[3]) : 50;
                    break;

                case "area_enter":
                    trigger.Type = BattleTriggerType.AreaEnter;
                    trigger.TeamFilter = tokens.Count > 2 ? tokens[2] : "any";
                    if (tokens.Count >= 7)
                    {
                        trigger.AreaMin = new Vector2Int(ParseInt(tokens[3]), ParseInt(tokens[4]));
                        trigger.AreaMax = new Vector2Int(ParseInt(tokens[5]), ParseInt(tokens[6]));
                    }
                    break;

                default:
                    Debug.LogWarning($"[BattleEventDSL] Unknown trigger type: {type}");
                    break;
            }
            return trigger;
        }

        BattleEventCondition ParseCondition(List<string> tokens)
        {
            // condition <type> [args...]
            if (tokens.Count < 2) return null;
            var cond = new BattleEventCondition();
            string type = tokens[1];

            switch (type)
            {
                case "flag":
                    cond.Type = BattleConditionType.Flag;
                    cond.Param = tokens.Count > 2 ? tokens[2] : "";
                    break;
                case "not_flag":
                    cond.Type = BattleConditionType.NotFlag;
                    cond.Param = tokens.Count > 2 ? tokens[2] : "";
                    break;
                case "unit_alive":
                    cond.Type = BattleConditionType.UnitAlive;
                    cond.Param = tokens.Count > 2 ? tokens[2] : "";
                    break;
                case "unit_dead":
                    cond.Type = BattleConditionType.UnitDead;
                    cond.Param = tokens.Count > 2 ? tokens[2] : "";
                    break;
                case "turn_ge":
                    cond.Type = BattleConditionType.TurnGe;
                    cond.IntParam = tokens.Count > 2 ? ParseInt(tokens[2]) : 1;
                    break;
                default:
                    Debug.LogWarning($"[BattleEventDSL] Unknown condition: {type}");
                    return null;
            }
            return cond;
        }

        BattleAction ParseAction(string cmd, List<string> tokens)
        {
            var action = new BattleAction();

            switch (cmd)
            {
                case "talk":
                    // talk <unitName> "text"
                    action.Type = BattleActionType.Talk;
                    action.UnitName = tokens.Count > 1 ? tokens[1] : "narrator";
                    action.Text = tokens.Count > 2 ? Unescape(JoinText(tokens, 2)) : "";
                    action.TalkSide = _script.GetActorSide(action.UnitName);
                    break;

                case "hide_dialogue":
                    action.Type = BattleActionType.HideDialogue;
                    break;

                case "wait":
                    action.Type = BattleActionType.Wait;
                    action.Seconds = tokens.Count > 1 ? ParseFloat(tokens[1]) : 0.5f;
                    break;

                case "camera_focus":
                    action.Type = BattleActionType.CameraFocus;
                    if (tokens.Count == 2)
                    {
                        // camera_focus <unitName>
                        action.UnitName = tokens[1];
                    }
                    else if (tokens.Count >= 3)
                    {
                        // camera_focus <x> <y>
                        if (int.TryParse(tokens[1], out int cx))
                        {
                            action.X = cx;
                            action.Y = ParseInt(tokens[2]);
                        }
                        else
                        {
                            action.UnitName = tokens[1];
                        }
                    }
                    break;

                case "spawn":
                    // spawn <name> <player|enemy> <x> <y> [hp=N atk=N def=N mov=N class=X]
                    action.Type = BattleActionType.Spawn;
                    action.UnitName = tokens.Count > 1 ? tokens[1] : "敌兵";
                    action.Team = tokens.Count > 2 ? tokens[2] : "enemy";
                    action.X = tokens.Count > 3 ? ParseInt(tokens[3]) : 0;
                    action.Y = tokens.Count > 4 ? ParseInt(tokens[4]) : 0;
                    // Parse key=value pairs
                    for (int i = 5; i < tokens.Count; i++)
                        ParseKeyValue(tokens[i], action);
                    break;

                case "move":
                    // move <unitName> <x> <y>
                    action.Type = BattleActionType.Move;
                    action.UnitName = tokens.Count > 1 ? tokens[1] : "";
                    action.X = tokens.Count > 2 ? ParseInt(tokens[2]) : 0;
                    action.Y = tokens.Count > 3 ? ParseInt(tokens[3]) : 0;
                    break;

                case "face":
                    // face <unitName> <up|down|left|right>
                    action.Type = BattleActionType.Face;
                    action.UnitName = tokens.Count > 1 ? tokens[1] : "";
                    action.Direction = tokens.Count > 2 ? tokens[2] : "down";
                    break;

                case "setpos":
                    // setpos <unitName> <x> <y>
                    action.Type = BattleActionType.SetPos;
                    action.UnitName = tokens.Count > 1 ? tokens[1] : "";
                    action.X = tokens.Count > 2 ? ParseInt(tokens[2]) : 0;
                    action.Y = tokens.Count > 3 ? ParseInt(tokens[3]) : 0;
                    break;

                case "damage":
                    // damage <unitName> <amount>
                    action.Type = BattleActionType.Damage;
                    action.UnitName = tokens.Count > 1 ? tokens[1] : "";
                    action.Amount = tokens.Count > 2 ? ParseInt(tokens[2]) : 0;
                    break;

                case "heal":
                    // heal <unitName> <amount>
                    action.Type = BattleActionType.Heal;
                    action.UnitName = tokens.Count > 1 ? tokens[1] : "";
                    action.Amount = tokens.Count > 2 ? ParseInt(tokens[2]) : 0;
                    break;

                case "kill":
                    // kill <unitName>
                    action.Type = BattleActionType.Kill;
                    action.UnitName = tokens.Count > 1 ? tokens[1] : "";
                    break;

                case "buff":
                    // buff <unitName> <stat> <amount>
                    action.Type = BattleActionType.Buff;
                    action.UnitName = tokens.Count > 1 ? tokens[1] : "";
                    action.Stat = tokens.Count > 2 ? tokens[2] : "atk";
                    action.Amount = tokens.Count > 3 ? ParseInt(tokens[3]) : 0;
                    break;

                case "buff_team":
                    // buff_team <player|enemy> <stat> <amount>
                    action.Type = BattleActionType.BuffTeam;
                    action.Team = tokens.Count > 1 ? tokens[1] : "player";
                    action.Stat = tokens.Count > 2 ? tokens[2] : "atk";
                    action.Amount = tokens.Count > 3 ? ParseInt(tokens[3]) : 0;
                    break;

                case "change_team":
                    // change_team <unitName> <player|enemy>
                    action.Type = BattleActionType.ChangeTeam;
                    action.UnitName = tokens.Count > 1 ? tokens[1] : "";
                    action.Team = tokens.Count > 2 ? tokens[2] : "player";
                    break;

                case "set_flag":
                    action.Type = BattleActionType.SetFlag;
                    action.FlagName = tokens.Count > 1 ? tokens[1] : "";
                    break;

                case "clear_flag":
                    action.Type = BattleActionType.ClearFlag;
                    action.FlagName = tokens.Count > 1 ? tokens[1] : "";
                    break;

                case "attack":
                    // attack <attacker> <target>  (duel animation only)
                    action.Type = BattleActionType.Attack;
                    action.UnitName = tokens.Count > 1 ? tokens[1] : "";
                    action.TargetUnit = tokens.Count > 2 ? tokens[2] : "";
                    break;

                default:
                    Debug.LogWarning($"[BattleEventDSL] Unknown action: {cmd}");
                    return null;
            }

            return action;
        }

        // ── Key=Value parsing for spawn ──

        void ParseKeyValue(string token, BattleAction action)
        {
            int eq = token.IndexOf('=');
            if (eq < 0) return;
            string key = token.Substring(0, eq).ToLower();
            string val = token.Substring(eq + 1);

            switch (key)
            {
                case "hp": action.SpawnHp = ParseInt(val); break;
                case "atk": action.SpawnAtk = ParseInt(val); break;
                case "def": action.SpawnDef = ParseInt(val); break;
                case "mov": action.SpawnMov = ParseInt(val); break;
                case "class": action.SpawnUnitClass = val; break;
            }
        }

        // ── Utility ──

        static DialogueSide ParseSide(string s)
        {
            return s.ToLower() switch
            {
                "right" => DialogueSide.Right,
                "center" => DialogueSide.Center,
                _ => DialogueSide.Left,
            };
        }

        static int ParseInt(string s)
        {
            int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v);
            return v;
        }

        static float ParseFloat(string s)
        {
            float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float v);
            return v;
        }

        static bool IsComment(string line)
        {
            foreach (var p in CommentPrefixes)
                if (line.StartsWith(p))
                    return true;
            return false;
        }

        static List<string> Tokenize(string line)
        {
            var result = new List<string>();
            string buf = "";
            bool inQuote = false;

            for (int i = 0; i < line.Length; i++)
            {
                char ch = line[i];
                if (ch == '"')
                {
                    inQuote = !inQuote;
                }
                else if (ch == ' ' && !inQuote)
                {
                    if (buf.Length > 0)
                    {
                        result.Add(buf);
                        buf = "";
                    }
                }
                else
                {
                    buf += ch;
                }
            }
            if (buf.Length > 0)
                result.Add(buf);

            return result;
        }

        static string JoinText(List<string> tokens, int start)
        {
            var parts = new List<string>();
            for (int i = start; i < tokens.Count; i++)
                parts.Add(tokens[i]);
            string text = string.Join(" ", parts);
            if (text.StartsWith("\"") && text.EndsWith("\"") && text.Length >= 2)
                text = text.Substring(1, text.Length - 2);
            return text;
        }

        static string Unescape(string text)
        {
            return text.Replace("\\n", "\n").Replace("\\\"", "\"");
        }
    }
}
