using System;
using Godot;

public partial class ChipManager : Node
{
	// Balance indicator
	[Export] public Label BalanceDisplay;

    public event Action<int> BalanceChanged;
    public int Balance { get; private set; } = 100;
	private float _visualBalance; // dummy variable to hold the value of VisualBalance
	private float VisualBalance // bc setter method is tastier than function
	{
		get => _visualBalance;
		set
		{
			_visualBalance = value;
			
			if (BalanceDisplay == null) return;
			BalanceDisplay.Text = Mathf.RoundToInt(_visualBalance).ToString();
		}
	}
	private Tween _activeTween;


    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("GiveMoney") && @event.IsPressed())
		{
			AddChips(50);
		}
        if (@event.IsActionPressed("TakeMoney") && @event.IsPressed())
		{
			SpendChips(50);
		}
    }


    public override void _Ready()
    {
		_visualBalance = Balance;

        if (BalanceDisplay != null)
		{
			BalanceDisplay.Text = Balance.ToString();
		}

		BalanceChanged += LerpBalanceDisplay;
    }

	private void LerpBalanceDisplay(int newBalance)
	{
		if (BalanceDisplay == null) return;

		_activeTween?.Kill();
		_activeTween = CreateTween();

		// tweening a dummy value to display on screen without affecting balance
		_activeTween.TweenProperty(this, nameof(VisualBalance), (float)newBalance, 0.8f)
			.SetTrans(Tween.TransitionType.Cubic)
			.SetEase(Tween.EaseType.Out);
	}
    public int AddChips(int amount)
    {
        Balance += amount;
        BalanceChanged?.Invoke(Balance);
        return Balance;
    }

    public bool HasEnoughChips(int amount) => (Balance >= amount);

    public bool SpendChips(int amount)
    {
        if (!HasEnoughChips(amount)) return false;

        Balance -= amount;
        BalanceChanged?.Invoke(Balance);
        return true;
    }
}