using Godot;
using System;

public partial class SlotReel : Node3D
{
    [Export] public int NumberOfSymbols = 8;
    [Export] public float SpinSpeed { get; set; } = Mathf.DegToRad(720f); // ~2 full rotations/sec
    public bool IsSpinning {get; private set;}
    private float _angle;
    private Basis _baseBasis;
    private Tween _stopTween;




    public override void _Ready()
    {
        _baseBasis = this.Basis;
    }
    public override void _Process(double delta)
    {
        if (!IsSpinning) return;
        SetAngle(_angle + SpinSpeed * (float)delta);
    }

    // use this to rotate instead of RotateX() bc i was running into issues with Godot auto-handling euler decomposition
    private void SetAngle(float a)
    {
        _angle = a;
        Basis = new Basis(Vector3.Right, _angle) * _baseBasis;
    }

    public void Spin(){this.IsSpinning = true;}

    public async void StopSpinOnFace(int targetSymbolIndex)
    {
        IsSpinning = false;
        _stopTween?.Kill();

        float step = Mathf.Tau / NumberOfSymbols;
        float targetAngle = targetSymbolIndex * step - step;

        // normalize so the math below is stable
        float current = Mathf.PosMod(_angle, Mathf.Tau);
        float minTarget = current + Mathf.Tau; // always roll forward at least one rev
        float revolutions = Mathf.Ceil((minTarget - targetAngle) / Mathf.Tau);
        float finalAngle = targetAngle + revolutions * Mathf.Tau;

        _angle = current;
        Rotation = new Vector3(_angle, 0f, 0f);

        _stopTween = CreateTween();
        _stopTween.TweenMethod(Callable.From<float>(SetAngle), current, finalAngle, 0.8f)
            .SetTrans(Tween.TransitionType.Linear)
            .SetEase(Tween.EaseType.Out);

        await ToSignal(_stopTween, Tween.SignalName.Finished);

        SetAngle(Mathf.PosMod(_angle, Mathf.Tau)); // keep the value small
    }

}
