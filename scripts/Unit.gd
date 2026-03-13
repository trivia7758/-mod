extends Node2D
class_name Unit

signal reached_tile

const LOGIC_FPS := 60

@export var step_frames: int = 10
@export var anim_tick: int = 2
@export var tile_size := Vector2i(48, 48)
@export var foot_offset := Vector2(0, -8)
@export var is_isometric := true
@export var grid_origin := Vector2.ZERO

@onready var sprite: AnimatedSprite2D = $Sprite

var tile: Vector2i = Vector2i.ZERO

var _moving := false
var _dir := Vector2i.DOWN
var _path: Array[Vector2i] = []
var _path_index := 0

var _start_world := Vector2.ZERO
var _target_world := Vector2.ZERO
var _progress_frame := 0
var _accumulator := 0.0
var _logic_step := 1.0 / float(LOGIC_FPS)
var _anim_counter := 0
var _walk_anim := ""

func _ready() -> void:
	if tile == Vector2i.ZERO:
		tile = world_to_tile(global_position)
	global_position = tile_to_world(tile)
	_play_idle(_dir)

func _process(delta: float) -> void:
	if not _moving:
		return
	_accumulator += delta
	while _accumulator >= _logic_step:
		_accumulator -= _logic_step
		_step_logic_frame()

func world_to_tile(pos: Vector2) -> Vector2i:
	var p = pos - grid_origin
	if not is_isometric:
		return Vector2i(round(p.x / tile_size.x), round(p.y / tile_size.y))
	var half_w = tile_size.x * 0.5
	var half_h = tile_size.y * 0.5
	var x = (p.x / half_w + p.y / half_h) * 0.5
	var y = (p.y / half_h - p.x / half_w) * 0.5
	return Vector2i(round(x), round(y))

func tile_to_world(t: Vector2i) -> Vector2:
	if not is_isometric:
		return Vector2(t.x * tile_size.x, t.y * tile_size.y) + foot_offset + grid_origin
	var half_w = tile_size.x * 0.5
	var half_h = tile_size.y * 0.5
	var wx = (t.x - t.y) * half_w
	var wy = (t.x + t.y) * half_h
	return Vector2(wx, wy) + foot_offset + grid_origin

func face_dir(dir: Vector2i) -> void:
	_dir = _clamp_dir(dir)
	_play_idle(_dir)

func move_along(path: Array[Vector2i]) -> void:
	if _moving:
		return
	if path.is_empty():
		emit_signal("reached_tile")
		return
	_path = path
	_path_index = 0
	_begin_step()
	_moving = true

func _begin_step() -> void:
	if _path_index >= _path.size():
		_finish_move()
		return
	var next_tile = _path[_path_index]
	var dir = next_tile - tile
	_dir = _clamp_dir(dir)
	_start_world = global_position
	_target_world = tile_to_world(next_tile)
	_progress_frame = 0
	_anim_counter = 0
	_play_walk(_dir)

func _step_logic_frame() -> void:
	if not _moving:
		return
	_progress_frame += 1
	var t = float(_progress_frame) / float(step_frames)
	global_position = _start_world.lerp(_target_world, t)
	_tick_walk_anim()
	if _progress_frame >= step_frames:
		global_position = _target_world
		tile = _path[_path_index]
		_path_index += 1
		_begin_step()

func _finish_move() -> void:
	_moving = false
	_play_idle(_dir)
	emit_signal("reached_tile")

func _clamp_dir(dir: Vector2i) -> Vector2i:
	if abs(dir.x) > abs(dir.y):
		return Vector2i.RIGHT if dir.x > 0 else Vector2i.LEFT
	return Vector2i.DOWN if dir.y > 0 else Vector2i.UP

func _play_walk(dir: Vector2i) -> void:
	_walk_anim = _resolve_walk_anim(dir)
	if sprite.sprite_frames and sprite.sprite_frames.has_animation(_walk_anim):
		sprite.animation = _walk_anim
		sprite.frame = 0
		sprite.speed_scale = 0.0

func _tick_walk_anim() -> void:
	if _walk_anim == "" or not sprite.sprite_frames:
		return
	_anim_counter += 1
	if _anim_counter % max(1, anim_tick) != 0:
		return
	var count = sprite.sprite_frames.get_frame_count(_walk_anim)
	if count <= 0:
		return
	sprite.frame = (sprite.frame + 1) % count

func _play_idle(dir: Vector2i) -> void:
	var anim = _resolve_idle_anim(dir)
	if sprite.sprite_frames and sprite.sprite_frames.has_animation(anim):
		sprite.speed_scale = 1.0
		sprite.play(anim)

func play_talk() -> void:
	var anim = "talk_" + _dir_to_name(_dir)
	if sprite.sprite_frames and sprite.sprite_frames.has_animation(anim):
		sprite.speed_scale = 1.0
		sprite.play(anim)
	else:
		_play_idle(_dir)

func stop_talk() -> void:
	_play_idle(_dir)

func _dir_to_name(dir: Vector2i) -> String:
	if dir == Vector2i.UP:
		return "up"
	if dir == Vector2i.DOWN:
		return "down"
	if dir == Vector2i.LEFT:
		return "left"
	return "right"

func _resolve_walk_anim(dir: Vector2i) -> String:
	if is_isometric:
		return _resolve_iso_anim(dir, "walk")
	var name = "walk_" + _dir_to_name(dir)
	if sprite.sprite_frames and sprite.sprite_frames.has_animation(name):
		_apply_flip(dir)
		return name
	var fallback = "walk_down" if dir == Vector2i.LEFT or dir == Vector2i.RIGHT or dir == Vector2i.DOWN else "walk_up"
	_apply_flip(dir)
	return fallback

func _resolve_idle_anim(dir: Vector2i) -> String:
	if is_isometric:
		return _resolve_iso_anim(dir, "idle")
	var name = "idle_" + _dir_to_name(dir)
	if sprite.sprite_frames and sprite.sprite_frames.has_animation(name):
		_apply_flip(dir)
		return name
	var fallback = "idle_down" if dir == Vector2i.LEFT or dir == Vector2i.RIGHT or dir == Vector2i.DOWN else "idle_up"
	_apply_flip(dir)
	return fallback

func _apply_flip(dir: Vector2i) -> void:
	if sprite == null:
		return
	sprite.flip_h = (dir == Vector2i.RIGHT)

func _resolve_iso_anim(dir: Vector2i, base: String) -> String:
	# Logical grid directions -> isometric screen directions
	# RIGHT (+x) = down-right (front, flip)
	# LEFT (-x) = up-left (back, flip)
	# DOWN (+y) = down-left (front)
	# UP (-y) = up-right (back)
	if sprite == null:
		return base + "_down"
	if dir == Vector2i.RIGHT:
		sprite.flip_h = true
		return base + "_down"
	if dir == Vector2i.LEFT:
		sprite.flip_h = true
		return base + "_up"
	if dir == Vector2i.DOWN:
		sprite.flip_h = false
		return base + "_down"
	if dir == Vector2i.UP:
		sprite.flip_h = false
		return base + "_up"
	sprite.flip_h = false
	return base + "_down"
