using System;
using System.Numerics;
using Godot;
using Godot.Collections;

public partial class SlotMachineLogic : Node3D
{
	[Export] public StaticPlayerController Player {get; private set;}
	[Export] public SlotMachineAnimator AnimationComponent {get; private set;}
	[Export] public SlotMachineRewarder RewardComponent {get; private set;}
	private Random _random = new Random();
	private bool _isActive = false;
	public event Action SpinFinished;
	public event Action SpinStarted;

	[Export] public Label BetDisplay;
	private BigInteger _currentBet = BigInteger.Zero;
	private BigInteger CurrentBet
	{
		get => _currentBet;
		set
		{
			_currentBet = value;
			if (BetDisplay != null)
				BetDisplay.Text = $"Current bet : {ChipManager.FormatBigNumber(_currentBet)}";
		}
	}

	private Dictionary<int, int> _faceWeights = new()
	{
		{ 1, 22 }, // slot number then weight
		{ 2, 20 },
		{ 3, 18 },
		{ 4, 17 },
		{ 5, 16 },
		{ 6, 15 },
		{ 7, 14 },
		{ 8, 13 },
	};
	private int _matchWeightBonus = 5000; // weight added to a face each time its rolled in a sequence to give the player the illusion they're lucky=
	[Export] public Label3D SlotMachineHeadDisplay;


	[ExportCategory("Idle Vignette")]
	[Export] public IdleVignette Vignette;
	[Export] public float TimeUntilVignetteStarts = 35f; // 10s grace period until you start getting pressured to gamble
	[Export] public float TimeMaxLimit = 42f; // 50-30 = 20s until the vignette peaks
	public event Action IdlePressureMaxed;
	private float _idleTime = 0f;
	private bool _maxedFired = false;
	private bool _pressureDebounce = false;


	[ExportCategory("DEBUG")]
	[Export] public bool ShowChances = false;













    public override void _Ready()
    {
        if (AnimationComponent == null)
			GD.PrintErr($"[{nameof(SlotMachineLogic)}] Missing animation component, please attach in editor");
		AnimationComponent.init(this);

        if (RewardComponent == null)
			GD.PrintErr($"[{nameof(SlotMachineLogic)}] Missing reward component, please attach in editor");
		RewardComponent.init(this);
		
		CurrentBet = 0;
    }

    public override void _Process(double delta)
    {
		if (Player.IsDead) return;

		if (_isActive) // player is gambling
		{
			_idleTime = 0f; // spinning counts as playing
			_maxedFired = false;
		}
		else // player isn't gambling
		{
			_idleTime += (float)delta;
		}



		VignetteLogic();
    }

	private void VignetteLogic()
	{
		float t = Mathf.Clamp((_idleTime - TimeUntilVignetteStarts) / (TimeMaxLimit - TimeUntilVignetteStarts), 0f, 1f);
		Vignette?.SetTarget(t);

		if (t >= 1f && !_maxedFired) // max vignette intensity reached
		{
			_maxedFired = true;
			IdlePressureMaxed?.Invoke();
			ForceSpin(); // forced gamble always gambles all your money
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
		if (Player == null) return;
    
		// scale bet adjustment steps proportionally if balance is massive
		BigInteger step = _shiftHeld ? BigInteger.Max(5, Player.Chips.Balance / 20) : 1;
		if (@event.IsActionPressed("MoveLeft", true))
		{
			if (CurrentBet <= BigInteger.Zero)
				CurrentBet = Player.Chips.Balance;
			else
				CurrentBet -= step;
				if (CurrentBet < BigInteger.Zero)
					CurrentBet = BigInteger.Zero;
		}

		if (@event.IsActionPressed("MoveRight", true))
		{
			if (CurrentBet >= Player.Chips.Balance)
				CurrentBet = BigInteger.Zero;
			else
				CurrentBet += step;
				if (CurrentBet > Player.Chips.Balance)
					CurrentBet = Player.Chips.Balance;
   		}

		if (Player.IsHiding == false && @event.IsActionPressed("StartGambling"))
		{
			if (Player.Chips.HasEnoughChips(1) && CurrentBet > 0) // has atleast 1 chip to bet
				Start();
		}
	}



	private async void Start()
	{
		if (_isActive == true || Player == null) return;
		BigInteger bet = CurrentBet;
		if (!Player.Chips.SpendChips(bet)) return; // check if they have enough chips first

		_isActive = true;
		_idleTime = 0f;
		CurrentBet = 0;

		SpinStarted?.Invoke();

		int[] results = CalculateReelFaces(3);
		await AnimationComponent?.Start(results);
		
    	var reward = RewardComponent?.Reward(results, bet); // payout
		if (reward is SpinOutcome outcome)
		{
			if (outcome.IsJackpot) SlotMachineHeadDisplay.Text = "JACKPOT";
			else if(outcome.IsWin) SlotMachineHeadDisplay.Text = "MINI-JACKPOT";
			else SlotMachineHeadDisplay.Text = "LOSE";
			
			GetTree().CreateTimer(5.0).Timeout += () => SlotMachineHeadDisplay.Text = "* * * * *";
		}
			

		_isActive = false;
		SpinFinished?.Invoke();
	}

	public void ForceSpin()
	{
		if (_isActive || Player == null) return;

		BigInteger bet = Player.Chips.Balance;
		if (bet <= 0) return;

		CurrentBet = bet;
		Start();
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
