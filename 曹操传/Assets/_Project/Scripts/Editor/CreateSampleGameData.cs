#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using CaoCao.Common;
using CaoCao.Data;

namespace CaoCao.Editor
{
    /// <summary>
    /// Creates sample ScriptableObject assets for testing.
    /// Menu: CaoCao/Create Sample Game Data
    /// </summary>
    public static class CreateSampleGameData
    {
        const string BasePath = "Assets/_Project/ScriptableObjects";

        [MenuItem("CaoCao/Create Sample Game Data")]
        public static void Execute()
        {
            EnsureFolders();

            // Unit Types
            var student = CreateUnitType("student", "大学生",
                MovementType.Infantry, 0, 0, 0, 0);
            var lightInfantry = CreateUnitType("light_infantry", "轻步兵",
                MovementType.Infantry, 0, 0, 0, 0);
            var heavyCavalry = CreateUnitType("heavy_cavalry", "重骑兵",
                MovementType.Cavalry, 3, 2, 1, -1);

            // Set upgrade paths
            lightInfantry.upgradeTo = heavyCavalry;
            lightInfantry.upgradeLevel = 10;
            EditorUtility.SetDirty(lightInfantry);

            // Skills
            var fireAttack = CreateSkill("fire_attack", "火攻",
                "对敌方造成智力伤害", 8, 2, 15, SkillEffectType.Damage, 5);
            var heal = CreateSkill("heal", "治疗",
                "恢复友方HP", 6, 1, 20, SkillEffectType.Heal, 3);
            var encourage = CreateSkill("encourage", "激励",
                "提升友方攻击力", 5, 1, 0, SkillEffectType.Buff, 8);

            // Unit type ID arrays for restrictions
            string[] infantryOnly = { "student", "light_infantry" };
            string[] cavalryOnly = { "heavy_cavalry" };
            string[] allCombat = { "student", "light_infantry", "heavy_cavalry" };

            // ── Weapons (武器) ──

            // Protagonist starting weapon (no restriction)
            var normalBook = CreateItem("normal_book", "普通书本", "一本普通的大学教材",
                ItemType.Weapon, atkBonus: 2, buyPrice: 0, sellPrice: 10);
            var crushedSoulSword = CreateItem("crushed_soul_sword", "碎魂剑", "蕴含神秘力量的古剑",
                ItemType.Weapon, atkBonus: 8, buyPrice: 0, sellPrice: 100,
                heroIds: new[] { "zhang_qiang" });

            // Infantry weapons
            var ironSword = CreateItem("iron_sword", "铁剑", "步兵制式铁剑",
                ItemType.Weapon, atkBonus: 3, buyPrice: 80, sellPrice: 40,
                unitTypeIds: infantryOnly);
            var steelSword = CreateItem("steel_sword", "钢剑", "精钢锻造的利剑",
                ItemType.Weapon, atkBonus: 5, buyPrice: 200, sellPrice: 100,
                unitTypeIds: infantryOnly);
            var blueSteel = CreateItem("blue_steel_blade", "青钢剑", "名匠打造的青钢宝剑",
                ItemType.Weapon, atkBonus: 8, buyPrice: 500, sellPrice: 250,
                unitTypeIds: infantryOnly);

            // Cavalry weapons
            var ironSpear = CreateItem("iron_spear", "铁枪", "骑兵用铁制长枪",
                ItemType.Weapon, atkBonus: 4, buyPrice: 120, sellPrice: 60,
                unitTypeIds: cavalryOnly);
            var steelSpear = CreateItem("steel_spear", "钢枪", "精钢锻造的重枪",
                ItemType.Weapon, atkBonus: 7, buyPrice: 350, sellPrice: 175,
                unitTypeIds: cavalryOnly);
            var dragonSpear = CreateItem("dragon_spear", "龙胆枪", "传说中的名枪",
                ItemType.Weapon, atkBonus: 10, buyPrice: 800, sellPrice: 400,
                unitTypeIds: cavalryOnly);

            // ── Armor (防具) ──

            var cottonClothes = CreateItem("cotton_clothes", "棉布衣", "普通的棉布衣服",
                ItemType.Armor, defBonus: 2, buyPrice: 0, sellPrice: 10);

            // Infantry armor
            var ironArmor = CreateItem("iron_armor", "铁甲", "步兵制式铁甲",
                ItemType.Armor, defBonus: 3, buyPrice: 80, sellPrice: 40,
                unitTypeIds: infantryOnly);
            var steelArmor = CreateItem("steel_armor", "钢甲", "精钢打造的重甲",
                ItemType.Armor, defBonus: 5, hpBonus: 10, buyPrice: 250, sellPrice: 125,
                unitTypeIds: infantryOnly);
            var darkIronArmor = CreateItem("dark_iron_armor", "玄铁甲", "极为坚固的玄铁重甲",
                ItemType.Armor, defBonus: 8, hpBonus: 20, buyPrice: 600, sellPrice: 300,
                unitTypeIds: infantryOnly);

            // Cavalry armor
            var leatherArmor = CreateItem("leather_armor", "皮甲", "轻便的皮革铠甲",
                ItemType.Armor, defBonus: 2, speedBonus: 1, buyPrice: 100, sellPrice: 50,
                unitTypeIds: cavalryOnly);
            var chainMail = CreateItem("chain_mail", "锁子甲", "链环编织的锁甲",
                ItemType.Armor, defBonus: 4, buyPrice: 300, sellPrice: 150,
                unitTypeIds: cavalryOnly);
            var goldenArmor = CreateItem("golden_armor", "金缕甲", "镶金的华丽战甲",
                ItemType.Armor, defBonus: 7, hpBonus: 15, buyPrice: 700, sellPrice: 350,
                unitTypeIds: cavalryOnly);

            // ── Auxiliary (辅助) ── no unit type restriction

            var speedBoots = CreateItem("speed_boots", "疾风靴", "提升移动速度",
                ItemType.Auxiliary, speedBonus: 2, buyPrice: 150, sellPrice: 75);
            var lifeRing = CreateItem("life_ring", "生命之戒", "蕴含生命力的戒指",
                ItemType.Auxiliary, hpBonus: 30, buyPrice: 200, sellPrice: 100);
            var manaAmulet = CreateItem("mana_amulet", "法力护符", "增加策略力的护符",
                ItemType.Auxiliary, mpBonus: 20, buyPrice: 180, sellPrice: 90);
            var warDrum = CreateItem("war_drum", "战鼓", "鼓舞士气的战鼓",
                ItemType.Auxiliary, atkBonus: 2, defBonus: 2, buyPrice: 300, sellPrice: 150);
            var jadependant = CreateItem("jade_pendant", "玉佩", "温润的玉石吊坠",
                ItemType.Auxiliary, hpBonus: 15, mpBonus: 10, buyPrice: 250, sellPrice: 125);

            // ── Consumable (道具) ──

            var hpPotion = CreateItem("hp_potion", "回复药", "恢复50点HP",
                ItemType.Consumable, healAmount: 50, buyPrice: 60, sellPrice: 30);
            var hpPotionL = CreateItem("hp_potion_large", "大回复药", "恢复120点HP",
                ItemType.Consumable, healAmount: 120, buyPrice: 150, sellPrice: 75);
            var mpPotion = CreateItem("mp_potion", "策略恢复药", "恢复30点MP",
                ItemType.Consumable, mpBonus: 30, buyPrice: 80, sellPrice: 40);
            var atkDrug = CreateItem("atk_drug", "力量丹", "永久提升攻击力1点",
                ItemType.Consumable, atkBonus: 1, buyPrice: 500, sellPrice: 250);
            var defDrug = CreateItem("def_drug", "防御丹", "永久提升防御力1点",
                ItemType.Consumable, defBonus: 1, buyPrice: 500, sellPrice: 250);

            // Heroes — Protagonist
            var zhangQiang = CreateHero("zhang_qiang", "张强",
                force: 50, intelligence: 75, command: 40, agility: 60, luck: 70, breakthrough: 30,
                maxHp: 100, maxMp: 40, atk: 10, def: 8, mov: 5, speed: 6,
                unitType: student, isRequired: true, recruitChapter: 0,
                skills: new SkillDefinition[0]);

            // Heroes — Three Kingdoms
            var caoCao = CreateHero("cao_cao", "曹操",
                force: 85, intelligence: 90, command: 95, agility: 70, luck: 80, breakthrough: 75,
                maxHp: 120, maxMp: 60, atk: 15, def: 12, mov: 5, speed: 7,
                unitType: lightInfantry, isRequired: true, recruitChapter: 0,
                skills: new[] { fireAttack, encourage });

            var xiahouDun = CreateHero("xiahou_dun", "夏侯惇",
                force: 92, intelligence: 50, command: 78, agility: 75, luck: 60, breakthrough: 88,
                maxHp: 150, maxMp: 30, atk: 18, def: 14, mov: 5, speed: 6,
                unitType: lightInfantry, isRequired: false, recruitChapter: 0,
                skills: new[] { encourage });

            var guoJia = CreateHero("guo_jia", "郭嘉",
                force: 40, intelligence: 97, command: 82, agility: 65, luck: 85, breakthrough: 30,
                maxHp: 80, maxMp: 80, atk: 8, def: 6, mov: 4, speed: 8,
                unitType: lightInfantry, isRequired: false, recruitChapter: 0,
                skills: new[] { fireAttack, heal });

            // Battle Definition
            var battle1 = CreateBattleDefinition("Battle_01", "黄巾之乱",
                maxDeploy: 4, requiredHero: "zhang_qiang");

            // Game Data Registry
            var registry = ScriptableObject.CreateInstance<GameDataRegistry>();
            registry.allHeroes = new[] { zhangQiang, caoCao, xiahouDun, guoJia };
            registry.allItems = new[]
            {
                normalBook, crushedSoulSword,
                ironSword, steelSword, blueSteel,
                ironSpear, steelSpear, dragonSpear,
                cottonClothes, ironArmor, steelArmor, darkIronArmor,
                leatherArmor, chainMail, goldenArmor,
                speedBoots, lifeRing, manaAmulet, warDrum, jadependant,
                hpPotion, hpPotionL, mpPotion, atkDrug, defDrug
            };
            registry.allSkills = new[] { fireAttack, heal, encourage };
            registry.allUnitTypes = new[] { student, lightInfantry, heavyCavalry };
            registry.allBattles = new[] { battle1 };

            string registryPath = "Assets/_Project/Resources/Data/GameDataRegistry.asset";
            EnsureFolder("Assets/_Project/Resources/Data");

            // Delete old registry if it exists
            AssetDatabase.DeleteAsset(registryPath);
            AssetDatabase.CreateAsset(registry, registryPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[CreateSampleGameData] Sample data created successfully!");
            EditorUtility.DisplayDialog("Sample Game Data",
                "Created:\n" +
                "- 3 Unit Types (大学生, 轻步兵, 重骑兵)\n" +
                "- 3 Skills (火攻, 治疗, 激励)\n" +
                "- 25 Items (武器8, 防具7, 辅助5, 道具5)\n" +
                "- 4 Heroes (张强, 曹操, 夏侯惇, 郭嘉)\n" +
                "- 1 Battle (黄巾之乱)\n" +
                "- GameDataRegistry at Resources/Data/",
                "OK");
        }

        static void EnsureFolders()
        {
            EnsureFolder($"{BasePath}/Heroes");
            EnsureFolder($"{BasePath}/Items");
            EnsureFolder($"{BasePath}/Skills");
            EnsureFolder($"{BasePath}/UnitTypes");
            EnsureFolder($"{BasePath}/Battles");
        }

        static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
                string folder = System.IO.Path.GetFileName(path);
                if (!AssetDatabase.IsValidFolder(parent))
                    EnsureFolder(parent);
                AssetDatabase.CreateFolder(parent, folder);
            }
        }

        static UnitTypeDefinition CreateUnitType(string id, string name,
            MovementType moveType, int atk, int def, int mov, int spd)
        {
            var so = ScriptableObject.CreateInstance<UnitTypeDefinition>();
            so.id = id;
            so.displayName = name;
            so.movementType = moveType;
            so.atkModifier = atk;
            so.defModifier = def;
            so.movModifier = mov;
            so.speedModifier = spd;

            string assetPath = $"{BasePath}/UnitTypes/{id}.asset";
            AssetDatabase.DeleteAsset(assetPath);
            AssetDatabase.CreateAsset(so, assetPath);
            return so;
        }

        static SkillDefinition CreateSkill(string id, string name, string desc,
            int mpCost, int range, int power, SkillEffectType effect, int learnLv)
        {
            var so = ScriptableObject.CreateInstance<SkillDefinition>();
            so.id = id;
            so.displayName = name;
            so.description = desc;
            so.mpCost = mpCost;
            so.range = range;
            so.power = power;
            so.effectType = effect;
            so.learnLevel = learnLv;

            string assetPath = $"{BasePath}/Skills/{id}.asset";
            AssetDatabase.DeleteAsset(assetPath);
            AssetDatabase.CreateAsset(so, assetPath);
            return so;
        }

        static ItemDefinition CreateItem(string id, string name, string desc,
            ItemType type, int atkBonus = 0, int defBonus = 0, int speedBonus = 0,
            int hpBonus = 0, int mpBonus = 0, int healAmount = 0,
            int buyPrice = 100, int sellPrice = 50,
            string[] unitTypeIds = null, string[] heroIds = null)
        {
            var so = ScriptableObject.CreateInstance<ItemDefinition>();
            so.id = id;
            so.displayName = name;
            so.description = desc;
            so.itemType = type;
            so.atkBonus = atkBonus;
            so.defBonus = defBonus;
            so.speedBonus = speedBonus;
            so.hpBonus = hpBonus;
            so.mpBonus = mpBonus;
            so.healAmount = healAmount;
            so.buyPrice = buyPrice;
            so.sellPrice = sellPrice;
            so.usableInBattle = type == ItemType.Consumable;
            so.usableInCamp = type == ItemType.Consumable;
            so.restrictToUnitTypeIds = unitTypeIds ?? new string[0];
            so.restrictToHeroIds = heroIds ?? new string[0];

            string assetPath = $"{BasePath}/Items/{id}.asset";
            AssetDatabase.DeleteAsset(assetPath);
            AssetDatabase.CreateAsset(so, assetPath);
            return so;
        }

        static HeroDefinition CreateHero(string id, string name,
            int force, int intelligence, int command, int agility, int luck, int breakthrough,
            int maxHp, int maxMp, int atk, int def, int mov, int speed,
            UnitTypeDefinition unitType, bool isRequired, int recruitChapter,
            SkillDefinition[] skills)
        {
            var so = ScriptableObject.CreateInstance<HeroDefinition>();
            so.id = id;
            so.displayName = name;
            so.force = force;
            so.intelligence = intelligence;
            so.command = command;
            so.agility = agility;
            so.luck = luck;
            so.breakthrough = breakthrough;
            so.baseMaxHp = maxHp;
            so.baseMaxMp = maxMp;
            so.baseAtk = atk;
            so.baseDef = def;
            so.baseMov = mov;
            so.baseSpeed = speed;
            so.defaultUnitType = unitType;
            so.isRequired = isRequired;
            so.recruitChapter = recruitChapter;
            so.learnableSkills = skills;
            so.passiveAbilityIds = new string[0];
            so.hpGrowth = 8f;
            so.mpGrowth = 3f;
            so.atkGrowth = 1.5f;
            so.defGrowth = 1.2f;

            string assetPath = $"{BasePath}/Heroes/{id}.asset";
            AssetDatabase.DeleteAsset(assetPath);
            AssetDatabase.CreateAsset(so, assetPath);
            return so;
        }

        static BattleDefinition CreateBattleDefinition(string name, string battleName,
            int maxDeploy, string requiredHero)
        {
            var so = ScriptableObject.CreateInstance<BattleDefinition>();
            so.battleName = battleName;
            so.sceneName = "Battle";
            so.maxDeployCount = maxDeploy;

            so.requiredHeroes = new[]
            {
                new BattleUnitPlacement
                {
                    unitId = requiredHero,
                    displayName = "张强",
                    startCell = new Vector2Int(1, 3),
                    team = CaoCao.Battle.UnitTeam.Player,
                    isRequired = true
                }
            };

            so.playerSpawnPoints = new[]
            {
                new Vector2Int(1, 4),
                new Vector2Int(2, 3),
                new Vector2Int(2, 4)
            };

            // 黄巾之乱 enemies — Yellow Turban soldiers
            so.enemyUnits = new[]
            {
                new BattleUnitPlacement
                {
                    unitId = "yellow_turban_1",
                    displayName = "黄巾兵",
                    startCell = new Vector2Int(8, 5),
                    team = CaoCao.Battle.UnitTeam.Enemy,
                    maxHp = 40, atk = 8, def = 4, mov = 3
                },
                new BattleUnitPlacement
                {
                    unitId = "yellow_turban_2",
                    displayName = "黄巾兵",
                    startCell = new Vector2Int(9, 3),
                    team = CaoCao.Battle.UnitTeam.Enemy,
                    maxHp = 40, atk = 8, def = 4, mov = 3
                },
                new BattleUnitPlacement
                {
                    unitId = "yellow_turban_3",
                    displayName = "黄巾兵",
                    startCell = new Vector2Int(9, 5),
                    team = CaoCao.Battle.UnitTeam.Enemy,
                    maxHp = 35, atk = 7, def = 3, mov = 4
                },
                new BattleUnitPlacement
                {
                    unitId = "yellow_turban_leader",
                    displayName = "黄巾头目",
                    startCell = new Vector2Int(10, 4),
                    team = CaoCao.Battle.UnitTeam.Enemy,
                    maxHp = 60, atk = 12, def = 6, mov = 3
                }
            };

            string assetPath = $"{BasePath}/Battles/{name}.asset";
            AssetDatabase.DeleteAsset(assetPath);
            AssetDatabase.CreateAsset(so, assetPath);
            return so;
        }
    }
}
#endif
