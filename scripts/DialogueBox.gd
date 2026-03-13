extends Control
class_name DialogueBox

signal finished

@export var chars_per_sec := 40.0
@export var text_path: NodePath = NodePath("TextLabel")
@export var name_path: NodePath = NodePath("NameLabel")
@export var name_bar_path: NodePath = NodePath("NameBar")
@export var name_bar_inner_path: NodePath = NodePath("NameBarInner")
@export var portrait_left_frame_path: NodePath = NodePath("PortraitLeftFrame")
@export var portrait_left_tex_path: NodePath = NodePath("PortraitLeftFrame/PortraitLeftTex")
@export var portrait_right_frame_path: NodePath = NodePath("PortraitRightFrame")
@export var portrait_right_tex_path: NodePath = NodePath("PortraitRightFrame/PortraitRightTex")

@onready var label: RichTextLabel = get_node(text_path)
@onready var name_label: Label = get_node_or_null(name_path)
@onready var name_bar: Control = get_node_or_null(name_bar_path)
@onready var name_bar_inner: Control = get_node_or_null(name_bar_inner_path)
@onready var portrait_left_frame: Control = get_node_or_null(portrait_left_frame_path)
@onready var portrait_right_frame: Control = get_node_or_null(portrait_right_frame_path)
@onready var portrait_left: TextureRect = get_node_or_null(portrait_left_tex_path)
@onready var portrait_right: TextureRect = get_node_or_null(portrait_right_tex_path)

var _typing := false
var _total := 0
var _type_accum := 0.0
var _confirm_requested := false
var _default_layout := {}
var _default_align := {}
var _default_label_layout := {}

func _ready() -> void:
	set_process_input(true)
	_default_layout = {
		"anchor_left": anchor_left,
		"anchor_right": anchor_right,
		"anchor_top": anchor_top,
		"anchor_bottom": anchor_bottom,
		"offset_left": offset_left,
		"offset_right": offset_right,
		"offset_top": offset_top,
		"offset_bottom": offset_bottom
	}
	_default_align = {
		"h": label.horizontal_alignment,
		"v": label.vertical_alignment
	}
	_default_label_layout = {
		"anchor_left": label.anchor_left,
		"anchor_right": label.anchor_right,
		"anchor_top": label.anchor_top,
		"anchor_bottom": label.anchor_bottom,
		"offset_left": label.offset_left,
		"offset_right": label.offset_right,
		"offset_top": label.offset_top,
		"offset_bottom": label.offset_bottom
	}

func show_line(unit: Unit, text: String, name: String = "", portrait_path: String = "", side: String = "left") -> void:
	visible = true
	_confirm_requested = false
	_set_name_and_portrait(name, portrait_path, side)
	label.text = text
	_total = label.get_total_character_count()
	label.visible_characters = 0
	_typing = true
	_type_accum = 0.0

	if unit:
		unit.play_talk()

	while _typing:
		_type_accum += chars_per_sec * get_process_delta_time()
		var add = int(_type_accum)
		if add > 0:
			_type_accum -= add
			label.visible_characters = min(_total, label.visible_characters + add)
		if label.visible_characters >= _total:
			_typing = false
		await get_tree().process_frame

	await _wait_confirm()

	if unit:
		unit.stop_talk()

	emit_signal("finished")

func _wait_confirm() -> void:
	while true:
		await get_tree().process_frame
		if _confirm_requested:
			_confirm_requested = false
			break

func _input(event: InputEvent) -> void:
	if not visible:
		return
	if event is InputEventKey and event.pressed:
		if event.keycode == KEY_ENTER or event.keycode == KEY_KP_ENTER or event.keycode == KEY_SPACE:
			_confirm_requested = true
	if event is InputEventMouseButton and event.pressed:
		if event.button_index == MOUSE_BUTTON_LEFT or event.button_index == MOUSE_BUTTON_RIGHT:
			_confirm_requested = true

func request_confirm() -> void:
	_confirm_requested = true

func _set_name_and_portrait(name: String, portrait_path: String, side: String) -> void:
	if side == "center":
		_apply_center_layout(true)
		if name_label:
			name_label.text = ""
			name_label.visible = false
		if name_bar:
			name_bar.visible = false
		if name_bar_inner:
			name_bar_inner.visible = false
		if portrait_left_frame:
			portrait_left_frame.visible = false
		if portrait_right_frame:
			portrait_right_frame.visible = false
		_apply_center_text_layout(true)
		label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
		return
	_apply_center_layout(false)
	_apply_center_text_layout(false)
	if name_label:
		name_label.text = name
		name_label.visible = true
	if name_bar:
		name_bar.visible = true
	if name_bar_inner:
		name_bar_inner.visible = true
	if portrait_left_frame:
		portrait_left_frame.visible = false
	if portrait_right_frame:
		portrait_right_frame.visible = false
	if portrait_path == "":
		label.horizontal_alignment = _default_align["h"]
		label.vertical_alignment = _default_align["v"]
		return
	var tex: Texture2D = ResourceLoader.load(portrait_path)
	if side == "right":
		if portrait_right_frame:
			portrait_right_frame.visible = true
		if portrait_right:
			portrait_right.texture = tex
	else:
		if portrait_left_frame:
			portrait_left_frame.visible = true
		if portrait_left:
			portrait_left.texture = tex
	label.horizontal_alignment = _default_align["h"]
	label.vertical_alignment = _default_align["v"]

func _apply_center_layout(enable: bool) -> void:
	if enable:
		anchor_left = 0.5
		anchor_right = 0.5
		anchor_top = 0.5
		anchor_bottom = 0.5
		offset_left = -420.0
		offset_right = 420.0
		offset_top = -60.0
		offset_bottom = 60.0
	else:
		anchor_left = _default_layout["anchor_left"]
		anchor_right = _default_layout["anchor_right"]
		anchor_top = _default_layout["anchor_top"]
		anchor_bottom = _default_layout["anchor_bottom"]
		offset_left = _default_layout["offset_left"]
		offset_right = _default_layout["offset_right"]
		offset_top = _default_layout["offset_top"]
		offset_bottom = _default_layout["offset_bottom"]

func _apply_center_text_layout(enable: bool) -> void:
	if enable:
		label.anchor_left = 0.0
		label.anchor_right = 1.0
		label.anchor_top = 0.0
		label.anchor_bottom = 1.0
		label.offset_left = 24.0
		label.offset_right = -24.0
		label.offset_top = 10.0
		label.offset_bottom = -10.0
	else:
		label.anchor_left = _default_label_layout["anchor_left"]
		label.anchor_right = _default_label_layout["anchor_right"]
		label.anchor_top = _default_label_layout["anchor_top"]
		label.anchor_bottom = _default_label_layout["anchor_bottom"]
		label.offset_left = _default_label_layout["offset_left"]
		label.offset_right = _default_label_layout["offset_right"]
		label.offset_top = _default_label_layout["offset_top"]
		label.offset_bottom = _default_label_layout["offset_bottom"]
