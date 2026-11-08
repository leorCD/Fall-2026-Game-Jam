using System;
using System.Numerics;
using Godot;

public partial class ChipManager : Node
{
	// Balance indicator
	[Export] public Label BalanceDisplay;

	public event Action<BigInteger> BalanceChanged;
	public BigInteger Balance {get; private set;} = 0;
	private BigInteger _tweenStartBalance;
    private BigInteger _tweenTargetBalance;
    private double _tweenProgress;
    private Tween _activeTween;

    // exposed property to tween (0.0 to 1.0)
    public double TweenProgress
    {
        get => _tweenProgress;
        set
        {
            _tweenProgress = value;
            if (BalanceDisplay == null) return;

            // interpolate BigInteger, bc we cant lerp BigInteger like long or int
            BigInteger visual = _tweenStartBalance + (BigInteger)((double)(_tweenTargetBalance - _tweenStartBalance) * _tweenProgress);
            BalanceDisplay.Text = $"Chips : {FormatBigNumber(visual)}";
        }
    }





	public override void _Ready()
	{
		_tweenStartBalance = Balance;
        _tweenTargetBalance = Balance;

		if (BalanceDisplay != null)
		{
			BalanceDisplay.Text = "Chips : " + Balance.ToString();
		}

		BalanceChanged += OnBalanceChanged;
	}

	private void OnBalanceChanged(BigInteger newBalance)
	{
		if (BalanceDisplay == null) return;

		_activeTween?.Kill();
        _tweenStartBalance = _tweenTargetBalance;
        _tweenTargetBalance = newBalance;
        _tweenProgress = 0.0;

		// tweening a dummy value to display on screen without affecting balance
		_activeTween = CreateTween();
		_activeTween.TweenProperty(this, nameof(TweenProgress), 1.0, 0.8)
			.SetTrans(Tween.TransitionType.Cubic)
			.SetEase(Tween.EaseType.Out);
	}







	public BigInteger AddChips(BigInteger amount)
	{
		Balance += amount;
        BalanceChanged?.Invoke(Balance);
        return Balance;
	}

	public bool HasEnoughChips(BigInteger amount) => (Balance >= amount);

	public bool SpendChips(BigInteger amount)
	{
		if (!HasEnoughChips(amount)){
			GD.Print("Not enough chips");
			return false;
		}

		Balance -= amount;
		BalanceChanged?.Invoke(Balance);
		return true;
	}










	// displays compact suffix labels once numbers get impossibly wide (K, M, B, T, Qa, etc.)
	public static string FormatBigNumber(System.Numerics.BigInteger value)
	{
		// threshold: 1 quadrillion (10^15)
		// anything below this shows every single comma-separated digit
		if (value < 1_000_000_000_000_000)
		{
			return value.ToString("N0");
		}

		// suffixes starting directly from Trillions
		string[] suffixes = 
		{ 
			"Qa", "Qi", "Sx", "Sp", "Oc", "No", "Dc", "Ud", "Dd", "Td", "Qad", "Qid", "Sxd", "Spd", "Ocd", "Nod", "Vg" 
		};

		double d = (double)value;
		
		// scale down from 1 to 10^15 (quadrillion) first
		d /= Mathf.Pow(10, 15);
		int exp = 0;

		while (d >= 1000.0 && exp < suffixes.Length - 1)
		{
			d /= 1000.0;
			exp++;
		}

		// if it exceeds all named suffixes, fall back to scientific notation (e.g., 4.25e42)
		if (exp == suffixes.Length - 1 && d >= 1000.0)
		{
			return value.ToString("E2");
		}

		return $"{d:0.##} {suffixes[exp]}";
	}
}
