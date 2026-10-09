using System.Threading.Tasks;
using Godot;

public partial class SlotMachine : Node3D
{
	[Export] public Node3D Lever {get; private set;}

	private Tween _bodyTween;
	private Tween _leverTween;
	private Transform3D _bodyDefaultTransform;
	private Transform3D _leverDefaultTransform;




	public override void _Ready()
	{
		_bodyDefaultTransform = this.Transform;
		_leverDefaultTransform = (Lever != null) ? Lever.Transform : Transform3D.Identity;
	}


	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("StartGambling")){
			StartMachine();
		}
	}

	private async void StartMachine()
	{
		if (Lever != null)
		{
			await PullHandleDown();
			ReturnHandleUp();
		}

		AnimateMachineJump();
	}

	private async Task PullHandleDown()
	{
		_leverTween?.Kill();
		_leverTween = CreateTween();

		Vector3 leverDefaultRotation = _leverDefaultTransform.Basis.GetEuler();
		Vector3 leverPulledRotation = leverDefaultRotation + new Vector3(Mathf.DegToRad(100), 0, 0);

		_leverTween.TweenProperty(Lever, "rotation", leverPulledRotation, 0.5f)
			.SetTrans(Tween.TransitionType.Linear);

		await ToSignal(_leverTween, Tween.SignalName.Finished);
	}

	private void ReturnHandleUp()
	{
		_leverTween?.Kill();
		_leverTween = CreateTween();

		Vector3 leverDefaultRotation = _leverDefaultTransform.Basis.GetEuler();
		Vector3 overshootRot = leverDefaultRotation - new Vector3(Mathf.DegToRad(10), 0, 0);

		// fast whip back past the rest point
		_leverTween.TweenProperty(Lever, "rotation", overshootRot, 0.15f)
			.SetTrans(Tween.TransitionType.Expo)
			.SetEase(Tween.EaseType.Out);

		// settle back to rest
		_leverTween.TweenProperty(Lever, "rotation", leverDefaultRotation, 0.25f)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.In);
	}

	private void AnimateMachineJump()
	{
		_bodyTween?.Kill();
		_bodyTween = CreateTween();

		Vector3 basePos = _bodyDefaultTransform.Origin;
		Vector3 baseRot = _bodyDefaultTransform.Basis.GetEuler();
		Vector3 baseScale = _bodyDefaultTransform.Basis.Scale;

		// Offsets
		Vector3 jumpOffset = new Vector3(0, 0.11f, 0);
		Vector3 preSquashScale = new Vector3(1.08f, 0.85f, 1.08f); // Compressing before jump
		Vector3 airStretchScale = new Vector3(0.94f, 1.12f, 0.94f); // Stretching in air
		Vector3 landSquashScale = new Vector3(1.14f, 0.78f, 1.14f); // Slamming onto ground

		Vector3 tiltOffset = new Vector3(
			Mathf.DegToRad((float)GD.RandRange(-6, -2)), // Tilt back slightly
			Mathf.DegToRad((float)GD.RandRange(-4, 4)),
			Mathf.DegToRad((float)GD.RandRange(-3, 3))
		);

		// sinks down and compresses slightly under strain
		_bodyTween.TweenProperty(this, "position", basePos - new Vector3(0, 0.04f, 0), 0.12f)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.Out);
		_bodyTween.Parallel().TweenProperty(this, "scale", preSquashScale, 0.12f)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.Out);

		// bursts upward, stretches vertically, tilts back
		_bodyTween.TweenProperty(this, "position", basePos + jumpOffset, 0.18f)
			.SetTrans(Tween.TransitionType.Cubic)
			.SetEase(Tween.EaseType.Out);
		_bodyTween.Parallel().TweenProperty(this, "scale", airStretchScale, 0.18f)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.Out);
		_bodyTween.Parallel().TweenProperty(this, "rotation", baseRot + tiltOffset, 0.18f)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.Out);

		// gravity pulls it down fast; EaseType.In gives accelerating velocity feel
		_bodyTween.TweenProperty(this, "position", basePos, 0.10f)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.In);
		_bodyTween.Parallel().TweenProperty(this, "rotation", baseRot, 0.10f)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.In);

		// flattens out wide upon contact
		_bodyTween.TweenProperty(this, "scale", landSquashScale, 0.06f)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.Out);

		// recoils back to rest; boing
		_bodyTween.TweenProperty(this, "scale", baseScale, 0.22f)
			.SetTrans(Tween.TransitionType.Elastic)
			.SetEase(Tween.EaseType.Out);
	}
}
