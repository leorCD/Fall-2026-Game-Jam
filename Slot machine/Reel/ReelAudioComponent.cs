using Godot;

public partial class ReelAudioComponent : AudioStreamPlayer3D
{
    [Export] public AudioStream[] TickSounds;
    [Export] public float PitchVariance = 0.05f;
	

    public override void _Ready()
    {
        MaxPolyphony = 6;   // lets ticks overlap instead of cutting each other off
    }

    public void PlayTick(float speed01 = 1f)
    {
        if (TickSounds == null || TickSounds.Length == 0) return;

        // pick a random sound, but never the same one twice in a row
        int index = (int)(GD.Randi() % TickSounds.Length);

        Stream = TickSounds[index];

        // slower reel = lower, heavier tick, plus a little random variation
        float basePitch = Mathf.Lerp(0.8f, 1.05f, speed01);
        PitchScale = basePitch + (float)GD.RandRange(-PitchVariance, PitchVariance);

        Play();
    }
}