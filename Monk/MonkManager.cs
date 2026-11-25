using Godot;
using System.Collections.Generic;
using System.Linq;



// Why threat was added. The last cause before the bar fills decides which death line plays.
public enum ThreatCause
{
    Generic,      // fallback, no specific cause
    Arrival,      // monk reached the doorway
    Seen,         // out in the open while a monk stood at the doorway
    LightOn,      // room light on while Tall Monk was coming in or checking
    FoundHiding,  // player was inside the closet when Tall Monk opened it
    LeftHiding,   // player stepped out while Fast Monk was watching
    Gambled,      // spin while a monk was listening or watching (Old Monk window, Tall Monk closet)
    ForceGambled, // force gambling made you vulnerable
    Moved,        // any other click while Tall Monk was checking
}





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
		// read these before FreezeAll, which clears IsEngaged
		Monk killer = Player.LastThreatSource ?? activeMonks.FirstOrDefault(m => m != null && m.IsEngaged);
		ThreatCause cause = Player.LastThreatCause;
		FreezeAll();

		// GD.Print($"[MonkManager] caught by {killer?.Name ?? "nobody"} | cause={cause} | hiding={Player.IsHiding}");

		killer?.Jumpscare();
		GetTree().CreateTimer(KillDelaySeconds).Timeout += () =>
		{
			string deathtext = "you couldn't take it anymore";

			switch (killer)
			{
				case FastMonk:
					deathtext = Pick(cause == ThreatCause.LeftHiding ? FastInsideLines : FastLines);
					break;
				case TallMonk:
					deathtext = Pick(TallLinesFor(cause));
					break;
				case OldMonk:
					deathtext = Pick(cause == ThreatCause.ForceGambled ? OldForcedLines : OldWindowLines);
					break;
			}

			// GD.Print($"[MonkManager] death text: {deathtext}");
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















	private static string[] TallLinesFor(ThreatCause cause) => cause switch
	{
		ThreatCause.FoundHiding => TallInClosetLines,
		ThreatCause.LightOn => TallLightLines,
		ThreatCause.Seen => TallLightLines,
		ThreatCause.Gambled => TallGambledLines,
		ThreatCause.ForceGambled => TallForcedLines,
		ThreatCause.Moved => TallMovedLines,
		_ => TallLines, // arrival bump or anything unlabeled
	};

	// tall monk, fallback (arrival bump or unlabeled)
	private static readonly string[] TallLines =
	{
		"just hide.",
		"you were too ashamed to move.",
	};

	// tall monk: you were in the closet
	private static readonly string[] TallInClosetLines =
	{
		"the closet was the first place he looked.",
	};

	// tall monk: the light was on
	private static readonly string[] TallLightLines =
	{
		"the light was on.",
		"you left the light on.",
	};

	// tall monk: you spun while he was looking
	private static readonly string[] TallGambledLines =
	{
		"you reached for the machine while he was there.",
	};

	// tall monk: the urge forced a spin while he was checking
	private static readonly string[] TallForcedLines =
	{
		"you stayed still so long the urge moved for you.",
		"the machine got impatient.",
	};

	// tall monk: any other click
	private static readonly string[] TallMovedLines =
	{
		"he was looking.",
		"i couldn't stay still.",
	};

	// old monk at the window: you spun
	private static readonly string[] OldWindowLines =
	{
		"you only wanted one more spin.",
		"patience."
	};

	// old monk at the window: the urge spun for you
	private static readonly string[] OldForcedLines =
	{
		"you couldn't wait him out.",
	};

	// fast monk at the doorway
	private static readonly string[] FastLines =
	{
		"your fire was put out.",
		"you were still out in the open.",
		"you were caught doing something wrong.",
	};

	// fast monk inside the room: you stepped out
	private static readonly string[] FastInsideLines =
	{
		"why did i step out.",
		"you couldn't ignore the voice in the dark.",
	};

	private static string Pick(string[] lines) => lines[(int)(GD.Randi() % (uint)lines.Length)];
}