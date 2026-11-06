using Godot;
using System;

public partial class MonkManager : Node
{
	[ExportCategory("Markers")]
    [Export] public Marker3D Marker_Spawn {get;private set;}
    [Export] public Marker3D Marker_Away {get;private set;}
    [Export] public Marker3D Marker_Doorway {get;private set;}

	[Export] public FastMonk[] activeMonks; // temporary

    public async override void _Ready()
    {
		if (Marker_Spawn == null || Marker_Doorway == null || Marker_Away == null)
		{
			GD.PrintErr("Missing critical marker");
			return;
		}





		for (int i = 0; i < activeMonks.Length; i++)
		{
			activeMonks[i].Init(Marker_Spawn, Marker_Doorway, Marker_Away);
		}
    }

}
