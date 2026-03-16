using System.Collections.Generic;
using UnityEngine;
using CaoCao.Story;

namespace CaoCao.Battle.Events
{
    // ── Trigger Types ──

    public enum BattleTriggerType
    {
        BattleStart,    // trigger battle_start
        TurnStart,      // trigger turn_start <N>
        Proximity,      // trigger proximity <unitA> <unitB>  (8-neighbor)
        UnitDied,       // trigger unit_died <unitName>
        HpBelow,        // trigger hp_below <unitName> <percent>
        AreaEnter,      // trigger area_enter <team> <x1> <y1> <x2> <y2>
    }

    public class BattleEventTrigger
    {
        public BattleTriggerType Type;
        public int TurnNumber;                      // TurnStart
        public string UnitA, UnitB;                 // Proximity
        public string UnitName;                     // UnitDied, HpBelow
        public int HpPercent;                       // HpBelow
        public string TeamFilter;                   // AreaEnter: "player"/"enemy"/"any"
        public Vector2Int AreaMin, AreaMax;          // AreaEnter rect
    }

    // ── Conditions ──

    public enum BattleConditionType
    {
        Flag,           // condition flag <name>
        NotFlag,        // condition not_flag <name>
        UnitAlive,      // condition unit_alive <name>
        UnitDead,       // condition unit_dead <name>
        TurnGe,         // condition turn_ge <N>
    }

    public class BattleEventCondition
    {
        public BattleConditionType Type;
        public string Param;    // flag name or unit name
        public int IntParam;    // for TurnGe
    }

    // ── Action Types ──

    public enum BattleActionType
    {
        Talk,
        HideDialogue,
        Wait,
        CameraFocus,
        Spawn,
        Move,
        Face,
        SetPos,
        Damage,
        Heal,
        Kill,
        Buff,
        BuffTeam,
        ChangeTeam,
        SetFlag,
        ClearFlag,
        Attack,         // duel-only: play attack anim (no real damage)
        DuelBegin,      // start duel block
        DuelEnd,        // end duel block (marker only)
    }

    public class BattleAction
    {
        public BattleActionType Type;
        public string UnitName;
        public string Text;                 // Talk text
        public float Seconds;               // Wait duration
        public int X, Y;                    // CameraFocus cell, Move/Spawn/SetPos position
        public string Team;                 // Spawn team, BuffTeam team, ChangeTeam target
        public int Amount;                  // Damage/Heal/Buff amount
        public string Stat;                 // Buff stat name (atk/def/mov)
        public string TargetUnit;           // Attack target (duel), second unit
        public string FlagName;             // SetFlag/ClearFlag
        public string Direction;            // Face direction (up/down/left/right)

        // Spawn stats (key=value)
        public int SpawnHp = 20, SpawnAtk = 6, SpawnDef = 2, SpawnMov = 4;
        public string SpawnUnitClass = "";  // optional unit class override

        // Nested actions for duel block
        public List<BattleAction> DuelActions;

        // Actor side for Talk (resolved at parse time from actor declarations)
        public DialogueSide TalkSide = DialogueSide.Left;
    }

    // ── Actor Declaration ──

    public class BattleActorDecl
    {
        public string Name;
        public DialogueSide Side;
    }

    // ── Complete Event ──

    public class BattleEventDef
    {
        public string Id;
        public BattleEventTrigger Trigger;
        public List<BattleEventCondition> Conditions = new();
        public List<BattleAction> Actions = new();
        public bool Fired;  // one-shot: once fired, never fires again
    }

    // ── Top-Level Parse Result ──

    public class BattleEventScript
    {
        public List<BattleActorDecl> Actors = new();
        public List<BattleEventDef> Events = new();

        /// <summary>Get the declared side for an actor name, defaulting to Left.</summary>
        public DialogueSide GetActorSide(string name)
        {
            foreach (var a in Actors)
                if (a.Name == name)
                    return a.Side;
            return DialogueSide.Left;
        }
    }
}
