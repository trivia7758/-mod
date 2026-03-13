extends Node
class_name EnemyAI

var map: BattleGridMap
var pathfinder: BattlePathfinding

func choose_attack_target(unit: BattleUnit, players: Array[BattleUnit]) -> BattleUnit:
	var in_range := []
	for p in players:
		if _manhattan(unit.cell, p.cell) == 1:
			in_range.append(p)
	if in_range.is_empty():
		return null
	in_range.sort_custom(func(a, b): return a.hp < b.hp)
	return in_range[0]

func choose_move_towards(unit: BattleUnit, players: Array[BattleUnit], enemies: Array[BattleUnit]) -> Array[Vector2i]:
	if players.is_empty():
		return []
	var closest = players[0]
	var best_d = _manhattan(unit.cell, closest.cell)
	for p in players:
		var d = _manhattan(unit.cell, p.cell)
		if d < best_d:
			best_d = d
			closest = p
	pathfinder.get_cost_cb = func(c): return map.get_cost(c)
	pathfinder.is_passable_cb = func(c): return map.is_passable(c)
	pathfinder.is_blocked_cb = func(c): return _is_occupied(c, unit, players, enemies)
	pathfinder.map_size = map.map_size
	var result = pathfinder.compute_reachable(unit.cell, unit.mov)
	var reachable: Dictionary = result["reachable"]
	var parent: Dictionary = result["parent"]
	var best_cell = unit.cell
	var best_dist = _manhattan(unit.cell, closest.cell)
	for c in reachable.keys():
		var d = _manhattan(c, closest.cell)
		if d < best_dist:
			best_dist = d
			best_cell = c
		elif d == best_dist:
			if reachable[c] > reachable[best_cell]:
				best_cell = c
	var path = pathfinder.build_path(best_cell, parent)
	return path

func _manhattan(a: Vector2i, b: Vector2i) -> int:
	return abs(a.x - b.x) + abs(a.y - b.y)

func _is_occupied(cell: Vector2i, self_unit: BattleUnit, players: Array[BattleUnit], enemies: Array[BattleUnit]) -> bool:
	for p in players:
		if p != self_unit and p.cell == cell and p.hp > 0:
			return true
	for e in enemies:
		if e != self_unit and e.cell == cell and e.hp > 0:
			return true
	return false
