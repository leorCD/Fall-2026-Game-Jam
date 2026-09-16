extends CharacterBody3D

@onready var camera : Camera3D = $Neck/Camera3D

@onready var SlotMachinePosition : Marker3D = $"../StaticPlayerPositions/SlotMachine"
@onready var HiddenPosition : Marker3D = $"../StaticPlayerPositions/Hidden"



@export var max_yaw_deg: float = 45.0    # horizontal look limit
@export var max_pitch_deg: float = 15.0  # vertical look limit
@export var smooth_speed: float = 8.0    # how quickly the view follows the mouse

var _target_rotation : Vector3 = Vector3.ZERO

func _process(delta: float) -> void:
	if !camera:
		return

	var viewport := get_viewport()
	var mouse_pos := viewport.get_mouse_position()
	var screen_size := viewport.get_visible_rect().size

	# normalize mouse position to range [-1.0, 1.0] from screen center
	var norm_x: float = (mouse_pos.x / screen_size.x - 0.5) * 2.0
	var norm_y: float = (mouse_pos.y / screen_size.y - 0.5) * 2.0

	# clamp to ensure fast flicks off-window don't overshoot
	norm_x = clampf(norm_x, -1.0, 1.0)
	norm_y = clampf(norm_y, -1.0, 1.0)

	# calculate target angles in radians
	var target_yaw := -norm_x * deg_to_rad(max_yaw_deg)
	var target_pitch := -norm_y * deg_to_rad(max_pitch_deg)

	_target_rotation = Vector3(target_pitch, target_yaw, 0.0)

	# smoothly interpolate rotation toward target
	camera.rotation.x = lerp_angle(camera.rotation.x, _target_rotation.x, smooth_speed * delta)
	camera.rotation.y = lerp_angle(camera.rotation.y, _target_rotation.y, smooth_speed * delta)

var _hiding : bool = false
var tween: Tween
func _unhandled_input(event: InputEvent) -> void:
	if (event.is_action_pressed("Spacebar")):
		_hiding = !_hiding
		
		var target_pos = SlotMachinePosition.global_position if _hiding else HiddenPosition.global_position
		
		# Kill any existing tween to prevent movement conflicts
		if tween and tween.is_running():
			tween.kill()
			
		# Create and run the new interpolation animation
		tween = create_tween()
		tween.tween_property(self, "global_position", target_pos, 0.5)\
			.set_trans(Tween.TRANS_CIRC)\
			.set_ease(Tween.EASE_OUT)
