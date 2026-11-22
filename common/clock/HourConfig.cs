using Godot;
using System;

[GlobalClass]
public partial class HourConfig : Resource
{
    [Export] public float TallThreatRate_Additive = 0.1f;
    [Export] public float OldThreatRate_Additive = 0.1f;
    [Export] public float FastThreatRate_Additive = 0.1f;
    [Export] public float OverallMoveSpeed = 0.1f;
    [Export] public int Aggressiveness_Additive = 0;
}