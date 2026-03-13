extends Control

signal finished

@onready var logo_label: Label = $Logo
@onready var sub_label: Label = $Sub

func _ready() -> void:
    var tween = create_tween()
    tween.tween_property(logo_label, "modulate:a", 1.0, 0.7)
    tween.parallel().tween_property(sub_label, "modulate:a", 1.0, 0.7)
    tween.tween_interval(0.9)
    tween.tween_property(logo_label, "modulate:a", 0.0, 0.6)
    tween.parallel().tween_property(sub_label, "modulate:a", 0.0, 0.6)
    tween.finished.connect(func(): emit_signal("finished"))

func set_texts(t: Dictionary) -> void:
    logo_label.text = t["logo_name"]
    sub_label.text = t["logo_sub"]
