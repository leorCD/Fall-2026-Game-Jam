using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class SlotMachineAnimator : Node
{
	private Node3D SlotMachine;
	[Export] public Node3D Lever {get; private set;}
	[Export] public Array<Node3D> Reels {get; private set;} = new();

	private Tween _bodyTween;
	private Tween _leverTween;
	private Transform3D _bodyDefaultTransform;
	private Transform3D _leverDefaultTransform;



	public void init(SlotMachineLogic slotmachine)
	{
		this.SlotMachine = slotmachine;

		_bodyDefaultTransform = (SlotMachine != null) ? SlotMachine.Transform : Transform3D.Identity;
		_leverDefaultTransform = (Lever != null) ? Lever.Transform : Transform3D.Identity;
	}

	public async Task<bool> Start(int[] results)
	{
		if (Lever != null)
		{
			await PullHandleDown();
			ReturnHandleUp();
		}

		await AnimateMachineJump();
		await RollReels(results);

		return true;
	}

	private async Task PullHandleDown()
	{
		_leverTween?.Kill();
		_leverTween = CreateTween();

		Vector3 leverDefaultRotation = _leverDefaultTransform.Basis.GetEuler();
		Vector3 leverPulledRotation = leverDefaultRotation - new Vector3(0, 0, Mathf.DegToRad(100));

		_leverTween.TweenProperty(Lever, "rotation", leverPulledRotation, 0.5f)
			.SetTrans(Tween.TransitionType.Linear);

		await ToSignal(_leverTween, Tween.SignalName.Finished);
	}

	private void ReturnHandleUp()
	{
		_leverTween?.Kill();
		_leverTween = CreateTween();

		Vector3 leverDefaultRotation = _leverDefaultTransform.Basis.GetEuler();
		Vector3 overshootRot = leverDefaultRotation + new Vector3(0, 0, Mathf.DegToRad(30));

		// fast whip back past the rest point
		_leverTween.TweenProperty(Lever, "rotation", overshootRot, 0.15f)
			.SetTrans(Tween.TransitionType.Expo)
			.SetEase(Tween.EaseType.Out);

		// settle back to rest
		_leverTween.TweenProperty(Lever, "rotation", leverDefaultRotation, 0.25f)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.In);
	}

	private async Task AnimateMachineJump()
	{
		_bodyTween?.Kill();
		_bodyTween = CreateTween();

		Vector3 basePos = _bodyDefaultTransform.Origin;
		Vector3 baseRot = _bodyDefaultTransform.Basis.GetEuler();
		Vector3 baseScale = _bodyDefaultTransform.Basis.Scale;

		// Offsets
		Vector3 jumpOffset = new Vector3(0, 0.04f, 0);
		// Vector3 preSquashScale = new Vector3(baseScale.X, baseScale.Y - .00f, baseScale.Z + .00f); // Compressing before jump
		// Vector3 airStretchScale = new Vector3(baseScale.X, baseScale.Y + .00f, baseScale.Z - .00f); // Stretching in air
		// Vector3 landSquashScale = new Vector3(baseScale.X, baseScale.Y - .00f, baseScale.Z + .00f); // Slamming onto ground

		Vector3 tiltOffset = new Vector3(
			Mathf.DegToRad((float)GD.RandRange(-3, 3)), // Tilt back slightly
			0,
			Mathf.DegToRad((float)GD.RandRange(0, 3))
		);

		// sinks down and compresses slightly under strain
		_bodyTween.TweenProperty(SlotMachine, "position", basePos - new Vector3(0, 0.04f, 0), 0.12f)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.Out);
		// _bodyTween.Parallel().TweenProperty(SlotMachine, "scale", preSquashScale, 0.12f)
		// 	.SetTrans(Tween.TransitionType.Quad)
		// 	.SetEase(Tween.EaseType.Out);

		// bursts upward, stretches vertically, tilts back
		_bodyTween.TweenProperty(SlotMachine, "position", basePos + jumpOffset, 0.18f)
			.SetTrans(Tween.TransitionType.Cubic)
			.SetEase(Tween.EaseType.Out);
		// _bodyTween.Parallel().TweenProperty(SlotMachine, "scale", airStretchScale, 0.18f)
		// 	.SetTrans(Tween.TransitionType.Quad)
		// 	.SetEase(Tween.EaseType.Out);
		_bodyTween.Parallel().TweenProperty(SlotMachine, "rotation", baseRot + tiltOffset, 0.18f)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.Out);

		// gravity pulls it down fast; EaseType.In gives accelerating velocity feel
		_bodyTween.TweenProperty(SlotMachine, "position", basePos, 0.10f)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.In);
		_bodyTween.Parallel().TweenProperty(SlotMachine, "rotation", baseRot, 0.5f)
			.SetTrans(Tween.TransitionType.Back)
			.SetEase(Tween.EaseType.In);

		// flattens out wide upon contact
		// _bodyTween.TweenProperty(SlotMachine, "scale", landSquashScale, 0.06f)
		// 	.SetTrans(Tween.TransitionType.Quad)
		// 	.SetEase(Tween.EaseType.Out);

		// // recoils back to rest; boing
		// _bodyTween.TweenProperty(SlotMachine, "scale", baseScale, 0.22f)
		// 	.SetTrans(Tween.TransitionType.Elastic)
		// 	.SetEase(Tween.EaseType.Out);

			
		await ToSignal(_bodyTween, Tween.SignalName.Finished);
	}

	private async Task RollReels(int[] results)
	{
    	// collect all valid SlotReels first
		List<SlotReel> activeReels = new List<SlotReel>();
		for (int i = 0; i < Reels.Count; i++)
		{
			if (Reels[i] is SlotReel reel)
			{
				activeReels.Add(reel);
			}
			else
			{
				GD.PrintErr($"Skipping invalid SlotReel object at index {i}");
			}
        }

		// start all reels spinning at the same time
		foreach (SlotReel reel in activeReels)
		{
			reel.Spin();
		}

		// let all reels spin together for a base duration before stopping starts
		await ToSignal(GetTree().CreateTimer(1.5f), SceneTreeTimer.SignalName.Timeout);

		// stop each reel sequentially with a delay between each
		int j = 0;
		foreach (SlotReel reel in activeReels)
		{
			reel.StopSpinOnFace(results[j]);
			j++;

			await ToSignal(GetTree().CreateTimer(0.5f), SceneTreeTimer.SignalName.Timeout); // 0.5 seconds before stopping the next reel
		}
	}
}
