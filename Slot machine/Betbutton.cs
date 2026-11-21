using System;
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
	[Export] public BetAction Action;

	[ExportGroup("SFX")]
	[Export] public AudioStream ButtonPress;

	[ExportGroup("Highlight")]
	[Export] private Color _highlightOutlineColor = Colors.White;
	[Export] private int _highlightOutlineSize = 8;
	[Export] private float _fadeTime = 0.1f;

	private Label3D _label;
	private Color _baseOutlineColor;
	private int _baseOutlineSize;
	private Tween _tween;
	private bool _playerIsHiding;

	public override void _Ready()
	{
		var parent = GetParent<Node3D>();
		if (parent is Label3D label)
		{
			this._label = label;
			label.Shaded = true;
			_baseOutlineColor = _label.OutlineModulate;
			_baseOutlineSize = _label.OutlineSize;
		}

		_slotMachine.Player.MoveFinished += OnMoveFinished;
		Disable();
	}

	public void OnFocus() => Highlight(true);
	public void OnUnfocus() => Highlight(false);

	private void Highlight(bool on)
	{
		if (_label == null) return;
		
		// _tween?.Kill();
		// _tween = CreateTween().SetParallel(true);
		// _tween.TweenProperty(_label, "outline_modulate",
		// 	on ? _highlightOutlineColor : _baseOutlineColor, _fadeTime);
		// _tween.TweenProperty(_label, "outline_size",
		// 	on ? _highlightOutlineSize : _baseOutlineSize, _fadeTime);

		_label.Shaded = !on;
	}

	public override void _ExitTree()
	{
		_tween?.Kill();
	}



	private void OnMoveFinished()
	{
		_playerIsHiding = _slotMachine.Player.IsHiding;
	}

	public void Interact(StaticPlayerController player)
	{
		if (_slotMachine == null)
		{
			GD.PrintErr($"[{nameof(Betbutton)}] No slot machine assigned on {GetParent<Node3D>().Name}");
			return;
		}
		if (_slotMachine.isActive || _playerIsHiding) return; // no bet changes mid-spin

		BigInteger balance = player.Chips.Balance;
		BigInteger great = BigInteger.Max(5, balance / 20); // scales with a massive balance

		switch (Action)
		{
			case BetAction.GreatDecrease:
				Adjust(-great, balance);
				AudioManager.Instance.PlaySfx3D(ButtonPress, this, "SFX_SlotMachine", -0.2f, -4f);
				break;
			case BetAction.Decrease:      
				Adjust(-1, balance);
				AudioManager.Instance.PlaySfx3D(ButtonPress, this, "SFX_SlotMachine", -0.1f, -4f);
				break;
			case BetAction.Increase:      
				Adjust(1, balance);
				AudioManager.Instance.PlaySfx3D(ButtonPress, this, "SFX_SlotMachine", 0.1f, -4f);
				break;
			case BetAction.GreatIncrease: 
				Adjust(great, balance);
				AudioManager.Instance.PlaySfx3D(ButtonPress, this, "SFX_SlotMachine", 0.2f, -4f);
				break;
			case BetAction.Spin:
				_slotMachine.TrySpin(player);
				break;
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

	public void Enable()
	{
		SetCollisionLayerValue(1, true);
	}

	public void Disable()
	{
		SetCollisionLayerValue(1, false);
	}
}