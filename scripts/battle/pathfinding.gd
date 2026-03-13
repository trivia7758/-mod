extends Node
class_name BattlePathfinding

@export var map_size := Vector2i(12, 8)

var get_cost_cb: Callable
var is_passable_cb: Callable
var is_blocked_cb: Callable

func compute_reachable(start: Vector2i, mov_points: int) -> Dictionary:
	var reachable := {}
	var parent := {}
	var frontier := [start]
	var cost_so_far := {start: 0}
	reachable[start] = mov_points
	parent[start] = null

	while frontier.size() > 0:
		var current = _pop_lowest_cost(frontier, cost_so_far)
		for n in _neighbors(current):
			if not _in_bounds(n):
				continue
			if is_blocked_cb and is_blocked_cb.call(n):
				continue
			if is_passable_cb and not is_passable_cb.call(n):
				continue
			var step_cost = get_cost_cb.call(n) if get_cost_cb else 1
			var new_cost = cost_so_far[current] + step_cost
			if new_cost > mov_points:
				continue
			if not cost_so_far.has(n) or new_cost < cost_so_far[n]:
				cost_so_far[n] = new_cost
				reachable[n] = mov_points - new_cost
				parent[n] = current
				if not frontier.has(n):
					frontier.append(n)
	return {"reachable": reachable, "parent": parent}

func build_path(target: Vector2i, parent: Dictionary) -> Array[Vector2i]:
	var path: Array[Vector2i] = []
	var current = target
	while current != null and parent.has(current):
		path.append(current)
		current = parent[current]
	path.reverse()
	if path.size() > 0 and path[0] == path[-1]:
		path.pop_front()
	return path

func _neighbors(cell: Vector2i) -> Array[Vector2i]:
	return [
		cell + Vector2i.UP,
		cell + Vector2i.DOWN,
		cell + Vector2i.LEFT,
		cell + Vector2i.RIGHT
	]

func _in_bounds(cell: Vector2i) -> bool:
	return cell.x >= 0 and cell.y >= 0 and cell.x < map_size.x and cell.y < map_size.y

func _pop_lowest_cost(frontier: Array, cost_so_far: Dictionary) -> Vector2i:
	var best = frontier[0]
	var best_cost = cost_so_far.get(best, 999999)
	for c in frontier:
		var ccost = cost_so_far.get(c, 999999)
		if ccost < best_cost:
			best_cost = ccost
			best = c
	frontier.erase(best)
	return best

func _ready() -> void:
	if get_cost_cb == null:
		return
	var res = compute_reachable(Vector2i(0, 0), 3)
	print("pathfinding test reachable size: ", res["reachable"].size())
