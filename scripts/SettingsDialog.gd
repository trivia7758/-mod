extends Control

signal language_changed(lang: String)

@onready var title_label: Label = $Panel/Title
@onready var close_button: Button = $Panel/Close
@onready var audio_title: Label = $Panel/Content/AudioTitle
@onready var display_title: Label = $Panel/Content/DisplayTitle
@onready var speed_title: Label = $Panel/Content/SpeedTitle
@onready var language_title: Label = $Panel/Content/LanguageTitle
@onready var message_label: Label = $Panel/Content/MessageSpeed/MessageLabel
@onready var move_label: Label = $Panel/Content/MoveSpeed/MoveLabel
@onready var language_label: Label = $Panel/Content/LanguageRow/LanguageLabel
@onready var message_fast: Button = $Panel/Content/MessageSpeed/MessageFast
@onready var message_mid: Button = $Panel/Content/MessageSpeed/MessageMid
@onready var message_slow: Button = $Panel/Content/MessageSpeed/MessageSlow
@onready var move_fast: Button = $Panel/Content/MoveSpeed/MoveFast
@onready var move_mid: Button = $Panel/Content/MoveSpeed/MoveMid
@onready var move_slow: Button = $Panel/Content/MoveSpeed/MoveSlow
@onready var auto_play_check: CheckBox = $Panel/Content/AutoPlay
@onready var language_option: OptionButton = $Panel/Content/LanguageRow/LanguageOption

var _message_group := ButtonGroup.new()
var _move_group := ButtonGroup.new()
var _settings_path := "user://data/settings.json"

func _ready() -> void:
    close_button.pressed.connect(func(): visible = false)
    _setup_options()
    language_option.item_selected.connect(_on_language_selected)
    auto_play_check.toggled.connect(_on_auto_play_toggled)
    _load_settings()
    _apply_styles()

func _setup_options() -> void:
    _setup_speed_groups()
    message_fast.text = "快"
    message_mid.text = "中"
    message_slow.text = "慢"
    move_fast.text = "快"
    move_mid.text = "中"
    move_slow.text = "慢"
    language_option.clear()
    language_option.add_item("中文")
    language_option.add_item("English")
    message_mid.button_pressed = true
    move_mid.button_pressed = true

func set_texts(t: Dictionary, lang: String) -> void:
    title_label.text = t.get("settings_title", "设置")
    close_button.text = t.get("common_close", "关闭")
    audio_title.text = t.get("settings_audio", "音频设置")
    display_title.text = t.get("settings_display", "显示设置")
    speed_title.text = t.get("settings_speed", "速度设置")
    language_title.text = t.get("settings_language", "语言设置")
    message_label.text = t.get("settings_message_speed", "信息显示速度")
    move_label.text = t.get("settings_move_speed", "武将移动速度")
    language_label.text = t.get("settings_language_label", "语言")
    auto_play_check.text = t.get("settings_autoplay", "剧情自动播放")
    if lang == "en":
        _setup_options_en()
    else:
        _setup_options()
        language_option.select(0)

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
    _style_button(message_fast)
    _style_button(message_mid)
    _style_button(message_slow)
    _style_button(move_fast)
    _style_button(move_mid)
    _style_button(move_slow)

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

func _setup_options_en() -> void:
    _setup_speed_groups()
    message_fast.text = "快"
    message_mid.text = "中"
    message_slow.text = "慢"
    move_fast.text = "快"
    move_mid.text = "中"
    move_slow.text = "慢"
    language_option.clear()
    language_option.add_item("中文")
    language_option.add_item("English")
    language_option.select(1)
    message_mid.button_pressed = true
    move_mid.button_pressed = true

func _setup_speed_groups() -> void:
    message_fast.button_group = _message_group
    message_mid.button_group = _message_group
    message_slow.button_group = _message_group
    move_fast.button_group = _move_group
    move_mid.button_group = _move_group
    move_slow.button_group = _move_group

func _on_language_selected(index: int) -> void:
    var lang = "zh" if index == 0 else "en"
    emit_signal("language_changed", lang)

func _on_auto_play_toggled(on: bool) -> void:
    _save_settings({"auto_play": on})

func _load_settings() -> void:
    if not FileAccess.file_exists(_settings_path):
        return
    var raw = FileAccess.get_file_as_string(_settings_path)
    var parsed = JSON.parse_string(raw)
    if typeof(parsed) != TYPE_DICTIONARY:
        return
    auto_play_check.button_pressed = bool(parsed.get("auto_play", true))

func _save_settings(new_values: Dictionary) -> void:
    var data := {}
    if FileAccess.file_exists(_settings_path):
        var raw = FileAccess.get_file_as_string(_settings_path)
        var parsed = JSON.parse_string(raw)
        if typeof(parsed) == TYPE_DICTIONARY:
            data = parsed
    for k in new_values.keys():
        data[k] = new_values[k]
    var f = FileAccess.open(_settings_path, FileAccess.WRITE)
    if f:
        f.store_string(JSON.stringify(data, "\t"))
        f.close()
