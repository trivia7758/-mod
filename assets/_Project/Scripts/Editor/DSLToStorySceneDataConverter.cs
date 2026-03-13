using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using CaoCao.Story;

namespace CaoCao.Editor
{
    /// <summary>
    /// One-time migration tool: converts the existing story.dsl.txt
    /// into a StorySceneData ScriptableObject with proper Sprite references.
    /// Menu: CaoCao/Convert DSL to StorySceneData
    /// </summary>
    public static class DSLToStorySceneDataConverter
    {
        [MenuItem("CaoCao/Convert DSL to StorySceneData")]
        public static void Convert()
        {
            // Load sprites
            var bgHome = LoadSprite("Backgrounds/Story/home");
            var bgXuanwo = LoadSprite("Backgrounds/xuanwo");
            var bgJiaoqu2 = LoadSprite("Backgrounds/Story/jiaoqu2");
            var portraitHero = LoadSprite("Portraits/nanzhu");
            var portraitOldman = LoadSprite("Portraits/shenmilaoren");

            // Create asset
            var data = ScriptableObject.CreateInstance<StorySceneData>();

            // ── Actors ──
            data.actors = new List<ActorDefinition>
            {
                new() { id = "hero",     displayName = "张强",     portrait = portraitHero,   defaultSide = DialogueSide.Left },
                new() { id = "oldman",   displayName = "神秘老人", portrait = portraitOldman, defaultSide = DialogueSide.Right },
                new() { id = "narrator", displayName = "",         portrait = null,           defaultSide = DialogueSide.Center },
                new() { id = "system",   displayName = "",         portrait = null,           defaultSide = DialogueSide.Center },
            };

            // ── Actions ──
            data.actions = new List<StoryActionBase>();
            var a = data.actions;

            // ===== Scene 1: 张强家 =====
            a.Add(new SetBackgroundAction { backgroundSprite = bgHome });
            a.Add(new SetLocationAction { locationText = "时空穿越 成都 张强家" });
            a.Add(new SetMoralityAction { value = 50 });
            a.Add(new SetPositionAction { actorId = "hero", worldPosition = new Vector2(9.5f, -4.5f) });
            a.Add(new FaceDirectionAction { actorId = "hero", direction = FaceDirection.Up });

            a.Add(new TalkAction { actorId = "hero", text = "又是加班到深夜……\n算了，先看看新出的版本更新了什么。" });
            a.Add(new TalkAction { actorId = "hero", text = "咦，网页怎么一直转圈……" });
            a.Add(new TalkAction { actorId = "narrator", text = "检测到时空裂缝，是否进入？" });

            // 选择1: 进入还是关掉
            a.Add(new ChoiceAction
            {
                options = new List<StoryChoiceOption>
                {
                    new() { text = "1. 点击进去看看", gotoLabel = "after_choice_1", moralityDelta = 1 },
                    new() { text = "2. 先关掉", gotoLabel = "after_choice_1", moralityDelta = -1 },
                }
            });

            a.Add(new LabelAction { labelName = "after_choice_1" });
            a.Add(new TalkAction { actorId = "hero", text = "……等等，这不是网页！" });

            // ===== Scene 2: 漩涡 =====
            a.Add(new SetBackgroundAction { backgroundSprite = bgXuanwo });
            a.Add(new SetLocationAction { locationText = "时空穿越" });
            a.Add(new TalkAction { actorId = "hero", text = "眼前一黑，我被拉进了某个漩涡里。" });
            a.Add(new TalkAction { actorId = "narrator", text = "耳边风声呼啸，像是穿过了一条看不见的长廊。" });

            // ===== Scene 3: 郊外 =====
            a.Add(new SetBackgroundAction { backgroundSprite = bgJiaoqu2 });
            a.Add(new SetLocationAction { locationText = "郊外" });
            a.Add(new TalkAction { actorId = "narrator", text = "再睁眼时，我已身处荒野。" });
            a.Add(new HideDialogueAction());
            a.Add(new WaitAction { seconds = 0.2f });

            // 角色入场
            a.Add(new SetPositionAction { actorId = "hero", worldPosition = new Vector2(5f, -4.5f) });
            a.Add(new SetPositionAction { actorId = "oldman", worldPosition = new Vector2(12f, -2f) });
            a.Add(new WaitAction { seconds = 0.2f });

            // 老人走到男主面前
            a.Add(new MoveToAction
            {
                actorId = "oldman",
                waypoints = new List<Waypoint>
                {
                    new(new Vector2(8f, -3.5f), FaceDirection.Down),
                    new(new Vector2(6.5f, -4.2f), FaceDirection.Left),
                },
                speed = 2f
            });
            a.Add(new FaceDirectionAction { actorId = "oldman", direction = FaceDirection.Down });
            a.Add(new FaceDirectionAction { actorId = "hero", direction = FaceDirection.Up });

            // 对话
            a.Add(new TalkAction { actorId = "hero", text = "这……这是哪儿？" });
            a.Add(new TalkAction { actorId = "oldman", text = "小友气色不凡，竟然来自异世。" });
            a.Add(new TalkAction { actorId = "hero", text = "你是谁？这里还是成都吗？" });
            a.Add(new TalkAction { actorId = "oldman", text = "此地乃三国乱世，成都尚安，但天下将乱。" });
            a.Add(new TalkAction { actorId = "oldman", text = "你手中握着不同于常人的\"知识\"，或可改写命数。" });

            // 选择2: 三种反应
            a.Add(new ChoiceAction
            {
                options = new List<StoryChoiceOption>
                {
                    new() { text = "1. 追问发生了什么", gotoLabel = "after_choice_2", moralityDelta = 1 },
                    new() { text = "2. 先确认自己的处境", gotoLabel = "after_choice_2", moralityDelta = 0 },
                    new() { text = "3. 质疑对方身份", gotoLabel = "after_choice_2", moralityDelta = -1 },
                }
            });

            a.Add(new LabelAction { labelName = "after_choice_2" });
            a.Add(new TalkAction { actorId = "hero", text = "你说三国……难道我穿越了？" });
            a.Add(new TalkAction { actorId = "oldman", text = "世间有因，必有果。你来此，自有缘由。" });
            a.Add(new TalkAction { actorId = "oldman", text = "若想活下去，先随老夫去见一人。" });
            a.Add(new TalkAction { actorId = "hero", text = "……行吧，先走一步看一步。" });
            a.Add(new TalkAction { actorId = "narrator", text = "穿越的序章，就此展开。" });

            // ── Save asset ──
            string folder = "Assets/_Project/ScriptableObjects/StoryScenes";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Project/ScriptableObjects"))
                    AssetDatabase.CreateFolder("Assets/_Project", "ScriptableObjects");
                AssetDatabase.CreateFolder("Assets/_Project/ScriptableObjects", "StoryScenes");
            }

            string path = $"{folder}/Prologue.asset";
            AssetDatabase.CreateAsset(data, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = data;
            EditorGUIUtility.PingObject(data);

            Debug.Log($"[DSL Converter] Created StorySceneData at {path} with {data.actors.Count} actors and {data.actions.Count} actions.");
        }

        static Sprite LoadSprite(string resourcePath)
        {
            var sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite == null)
                Debug.LogWarning($"[DSL Converter] Sprite not found: {resourcePath}");
            return sprite;
        }
    }
}
