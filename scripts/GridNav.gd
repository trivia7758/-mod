extends Node
class_name GridNav

@export var grid_size := Vector2i(40, 30)
@export var cell_size := Vector2i(48, 48)
@export var grid_origin := Vector2i.ZERO

var astar := AStarGrid2D.new()

func _ready() -> void:
	setup(grid_size, cell_size, grid_origin)

func setup(new_grid_size: Vector2i, new_cell_size: Vector2i, new_origin: Vector2i = Vector2i.ZERO) -> void:
	grid_size = new_grid_size
	cell_size = new_cell_size
	grid_origin = new_origin
	astar.region = Rect2i(grid_origin, grid_size)
	astar.cell_size = Vector2(float(cell_size.x), float(cell_size.y))
	astar.diagonal_mode = AStarGrid2D.DIAGONAL_MODE_NEVER
	astar.default_compute_heuristic = AStarGrid2D.HEURISTIC_MANHATTAN
	astar.default_estimate_heuristic = AStarGrid2D.HEURISTIC_MANHATTAN
	astar.update()

func set_blocked(tile: Vector2i, blocked: bool) -> void:
	astar.set_point_solid(tile, blocked)

func find_path(from: Vector2i, to: Vector2i) -> Array[Vector2i]:
	if not astar.region.has_point(from) or not astar.region.has_point(to):
		return []
	if from == to:
		return []
	var path = astar.get_id_path(from, to)
	var result: Array[Vector2i] = []
	for p in path:
		result.append(p)
	if not result.is_empty() and result[0] == from:
		result.remove_at(0)
	return result
