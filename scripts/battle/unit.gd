extends Node2D
class_name BattleUnit

signal moved(unit)
signal attacked(attacker, target)
signal died(unit)

@export var max_hp := 20
@export var hp := 20
@export var atk := 6
@export var def := 2
@export var mov := 5
@export var team := "player" # player/enemy
@export var acted := false

@export var cell := Vector2i.ZERO
@export var tile_size := Vector2i(48, 48)

func _ready() -> void:
	if hp > max_hp:
		hp = max_hp
	_ensure_visual()
	_update_position()

func _ensure_visual() -> void:
	if get_node_or_null("Sprite") != null:
		_ensure_hp_label()
		return
	var spr = Sprite2D.new()
	spr.name = "Sprite"
	var img = Image.create(1, 1, false, Image.FORMAT_RGBA8)
	img.fill(Color(0.2, 0.6, 1.0) if team == "player" else Color(0.9, 0.25, 0.2))
	var tex = ImageTexture.create_from_image(img)
	spr.texture = tex
	spr.scale = Vector2(tile_size.x, tile_size.y) * 0.8
	spr.centered = true
	add_child(spr)
	_ensure_hp_label()

func _ensure_hp_label() -> void:
	if get_node_or_null("HpLabel") != null:
		return
	var lbl = Label.new()
	lbl.name = "HpLabel"
	lbl.text = ""
	lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	lbl.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	lbl.position = Vector2(0, -tile_size.y * 0.6)
	lbl.z_index = 10
	add_child(lbl)
	_update_hp_label()

func _update_position() -> void:
	position = Vector2(cell.x * tile_size.x + tile_size.x * 0.5, cell.y * tile_size.y + tile_size.y * 0.5)

func move_to(target: Vector2i, animate: bool = true) -> void:
	cell = target
	if not animate:
		_update_position()
		_update_hp_label()
		emit_signal("moved", self)
		return
	var target_pos = Vector2(cell.x * tile_size.x + tile_size.x * 0.5, cell.y * tile_size.y + tile_size.y * 0.5)
	var tween = create_tween()
	tween.tween_property(self, "position", target_pos, 0.2)
	tween.finished.connect(func(): emit_signal("moved", self))

func attack(target: BattleUnit) -> void:
	if target == null:
		return
	var dmg = max(1, atk - target.def)
	target.hp -= dmg
	target._update_hp_label()
	emit_signal("attacked", self, target)
	if target.hp <= 0:
		target.hp = 0
		target._update_hp_label()
		target.emit_signal("died", target)

func wait() -> void:
	acted = true

func _update_hp_label() -> void:
	var lbl = get_node_or_null("HpLabel") as Label
	if lbl:
		lbl.text = "HP %d/%d" % [hp, max_hp]
