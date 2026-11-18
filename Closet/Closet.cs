using Godot;
using System;

public partial class Closet : Node3D
{
	[Export] public ClosetInteraction interactor;
	[Export] public Marker3D HiddenPosition;
    public override void _Ready()
    {
		if (interactor == null) GD.PrintErr("[Closet] No closet interaction");
		if (HiddenPosition == null) GD.PrintErr("[Closet] No hidden position");
    }
}
