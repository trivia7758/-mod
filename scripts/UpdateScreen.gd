extends Control

signal update_completed(has_update: bool)

@export var simulate_check_seconds: float = 1.2
@export var simulate_update_seconds: float = 2.0

@onready var title_label: Label = $Panel/Title
@onready var status_label: Label = $Panel/Status
@onready var progress_bar: ProgressBar = $Panel/ProgressBar

var _texts: Dictionary = {}
var _local_version_path := "user://data/version_local.json"
var _state := "idle"
var _elapsed := 0.0
var _has_update := false
var _pending_remote := {}

func _ready() -> void:
    _apply_styles()

func set_texts(t: Dictionary, local_version_path: String) -> void:
    _texts = t
    _local_version_path = local_version_path
    title_label.text = t["update_title"]
    status_label.text = t["update_checking"]

func start_check() -> void:
    _state = "checking"
    _elapsed = 0.0
    progress_bar.value = 0.0
    status_label.text = _texts.get("update_checking", "正在检查...")
    _has_update = _check_local_vs_remote()

func _process(delta: float) -> void:
    if _state == "checking":
        _elapsed += delta
        progress_bar.value = min(100.0, (_elapsed / simulate_check_seconds) * 100.0)
        if _elapsed >= simulate_check_seconds:
            if _has_update:
                _state = "updating"
                _elapsed = 0.0
                status_label.text = _texts.get("update_updating", "正在更新...")
            else:
                _state = "done"
                call_deferred("_emit_done", false)
    elif _state == "updating":
        _elapsed += delta
        progress_bar.value = min(100.0, (_elapsed / simulate_update_seconds) * 100.0)
        if _elapsed >= simulate_update_seconds:
            _state = "done"
            _apply_update()
            call_deferred("_emit_done", true)

func _emit_done(has_update: bool) -> void:
    emit_signal("update_completed", has_update)

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
    status_label.add_theme_color_override("font_color", Color(0.92, 0.85, 0.72))

    var bg = StyleBoxFlat.new()
    bg.bg_color = Color(0.12, 0.08, 0.07)
    bg.border_color = Color(0.4, 0.3, 0.18)
    bg.border_width_left = 2
    bg.border_width_right = 2
    bg.border_width_top = 2
    bg.border_width_bottom = 2
    bg.corner_radius_top_left = 6
    bg.corner_radius_top_right = 6
    bg.corner_radius_bottom_left = 6
    bg.corner_radius_bottom_right = 6
    progress_bar.add_theme_stylebox_override("background", bg)

    var fill = StyleBoxFlat.new()
    fill.bg_color = Color(0.72, 0.58, 0.32)
    fill.corner_radius_top_left = 6
    fill.corner_radius_top_right = 6
    fill.corner_radius_bottom_left = 6
    fill.corner_radius_bottom_right = 6
    progress_bar.add_theme_stylebox_override("fill", fill)

func _check_local_vs_remote() -> bool:
    var local = _read_version(_local_version_path)
    var remote = _read_version("user://data/version_remote.json")
    _pending_remote = remote
    var local_code = int(local.get("version_code", 0))
    var remote_code = int(remote.get("version_code", 0))
    return remote_code > local_code

func _read_version(path: String) -> Dictionary:
    if not FileAccess.file_exists(path):
        return {}
    var raw = FileAccess.get_file_as_string(path)
    var parsed = JSON.parse_string(raw)
    return parsed if typeof(parsed) == TYPE_DICTIONARY else {}

func _apply_update() -> void:
    if _pending_remote.is_empty():
        return
    var f = FileAccess.open(_local_version_path, FileAccess.WRITE)
    if f:
        f.store_string(JSON.stringify(_pending_remote, "\t"))
        f.close()
    var ann_path = _pending_remote.get("announcement_file", "")
    if ann_path != "":
        var src = ann_path.replace("res://", "user://data/")
        if FileAccess.file_exists(src):
            var content = FileAccess.get_file_as_string(src)
            var out = FileAccess.open("user://data/announcement.txt", FileAccess.WRITE)
            if out:
                out.store_string(content)
                out.close()
