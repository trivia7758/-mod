extends Control

signal closed
signal slot_selected(index: int)

@onready var title_label: Label = $Panel/Title
@onready var close_button: Button = $Panel/Close
@onready var header: HBoxContainer = $Panel/Header
@onready var rows_container: VBoxContainer = $Panel/Scroll/Rows
@onready var bottom_button: Button = $Panel/BottomButton

var _rows := []
var _slots: Array = []

func _ready() -> void:
    close_button.pressed.connect(func(): _close())
    bottom_button.pressed.connect(func(): _close())
    _apply_styles()
    _build_rows(50)

func set_texts(t: Dictionary) -> void:
    title_label.text = t.get("load_title", "读取记录")
    bottom_button.text = t.get("load_bottom", "读取回合初始")

func set_slots(data: Array) -> void:
    _slots = data
    _build_rows(50)

func _build_rows(count: int) -> void:
    for c in rows_container.get_children():
        c.queue_free()
    _rows.clear()
    for i in range(count):
        var row = _make_row(i, _slots[i] if i < _slots.size() else null)
        rows_container.add_child(row)
        _rows.append(row)

func _make_row(index: int, data) -> Control:
    var row = Button.new()
    row.toggle_mode = false
    row.text = ""
    row.custom_minimum_size = Vector2(0, 36)
    row.size_flags_horizontal = Control.SIZE_EXPAND_FILL
    row.alignment = HORIZONTAL_ALIGNMENT_LEFT
    row.pressed.connect(func(): emit_signal("slot_selected", index))

    var h = HBoxContainer.new()
    h.anchor_left = 0.0
    h.anchor_right = 1.0
    h.size_flags_horizontal = Control.SIZE_EXPAND_FILL
    row.add_child(h)

    var no = Label.new()
    no.text = "No.%d" % (index + 1)
    no.custom_minimum_size = Vector2(90, 0)
    h.add_child(no)

    var level = Label.new()
    level.text = _format_level(data)
    level.custom_minimum_size = Vector2(90, 0)
    h.add_child(level)

    var record = Label.new()
    record.text = _format_record(data)
    record.size_flags_horizontal = Control.SIZE_EXPAND_FILL
    h.add_child(record)

    var time = Label.new()
    time.text = _format_time(data)
    time.custom_minimum_size = Vector2(190, 0)
    h.add_child(time)

    return row

func _format_level(data) -> String:
    if data == null:
        return ""
    if not (data is Dictionary):
        return ""
    if not data.has("top5_levels"):
        return ""
    var levels = data["top5_levels"]
    if not (levels is Array) or levels.is_empty():
        return ""
    var parts := []
    for v in levels:
        parts.append(str(v))
    return "LV.%s" % "/".join(parts)

func _format_record(data) -> String:
    if data == null or not (data is Dictionary):
        return ""
    var stage = data.get("stage_name", "")
    if stage == "":
        return ""
    var kind = data.get("record_type", "prebattle")
    if kind == "battle":
        var turn = int(data.get("turn", 1))
        return "%s -- 第 %d 回合" % [stage, turn]
    return stage

func _format_time(data) -> String:
    if data == null or not (data is Dictionary):
        return ""
    return str(data.get("time", ""))

func _apply_styles() -> void:
    var panel = $Panel as Panel
    var sb = StyleBoxFlat.new()
    sb.bg_color = Color(0.92, 0.9, 0.86)
    sb.border_color = Color(0.45, 0.36, 0.2)
    sb.border_width_left = 2
    sb.border_width_right = 2
    sb.border_width_top = 2
    sb.border_width_bottom = 2
    sb.corner_radius_top_left = 6
    sb.corner_radius_top_right = 6
    sb.corner_radius_bottom_left = 6
    sb.corner_radius_bottom_right = 6
    panel.add_theme_stylebox_override("panel", sb)

    title_label.add_theme_color_override("font_color", Color(0.6, 0.48, 0.28))
    _style_button(close_button, Color(0.7, 0.7, 0.7), Color(0.82, 0.82, 0.82))
    _style_button(bottom_button, Color(0.86, 0.74, 0.45), Color(0.95, 0.86, 0.6))

    for n in header.get_children():
        if n is Label:
            (n as Label).add_theme_color_override("font_color", Color(0.55, 0.45, 0.28))

func _style_button(btn: Button, normal: Color, hover: Color) -> void:
    var sb_normal = StyleBoxFlat.new()
    sb_normal.bg_color = normal
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
    sb_hover.bg_color = hover
    btn.add_theme_stylebox_override("normal", sb_normal)
    btn.add_theme_stylebox_override("hover", sb_hover)
    btn.add_theme_color_override("font_color", Color(0.3, 0.2, 0.1))

func _close() -> void:
    visible = false
    emit_signal("closed")
