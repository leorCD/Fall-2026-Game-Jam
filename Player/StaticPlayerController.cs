using System.Dynamic;
using Godot;

public partial class StaticPlayerController : CharacterBody3D
{
	[Export] public bool InputEnabled = true;
	public bool IsHiding = false;




	[ExportCategory("References")]
	[Export] public ChipManager Chips {get; private set;}
	[Export] public SlotMachineLogic SlotMachine {get; private set;}

	[Export] public Marker3D StartingPosition { get; private set; }
	[Export] public Marker3D GamblingPosition { get; private set; }
	[Export] public Marker3D HidingPosition { get; private set; }
	[Export] public CanvasLayer DeadScreen;





	[ExportCategory("Camera")]

	// Maximum horizontal camera rotation
	[Export] public float max_yaw_deg = 45.0f;

	// Maximum vertical camera rotation.
	[Export] public float max_pitch_deg = 15.0f;

	// Controls how smoothly the camera moves.
	[Export] public float smooth_speed = 8.0f;

	private Camera3D _camera;
	private Vector3 _targetRotation = Vector3.Zero;
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

		SlotMachine.IdlePressureMaxed += () => // when the player's temptation reaches peak, force them into the gambling position
		{
			IsHiding = false;

			InputEnabled = false;
			MoveTo(GamblingPosition.GlobalTransform);
		};
		SlotMachine.SpinFinished += () => // on spin finished, return player input
		{
			InputEnabled = true;

			if (Chips != null && Chips.Balance <= 0)
			{
				InputEnabled = false;
				YouLoseScreen();
			}
		};
	}

	// Runs every frame. delta is the amount of time since the previous frame.
	public override void _Process(double delta)
	{
		if (_camera == null) return;

		Vector2 screenSize = GetViewport().GetVisibleRect().Size;
		Vector2 mousePosition = InputEnabled ? GetViewport().GetMousePosition() : new Vector2(screenSize.X / 2, screenSize.Y / 2);

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
		if (!InputEnabled) return;
		if (!@event.IsActionPressed("Spacebar")) return;
		if (GamblingPosition == null || HidingPosition == null) return;

		IsHiding = !IsHiding;

		Transform3D targetTransform = !IsHiding
			? GamblingPosition.GlobalTransform
			: HidingPosition.GlobalTransform;
		
		MoveTo(targetTransform);
	}

	public void MoveTo(Transform3D target)
	{
		if (_tween != null && _tween.IsRunning())
			_tween.Kill();

		_tween = CreateTween();

		_tween.TweenProperty(this, "global_transform", target, 0.5f)
			.SetTrans(Tween.TransitionType.Circ)
			.SetEase(Tween.EaseType.Out);
	}









	private void YouLoseScreen()
	{
		if (DeadScreen == null) return;
		Button RestartGameButton = DeadScreen.GetNode<Button>("Button");
		if (RestartGameButton == null) return;

		DeadScreen.Visible = true;
		RestartGameButton.Pressed += () =>
		{
			GetTree().ReloadCurrentScene();
		};
	}
}
