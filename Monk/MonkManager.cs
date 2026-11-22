using Godot;
using System.Collections.Generic;
using System.Linq;



public partial class MonkManager : Node
{
	[Export] public StaticPlayerController Player;
	[Export] public Monk[] activeMonks;

	[ExportCategory("Markers")]
	[Export] public Marker3D TopRight;
	[Export] public Marker3D TopLeft;
	[Export] public Marker3D BottomLeft;
	[Export] public Marker3D BottomRight;
	[Export] public Marker3D Marker_Doorway;
	[Export] public Marker3D Marker_Inside;
	[Export] public Marker3D Marker_DeepInside;
	[Export] public Marker3D Marker_Window;

	private MonkMarkers _markers;
	private readonly HashSet<Monk> _started = new();
	

	[ExportCategory("Jumpscare")]
	[Export] public double KillDelaySeconds = 0.8; // lets the scare play before the death screen

	[ExportCategory("Night")]
	[Export] public NightClock Clock;


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

		if (Clock != null)
		{
			Clock.HourChanged += OnHourChanged;
			Clock.NightEnded += OnNightEnded;
		}
	}

	public void EnableMonks()
	{
		if (activeMonks == null || Player == null) return;

		_markers = new MonkMarkers(TopRight, TopLeft, BottomLeft, BottomRight,
			Marker_Doorway, Marker_Inside, Marker_DeepInside, Marker_Window);

		foreach (var m in activeMonks)
			if (m != null) m.Visible = false;

		Clock?.StartNight();
	}






	private void OnNightEnded()
	{
		GD.Print("[Night] 6:00 AM, freezing all monks");
		FreezeAll();
    	Player.Win();
	}
	private void OnHourChanged(int hour)
	{
		if (_markers == null || activeMonks == null) return;

		foreach (var monk in activeMonks)
		{
			if (monk == null || hour < monk.StartHour) continue;

			if (_started.Add(monk)) // true only the first time
			{
				monk.Init(Player, _markers);
				monk.Visible = true;
			}
		}
	}






	private void OnPlayerCaught()
	{
		// pick the culprit before Freeze, because Freeze clears IsEngaged
		Monk killer = activeMonks.FirstOrDefault(m => m != null && m.IsEngaged);
		FreezeAll();

		killer?.Jumpscare();
		GetTree().CreateTimer(KillDelaySeconds).Timeout += () =>
		{
			string deathtext = "you couldn't take it anymore";

			switch (killer)
			{
				case FastMonk fast:
					deathtext = Pick(fast.CurrentMarker == fast.Markers.DeepInside ? FastInsideLines : FastLines);
					break;
				case TallMonk:
					deathtext = Pick(TallLines);
					break;
				case OldMonk old:
					deathtext = Pick(old.BangedOnWindow ? OldWindowLines : OldDoorLines);
					break;
			};

			Player.Kill(deathtext);
		};
	}
	
	private void FreezeAll()
	{
		Clock?.Stop();
		if (activeMonks == null) return;
		foreach (var m in activeMonks) m?.Freeze();
	}

	public override void _ExitTree()
	{
		if (Clock != null)
		{
			Clock.HourChanged -= OnHourChanged;
			Clock.NightEnded -= OnNightEnded;
		}

		if (Player == null) return;
		Player.ThreatMaxed -= OnPlayerCaught;
		Player.Died -= FreezeAll;
	}











	// old monk at the door
	private static readonly string[] OldDoorLines =
	{
		"you knew better, but you wanted one more spin.",
		"they watched, and you gambled anyway.",
		"you couldn't stop yourself, even with them standing there.",
	};

	// old monk at the window
	private static readonly string[] OldWindowLines =
	{
		"you heard the knocking and spun anyway.",
		"the machine was louder than the warning.",
		"you wanted one more spin more than you wanted to listen.",
	};

	// tall monk
	private static readonly string[] TallLines =
	{
		"you couldn't hide from yourself.",
		"you hated how this felt, and you stayed anyway.",
		"you were too ashamed to move.",
	};

	// fast monk at the doorway
	private static readonly string[] FastLines =
	{
		"you couldn't look at who you used to be.",
		"you felt like a kid caught doing something wrong.",
		"you were just a kid once.",
	};

	// fast monk inside the room
	private static readonly string[] FastInsideLines =
	{
		"you wanted to be found, so you stepped out.",
		"you couldn't ignore the voice in the dark.",
		"you couldn't stay hidden forever.",
	};

	private static string Pick(string[] lines) => lines[(int)(GD.Randi() % (uint)lines.Length)];
}