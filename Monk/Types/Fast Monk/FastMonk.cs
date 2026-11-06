using Godot;
using System;

public partial class FastMonk : Monk
{
    protected override float GetSpeed() => this.MovementSpeed * 2f;
}
