extends CharacterBody3D


@onready var HiddenPosition : Marker3D = $"../StaticPlayerPositions/Hidden"
@onready var BasePosition : Marker3D = $"../StaticPlayerPositions/Base"

@export var turn_duration: float = 0.15 # time to complete the turn
var target_angle_y: float = 0.0
var tween: Tween

var _facing : Array = ["Slot", "n/a", "Hide", "n/a"]
var _findex : int = 0

var _hiding : bool = false

func _ready() -> void:
	Input.mouse_mode = Input.MOUSE_MODE_VISIBLE
	target_angle_y = rotation_degrees.y
	global_position = BasePosition.global_position

func _unhandled_input(event: InputEvent) -> void:
	if _hiding == false:
		# turn left
		if event.is_action_pressed("MoveLeft"):
			target_angle_y += 90.0
			
			_findex += 1
			if _findex > _facing.size() - 1:
				_findex = 0
			
			animate_rotation()
			
		# turn right
		elif event.is_action_pressed("MoveRight"):
			target_angle_y -= 90.0
			
			_findex -= 1
			if _findex < 0:
				_findex = _facing.size() - 1
			
			animate_rotation()
		
		elif event.is_action_pressed("Spacebar"):
			if (_facing[_findex] == "Hide"):
				_hiding = true
				target_angle_y = 0.0
				_findex = 0
				animate_rotation()
				
				var newtween = create_tween()
				newtween.tween_property(self, "global_position", HiddenPosition.position, 0.5)\
					.set_trans(Tween.TRANS_CIRC)\
					.set_ease(Tween.EASE_OUT)
				
			elif (_facing[_findex] == "Slot"):
				print("gambling")
			else:
				print("nothing here")
	else: # are hiding
		if event.is_action_pressed("Spacebar"):
			_hiding = false
			
			var newtween = create_tween()
			newtween.tween_property(self, "global_position", BasePosition.position, 0.5)\
				.set_trans(Tween.TRANS_CIRC)\
				.set_ease(Tween.EASE_OUT)
			


func animate_rotation() -> void:
	if tween and tween.is_running():
		tween.kill()
		
	tween = create_tween()
	tween.set_trans(Tween.TRANS_QUAD)
	tween.set_ease(Tween.EASE_OUT)
	tween.tween_property(self, "rotation_degrees:y", target_angle_y, turn_duration)
