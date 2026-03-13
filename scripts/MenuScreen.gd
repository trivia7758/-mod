extends Control

signal start_game
signal load_game
signal open_announcement
signal language_changed(lang: String)

@onready var start_button: Button = $CenterButtons/StartButton
@onready var load_button: Button = $CenterButtons/LoadButton
@onready var settings_button: Button = $RightButtons/SettingsButton
@onready var notice_button: Button = $RightButtons/NoticeButton
@onready var version_label: Label = $Version
@onready var settings_dialog: Control = $SettingsDialog
@onready var load_dialog: Control = $LoadDialog
const BG_TEX := preload("res://assets/ui/backgrounds/bg_home_v1.jpg")

func _ready() -> void:
	start_button.pressed.connect(func(): emit_signal("start_game"))
	load_button.pressed.connect(_on_open_load)
	notice_button.pressed.connect(func(): emit_signal("open_announcement"))
	settings_button.pressed.connect(_on_open_settings)
	settings_dialog.visible = false
	load_dialog.visible = false
	settings_dialog.language_changed.connect(func(lang): emit_signal("language_changed", lang))
	_apply_background()

func set_texts(t: Dictionary) -> void:
	start_button.text = t["menu_start"]
	load_button.text = t["menu_load"]
	settings_button.text = t["menu_settings"]
	notice_button.text = t["menu_notice"]
	_apply_styles()

func _on_open_settings() -> void:
	settings_dialog.visible = true

func _on_open_load() -> void:
	load_dialog.visible = true

func apply_language(t: Dictionary, lang: String) -> void:
	set_texts(t)
	settings_dialog.set_texts(t, lang)
	if load_dialog.has_method("set_texts"):
		load_dialog.set_texts(t)

func hide_settings() -> void:
	settings_dialog.visible = false

func set_version_text(version_name: String) -> void:
	version_label.text = "version: " + version_name

func _apply_styles() -> void:
	var gold = Color(0.75, 0.6, 0.35)
	var dark = Color(0.15, 0.08, 0.08)
	var btn_bg = Color(0.2, 0.12, 0.1)
	var btn_hover = Color(0.32, 0.2, 0.16)
	var btn_pressed = Color(0.14, 0.09, 0.07)
	var bg = get_node("Background")
	if bg is ColorRect:
		bg.color = dark
	_style_button(start_button, btn_bg, btn_hover, btn_pressed, gold)
	_style_button(load_button, btn_bg, btn_hover, btn_pressed, gold)
	_style_button(settings_button, btn_bg, btn_hover, btn_pressed, gold)
	_style_button(notice_button, btn_bg, btn_hover, btn_pressed, gold)
	version_label.add_theme_color_override("font_color", Color(0.9, 0.85, 0.75))

func _style_button(btn: Button, normal: Color, hover: Color, pressed: Color, font_color: Color) -> void:
	if btn == null:
		return
	var sb_normal = StyleBoxFlat.new()
	sb_normal.bg_color = normal
	sb_normal.border_color = Color(0.45, 0.32, 0.18)
	sb_normal.border_width_left = 2
	sb_normal.border_width_right = 2
	sb_normal.border_width_top = 2
	sb_normal.border_width_bottom = 2
	sb_normal.corner_radius_top_left = 6
	sb_normal.corner_radius_top_right = 6
	sb_normal.corner_radius_bottom_left = 6
	sb_normal.corner_radius_bottom_right = 6
	var sb_hover = sb_normal.duplicate()
	sb_hover.bg_color = hover
	var sb_pressed = sb_normal.duplicate()
	sb_pressed.bg_color = pressed
	btn.add_theme_stylebox_override("normal", sb_normal)
	btn.add_theme_stylebox_override("hover", sb_hover)
	btn.add_theme_stylebox_override("pressed", sb_pressed)
	btn.add_theme_color_override("font_color", font_color)
	btn.add_theme_color_override("font_hover_color", Color(0.98, 0.9, 0.7))
	btn.add_theme_color_override("font_pressed_color", Color(0.85, 0.75, 0.55))

func _apply_background() -> void:
	var bg = get_node("Background")
	if bg is TextureRect:
		bg.texture = BG_TEX
		bg.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
		bg.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_COVERED
		bg.modulate = Color(1, 1, 1, 1)
