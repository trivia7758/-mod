extends Node
class_name BattleGridMap

@export var map_size := Vector2i(12, 8)
@export var tile_size := Vector2i(48, 48)
@export var origin := Vector2.ZERO

const TERRAIN_COSTS := {
	"road": 1,
	"plain": 1,
	"forest": 2,
	"water": -1,
	"mountain": -1
}

var _terrain := {}

func _ready() -> void:
	_build_demo_map()

func _build_demo_map() -> void:
	_terrain.clear()
	for y in range(map_size.y):
		for x in range(map_size.x):
			var t = "plain"
			if y == 3:
				t = "road"
			if x in [2, 3, 4] and y in [1, 2]:
				t = "forest"
			if x in [7, 8] and y in [5, 6]:
				t = "water"
			if x == 10 and y in [2, 3, 4]:
				t = "mountain"
			_terrain[Vector2i(x, y)] = t

func rebuild(new_size: Vector2i) -> void:
	map_size = new_size
	_build_demo_map()

func is_in_bounds(cell: Vector2i) -> bool:
	return cell.x >= 0 and cell.y >= 0 and cell.x < map_size.x and cell.y < map_size.y

func get_terrain(cell: Vector2i) -> String:
	return _terrain.get(cell, "plain")

func is_passable(cell: Vector2i) -> bool:
	if not is_in_bounds(cell):
		return false
	var cost = TERRAIN_COSTS.get(get_terrain(cell), 1)
	return cost > 0

func get_cost(cell: Vector2i) -> int:
	return TERRAIN_COSTS.get(get_terrain(cell), 1)

func cell_to_world(cell: Vector2i) -> Vector2:
	return origin + Vector2(cell.x * tile_size.x + tile_size.x * 0.5, cell.y * tile_size.y + tile_size.y * 0.5)

func world_to_cell(pos: Vector2) -> Vector2i:
	var p = pos - origin
	return Vector2i(int(floor(p.x / tile_size.x)), int(floor(p.y / tile_size.y)))

func get_terrain_info(cell: Vector2i) -> Dictionary:
	var t = get_terrain(cell)
	return {
		"name": t,
		"hit": 0,
		"avoid": 0
	}
