using Godot;
using System.Linq;


public partial class MonkManager : Node
{
	[Export] public StaticPlayerController Player;

	[ExportCategory("Markers")]
	[Export] public Marker3D TopRight;
	[Export] public Marker3D TopLeft;
	[Export] public Marker3D BottomLeft;
	[Export] public Marker3D BottomRight;
	[Export] public Marker3D Marker_Doorway;
	[Export] public Marker3D Marker_Inside;
	[Export] public Marker3D Marker_DeepInside;
	[Export] public Marker3D Marker_Window;


	[Export] public Monk[] activeMonks;

	[ExportCategory("Jumpscare")]
	[Export] public double KillDelaySeconds = 0.8; // lets the scare play before the death screen


	public override void _Ready()
	{
		if (Player == null) { GD.PrintErr("[MonkManager] Missing player export"); return; }
		if (TopRight == null || TopLeft == null || BottomLeft == null || BottomRight == null || Marker_Doorway == null || Marker_Inside == null || Marker_DeepInside == null)
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

		var markers = new MonkMarkers(TopRight, TopLeft, BottomLeft, BottomRight, Marker_Doorway, Marker_Inside, Marker_DeepInside, Marker_Window);

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

		killer?.Jumpscare();
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