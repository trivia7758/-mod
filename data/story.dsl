# R-?? DSL (from story.json)
# event_label name
# event_setpos <unit> <x> <y>
# event_move <unit> <x> <y>
# event_face <unit> <up|down|left|right>
# event_talk <unit> "??"
# event_bg "path"
# event_location "text"
# event_morality <value>
# event_choice / option / end_choice

event_setpos hero 18 1
event_face hero up
event_label idx_0
event_face hero up

event_label idx_1
event_bg "res://assets/ui/backgrounds/story/home.png"
event_location "时空穿越 成都 张强家"
event_morality 50
event_talk hero "又是加班到深夜……\n算了，先看看新出的版本更新了什么。"
event_face hero up

event_label idx_2
event_bg "res://assets/ui/backgrounds/story/home.png"
event_location "时空穿越 成都 张强家"
event_talk hero "咦，网页怎么一直转圈……"
event_face hero up

event_label idx_3
event_bg "res://assets/ui/backgrounds/story/home.png"
event_location "时空穿越 成都 张强家"
event_talk narrator "检测到时空裂缝，是否进入？"

event_label idx_4
event_choice
option "1. 点击进去看看" goto idx_5 morality 1
option "2. 先关掉" goto idx_5 morality -1
end_choice

event_label idx_5
event_bg "res://assets/ui/backgrounds/story/home.png"
event_location "时空穿越 成都 张强家"
event_talk hero "……等等，这不是网页！"

event_label idx_6
event_bg "res://assets/ui/backgrounds/xuanwo.jpg"
event_location "时空穿越 成都 张强家"
event_talk hero "眼前一黑，我被拉进了某个漩涡里。"

event_label idx_7
event_bg "res://assets/ui/backgrounds/xuanwo.jpg"
event_location "时空穿越"
event_talk narrator "耳边风声呼啸，像是穿过了一条看不见的长廊。"

event_label idx_8
event_bg "res://assets/ui/backgrounds/story/jiaoqu2.png"
event_location "郊外"
event_talk narrator "再睁眼时，我已身处荒野。"
event_hide_dialogue
event_wait 0.2
event_setpos oldman 19 -19
event_wait 0.2
event_move oldman 18 -1
event_face oldman down
event_face hero up

event_label idx_9
event_bg "res://assets/ui/backgrounds/story/jiaoqu2.png"
event_location "郊外"
event_talk hero "这……这是哪儿？"

event_label idx_10

event_label idx_11

event_label idx_12

event_label idx_13

event_label idx_14
event_bg "res://assets/ui/backgrounds/story/jiaoqu2.png"
event_location "郊外"
event_talk oldman "小友气色不凡，竟然来自异世。"

event_label idx_15
event_bg "res://assets/ui/backgrounds/story/jiaoqu2.png"
event_location "郊外"
event_talk hero "你是谁？这里还是成都吗？"

event_label idx_16
event_bg "res://assets/ui/backgrounds/story/jiaoqu2.png"
event_location "郊外"
event_talk oldman "此地乃三国乱世，成都尚安，但天下将乱。"

event_label idx_17
event_bg "res://assets/ui/backgrounds/story/jiaoqu2.png"
event_location "郊外"
event_talk oldman "你手中握着不同于常人的“知识”，或可改写命数。"

event_label idx_18
event_choice
option "1. 追问发生了什么" goto idx_19 morality 1
option "2. 先确认自己的处境" goto idx_19 morality 0
option "3. 质疑对方身份" goto idx_19 morality -1
end_choice

event_label idx_19
event_bg "res://assets/ui/backgrounds/story/jiaoqu2.png"
event_location "郊外"
event_talk hero "你说三国……难道我穿越了？"

event_label idx_20
event_bg "res://assets/ui/backgrounds/story/jiaoqu2.png"
event_location "郊外"
event_talk oldman "世间有因，必有果。你来此，自有缘由。"

event_label idx_21
event_bg "res://assets/ui/backgrounds/story/jiaoqu2.png"
event_location "郊外"
event_talk oldman "若想活下去，先随老夫去见一人。"

event_label idx_22
event_bg "res://assets/ui/backgrounds/story/jiaoqu2.png"
event_location "郊外"
event_talk hero "……行吧，先走一步看一步。"

event_label idx_23
event_bg "res://assets/ui/backgrounds/story/jiaoqu2.png"
event_location "郊外"
event_talk narrator "穿越的序章，就此展开。"