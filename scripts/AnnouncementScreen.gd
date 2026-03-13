extends Control

signal closed

@onready var title_label: Label = $Panel/Title
@onready var body_label: RichTextLabel = $Panel/Body
@onready var close_button: Button = $Panel/Close

var _texts: Dictionary = {}
var _lang := "zh"

func _ready() -> void:
    close_button.pressed.connect(_on_close_pressed)
    _apply_styles()
    _load_body()

func set_texts(t: Dictionary, lang: String) -> void:
    _texts = t
    _lang = lang
    title_label.text = t["announcement_title"]
    close_button.text = t["common_close"]
    _load_body()

func _load_body() -> void:
    var path = "user://data/announcement.txt"
    if _lang == "en" and FileAccess.file_exists("user://data/announcement_en.txt"):
        path = "user://data/announcement_en.txt"
    if not FileAccess.file_exists(path):
        path = "res://data/announcement.txt"
        if _lang == "en" and FileAccess.file_exists("res://data/announcement_en.txt"):
            path = "res://data/announcement_en.txt"
    if FileAccess.file_exists(path):
        body_label.text = FileAccess.get_file_as_string(path)
    else:
        body_label.text = ""

func _on_close_pressed() -> void:
    emit_signal("closed")

func _apply_styles() -> void:
    var panel = $Panel as Panel
    var sb = StyleBoxFlat.new()
    sb.bg_color = Color(0.18, 0.12, 0.1)
    sb.border_color = Color(0.5, 0.38, 0.2)
    sb.border_width_left = 2
    sb.border_width_right = 2
    sb.border_width_top = 2
    sb.border_width_bottom = 2
    sb.corner_radius_top_left = 6
    sb.corner_radius_top_right = 6
    sb.corner_radius_bottom_left = 6
    sb.corner_radius_bottom_right = 6
    panel.add_theme_stylebox_override("panel", sb)
    title_label.add_theme_color_override("font_color", Color(0.98, 0.92, 0.8))
    _style_button(close_button)

func _style_button(btn: Button) -> void:
    var sb_normal = StyleBoxFlat.new()
    sb_normal.bg_color = Color(0.24, 0.16, 0.12)
    sb_normal.border_color = Color(0.5, 0.38, 0.2)
    sb_normal.border_width_left = 2
    sb_normal.border_width_right = 2
    sb_normal.border_width_top = 2
    sb_normal.border_width_bottom = 2
    sb_normal.corner_radius_top_left = 6
    sb_normal.corner_radius_top_right = 6
    sb_normal.corner_radius_bottom_left = 6
    sb_normal.corner_radius_bottom_right = 6
    var sb_hover = sb_normal.duplicate()
    sb_hover.bg_color = Color(0.32, 0.2, 0.16)
    var sb_pressed = sb_normal.duplicate()
    sb_pressed.bg_color = Color(0.18, 0.12, 0.1)
    btn.add_theme_stylebox_override("normal", sb_normal)
    btn.add_theme_stylebox_override("hover", sb_hover)
    btn.add_theme_stylebox_override("pressed", sb_pressed)
    btn.add_theme_color_override("font_color", Color(0.9, 0.82, 0.65))
