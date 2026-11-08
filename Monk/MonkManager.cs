using Godot;
using System;

public partial class MonkManager : Node
{
	[Export] public StaticPlayerController Player;

	[ExportCategory("Markers")]
    [Export] public Marker3D Marker_Spawn {get;private set;}
    [Export] public Marker3D Marker_Away {get;private set;}
    [Export] public Marker3D Marker_Doorway {get;private set;}

	[Export] public Monk[] activeMonks; // temporary

    public async override void _Ready()
    {
		if (Player == null)
		{
			GD.PrintErr("Missing player export");
			return;
		}

		if (Marker_Spawn == null || Marker_Doorway == null || Marker_Away == null)
		{
			GD.PrintErr("Missing critical marker");
			return;
		}





		for (int i = 0; i < activeMonks.Length; i++)
		{
			if (activeMonks[i] is not Monk monk) continue;

			monk.Init(Player, Marker_Spawn, Marker_Doorway, Marker_Away);
			monk.Visible = true;
		}
    }

}
