extends Node2D
class_name HighlightLayer

@export var tile_size := Vector2i(48, 48)
@export var origin := Vector2.ZERO

var _move_cells: Array = []
var _attack_cells: Array = []
var _hover_cell := Vector2i(9999, 9999)

var move_color := Color(0.2, 0.5, 0.95, 0.35)
var attack_color := Color(0.9, 0.2, 0.2, 0.35)
var hover_color := Color(1.0, 0.9, 0.2, 1.0)

func set_move_cells(cells: Array) -> void:
	_move_cells = cells.duplicate()
	queue_redraw()

func set_attack_cells(cells: Array) -> void:
	_attack_cells = cells.duplicate()
	queue_redraw()

func set_hover_cell(cell: Vector2i) -> void:
	_hover_cell = cell
	queue_redraw()

func clear_all() -> void:
	_move_cells.clear()
	_attack_cells.clear()
	_hover_cell = Vector2i(9999, 9999)
	queue_redraw()

func cell_to_world(cell: Vector2i) -> Vector2:
	return origin + Vector2(cell.x * tile_size.x, cell.y * tile_size.y)

func world_to_cell(pos: Vector2) -> Vector2i:
	var p = pos - origin
	return Vector2i(int(floor(p.x / tile_size.x)), int(floor(p.y / tile_size.y)))

func _draw() -> void:
	for cell in _move_cells:
		draw_rect(Rect2(cell_to_world(cell), Vector2(tile_size)), move_color, true)
	for cell in _attack_cells:
		draw_rect(Rect2(cell_to_world(cell), Vector2(tile_size)), attack_color, true)
	if _hover_cell.x < 9999:
		draw_rect(Rect2(cell_to_world(_hover_cell), Vector2(tile_size)), hover_color, false, 2.0)
