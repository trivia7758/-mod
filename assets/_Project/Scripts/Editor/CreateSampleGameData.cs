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

            // Items — Protagonist starting gear
            var normalBook = CreateItem("normal_book", "普通书本", "一本普通的大学教材",
                ItemType.Weapon, atkBonus: 2);
            var cottonClothes = CreateItem("cotton_clothes", "棉布衣", "普通的棉布衣服",
                ItemType.Armor, defBonus: 2);

            // Items — Standard gear
            var ironSword = CreateItem("iron_sword", "铁剑", "基础武器",
                ItemType.Weapon, atkBonus: 3);
            var ironArmor = CreateItem("iron_armor", "铁甲", "基础防具",
                ItemType.Armor, defBonus: 3);
            var speedBoots = CreateItem("speed_boots", "疾风靴", "提升移动速度",
                ItemType.Auxiliary, speedBonus: 2);
            var hpPotion = CreateItem("hp_potion", "回复药", "恢复50点HP",
                ItemType.Consumable, healAmount: 50);

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
            registry.allItems = new[] { normalBook, cottonClothes, ironSword, ironArmor, speedBoots, hpPotion };
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
                "- 6 Items (普通书本, 棉布衣, 铁剑, 铁甲, 疾风靴, 回复药)\n" +
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
            int hpBonus = 0, int mpBonus = 0, int healAmount = 0)
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
            so.buyPrice = 100;
            so.sellPrice = 50;
            so.usableInBattle = type == ItemType.Consumable;
            so.usableInCamp = type == ItemType.Consumable;

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
                    startCell = new Vector2Int(2, 2),
                    team = CaoCao.Battle.UnitTeam.Player,
                    isRequired = true
                }
            };

            so.playerSpawnPoints = new[]
            {
                new Vector2Int(2, 3),
                new Vector2Int(3, 2),
                new Vector2Int(3, 3)
            };

            so.enemyUnits = new[]
            {
                new BattleUnitPlacement
                {
                    unitId = "enemy_soldier_1",
                    startCell = new Vector2Int(8, 5),
                    team = CaoCao.Battle.UnitTeam.Enemy
                },
                new BattleUnitPlacement
                {
                    unitId = "enemy_soldier_2",
                    startCell = new Vector2Int(9, 3),
                    team = CaoCao.Battle.UnitTeam.Enemy
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
