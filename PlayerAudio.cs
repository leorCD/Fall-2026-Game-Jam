using Godot;
using System;

public partial class PlayerAudio : Node
{
	[Export] public StaticPlayerController player;
	[Export] public AudioStream Droning;

    public override void _Ready()
    {
		if (player != null)
		{
			player.Died += StopLoop;
		}

        AudioManager.Instance.PlayLoop("Droning", Droning, -20f, 1f, "Ambience");
    }

	private void StopLoop()
	{
		AudioManager.Instance.StopLoop("Droning", 0.5f);
	}
}
