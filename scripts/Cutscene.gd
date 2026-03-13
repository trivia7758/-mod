extends Node
class_name Cutscene

signal finished

var _running := false
var _input_prev := true
var _unhandled_prev := true

func run(steps: Array[Callable]) -> void:
	if _running:
		return
	_running = true
	_lock_input(true)
	for step in steps:
		await step.call()
	_lock_input(false)
	_running = false
	emit_signal("finished")

func _lock_input(lock: bool) -> void:
	return

func step_move_to(unit: Unit, nav: GridNav, target_tile: Vector2i) -> Callable:
	return func():
		var path = nav.find_path(unit.tile, target_tile)
		if path.is_empty():
			return
		unit.move_along(path)
		await unit.reached_tile

func step_face(unit: Unit, dir: Vector2i) -> Callable:
	return func():
		unit.face_dir(dir)

func step_say(box: DialogueBox, unit: Unit, text: String, name: String = "", portrait_path: String = "", side: String = "left") -> Callable:
	return func():
		await box.show_line(unit, text, name, portrait_path, side)

func step_wait(seconds: float) -> Callable:
	return func():
		await get_tree().create_timer(seconds).timeout
