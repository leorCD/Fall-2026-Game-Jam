using System;
using System.Dynamic;
using Godot;

public partial class StaticPlayerController : CharacterBody3D
{
	public bool IsDead = false;
	public bool IsHiding = false;

	public event Action Died;



	[ExportCategory("References")]
	[Export] public ChipManager Chips {get; private set;}
	[Export] public SlotMachineLogic SlotMachine {get; private set;}
	[Export] public Marker3D GamblingPosition { get; private set; }
	[Export] public Marker3D HidingPosition { get; private set; }
	[Export] public CanvasLayer DeadScreen;

	[ExportCategory("Camera")]
	[Export] public float max_yaw_deg = 45.0f; // maximum horizontal camera rotation
	[Export] public float max_pitch_deg = 15.0f; // maximum vertical camera rotation.
	[Export] public float smooth_speed = 8.0f; // controls how smoothly the camera moves.
	[Export] public float base_fov = 75f;

	private Camera3D _camera;
	private Vector3 _targetRotation = Vector3.Zero;
	private Tween _zoomTween;
	private Tween _moveTween;

    public bool IsForcedToGamble = false;
    public float MousePullSpeed = 60f; // how strongly the camera is dragged back to center (radians/sec)





	// Runs once when the Player enters the scene.
	public override void _Ready()
	{
		Chips = GetNodeOrNull<ChipManager>("ChipManager");

		_camera = GetNodeOrNull<Camera3D>("Neck/Camera3D");


		if (_camera != null)
			_camera.Fov = base_fov;
		else
			GD.PrintErr("No camera");

		if (GamblingPosition != null)
		{
			Position = GamblingPosition.GlobalPosition;
		}

		SlotMachine.IdlePressureMaxed += () => // when the player's temptation reaches peak, force them into the gambling position
		{
			IsHiding = false;
			IsForcedToGamble = true;

			_zoomTween?.Kill();
			_zoomTween = CreateTween();
			_zoomTween.TweenProperty(_camera, "fov", base_fov-10, 2f)
				.SetTrans(Tween.TransitionType.Sine)
				.SetEase(Tween.EaseType.Out);

			MoveTo(GamblingPosition.GlobalTransform);
		};
		SlotMachine.SpinFinished += () => // on spin finished, return player input
		{
			IsForcedToGamble = false;

			if (Chips != null && Chips.Balance <= 0)
			{
				IsDead = true;
				YouLoseScreen();
				return;
			}
			
			_zoomTween?.Kill();
			_zoomTween = CreateTween();
			_zoomTween.TweenProperty(_camera, "fov", base_fov, 0.65f)
				.SetTrans(Tween.TransitionType.Sine)
				.SetEase(Tween.EaseType.Out);
		};
	}

	// Runs every frame. delta is the amount of time since the previous frame.
	public override void _Process(double delta)
	{
		if (_camera == null) return;
		
		Vector2I screenSize = DisplayServer.WindowGetSize();
		Vector2 screenCenter = new Vector2(screenSize.X * 0.5f, screenSize.Y * 0.5f);
		Vector2 mousePosition = !IsDead 
			? DisplayServer.MouseGetPosition() - (Vector2)DisplayServer.WindowGetPosition()
			: screenCenter;
		
		// 		old : for some reason would not fix itself when the screen size was readjusted
		// Vector2 screenSize = GetViewport().GetVisibleRect().Size;
		// Vector2 screenCenter = screenSize * 0.5f;
		// Vector2 mousePosition = !IsDead ? GetViewport().GetMousePosition() : screenCenter;

		if (IsForcedToGamble)
        {
			float pullWeight = Mathf.Clamp((float)(MousePullSpeed * delta), 0f, 1f);
			mousePosition = mousePosition.Lerp(screenCenter, pullWeight);

			Input.WarpMouse(mousePosition);
        }

		float normX = (mousePosition.X / screenSize.X - 0.5f) * 2.0f;
		float normY = (mousePosition.Y / screenSize.Y - 0.5f) * 2.0f;
		normX = Mathf.Clamp(normX, -1.0f, 1.0f);
		normY = Mathf.Clamp(normY, -1.0f, 1.0f);
		float targetYaw = -normX * Mathf.DegToRad(max_yaw_deg);
		float targetPitch = -normY * Mathf.DegToRad(max_pitch_deg);


		_targetRotation = new Vector3(targetPitch, targetYaw, 0.0f);
		float interpolation = IsForcedToGamble ? 0.85f : (float)(smooth_speed * delta);
		
		Vector3 cameraRotation = _camera.Rotation;
		cameraRotation.X = Mathf.LerpAngle(cameraRotation.X, _targetRotation.X, interpolation);
		cameraRotation.Y = Mathf.LerpAngle(cameraRotation.Y, _targetRotation.Y, interpolation);
		_camera.Rotation = cameraRotation;
	}

	// Runs whenever the player sends an input event.
	public override void _UnhandledInput(InputEvent @event)
	{
		if (IsForcedToGamble || IsDead) return;
		if (!@event.IsActionPressed("Spacebar")) return;
		if (GamblingPosition == null || HidingPosition == null) return;


		Transform3D targetTransform = IsHiding
			? GamblingPosition.GlobalTransform
			: HidingPosition.GlobalTransform;
		
		MoveTo(targetTransform);

		IsHiding = !IsHiding;
	}

	public async void MoveTo(Transform3D target)
	{
		if (_moveTween != null && _moveTween.IsRunning())
			_moveTween.Kill();

		_moveTween = CreateTween();

		_moveTween.TweenProperty(this, "global_transform", target, 0.5f)
			.SetTrans(Tween.TransitionType.Circ)
			.SetEase(Tween.EaseType.Out);
		
		await ToSignal(_moveTween, Tween.SignalName.Finished);
	}









	private void YouLoseScreen()
	{
		if (DeadScreen == null) return;
		Button RestartGameButton = DeadScreen.GetNode<Button>("Button");
		if (RestartGameButton == null) return;

		DeadScreen.Visible = true;
		Died?.Invoke();
		
		RestartGameButton.Pressed += () =>
		{
			GetTree().ReloadCurrentScene();
		};
	}
}
