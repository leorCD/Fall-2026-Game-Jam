using Godot;
using System;

public partial class PlayerAudio : Node
{
	[Export] public StaticPlayerController player;
	[ExportGroup("SFX")]
	[Export] public AudioStream Droning;
	[Export] public AudioStream PlayerFootsteps;

    public override void _Ready()
    {
		if (player == null) return;

		player.Died += StopLoop;
		player.ClosetInteracted += OnClosetInteracted;

        AudioManager.Instance.PlayLoop("Droning", Droning, -25f, 0.47f, 1f, "Ambience");
    }

	private void StopLoop()
	{
		AudioManager.Instance.StopLoop("Droning", 0.5f);
	}

	private void OnClosetInteracted()
	{
		AudioManager.Instance.PlaySfxClip(PlayerFootsteps, 0f, 0.85f, pitch: 1.2f, volumeDb: -5f, bus: "Ambience");
	}
}
