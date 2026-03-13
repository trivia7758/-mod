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
    /// </summary>
    public enum MovementType
    {
        Infantry,   // 步兵
        Cavalry,    // 骑兵
        Archer,     // 弓兵
        Naval       // 水军
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
}
