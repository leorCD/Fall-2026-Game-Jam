using Godot;
using System;

public partial class MonkManager : Node
{
	[ExportCategory("Markers")]
    [Export] public Marker3D Marker_Spawn {get;private set;}
    [Export] public Marker3D Marker_Away {get;private set;}
    [Export] public Marker3D Marker_Doorway {get;private set;}

	[Export] public FastMonk testmonk; // temporary

    public override void _Ready()
    {
		if (Marker_Spawn == null || Marker_Doorway == null || Marker_Away == null)
		{
			GD.PrintErr("Missing critical marker");
			return;
		}

		if (testmonk != null)
		{
			testmonk.Init(Marker_Spawn, Marker_Doorway, Marker_Away);
		}
    }

}
