using Godot;

public partial class StaticPlayerController : CharacterBody3D
{
	// ChipManager reference
	[Export] public ChipManager Chips {get; private set;}

	[Export] public Marker3D StartingPosition { get; private set; }
	[Export] public Marker3D GamblingPosition { get; private set; }
	[Export] public Marker3D HidingPosition { get; private set; }

	// Maximum horizontal camera rotation
	[Export] public float max_yaw_deg = 45.0f;

	// Maximum vertical camera rotation.
	[Export] public float max_pitch_deg = 15.0f;

	// Controls how smoothly the camera moves.
	[Export] public float smooth_speed = 8.0f;

	private Camera3D _camera;
	private Vector3 _targetRotation = Vector3.Zero;
	private bool _hiding = false;
	private Tween _tween;

	// Runs once when the Player enters the scene.
	public override void _Ready()
	{
		Chips = GetNodeOrNull<ChipManager>("ChipManager");

		_camera = GetNodeOrNull<Camera3D>("Neck/Camera3D");

		if (StartingPosition != null)
		{
			Position = StartingPosition.GlobalPosition;
		}
	}

	// Runs every frame. delta is the amount of time since the previous frame.
	public override void _Process(double delta)
	{
		if (_camera == null)
		{
			return;
		}

		Vector2 mousePosition = GetViewport().GetMousePosition();
		Vector2 screenSize = GetViewport().GetVisibleRect().Size;

		float normX = (mousePosition.X / screenSize.X - 0.5f) * 2.0f;
		float normY = (mousePosition.Y / screenSize.Y - 0.5f) * 2.0f;

		normX = Mathf.Clamp(normX, -1.0f, 1.0f);
		normY = Mathf.Clamp(normY, -1.0f, 1.0f);

		float targetYaw = -normX * Mathf.DegToRad(max_yaw_deg);
		float targetPitch = -normY * Mathf.DegToRad(max_pitch_deg);

		_targetRotation = new Vector3(targetPitch, targetYaw, 0.0f);

		float interpolation = (float)(smooth_speed * delta);

		Vector3 cameraRotation = _camera.Rotation;
		cameraRotation.X = Mathf.LerpAngle(
			cameraRotation.X,
			_targetRotation.X,
			interpolation
		);
		cameraRotation.Y = Mathf.LerpAngle(
			cameraRotation.Y,
			_targetRotation.Y,
			interpolation
		);

		_camera.Rotation = cameraRotation;
	}

	// Runs whenever the player sends an input event.
	public override void _UnhandledInput(InputEvent @event)
	{
		if (!@event.IsActionPressed("Spacebar"))
		{
			return;
		}

		_hiding = !_hiding;

		if (GamblingPosition == null || HidingPosition == null)
		{
			return;
		}

		Transform3D targetTransform = !_hiding
			? GamblingPosition.GlobalTransform
			: HidingPosition.GlobalTransform;

		if (_tween != null && _tween.IsRunning())
		{
			_tween.Kill();
		}

		_tween = CreateTween();

		_tween
			.TweenProperty(this, "global_transform", targetTransform, 0.5f)
			.SetTrans(Tween.TransitionType.Circ)
			.SetEase(Tween.EaseType.Out);
	}
}
