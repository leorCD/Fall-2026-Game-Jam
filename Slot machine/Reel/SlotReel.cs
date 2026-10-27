using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;

public partial class SlotReel : Node3D
{
    [Export] public float SpinSpeed { get; set; } = Mathf.DegToRad(720f); // ~2 full rotations/sec
    public bool IsSpinning {get; private set;}
    private float _angle;
    private Basis _baseBasis;
    private Tween _stopTween;

    [Export] public Array<Sprite3D> Faces; // 8 sprites, in order (slot 0 = face 1)
    private int[] _homeFrames;
    private static readonly Random _rng = new Random();



    public override void _Ready()
    {
        _baseBasis = this.Basis;

        // remember original layout, this is the source of truth
        _homeFrames = new int[Faces.Count];
        for (int i = 0; i < Faces.Count; i++)
            _homeFrames[i] = Faces[i].Frame;
    }

    public void Spin(int resultFace)
    {
        ShuffleAround(resultFace);
        IsSpinning = true;
    }

    private void ShuffleAround(int resultFace)
    {
        int keep = resultFace - 1;

        // Every slot except the result's
        var slots = new System.Collections.Generic.List<int>();
        for (int i = 0; i < Faces.Count; i++)
            if (i != keep) slots.Add(i);

        // Shuffle which home frame lands in each of those slots (Fisher-Yates)
        var frames = new System.Collections.Generic.List<int>();
        foreach (int s in slots) frames.Add(_homeFrames[s]);

        for (int i = frames.Count - 1; i > 0; i--)
        {
            int j = _rng.Next(i + 1);
            (frames[i], frames[j]) = (frames[j], frames[i]);
        }

        Faces[keep].Frame = _homeFrames[keep];
        for (int i = 0; i < slots.Count; i++)
            Faces[slots[i]].Frame = frames[i];
    }



    public async void StopSpinOnFace(int targetSymbolIndex)
    {
        IsSpinning = false;
        _stopTween?.Kill();

        float step = Mathf.Tau / Faces.Count;
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
            .SetTrans(Tween.TransitionType.Circ)
            .SetEase(Tween.EaseType.Out);

        await ToSignal(_stopTween, Tween.SignalName.Finished);

        SetAngle(Mathf.PosMod(_angle, Mathf.Tau)); // keep the value small
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
        Basis = new Basis(Vector3.Forward, _angle) * _baseBasis;
    }


}
