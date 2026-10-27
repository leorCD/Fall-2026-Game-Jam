using System;
using Godot;
using Godot.Collections;

public partial class SlotMachineLogic : Node3D
{
	[Export] public StaticPlayerController Player {get; private set;}
	[Export] public SlotMachineAnimator AnimationComponent {get; private set;}
	[Export] public SlotMachineRewarder RewardComponent {get; private set;}
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
	private Dictionary<int, int> _faceWeights = new()
	{
		{ 1, 21 }, // slot number then weight
		{ 2, 20 },
		{ 3, 18 },
		{ 4, 17 },
		{ 5, 17 },
		{ 6, 16 },
		{ 7, 15 },
		{ 8, 13 },
	};
	private int _matchWeightBonus = 100; // weight added to a face each time its rolled in a sequence to give the player the illusion they're lucky=
	[Export] public Label3D SlotMachineHeadDisplay;


	[ExportCategory("Idle Vignette")]
	[Export] public IdleVignette Vignette;
	[Export] public float TimeUntilVignetteStarts = 10f; // 10s grace period until you start getting pressured to gamble
	[Export] public float TimeMaxLimit = 20f; // 20s until the vignette peaks
	public event Action IdlePressureMaxed;
	private float _idleTime = 0f;
	private bool _maxedFired = false;


	[ExportCategory("DEBUG")]
	[Export] public bool ShowChances = false;













    public override void _Ready()
    {
        if (AnimationComponent == null)
			GD.PrintErr($"[{nameof(SlotMachineLogic)}] Missing animation component, please attach in editor");

        if (RewardComponent == null)
			GD.PrintErr($"[{nameof(SlotMachineLogic)}] Missing reward component, please attach in editor");
		
		CurrentBet = 0;

		AnimationComponent.init(this);
		RewardComponent.init(this);
    }

    public override void _Process(double delta)
    {
		if (_isActive) // player is gambling
		{
			_idleTime = 0f; // spinning counts as playing
			_maxedFired = false;
		}
		else // player isn't gambling
		{
			_idleTime += (float)delta;
		}

		float t = Mathf.Clamp((_idleTime - TimeUntilVignetteStarts) / (TimeMaxLimit - TimeUntilVignetteStarts), 0f, 1f);
		Vignette?.SetTarget(t);

		if (t >= 1f && !_maxedFired)
		{
			_maxedFired = true;
			IdlePressureMaxed?.Invoke();
		}
    }






	private bool _shiftHeld = false;
    public override void _Input(InputEvent @event)
    {
		if (Input.IsActionJustPressed("Shift"))
			_shiftHeld = true;
		if (Input.IsActionJustReleased("Shift"))
			_shiftHeld = false;
    }

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("MoveLeft", true)){
			if (CurrentBet > 0)
				if (_shiftHeld)
					CurrentBet -= 5;
				else
					CurrentBet -= 1;
			
			if (CurrentBet <= 0) CurrentBet = Player.Chips.Balance; // wrap around
		}
		if (@event.IsActionPressed("MoveRight", true)){
			if (CurrentBet < Player.Chips.Balance)
				if (_shiftHeld)
					CurrentBet += 5;
				else
					CurrentBet += 1;
			
			if (CurrentBet >= Player.Chips.Balance) CurrentBet = 0;
		}

		if (@event.IsActionPressed("StartGambling"))
		{
			if (Player.Chips.HasEnoughChips(1) && CurrentBet > 0) // has atleast 1 chip to bet
				Start();
		}
	}



	private async void Start()
	{
		if (_isActive == true || Player == null) return;
		int bet = CurrentBet;
		if (!Player.Chips.SpendChips(bet)) return;

		_isActive = true;
		_idleTime = 0f;
		CurrentBet = 0;

		int[] results = CalculateReelFaces(3);

		await AnimationComponent?.Start(results);
		
    	var reward = RewardComponent?.Reward(results, bet); // payout
		if (reward is SpinOutcome outcome)
			if (outcome.IsJackpot) SlotMachineHeadDisplay.Text = "JACKPOT";
			else if(outcome.IsWin) SlotMachineHeadDisplay.Text = "MINI-JACKPOT";
			else SlotMachineHeadDisplay.Text = "LOSE";
			
			GetTree().CreateTimer(2.0).Timeout += () => SlotMachineHeadDisplay.Text = "* * * * *";
			

		_isActive = false;
	}






	private int[] CalculateReelFaces(int numberOfReels)
	{
		// Per-spin copy so the exported table stays untouched
		var liveWeights = new Dictionary<int, int>();
		int total = 0;
		foreach (var pair in _faceWeights)
		{
			liveWeights[pair.Key] = pair.Value;
			total += pair.Value;
		}

		int[] results = new int[numberOfReels];
		for (int i = 0; i < numberOfReels; i++)
		{
			int face = RollFace(liveWeights, total);
			results[i] = face;

			liveWeights[face] += _matchWeightBonus; // boost for the next reel
			total += _matchWeightBonus;
		}

		return results;
	}

	private int RollFace(Dictionary<int, int> weights, int totalWeight)
	{
		int roll = _random.Next(0, totalWeight);
		int cumulative = 0;

		if (ShowChances)
			PrintChances(weights, totalWeight);

		foreach (var pair in weights)
		{
			cumulative += pair.Value;
			if (roll < cumulative)
				return pair.Key;
		}

		GD.PrintErr($"[{nameof(SlotMachineLogic)}] Weight table empty or invalid");
		return 1;
	}

	private void PrintChances(Dictionary<int, int> weights, int totalWeight)
	{
		GD.Print($"--- Slot chances (total weight: {totalWeight}) ---");
		foreach (var pair in weights)
		{
			float percent = pair.Value / (float)totalWeight * 100f;
			GD.Print($"Face {pair.Key}: weight {pair.Value} -> {percent:F2}%");
		}
	}
}
