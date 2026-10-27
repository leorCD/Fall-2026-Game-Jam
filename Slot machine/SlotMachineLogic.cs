using System;
using Godot;

public partial class SlotMachineLogic : Node3D
{
	[Export] public StaticPlayerController Player {get; private set;}
	[Export] public SlotMachineAnimator AnimationComponent {get; private set;}
	private Random _random = new Random();
	private bool _isActive = false;

	[Export] public Label BetDisplay;
	private int _currentBet = 0;
	private int CurrentBet
	{
		get => _currentBet;
		set
		{
			_currentBet = value;

			if (BetDisplay == null) return;
			BetDisplay.Text = "Current bet : " + _currentBet.ToString();
		}
	}






    public override void _Ready()
    {
        if (AnimationComponent == null)
			GD.PrintErr($"[{nameof(SlotMachineLogic)}] Missing slot machine animation component, please attach in editor");
		
		CurrentBet = 0;

		AnimationComponent.init(this);
    }

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("MoveLeft", true))
		{
			CurrentBet -= 1;
		}
		if (@event.IsActionPressed("MoveRight", true))
		{
			CurrentBet += 1;
		}

		if (@event.IsActionPressed("StartGambling")){
			Start();
		}
	}



	private async void Start()
	{
		if (_isActive == true || Player == null) return;
		if (!Player.Chips.SpendChips(CurrentBet)) return;
		_isActive = true;

		CurrentBet = 0;

		int[] results = CalculateReelFaces(3);
		GD.Print(String.Join(", ", results));

		await AnimationComponent?.Start(results);

		_isActive = false;
	}

	private int[] CalculateReelFaces(int numberOfReels)
	{
		int[] reelIndices = new int[numberOfReels];

		for (int i = 0; i < numberOfReels; i++)
		{
			reelIndices[i] = _random.Next(1, 8);
		}

		return reelIndices;
	}
}
