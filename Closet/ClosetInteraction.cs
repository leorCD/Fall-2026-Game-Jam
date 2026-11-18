using Godot;
using System;

public partial class ClosetInteraction : Area3D, IInteractable
{
	public event Action Interacted;

	[Export] private Node3D LeftHinge;
	[Export] private Node3D RightHinge;

	[ExportGroup("SFX")]
	[Export] public AudioStream DoorCloseOpen;

	[ExportGroup("Doors")]
	[Export] public float OpenAngleDeg = 110f;
	[Export] public float OpenTime = 0.2f;
	[Export] public float CloseTime = 0.15f;

	private float _leftClosedY;
	private float _rightClosedY;
	private Tween _doorTween;
	private StaticPlayerController _player;

	public override void _Ready()
	{
		// remember the closed angles so the doors return to exactly where you placed them
		if (LeftHinge != null) _leftClosedY = LeftHinge.RotationDegrees.Y;
		if (RightHinge != null) _rightClosedY = RightHinge.RotationDegrees.Y;
	}

	public void Interact(StaticPlayerController player)
	{
		// subscribe once, before invoking, so the first hide already opens the doors
		if (_player == null)
		{
			_player = player;
			_player.HidingChanged += SetOpen;
			_player.MoveFinished += OnMoveFinished;
		}

		Interacted?.Invoke();
	}

	private void OnHidingChanged(bool hiding)
	{
		SetOpen(true);   // entering or leaving: open up
		AudioManager.Instance.PlaySfxClip(DoorCloseOpen, 0.8f, 2f, pitch: 1.1f, volumeDb: -8f, bus: "Ambience", parent: this);
	}
	private void OnMoveFinished()
	{
		SetOpen(false);  // player has settled: shut the doors
		AudioManager.Instance.PlaySfxClip(DoorCloseOpen, 0.8f, 2f, pitch: 0.8f, volumeDb: -15f, bus: "Ambience", parent: this);
	}

	public async void SetOpen(bool open)
	{
		_doorTween?.Kill();
		_doorTween = CreateTween();

		float time = open ? OpenTime : CloseTime;
		float leftTarget = _leftClosedY + (open ? -OpenAngleDeg : 0f);
		float rightTarget = _rightClosedY + (open ? OpenAngleDeg : 0f);

		if (LeftHinge != null)
			_doorTween.Parallel().TweenProperty(LeftHinge, "rotation_degrees:y", leftTarget, time)
				.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);

		if (RightHinge != null)
			_doorTween.Parallel().TweenProperty(RightHinge, "rotation_degrees:y", rightTarget, time)
				.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
		
		await ToSignal(_doorTween, Tween.SignalName.Finished);
	}

	public override void _ExitTree()
	{
		if (_player == null) return;
		_player.HidingChanged -= OnHidingChanged;
		_player.MoveFinished -= OnMoveFinished;
	}
}