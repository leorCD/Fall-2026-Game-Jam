using Godot;
using System;
using System.Linq;

public partial class MonkManager : Node
{
	[Export] public StaticPlayerController Player;

	[ExportCategory("Markers")]
	[Export] public Marker3D Marker_Spawn;
	[Export] public Marker3D Marker_Away;
	[Export] public Marker3D Marker_Doorway;
	[Export] public Marker3D Marker_Inside;

	[Export] public Monk[] activeMonks;

	[ExportCategory("Jumpscare")]
	[Export] public double KillDelaySeconds = 0.8; // lets the scare play before the death screen


	public override void _Ready()
	{
		if (Player == null) { GD.PrintErr("[MonkManager] Missing player export"); return; }
		if (Marker_Spawn == null || Marker_Doorway == null || Marker_Away == null || Marker_Inside == null)
		{
			GD.PrintErr("[MonkManager] Missing critical marker");
			return;
		}

		Player.ThreatMaxed += OnPlayerCaught;
		Player.Died += FreezeAll;
	}

	public void EnableMonks()
	{
		if (activeMonks == null || Player == null) return;

		var markers = new MonkMarkers(Marker_Spawn, Marker_Doorway, Marker_Away, Marker_Inside);

		foreach (var monk in activeMonks)
		{
			if (monk == null) continue;

			monk.Init(Player, markers);
			monk.Visible = true;
		}
	}

	private void OnPlayerCaught()
	{
		// pick the culprit before Freeze, because Freeze clears IsEngaged
		Monk killer = activeMonks.FirstOrDefault(m => m != null && m.IsEngaged);
		FreezeAll();

		killer?.Jumpscare(Player.Camera);
		GetTree().CreateTimer(KillDelaySeconds).Timeout += Player.Kill;
	}

	private void FreezeAll()
	{
		if (activeMonks == null) return;
		foreach (var m in activeMonks) m?.Freeze();
	}

	public override void _ExitTree()
	{
		if (Player == null) return;
		Player.ThreatMaxed -= OnPlayerCaught;
		Player.Died -= FreezeAll;
	}
}