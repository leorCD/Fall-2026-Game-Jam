using System;
using System.Linq;
using Godot;

public partial class StaticPlayerController : CharacterBody3D
{
	public bool IsDead = false;
	public event Action Died;
	public bool IsHiding { get; private set; } = false;
	public event Action<bool> HidingChanged;
	public event Action ClosetInteracted;
	public event Action MoveFinished; // fires when the player stops moving (arrived inside, or back at the machine)

	[ExportGroup("Sound")]
	[Export] public float ForcedVolumeDropDb = -20f; // how much quieter the other buses get
	private readonly System.Collections.Generic.Dictionary<int, float> _originalBusVolumes = new();
	private Tween _audioTween;
	[Export] public AudioStream HeartbeatSFX;

	[ExportGroup("References")]
	[Export] public ChipManager Chips { get; private set; }
	[Export] public SlotMachineLogic SlotMachine { get; private set; }
	[Export] public Marker3D GamblingPosition { get; private set; }
	private Marker3D HidingPosition;
	[Export] public CanvasLayer DeadScreen;
	[Export] public Label Indicator;
	[Export] public Closet closet;

	[ExportGroup("Threat Meter")]
	[Export] private ProgressBar _threatBar;
	[Export] public float DecayRate { get; set; } = 0.15f; // per second while hiding
	public float Threat { get; private set; }
	public event Action ThreatMaxed;
	private bool _maxed;

	[ExportGroup("Camera")]
	[Export] public float max_yaw_deg = 45.0f; // maximum horizontal camera rotation
	[Export] public float max_pitch_deg = 30.0f; // maximum vertical camera rotation.
	[Export] public float smooth_speed = 8.0f; // controls how smoothly the camera moves.
	[Export] public float base_fov = 75f;
	[ExportSubgroup("Interaction")]
	[Export] public float Range = 5f;
	[Export(PropertyHint.Layers3DPhysics)] public uint Mask = 1;

	[ExportSubgroup("Hide Transition")]
	[Export] public float HideLookTime = 0.2f;  // turn toward the hiding spot
	[Export] public float HideWalkTime = 0.5f;  // move into it
	[Export] public float HideTurnTime = 0.2f;  // turn to the hiding marker's rotation
	
	private Camera3D _camera;
	public Camera3D Camera => _camera;
	private PlayerAudio _audio;
	private Vector3 _targetRotation = Vector3.Zero;
	private Tween _zoomTween;
	private Tween _moveTween;
	private bool _panLocked; // true during the hide-in: camera pans back to center and ignores the mouse

	public bool IsForcedToGamble = false;
	public float MousePullSpeed = 60f; // how strongly the camera is dragged back to center (radians/sec)

	private GodotObject _focused;











	// Runs once when the Player enters the scene.
	public override void _Ready()
	{
		SetForcedAudio(false);
		
		// chip manager
		Chips = GetNodeOrNull<ChipManager>("ChipManager");
		if (Chips == null)
		{
			GD.PrintErr("No chips component");
		}

		// camera
		_camera = GetNodeOrNull<Camera3D>("Neck/Camera3D");
		if (_camera != null)
			_camera.Fov = base_fov;
		else
			GD.PrintErr("No camera");

		// get position
		HidingPosition = closet.HiddenPosition;

		// set position
		if (GamblingPosition != null)
		{
			Position = GamblingPosition.GlobalPosition;
		}

		if (DeadScreen != null && IsDead == false) DeadScreen.Visible = false;

		Indicator.Text = "";

		SlotMachine.IdlePressureMaxed += OnIdlePressureMaxed;
		SlotMachine.SpinFinished += OnSpinFinished;

		closet.interactor.Interacted += OnClosetInteract;

	}


	// Runs every frame. delta is the amount of time since the previous frame.
	public override void _Process(double delta)
	{
		if (IsHiding && !_maxed)
			SetThreat(Threat - DecayRate * (float)delta);

		// camera logic (kept in _Process so the interaction raycast always aims from an up-to-date camera)
		if (_camera == null) return;

		Vector2I screenSize = DisplayServer.WindowGetSize();
		Vector2 screenCenter = new Vector2(screenSize.X * 0.5f, screenSize.Y * 0.5f);
		Vector2 mousePosition = !IsDead
			? DisplayServer.MouseGetPosition() - (Vector2)DisplayServer.WindowGetPosition()
			: screenCenter;

		if (IsForcedToGamble)
		{
			float pullWeight = Mathf.Clamp((float)(MousePullSpeed * delta), 0f, 1f);
			mousePosition = mousePosition.Lerp(screenCenter, pullWeight);

			Input.WarpMouse(mousePosition);
		}

		float normX = Mathf.Clamp((mousePosition.X / screenSize.X - 0.5f) * 2.0f, -1.0f, 1.0f);
		float normY = Mathf.Clamp((mousePosition.Y / screenSize.Y - 0.5f) * 2.0f, -1.0f, 1.0f);
		float targetYaw = -normX * Mathf.DegToRad(max_yaw_deg);
		float targetPitch = -normY * Mathf.DegToRad(max_pitch_deg);

		_targetRotation = _panLocked
			? Vector3.Zero // look straight along the body's facing
			: new Vector3(targetPitch, targetYaw, 0.0f);
		float interpolation = IsForcedToGamble ? 0.85f : (float)(smooth_speed * delta);

		Vector3 cameraRotation = _camera.Rotation;
		cameraRotation.X = Mathf.LerpAngle(cameraRotation.X, _targetRotation.X, interpolation);
		cameraRotation.Y = Mathf.LerpAngle(cameraRotation.Y, _targetRotation.Y, interpolation);
		_camera.Rotation = cameraRotation;

		UpdateFocus();
	}

	// Runs whenever the player sends an input event.
	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
		{
			if (!IsDead && !IsForcedToGamble)
			{
				var hit = GetObjectUnderCursor(out Vector3 pos);
				if (hit is IInteractable interactable)
				{
					interactable.Interact(this);
				}
			}
		}

	}

	private void SetHiding(bool value)
	{
		if (IsHiding == value) return;
		IsHiding = value;
		HidingChanged?.Invoke(value);
	}

	public async void MoveTo(Transform3D target)
	{
    	_panLocked = false;

		if (_moveTween != null && _moveTween.IsRunning())
			_moveTween.Kill();

		_moveTween = CreateTween();

		_moveTween.TweenProperty(this, "global_transform", target, HideWalkTime)
			.SetTrans(Tween.TransitionType.Circ)
			.SetEase(Tween.EaseType.Out);

		await ToSignal(_moveTween, Tween.SignalName.Finished);
		MoveFinished?.Invoke();
	}

	private void OnClosetInteract()
	{
		if (IsForcedToGamble || IsDead) return;
		if (GamblingPosition == null || HidingPosition == null) return;

		// ignore clicks while a move is still playing, otherwise spamming the closet restarts the sequence
		if (_moveTween != null && _moveTween.IsValid() && _moveTween.IsRunning()) return;

		bool entering = !IsHiding;
		float zoomTime = HideLookTime + HideWalkTime + HideTurnTime;

		float zoom = entering ? base_fov - 20 : base_fov;
		_zoomTween?.Kill();
		_zoomTween = CreateTween();
		_zoomTween.TweenProperty(_camera, "fov", zoom, zoomTime)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.Out);

		ClosetInteracted?.Invoke();

		if (entering)
			EnterHiding();
		else
			ExitHiding();

		SetHiding(entering);
	}

	private async void EnterHiding()
	{
		_moveTween?.Kill();
		_moveTween = CreateTween();

		_panLocked = true;
		Transform3D start = GlobalTransform;
		Transform3D hide = HidingPosition.GlobalTransform;

		// flat direction toward the hiding spot, so the body only yaws and never pitches
		Vector3 flatDir = hide.Origin - start.Origin;
		flatDir.Y = 0f;

		if (flatDir.LengthSquared() > 0.0025f) // skip the look + walk steps if the spot is basically on top of us
		{
			Basis lookBasis = Basis.LookingAt(flatDir.Normalized(), Vector3.Up);

			// 1) turn toward the hiding spot
			_moveTween.TweenProperty(this, "global_transform", new Transform3D(lookBasis, start.Origin), HideLookTime)
				.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);

			// 2) walk in, keeping that facing
			_moveTween.TweenProperty(this, "global_transform", new Transform3D(lookBasis, hide.Origin), HideWalkTime)
				.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
		}

		// 3) turn to match the hiding marker's rotation
		_moveTween.TweenProperty(this, "global_transform", hide, HideTurnTime)
			.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);

		await ToSignal(_moveTween, Tween.SignalName.Finished);
		_panLocked = false;
		MoveFinished?.Invoke();

		await ToSignal(GetTree().CreateTimer(6f), Timer.SignalName.Timeout);
		if (IsHiding == true)
			PlayHeartbeatLoop();
	}

	public async void ExitHiding()
	{
		_moveTween?.Kill();
		_moveTween = CreateTween();

		_panLocked = true;
		Transform3D start = GlobalTransform;
		Transform3D finish = GamblingPosition.GlobalTransform;

		// flat direction toward the hiding spot, so the body only yaws and never pitches
		Vector3 flatDir = finish.Origin - start.Origin;
		flatDir.Y = 0f;

		if (flatDir.LengthSquared() > 0.0025f) // skip the look + walk steps if the spot is basically on top of us
		{
			Basis lookBasis = Basis.LookingAt(flatDir.Normalized(), Vector3.Up);

			// 1) turn toward the hiding spot
			_moveTween.TweenProperty(this, "global_transform", new Transform3D(lookBasis, start.Origin), HideLookTime)
				.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);

			// 2) walk in, keeping that facing
			_moveTween.TweenProperty(this, "global_transform", new Transform3D(lookBasis, finish.Origin), HideWalkTime)
				.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
		}

		// 3) turn to match the hiding marker's rotation
		_moveTween.TweenProperty(this, "global_transform", finish, HideTurnTime)
			.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);

		await ToSignal(_moveTween, Tween.SignalName.Finished);
		_panLocked = false;
		MoveFinished?.Invoke();

		StopHeartbeatloop();
	}

	private void PlayHeartbeatLoop()
	{
		AudioManager.Instance.PlayLoop("closet_heartbeat", HeartbeatSFX, -25f, bus: "Master", parent: this);
	}
	private void StopHeartbeatloop()
	{
		AudioManager.Instance.StopLoop("closet_heartbeat");
	}




	// Returns the collider under the cursor (and the hit point), or null
	private GodotObject GetObjectUnderCursor(out Vector3 hitPosition)
	{
		hitPosition = Vector3.Zero;
		if (_camera == null) return null;

		Vector2 mousePos = GetViewport().GetMousePosition();
		Vector3 from = _camera.ProjectRayOrigin(mousePos);
		Vector3 to = from + _camera.ProjectRayNormal(mousePos) * Range;

		var query = PhysicsRayQueryParameters3D.Create(from, to, Mask);
		query.CollideWithAreas = true; // set false if you only want bodies
		query.Exclude = new Godot.Collections.Array<Rid> { GetRid() }; // ignore the player itself

		var result = GetWorld3D().DirectSpaceState.IntersectRay(query);
		if (result.Count == 0) return null;

		hitPosition = (Vector3)result["position"];
		return result["collider"].AsGodotObject();
	}

	private void UpdateFocus()
	{
		GodotObject target = null;
		if (!IsDead && !IsForcedToGamble || IsHiding)
			target = GetObjectUnderCursor(out _);

		if (target == _focused) return;

		if (GodotObject.IsInstanceValid(_focused))
		{
			(_focused as IFocusable)?.OnUnfocus();
			Indicator.Text = "";
		}

		_focused = target;
		(_focused as IFocusable)?.OnFocus();

		if (target is ClosetInteraction)
		{
			Indicator.Text = IsHiding ? "Leave" : "Hide";
		}
	}








	private void OnIdlePressureMaxed()
	{
		SetHiding(false);
		IsForcedToGamble = true;
		SetForcedAudio(true);

		_zoomTween?.Kill();
		_zoomTween = CreateTween();
		_zoomTween.TweenProperty(_camera, "fov", base_fov - 10, 2f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.Out);

		MoveTo(GamblingPosition.GlobalTransform);
	}

	private void OnSpinFinished()
	{
		IsForcedToGamble = false;
		SetForcedAudio(false); // before the death check so it always restores

		if (Chips != null && Chips.Balance <= 0)
		{
			Kill();
			return;
		}

		_zoomTween?.Kill();
		_zoomTween = CreateTween();
		_zoomTween.TweenProperty(_camera, "fov", base_fov, 0.65f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.Out);
	}

	private void SetForcedAudio(bool forced)
	{
		int forcedIdx = AudioServer.GetBusIndex("SFX_SlotMachine");

		// low-pass filters on/off
		for (int bus = 1; bus < AudioServer.BusCount; bus++) // 0 is Master
		{
			if (bus == forcedIdx) continue;

			for (int fx = 0; fx < AudioServer.GetBusEffectCount(bus); fx++)
			{
				if (AudioServer.GetBusEffect(bus, fx) is AudioEffectLowPassFilter)
					AudioServer.SetBusEffectEnabled(bus, fx, forced);
			}

			// remember the original volume once, so repeated calls don't overwrite it
			if (forced && !_originalBusVolumes.ContainsKey(bus))
				_originalBusVolumes[bus] = AudioServer.GetBusVolumeDb(bus);
		}

		if (_originalBusVolumes.Count == 0) return; // nothing to fade (e.g. the call from _Ready)

		_audioTween?.Kill();
		_audioTween = CreateTween().SetParallel(true);

		float duration = forced ? 1.0f : 0.65f;

		foreach (var pair in _originalBusVolumes)
		{
			int busIdx = pair.Key;
			float target = forced ? pair.Value + ForcedVolumeDropDb : pair.Value;

			_audioTween.TweenMethod(
				Callable.From((float db) => AudioServer.SetBusVolumeDb(busIdx, db)),
				AudioServer.GetBusVolumeDb(busIdx),
				target,
				duration
			).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
		}

		if (!forced)
			_audioTween.Finished += () => _originalBusVolumes.Clear();
	}

	public void ChangeIndicatorText(string newText)
	{
		Indicator.Text = newText;
	}






	public void AddThreat(float amount)
	{
		if (_maxed) return;
		SetThreat(Threat + amount);
	}
	private void SetThreat(float value)
	{
		Threat = Mathf.Clamp(value, 0f, 1f);
		if (_threatBar != null)
			_threatBar.Value = Threat * _threatBar.MaxValue;

		if (Threat >= 1f && !_maxed)
		{
			_maxed = true;
			ThreatMaxed?.Invoke(); // fires once
		}
	}










	public void Kill()
	{
		if (IsDead) return;
		IsDead = true;
		YouLoseScreen();
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