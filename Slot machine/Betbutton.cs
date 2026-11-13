using System.Numerics;
using Godot;

public enum BetAction
{
	GreatDecrease,
	Decrease,
	Increase,
	GreatIncrease,
	Spin
}

public partial class Betbutton : Area3D, IInteractable, IFocusable
{
	[Export] private SlotMachineLogic _slotMachine;
	[Export] private BetAction _action;

	[ExportCategory("Highlight")]
	[Export] private Color _highlightOutlineColor = Colors.White;
	[Export] private int _highlightOutlineSize = 8;
	[Export] private float _fadeTime = 0.1f;

	private Label3D _label;
	private Color _baseOutlineColor;
	private int _baseOutlineSize;
	private Tween _tween;

	public override void _Ready()
	{
		var parent = GetParent<Node3D>();
		if (parent is Label3D label)
		{
			this._label = label;
			_baseOutlineColor = _label.OutlineModulate;
			_baseOutlineSize = _label.OutlineSize;
		}
	}

	public void OnFocus() => Highlight(true);
	public void OnUnfocus() => Highlight(false);

	private void Highlight(bool on)
	{
		if (_label == null) return;
		
		_tween?.Kill();
		_tween = CreateTween().SetParallel(true);

		_tween.TweenProperty(_label, "outline_modulate",
			on ? _highlightOutlineColor : _baseOutlineColor, _fadeTime);
		_tween.TweenProperty(_label, "outline_size",
			on ? _highlightOutlineSize : _baseOutlineSize, _fadeTime);
	}

	public override void _ExitTree()
	{
		_tween?.Kill();
	}

	public void Interact(StaticPlayerController player)
	{
		if (_slotMachine == null)
		{
			GD.PrintErr($"[{nameof(Betbutton)}] No slot machine assigned on {GetParent<Node3D>().Name}");
			return;
		}
		if (_slotMachine.isActive || _slotMachine.Player.IsHiding) return; // no bet changes mid-spin

		BigInteger balance = player.Chips.Balance;
		BigInteger great = BigInteger.Max(5, balance / 20); // scales with a massive balance

		switch (_action)
		{
			case BetAction.GreatDecrease: Adjust(-great, balance); break;
			case BetAction.Decrease:      Adjust(-1, balance);     break;
			case BetAction.Increase:      Adjust(1, balance);      break;
			case BetAction.GreatIncrease: Adjust(great, balance);  break;
			case BetAction.Spin:          _slotMachine.TrySpin(player); break;
		}
	}

	private void Adjust(BigInteger delta, BigInteger balance)
	{
		BigInteger bet = _slotMachine.CurrentBet;

		if (delta < 0 && bet <= 0)
			bet = balance;            // wrap to max
		else if (delta > 0 && bet >= balance)
			bet = 0;                  // wrap to zero
		else
			bet += delta;

		_slotMachine.CurrentBet = BigInteger.Min(balance, BigInteger.Max(0, bet));
	}
}