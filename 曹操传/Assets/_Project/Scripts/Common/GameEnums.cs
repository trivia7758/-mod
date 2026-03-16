namespace CaoCao.Common
{
    /// <summary>
    /// Item category types matching the original game's warehouse tabs.
    /// </summary>
    public enum ItemType
    {
        Weapon,     // 武器
        Armor,      // 防具
        Auxiliary,  // 辅助
        Consumable  // 道具
    }

    /// <summary>
    /// Unit movement class, affects terrain traversal rules.
    /// (Legacy enum — use UnitClass for full terrain lookups)
    /// </summary>
    public enum MovementType
    {
        Infantry,   // 步兵
        Cavalry,    // 骑兵
        Archer,     // 弓兵
        Naval       // 水军
    }

    /// <summary>
    /// All unit classes in 曹操传. Order matches terrain_data.json arrays.
    /// Standard 13 classes + 9 special enemy classes = 22 total.
    /// </summary>
    public enum UnitClass
    {
        // ── Standard 13 ──
        Lord              = 0,   // 君主
        Infantry          = 1,   // 步兵
        Archer            = 2,   // 弓兵
        Cavalry           = 3,   // 骑兵
        MountedArcher     = 4,   // 弓骑
        Catapult          = 5,   // 炮车
        Martial           = 6,   // 武道
        Bandit            = 7,   // 贼兵
        Strategist        = 8,   // 策士
        Geomancer         = 9,   // 风水
        Taoist            = 10,  // 道士
        MountedStrategist = 11,  // 骑策
        Dancer            = 12,  // 舞娘

        // ── Special / Enemy-only 9 ──
        XiLiangCavalry    = 13,  // 西凉骑兵
        YellowTurban      = 14,  // 黄巾贼
        Pirate            = 15,  // 海盗
        Admiral            = 16,  // 都督
        Sorcerer          = 17,  // 咒术士
        BearTamer         = 18,  // 驯熊师
        TigerTamer        = 19,  // 驯虎师
        ClayGolem         = 20,  // 土偶
        WoodGolem         = 21   // 木偶
    }

    /// <summary>
    /// Skill/strategy effect categories.
    /// </summary>
    public enum SkillEffectType
    {
        Damage,     // 伤害
        Heal,       // 回复
        Buff,       // 增益
        Debuff,     // 减益
        Special     // 特殊
    }

    /// <summary>
    /// Equipment slot types (3 slots per hero).
    /// </summary>
    public enum EquipSlot
    {
        Weapon,     // 武器
        Armor,      // 防具
        Auxiliary   // 辅助
    }

    /// <summary>
    /// Growth grade for five-dimension attributes (五维成长档位).
    /// Determines per-level growth value and promotion thresholds.
    /// </summary>
    public enum GrowthGrade
    {
        C      = 0,  // 成长值 1
        B      = 1,  // 成长值 2
        A      = 2,  // 成长值 3
        S      = 3,  // 成长值 4
        SS     = 4,  // 成长值 5
        EX     = 5,  // 成长值 6
        EXPlus = 6   // 成长值 7 (EX+)
    }
}
