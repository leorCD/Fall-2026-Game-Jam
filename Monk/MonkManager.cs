using Godot;
using System;
using System.Collections.Generic;
using System.Linq;


public readonly record struct MonkMarkers(
	Marker3D TopRight, Marker3D TopLeft, Marker3D BottomLeft, Marker3D BottomRight,
	Marker3D Doorway, Marker3D Inside, Marker3D DeepInside)
{
	// Hallway is a ring around the room; the doorway is only reachable from TopRight or BottomRight
	public Marker3D[] Neighbors(Marker3D m)
	{
		if (m == TopRight)    return new[] { TopLeft, BottomRight, Doorway };
		if (m == TopLeft)     return new[] { TopRight, BottomLeft };
		if (m == BottomLeft)  return new[] { TopLeft, BottomRight };
		if (m == BottomRight) return new[] { BottomLeft, TopRight, Doorway };
		if (m == Doorway)     return new[] { TopRight, BottomRight, Inside };
		if (m == Inside)      return new[] { Doorway, DeepInside };
		if (m == DeepInside)  return new[] { Inside };
		return Array.Empty<Marker3D>();
	}

	/// Shortest list of markers from 'from' to 'to' (excludes 'from'). Empty if unreachable.
	public List<Marker3D> FindPath(Marker3D from, Marker3D to)
	{
		var path = new List<Marker3D>();
		if (from == null || to == null || from == to) return path;

		var cameFrom = new Dictionary<Marker3D, Marker3D>();
		var queue = new Queue<Marker3D>();
		queue.Enqueue(from);
		cameFrom[from] = null;

		while (queue.Count > 0)
		{
			var cur = queue.Dequeue();
			if (cur == to) break;
			foreach (var n in Neighbors(cur))
			{
				if (n == null || cameFrom.ContainsKey(n)) continue;
				cameFrom[n] = cur;
				queue.Enqueue(n);
			}
		}

		if (!cameFrom.ContainsKey(to)) return path;
		for (var c = to; c != from; c = cameFrom[c]) path.Insert(0, c);
		return path;
	}
}




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

	[Export] public Monk[] activeMonks;

	[ExportCategory("Jumpscare")]
	[Export] public double KillDelaySeconds = 0.8; // lets the scare play before the death screen


	public override void _Ready()
	{
		if (Player == null) { GD.PrintErr("[MonkManager] Missing player export"); return; }
		if (TopRight == null || TopLeft == null || BottomLeft == null || BottomRight == null
			|| Marker_Doorway == null || Marker_Inside == null)
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

		var markers = new MonkMarkers(TopRight, TopLeft, BottomLeft, BottomRight, Marker_Doorway, Marker_Inside, Marker_DeepInside);

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