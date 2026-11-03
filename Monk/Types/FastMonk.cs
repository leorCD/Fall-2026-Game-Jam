using Godot;
using System;

public partial class FastMonk : Monk
{
    private float _speed = 2.0f;
    private Tween _activeTween;
    public Marker3D CurrentMarker;

    public void Init(Marker3D SpawnMarker)
    {
        this.CurrentMarker = SpawnMarker;
        Position = SpawnMarker.GlobalPosition;
    }

    public void MoveTo(Marker3D targetMarker)
    {
        _activeTween?.Kill();

        float distanceToTarget = CurrentMarker.GlobalPosition.DistanceTo(targetMarker.GlobalPosition);
        if (distanceToTarget < 0.001) return; // prevent crashing when dividing by 0

        _activeTween.TweenProperty(this, "position", targetMarker.GlobalPosition, distanceToTarget * _speed);   
    }
}
