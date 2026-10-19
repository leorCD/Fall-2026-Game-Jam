using System;
using Godot;

public partial class SlotMachineLogic : Node3D
{
	[Export] public SlotMachineAnimator AnimationComponent {get; private set;}
	private Random random = new Random();
	private bool isActive = false;





    public override void _Ready()
    {
        if (AnimationComponent == null)
			GD.PrintErr($"[{nameof(SlotMachineLogic)}] Missing slot machine animation component, please attach in editor");
		
		AnimationComponent.init(this);
    }

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("StartGambling")){
			Start();
		}
	}

	private async void Start()
	{
		if (isActive == true) return;
		isActive = true;

		int[] results = CalculateReelFaces(3);
		GD.Print(String.Join(", ", results));

		await AnimationComponent?.Start(results);

		isActive = false;
	}

	private int[] CalculateReelFaces(int numberOfReels)
	{
		int[] reelIndices = new int[numberOfReels];

		for (int i = 0; i < numberOfReels; i++)
		{
			reelIndices[i] = random.Next(1, 8);
		}

		return reelIndices;
	}
}
