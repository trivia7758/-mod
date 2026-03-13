extends Control

@onready var logo_screen: Control = $LogoScreen
@onready var update_screen: Control = $UpdateScreen
@onready var announcement_screen: Control = $AnnouncementScreen
@onready var menu_screen: Control = $MenuScreen

var current_lang := "zh"
var user_data_dir := "user://data"
var user_version_path := "user://data/version_local.json"
var i18n: Dictionary = {}

func _ready() -> void:
	i18n = _load_i18n()
	_ensure_user_data()
	logo_screen.finished.connect(_on_logo_finished)
	update_screen.update_completed.connect(_on_update_completed)
	announcement_screen.closed.connect(_on_announcement_closed)
	menu_screen.open_announcement.connect(_on_open_announcement)
	menu_screen.language_changed.connect(_on_language_changed)
	menu_screen.start_game.connect(_on_start_game)
	_apply_language()
	menu_screen.set_version_text(_get_local_version_name())

func _on_logo_finished() -> void:
	_show_screen(update_screen)
	update_screen.start_check()

func _on_update_completed(has_update: bool) -> void:
	_show_screen(menu_screen)
	if has_update:
		_show_announcement_overlay()

func _on_announcement_closed() -> void:
	announcement_screen.visible = false
	menu_screen.mouse_filter = Control.MOUSE_FILTER_STOP

func _on_open_announcement() -> void:
	_show_announcement_overlay()

func _on_language_changed(lang: String) -> void:
	current_lang = lang
	_apply_language()

func _apply_language() -> void:
	var t = i18n.get(current_lang, {})
	logo_screen.set_texts(t)
	update_screen.set_texts(t, user_version_path)
	announcement_screen.set_texts(t, current_lang)
	menu_screen.apply_language(t, current_lang)

func _show_screen(target: Control) -> void:
	logo_screen.visible = false
	update_screen.visible = false
	announcement_screen.visible = false
	menu_screen.visible = false
	target.visible = true

func _show_announcement_overlay() -> void:
	menu_screen.visible = true
	announcement_screen.visible = true
	announcement_screen.z_index = 10
	menu_screen.z_index = 0
	menu_screen.mouse_filter = Control.MOUSE_FILTER_IGNORE

func _on_start_game() -> void:
	get_tree().change_scene_to_file("res://scenes/StoryScene.tscn")

func _ensure_user_data() -> void:
	if not DirAccess.dir_exists_absolute(user_data_dir):
		DirAccess.make_dir_recursive_absolute(user_data_dir)
	_copy_if_missing("res://data/announcement.txt", "user://data/announcement.txt")
	_copy_if_missing("res://data/announcement_en.txt", "user://data/announcement_en.txt")
	_copy_if_missing("res://data/version_local.json", user_version_path)
	_copy_if_missing("res://data/version_remote.json", "user://data/version_remote.json")
	_copy_if_missing("res://data/announcement_remote.txt", "user://data/announcement_remote.txt")

func _copy_if_missing(src: String, dst: String) -> void:
	if FileAccess.file_exists(dst) and not _needs_repair(dst):
		return
	if not FileAccess.file_exists(src):
		return
	var content = FileAccess.get_file_as_string(src)
	var f = FileAccess.open(dst, FileAccess.WRITE)
	if f:
		f.store_string(content)
		f.close()

func _needs_repair(path: String) -> bool:
	var content = FileAccess.get_file_as_string(path)
	return content.find("\uFFFD") != -1 or content.find("\u951F") != -1

func _get_local_version_name() -> String:
	if not FileAccess.file_exists(user_version_path):
		return "0.0.0"
	var raw = FileAccess.get_file_as_string(user_version_path)
	var parsed = JSON.parse_string(raw)
	if typeof(parsed) != TYPE_DICTIONARY:
		return "0.0.0"
	return parsed.get("version_name", "0.0.0")

func _load_i18n() -> Dictionary:
	var path = "res://data/i18n.json"
	if not FileAccess.file_exists(path):
		return {}
	var raw = FileAccess.get_file_as_string(path)
	var parsed = JSON.parse_string(raw)
	return parsed if typeof(parsed) == TYPE_DICTIONARY else {}
