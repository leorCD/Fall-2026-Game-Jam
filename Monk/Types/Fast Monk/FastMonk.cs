using Godot;
using System;

public partial class FastMonk : Monk
{
    protected override float GetSpeed() => this.MovementSpeed * 1.5f;
}
