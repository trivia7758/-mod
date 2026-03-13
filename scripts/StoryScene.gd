extends Control

@export var grid_size := Vector2i(40, 30)
@export var tile_size := Vector2i(48, 48)

@onready var nav: GridNav = $GridNav
@onready var tilemap: TileMap = $Stage/TileMap
@onready var stage: Control = $Stage
@onready var hero: Unit = $Stage/Hero
@onready var oldman: Unit = $Stage/Oldman
@onready var dialogue: DialogueBox = $DialogueBox
@onready var bg: TextureRect = $Background
@onready var location_label: Label = $TopBar/LocationLabel
@onready var morality_bar: Control = $TopBar/MoralityBar
@onready var morality_red: ColorRect = $TopBar/MoralityBar/MoralityRed
@onready var morality_blue: ColorRect = $TopBar/MoralityBar/MoralityBlue
@onready var morality_border: Panel = $TopBar/MoralityBar/MoralityBorder
@onready var morality_label: Label = $TopBar/MoralityLabel
@onready var choice_panel: Control = $ChoicePanel
@onready var choice_list: VBoxContainer = $ChoicePanel/ChoiceList

const DSL_PATH := "res://data/story.dsl"
const STORY_SHEET_FRONT := "res://assets/sprites/story/test_r1_ck.png"
const STORY_SHEET_BACK := "res://assets/sprites/story/test_r2_ck.png"
const STORY_FRAME_W := 48
const STORY_FRAME_H := 64

var _labels := {}
var _choice_result: Dictionary = {}
var _morality := 50

func _ready() -> void:
	_setup_grid()
	_setup_units()
	_setup_story_sprites()
	_setup_morality_ui()
	_run_script()

func _setup_grid() -> void:
	if tilemap and tilemap.tile_set:
		tile_size = tilemap.tile_set.tile_size
	nav.setup(grid_size, tile_size)

func _setup_units() -> void:
	hero.tile_size = tile_size
	oldman.tile_size = tile_size
	hero.is_isometric = true
	oldman.is_isometric = true
	hero.grid_origin = stage.global_position
	oldman.grid_origin = stage.global_position
	hero.foot_offset = Vector2(tile_size.x * 0.5, tile_size.y * 0.5)
	oldman.foot_offset = Vector2(tile_size.x * 0.5, tile_size.y * 0.5)
	hero.global_position = hero.tile_to_world(hero.tile)
	oldman.global_position = oldman.tile_to_world(oldman.tile)
	var hero_sprite = hero.get_node("Sprite") as AnimatedSprite2D
	var oldman_sprite = oldman.get_node("Sprite") as AnimatedSprite2D
	if hero_sprite:
		hero_sprite.centered = false
		hero_sprite.position = Vector2(-STORY_FRAME_W * 0.5, -STORY_FRAME_H)
	if oldman_sprite:
		oldman_sprite.centered = false
		oldman_sprite.position = Vector2(-STORY_FRAME_W * 0.5, -STORY_FRAME_H)

func _setup_story_sprites() -> void:
	var frames = _build_story_frames()
	if frames == null:
		return
	var hero_sprite = hero.get_node("Sprite") as AnimatedSprite2D
	var oldman_sprite = oldman.get_node("Sprite") as AnimatedSprite2D
	if hero_sprite:
		hero_sprite.sprite_frames = frames
	if oldman_sprite:
		oldman_sprite.sprite_frames = frames
	hero.face_dir(Vector2i.DOWN)
	oldman.face_dir(Vector2i.DOWN)

func _build_story_frames() -> SpriteFrames:
	var front = _load_story_texture(STORY_SHEET_FRONT)
	var back = _load_story_texture(STORY_SHEET_BACK)
	if front == null or back == null:
		return null
	var frames = SpriteFrames.new()
	_add_story_anim(frames, "idle_down", front, [0], 6, true)
	_add_story_anim(frames, "walk_down", front, [1, 2], 8, true)
	_add_story_anim(frames, "idle_up", back, [0], 6, true)
	_add_story_anim(frames, "walk_up", back, [1, 2], 8, true)
	return frames

func _add_story_anim(frames: SpriteFrames, name: String, tex: Texture2D, indices: Array, fps: float, loop: bool) -> void:
	if not frames.has_animation(name):
		frames.add_animation(name)
	frames.set_animation_speed(name, fps)
	frames.set_animation_loop(name, loop)
	for i in indices:
		frames.add_frame(name, _make_atlas(tex, int(i)))

func _make_atlas(tex: Texture2D, index: int) -> Texture2D:
	var atlas = AtlasTexture.new()
	atlas.atlas = tex
	atlas.region = Rect2(0, index * STORY_FRAME_H, STORY_FRAME_W, STORY_FRAME_H)
	return atlas

func _load_story_texture(path: String) -> Texture2D:
	if not FileAccess.file_exists(path):
		return null
	return ResourceLoader.load(path)

func _run_script() -> void:
	var raw = _load_dsl(DSL_PATH)
	var dsl = StoryDSL.new()
	var events = dsl.parse(raw)
	_apply_nav_bounds(events)
	_labels = _build_label_map(events)
	_execute_events(events)

func _apply_nav_bounds(events: Array) -> void:
	var min_x := 0
	var min_y := 0
	var max_x := 0
	var max_y := 0
	var first := true
	for ev in events:
		var t = String(ev.get("type", ""))
		if t == "move" or t == "setpos":
			var x = int(ev.get("x", 0))
			var y = int(ev.get("y", 0))
			if first:
				min_x = x
				max_x = x
				min_y = y
				max_y = y
				first = false
			else:
				min_x = min(min_x, x)
				max_x = max(max_x, x)
				min_y = min(min_y, y)
				max_y = max(max_y, y)
	if first:
		nav.setup(grid_size, tile_size, Vector2i.ZERO)
		return
	var pad := 8
	var origin = Vector2i(min_x - pad, min_y - pad)
	var size = Vector2i((max_x - min_x) + pad * 2 + 1, (max_y - min_y) + pad * 2 + 1)
	nav.setup(size, tile_size, origin)

func _execute_events(events: Array) -> void:
	var meta = {
		"hero": {"name": "张强", "portrait": "res://assets/ui/portraits/nanzhu.jpg", "side": "left"},
		"oldman": {"name": "神秘老人", "portrait": "res://assets/ui/portraits/shenmilaoren.jpg", "side": "right"},
		"narrator": {"name": "", "portrait": "", "side": "center"},
		"system": {"name": "", "portrait": "", "side": "center"}
	}
	var idx := 0
	while idx < events.size():
		var ev = events[idx]
		match ev.get("type", ""):
			"label":
				pass
			"goto":
				var label = String(ev.get("label", ""))
				if _labels.has(label):
					idx = int(_labels[label])
					continue
			"setpos":
				_set_unit_pos(ev)
			"move":
				await _move_unit(ev)
			"face":
				_face_unit(ev)
			"talk":
				await _talk(ev, meta)
			"hide_dialogue":
				dialogue.visible = false
			"wait":
				await get_tree().create_timer(float(ev.get("seconds", 0.0))).timeout
			"bg":
				_set_background(String(ev.get("path", "")))
			"location":
				location_label.text = String(ev.get("text", ""))
			"morality":
				_morality = int(ev.get("value", _morality))
				_update_morality_bar(_morality)
			"choice":
				var opt = await _show_choice(ev.get("options", []))
				if opt:
					_morality += int(opt.get("morality_delta", 0))
					_update_morality_bar(_morality)
					var goto_label = String(opt.get("goto", ""))
					if _labels.has(goto_label):
						idx = int(_labels[goto_label])
						continue
		idx += 1
	# Story finished -> enter battle
	get_tree().change_scene_to_file("res://scenes/Battle.tscn")

func _set_unit_pos(ev: Dictionary) -> void:
	var unit = _get_unit(String(ev.get("unit", "")))
	if unit == null:
		return
	unit.visible = true
	unit.tile = Vector2i(int(ev.get("x", 0)), int(ev.get("y", 0)))
	unit.global_position = unit.tile_to_world(unit.tile)

func _move_unit(ev: Dictionary) -> void:
	var unit = _get_unit(String(ev.get("unit", "")))
	if unit == null:
		return
	unit.visible = true
	var target = Vector2i(int(ev.get("x", 0)), int(ev.get("y", 0)))
	var path = nav.find_path(unit.tile, target)
	if path.is_empty():
		return
	unit.move_along(path)
	await unit.reached_tile

func _face_unit(ev: Dictionary) -> void:
	var unit = _get_unit(String(ev.get("unit", "")))
	if unit == null:
		return
	unit.face_dir(_parse_dir(String(ev.get("dir", "down"))))

func _talk(ev: Dictionary, meta: Dictionary) -> void:
	var unit_id = String(ev.get("unit", ""))
	var unit = _get_unit(unit_id)
	var info = meta.get(unit_id, {"name": "", "portrait": "", "side": "left"})
	var text = String(ev.get("text", ""))
	_set_talk(unit, true)
	await dialogue.show_line(unit, text, String(info.get("name", "")), String(info.get("portrait", "")), String(info.get("side", "left")))
	_set_talk(unit, false)

func _show_choice(options: Array) -> Dictionary:
	_choice_result = {}
	choice_panel.visible = true
	for c in choice_list.get_children():
		c.queue_free()
	for opt in options:
		var btn = Button.new()
		btn.text = String(opt.get("text", "选项"))
		btn.custom_minimum_size = Vector2(0, 38)
		btn.pressed.connect(func():
			_choice_result = opt
		)
		choice_list.add_child(btn)
	while _choice_result.is_empty():
		await get_tree().process_frame
	choice_panel.visible = false
	return _choice_result

func _set_background(path: String) -> void:
	if path == "":
		return
	var tex = ResourceLoader.load(path)
	if tex:
		bg.texture = tex

func _build_label_map(events: Array) -> Dictionary:
	var map := {}
	for i in range(events.size()):
		var ev = events[i]
		if ev.get("type", "") == "label":
			map[String(ev.get("name", ""))] = i
	return map

func _parse_dir(raw: String) -> Vector2i:
	match raw.to_lower():
		"up":
			return Vector2i.UP
		"down":
			return Vector2i.DOWN
		"left":
			return Vector2i.LEFT
		"right":
			return Vector2i.RIGHT
		_:
			return Vector2i.DOWN

func _get_unit(id: String) -> Unit:
	if id == "hero":
		return hero
	if id == "oldman":
		return oldman
	return null

func _set_talk(unit: Unit, on: bool) -> void:
	if unit == null:
		return
	if not unit.has_node("Talk"):
		return
	var talk = unit.get_node("Talk") as Label
	if talk:
		talk.visible = on
		if on:
			talk.text = "..."
			talk.reset_size()
			talk.position = Vector2(-talk.size.x * 0.5, -STORY_FRAME_H - 32)

func _load_dsl(path: String) -> String:
	if not FileAccess.file_exists(path):
		return ""
	return FileAccess.get_file_as_string(path)

func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventKey and event.pressed:
		if event.keycode == KEY_ENTER or event.keycode == KEY_KP_ENTER or event.keycode == KEY_SPACE:
			dialogue.request_confirm()
		if event.keycode == KEY_G:
			_debug_print_tile()
	if event is InputEventMouseButton and event.pressed:
		if event.button_index == MOUSE_BUTTON_LEFT or event.button_index == MOUSE_BUTTON_RIGHT:
			dialogue.request_confirm()

func _debug_print_tile() -> void:
	var pos = get_viewport().get_mouse_position()
	var t = hero.world_to_tile(pos)
	print("TILE ", t.x, ", ", t.y)

func _setup_morality_ui() -> void:
	morality_label.text = "中立值"
	morality_label.add_theme_color_override("font_color", Color(0.95, 0.9, 0.78))
	morality_red.color = Color(0.86, 0.28, 0.22)
	morality_blue.color = Color(0.25, 0.45, 0.9)
	var bg = morality_bar.get_node_or_null("MoralityBG")
	if bg and bg is ColorRect:
		bg.color = Color(0.18, 0.16, 0.16)
	_update_morality_bar(_morality)

func _update_morality_bar(value: int) -> void:
	var v = clamp(value, 0, 100)
	var t = v / 100.0
	var w = morality_bar.size.x
	var h = morality_bar.size.y
	var red_w = round(w * t)
	morality_red.set_deferred("position", Vector2.ZERO)
	morality_red.set_deferred("size", Vector2(red_w, h))
	morality_blue.set_deferred("position", Vector2(red_w, 0))
	morality_blue.set_deferred("size", Vector2(max(0.0, w - red_w), h))
	morality_border.set_deferred("position", Vector2.ZERO)
	morality_border.set_deferred("size", Vector2(w, h))
