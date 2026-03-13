extends Node
class_name BattleController

signal unit_selected(unit)
signal unit_moved(unit)
signal unit_attacked(attacker, target)
signal turn_changed(team)

enum State { Idle, UnitSelected, Moving, ActionMenu, Attacking, Animating, EnemyTurn, TurnEnd }

@export var tile_size := Vector2i(48, 48)

@onready var map: BattleGridMap = $BattleMap
@onready var highlight: HighlightLayer = $Highlight
@onready var units_root: Node2D = $Units
@onready var ai: EnemyAI = $EnemyAI
@onready var pathfinder: BattlePathfinding = $Pathfinding

var action_menu: Control
var attack_btn: Button
var wait_btn: Button
var terrain_label: Label
var hit_label: Label
var avoid_label: Label

var state := State.Idle
var current_team := "player"
var selected_unit: BattleUnit
var reachable := {}
var parent := {}
var selected_origin := Vector2i.ZERO
var moved_this_turn := false

func _ready() -> void:
	_ensure_ui_nodes()
	_bind_ui_nodes()
	_setup_demo_units()
	_setup_ui()
	_sync_map_size_from_level()
	ai.map = map
	ai.pathfinder = pathfinder
	_set_state(State.Idle)

func _setup_ui() -> void:
	if action_menu:
		action_menu.visible = false
	if attack_btn and not attack_btn.pressed.is_connected(_on_attack_pressed):
		attack_btn.pressed.connect(_on_attack_pressed)
	if wait_btn and not wait_btn.pressed.is_connected(_on_wait_pressed):
		wait_btn.pressed.connect(_on_wait_pressed)

func _ensure_ui_nodes() -> void:
	var ui_layer := get_node_or_null("UILayer")
	if ui_layer == null:
		ui_layer = CanvasLayer.new()
		ui_layer.name = "UILayer"
		add_child(ui_layer)
	var ui := ui_layer.get_node_or_null("UI")
	if ui == null:
		ui = Control.new()
		ui.name = "UI"
		ui.anchor_right = 1.0
		ui.anchor_bottom = 1.0
		ui.mouse_filter = Control.MOUSE_FILTER_IGNORE
		ui_layer.add_child(ui)
	else:
		ui.mouse_filter = Control.MOUSE_FILTER_IGNORE
	if ui.get_node_or_null("ActionMenu") == null:
		var panel := Panel.new()
		panel.name = "ActionMenu"
		panel.visible = false
		panel.mouse_filter = Control.MOUSE_FILTER_STOP
		panel.offset_left = 20
		panel.offset_top = 20
		panel.offset_right = 140
		panel.offset_bottom = 90
		ui.add_child(panel)
		var attack := Button.new()
		attack.name = "Attack"
		attack.text = "Attack"
		attack.offset_left = 10
		attack.offset_top = 10
		attack.offset_right = 110
		attack.offset_bottom = 40
		panel.add_child(attack)
		var wait := Button.new()
		wait.name = "Wait"
		wait.text = "Wait"
		wait.offset_left = 10
		wait.offset_top = 45
		wait.offset_right = 110
		wait.offset_bottom = 75
		panel.add_child(wait)
	if ui.get_node_or_null("Info") == null:
		var info := Panel.new()
		info.name = "Info"
		info.offset_left = 620
		info.offset_top = 20
		info.offset_right = 820
		info.offset_bottom = 100
		info.mouse_filter = Control.MOUSE_FILTER_IGNORE
		ui.add_child(info)
		var terrain := Label.new()
		terrain.name = "Terrain"
		terrain.text = "Terrain:"
		terrain.offset_left = 10
		terrain.offset_top = 10
		terrain.offset_right = 180
		terrain.offset_bottom = 30
		info.add_child(terrain)
		var hit := Label.new()
		hit.name = "Hit"
		hit.text = "Hit:"
		hit.offset_left = 10
		hit.offset_top = 35
		hit.offset_right = 180
		hit.offset_bottom = 55
		info.add_child(hit)
		var avoid := Label.new()
		avoid.name = "Avoid"
		avoid.text = "Avoid:"
		avoid.offset_left = 10
		avoid.offset_top = 60
		avoid.offset_right = 180
		avoid.offset_bottom = 80
		info.add_child(avoid)

func _bind_ui_nodes() -> void:
	action_menu = get_node_or_null("UILayer/UI/ActionMenu")
	attack_btn = get_node_or_null("UILayer/UI/ActionMenu/Attack")
	wait_btn = get_node_or_null("UILayer/UI/ActionMenu/Wait")
	terrain_label = get_node_or_null("UILayer/UI/Info/Terrain")
	hit_label = get_node_or_null("UILayer/UI/Info/Hit")
	avoid_label = get_node_or_null("UILayer/UI/Info/Avoid")

func _setup_demo_units() -> void:
	if units_root.get_child_count() > 0:
		return
	_spawn_unit(Vector2i(2, 2), "player")
	_spawn_unit(Vector2i(4, 3), "player")
	_spawn_unit(Vector2i(8, 5), "enemy")
	_spawn_unit(Vector2i(9, 2), "enemy")

func _sync_map_size_from_level() -> void:
	var level := get_node_or_null("LevelMap") as TileMap
	if level:
		var rect := level.get_used_rect()
		if rect.size.x > 0 and rect.size.y > 0:
			map.rebuild(rect.size)
		map.origin = level.global_position
		highlight.origin = level.global_position
		highlight.tile_size = tile_size

func _spawn_unit(cell: Vector2i, team: String) -> void:
	var u = BattleUnit.new()
	u.team = team
	u.tile_size = tile_size
	u.cell = cell
	u.z_index = cell.y
	u.moved.connect(func(_unit): _on_unit_moved(_unit))
	units_root.add_child(u)

func _unhandled_input(event: InputEvent) -> void:
	if state == State.EnemyTurn or state == State.Animating:
		return
	if event is InputEventMouseMotion:
		_update_hover()
	if event is InputEventMouseButton and event.pressed:
		if event.button_index == MOUSE_BUTTON_LEFT:
			_handle_click()
		elif event.button_index == MOUSE_BUTTON_RIGHT:
			_handle_cancel()
	if event is InputEventKey and event.pressed:
		if event.keycode == KEY_ESCAPE:
			_handle_cancel()

func _handle_cancel() -> void:
	if state == State.Attacking:
		highlight.clear_all()
		_show_action_menu(selected_unit)
		return
	if state == State.ActionMenu:
		if moved_this_turn and selected_unit:
			selected_unit.move_to(selected_origin, false)
			moved_this_turn = false
		if action_menu:
			action_menu.visible = false
		highlight.set_move_cells(reachable.keys())
		_set_state(State.UnitSelected)
		return
	if state == State.UnitSelected:
		_clear_selection()

func _handle_click() -> void:
	if state == State.ActionMenu:
		var screen_pos := _mouse_screen_pos()
		if action_menu == null or not action_menu.get_global_rect().has_point(screen_pos):
			if moved_this_turn and selected_unit:
				selected_unit.move_to(selected_origin, false)
				moved_this_turn = false
			if action_menu:
				action_menu.visible = false
			highlight.clear_all()
			_clear_selection()
		return
	var cell = map.world_to_cell(_mouse_world_pos())
	if state == State.UnitSelected and not map.is_in_bounds(cell):
		_clear_selection()
		return
	var unit = _get_unit_at(cell)
	if state == State.Idle:
		if unit and unit.team == "player" and not unit.acted:
			_select_unit(unit)
	elif state == State.UnitSelected:
		if unit and unit == selected_unit:
			highlight.clear_all()
			_show_action_menu(selected_unit)
			return
		if reachable.has(cell):
			_move_selected_to(cell)
		elif unit and unit.team == "player" and not unit.acted:
			_select_unit(unit)
		else:
			_clear_selection()
	elif state == State.Attacking:
		if unit and unit.team == "enemy" and _in_attack_range(selected_unit.cell, unit.cell):
			_attack(selected_unit, unit)
		else:
			if moved_this_turn and selected_unit:
				selected_unit.move_to(selected_origin, false)
				moved_this_turn = false
			highlight.clear_all()
			_clear_selection()

func _select_unit(unit: BattleUnit) -> void:
	selected_unit = unit
	selected_origin = unit.cell
	moved_this_turn = false
	unit_selected.emit(unit)
	_compute_reachable(unit)
	var cells: Array[Vector2i] = []
	for c in reachable.keys():
		cells.append(c)
	highlight.set_move_cells(cells)
	_set_state(State.UnitSelected)

func _compute_reachable(unit: BattleUnit) -> void:
	pathfinder.map_size = map.map_size
	pathfinder.get_cost_cb = func(c): return map.get_cost(c)
	pathfinder.is_passable_cb = func(c): return map.is_passable(c)
	pathfinder.is_blocked_cb = func(c): return _is_occupied(c, unit)
	var result = pathfinder.compute_reachable(unit.cell, unit.mov)
	reachable = result["reachable"]
	parent = result["parent"]

func _move_selected_to(cell: Vector2i) -> void:
	_set_state(State.Animating)
	var path = pathfinder.build_path(cell, parent)
	if path.is_empty():
		_set_state(State.UnitSelected)
		return
	moved_this_turn = true
	selected_unit.move_to(cell, true)

func _on_unit_moved(unit: BattleUnit) -> void:
	unit.z_index = unit.cell.y
	unit_moved.emit(unit)
	highlight.clear_all()
	if unit.team == "player" and current_team == "player":
		_show_action_menu(unit)

func _show_action_menu(unit: BattleUnit) -> void:
	if action_menu:
		action_menu.visible = true
		action_menu.global_position = map.cell_to_world(unit.cell) + Vector2(30, -20)
	_set_state(State.ActionMenu)

func _on_attack_pressed() -> void:
	if selected_unit == null:
		return
	if action_menu:
		action_menu.visible = false
	highlight.set_attack_cells(_attack_cells(selected_unit.cell))
	_set_state(State.Attacking)

func _on_wait_pressed() -> void:
	if action_menu:
		action_menu.visible = false
	if selected_unit == null:
		return
	selected_unit.wait()
	_end_player_action()

func _attack(attacker: BattleUnit, target: BattleUnit) -> void:
	_set_state(State.Animating)
	attacker.attack(target)
	unit_attacked.emit(attacker, target)
	await get_tree().create_timer(0.2).timeout
	selected_unit.wait()
	_end_player_action()

func _end_player_action() -> void:
	highlight.clear_all()
	_clear_selection()
	if _all_acted("player"):
		_start_enemy_turn()

func _clear_selection() -> void:
	selected_unit = null
	reachable = {}
	parent = {}
	moved_this_turn = false
	highlight.clear_all()
	if action_menu:
		action_menu.visible = false
	_set_state(State.Idle)

func _start_enemy_turn() -> void:
	_set_state(State.EnemyTurn)
	turn_changed.emit("enemy")
	for u in _units_by_team("enemy"):
		u.acted = false
	await _run_enemy_turn()

func _run_enemy_turn() -> void:
	for unit in _units_by_team("enemy"):
		if unit.acted:
			continue
		var target = ai.choose_attack_target(unit, _units_by_team("player"))
		if target:
			await _enemy_attack(unit, target)
			continue
		var path = ai.choose_move_towards(unit, _units_by_team("player"), _units_by_team("enemy"))
		if path.size() > 0:
			await _enemy_move(unit, path)
			target = ai.choose_attack_target(unit, _units_by_team("player"))
			if target:
				await _enemy_attack(unit, target)
		unit.acted = true
	_end_enemy_turn()

func _enemy_move(unit: BattleUnit, path: Array[Vector2i]) -> void:
	_set_state(State.Animating)
	unit.move_to(path[-1], true)
	await unit.moved
	_set_state(State.EnemyTurn)

func _enemy_attack(attacker: BattleUnit, target: BattleUnit) -> void:
	_set_state(State.Animating)
	attacker.attack(target)
	await get_tree().create_timer(0.2).timeout
	_set_state(State.EnemyTurn)

func _end_enemy_turn() -> void:
	for u in _units_by_team("player"):
		u.acted = false
	turn_changed.emit("player")
	_set_state(State.Idle)

func _attack_cells(cell: Vector2i) -> Array[Vector2i]:
	return [cell + Vector2i.UP, cell + Vector2i.DOWN, cell + Vector2i.LEFT, cell + Vector2i.RIGHT]

func _in_attack_range(a: Vector2i, b: Vector2i) -> bool:
	return abs(a.x - b.x) + abs(a.y - b.y) == 1

func _get_unit_at(cell: Vector2i) -> BattleUnit:
	for u in units_root.get_children():
		if u is BattleUnit and u.cell == cell:
			return u
	return null

func _is_occupied(cell: Vector2i, ignore: BattleUnit) -> bool:
	for u in units_root.get_children():
		if u is BattleUnit and u != ignore and u.cell == cell:
			return true
	return false

func _units_by_team(team: String) -> Array[BattleUnit]:
	var arr: Array[BattleUnit] = []
	for u in units_root.get_children():
		if u is BattleUnit and u.team == team and u.hp > 0:
			arr.append(u)
	return arr

func _all_acted(team: String) -> bool:
	for u in _units_by_team(team):
		if not u.acted:
			return false
	return true

func _update_hover() -> void:
	var cell = map.world_to_cell(_mouse_world_pos())
	highlight.set_hover_cell(cell)
	var info = map.get_terrain_info(cell)
	if terrain_label:
		terrain_label.text = "Terrain: %s" % info["name"]
	if hit_label:
		hit_label.text = "Hit: %s" % str(info["hit"])
	if avoid_label:
		avoid_label.text = "Avoid: %s" % str(info["avoid"])

func _set_state(next: State) -> void:
	state = next

func _mouse_world_pos() -> Vector2:
	var vp := get_viewport()
	var cam := vp.get_camera_2d()
	var screen_pos := vp.get_mouse_position()
	if cam:
		return cam.get_screen_to_world(screen_pos)
	return screen_pos

func _mouse_screen_pos() -> Vector2:
	return get_viewport().get_mouse_position()
