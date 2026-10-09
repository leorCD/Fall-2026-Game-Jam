using Godot;
using System;

public partial class SlotReel : Node3D
{
    [Export] public int _numberOfFaces = 8;
    [Export] public float SpinSpeed { get; set; } = -Mathf.DegToRad(720f); // ~2 full rotations/sec
    public bool IsSpinning {get; private set;}



    public void Spin(){this.IsSpinning = true;}

    public void StopSpin(){this.IsSpinning = false;}


    public override void _Process(double delta)
    {
        if (!IsSpinning) return;

        // smooth rotation
        RotateX(SpinSpeed * (float)delta);
    }
}
