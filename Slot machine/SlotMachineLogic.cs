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
	public bool isActive = false;
	public event Action SpinFinished;
	public event Action SpinStarted;

	[Export] public AutoFitLabel3D BetDisplay;
	private BigInteger _currentBet = BigInteger.Zero;
	public BigInteger CurrentBet // was private
	{
		get => _currentBet;
		set
		{
			_currentBet = value;
			if (BetDisplay != null)
				BetDisplay.SetText($"{ChipManager.FormatBigNumber(_currentBet)}");
		}
	}

	public void TrySpin(StaticPlayerController player)
	{
		if (player.IsHiding) return;
		if (player.Chips.HasEnoughChips(1) && CurrentBet > 0)
			Start();
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
	private int _matchWeightBonus = 50; // weight added to a face each time its rolled in a sequence to give the player the illusion they're lucky=
	[Export] public Label3D SlotMachineHeadDisplay;


	[ExportCategory("Idle Vignette")]
	[Export] public IdleVignette Vignette;
	[Export] public float TimeUntilVignetteStarts = 8f; // 10s grace period until you start getting pressured to gamble
	[Export] public float TimeMaxLimit = 7f; // 7s until the vignette peaks
	public event Action IdlePressureMaxed;
	private float _idleTime = 0f;
	private bool _maxedFired = false;
	private bool _isForcedSpin = false;


	[ExportCategory("DEBUG")]
	[Export] public bool ShowChances = false;










	private readonly Dictionary<SpotLight3D, float> _lightRanges = new();
	private Tween _lightTween;
	public void TurnOn()
	{
		_lightTween?.Kill();
		_lightTween = CreateTween().SetParallel(true);

		foreach (var pair in _lightRanges)
		{
			_lightTween.TweenProperty(pair.Key, "spot_range", pair.Value, 1f)
				.SetTrans(Tween.TransitionType.Sine)
				.SetEase(Tween.EaseType.Out);
		}
	}

    public override void _Ready()
    {
        if (AnimationComponent == null)
			GD.PrintErr($"[{nameof(SlotMachineLogic)}] Missing animation component, please attach in editor");
		AnimationComponent.init(this);

        if (RewardComponent == null)
			GD.PrintErr($"[{nameof(SlotMachineLogic)}] Missing reward component, please attach in editor");
		RewardComponent.init(this);
		
		CurrentBet = 0;

		foreach (Node child in GetChildren())
		{
			if (child is SpotLight3D light)
			{
            	_lightRanges[light] = light.SpotRange; // save original
				light.SpotRange = 0f;
			}
		}
    }

    public override void _Process(double delta)
    {
		if (this.Visible == false) return;
		if (Player.IsDead) return;

		if (isActive) // player is gambling
		{
			if (!_isForcedSpin)
            {
                _idleTime = 0f;
                _maxedFired = false;
            }
		}
		else // player isn't gambling
		{
			_idleTime += (float)delta;
		}



		VignetteLogic();
    }

	private void VignetteLogic()
	{
		float t =Mathf.Clamp((_idleTime - TimeUntilVignetteStarts) / (TimeMaxLimit), 0f, 1f);
		// float t = _isForcedSpin 
        //     ? 1f 
        //     : Mathf.Clamp((_idleTime - TimeUntilVignetteStarts) / (TimeMaxLimit), 0f, 1f);
		Vignette?.SetTarget(t);

		if (t >= 1f && !_maxedFired) // max vignette intensity reached
		{
			_maxedFired = true;
			IdlePressureMaxed?.Invoke();
			ForceSpin(); // forced gamble always gambles all your money
		}
	}





	public override void _UnhandledInput(InputEvent @event)
	{
		if (Player == null) return;

		if (Player.IsHiding == false && @event.IsActionPressed("StartGambling"))
		{
			if (Player.Chips.HasEnoughChips(1) && CurrentBet > 0) // has atleast 1 chip to bet
				Start();
		}
	}



	private async void Start()
	{
		if (isActive == true || Player == null) return;
		BigInteger bet = CurrentBet;
		if (!Player.Chips.SpendChips(bet)) return; // check if they have enough chips first

		isActive = true;
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

		_isForcedSpin = false;
        _idleTime = 0f;
        _maxedFired = false;
		isActive = false;
		SpinFinished?.Invoke();
	}

	public void ForceSpin()
	{
		if (isActive || Player == null) return;

		BigInteger bet = Player.Chips.Balance;
		if (bet <= 0) return;

		_isForcedSpin = true;
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
