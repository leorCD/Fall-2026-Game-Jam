using Godot;
using System;

public partial class SlotMachine : Node3D
{
	private Tween _activeTween;
	private Transform3D _defaultTransform;




	public override void _Ready()
	{
		_defaultTransform = this.Transform;
	}


	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("StartGambling")){
			StartMachine();
		}
	}

	private void StartMachine()
	{
		_activeTween?.Kill();
		_activeTween = CreateTween();

		Vector3 basePos = _defaultTransform.Origin;
		Vector3 baseRot = _defaultTransform.Basis.GetEuler();
		Vector3 baseScale = _defaultTransform.Basis.Scale;

		// Offsets
		Vector3 jumpOffset = new Vector3(0, 0.22f, 0);
		Vector3 preSquashScale = new Vector3(1.08f, 0.85f, 1.08f); // Compressing before jump
		Vector3 airStretchScale = new Vector3(0.94f, 1.12f, 0.94f); // Stretching in air
		Vector3 landSquashScale = new Vector3(1.14f, 0.78f, 1.14f); // Slamming onto ground

		Vector3 tiltOffset = new Vector3(
			Mathf.DegToRad((float)GD.RandRange(-6, -2)), // Tilt back slightly
			Mathf.DegToRad((float)GD.RandRange(-4, 4)),
			Mathf.DegToRad((float)GD.RandRange(-3, 3))
		);

		// sinks down and compresses slightly under strain
		_activeTween.TweenProperty(this, "position", basePos - new Vector3(0, 0.04f, 0), 0.12f)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.Out);
		_activeTween.Parallel().TweenProperty(this, "scale", preSquashScale, 0.12f)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.Out);

		// bursts upward, stretches vertically, tilts back
		_activeTween.TweenProperty(this, "position", basePos + jumpOffset, 0.18f)
			.SetTrans(Tween.TransitionType.Cubic)
			.SetEase(Tween.EaseType.Out);
		_activeTween.Parallel().TweenProperty(this, "scale", airStretchScale, 0.18f)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.Out);
		_activeTween.Parallel().TweenProperty(this, "rotation", baseRot + tiltOffset, 0.18f)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.Out);

		// gravity pulls it down fast; EaseType.In gives accelerating velocity feel
		_activeTween.TweenProperty(this, "position", basePos, 0.10f)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.In);
		_activeTween.Parallel().TweenProperty(this, "rotation", baseRot, 0.10f)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.In);

		// flattens out wide upon contact
		_activeTween.TweenProperty(this, "scale", landSquashScale, 0.06f)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.Out);

		// recoils back to rest; boing
		_activeTween.TweenProperty(this, "scale", baseScale, 0.22f)
			.SetTrans(Tween.TransitionType.Elastic)
			.SetEase(Tween.EaseType.Out);
	}

}
