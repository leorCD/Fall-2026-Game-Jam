using Godot;

public partial class TorchLight : OmniLight3D
{
	[ExportCategory("Pulse")]
	[Export] public float BaseEnergy = 1.5f;
	[Export] public float PulseAmount = 0.3f;  // how far the slow pulse swings
	[Export] public float PulseSpeed = 2.0f;   // pulses per ~6 seconds (radians/sec)

	[ExportCategory("Flicker")]
	[Export] public float FlickerAmount = 0.2f; // random fire-like variation
	[Export] public float FlickerSpeed = 6.0f;

	[ExportCategory("Range")]
	[Export] public float RangeSway = 0.4f;     // how much the light radius breathes

	private FastNoiseLite _noise = new();
	private float _time;
	private float _baseRange;

	public bool Active = true;

	public override void _Ready()
	{
		_baseRange = OmniRange;
		_noise.Seed = (int)GD.Randi();
		_time = GD.Randf() * 100f; // random start so multiple torches don't sync up
	}

	public override void _Process(double delta)
	{
		if (Active == false) return;
		_time += (float)delta;

		float pulse = Mathf.Sin(_time * PulseSpeed);
		float flicker = _noise.GetNoise1D(_time * FlickerSpeed * 50f); // -1 to 1

		LightEnergy = Mathf.Max(0f, BaseEnergy + pulse * PulseAmount + flicker * FlickerAmount);
		OmniRange = _baseRange + (pulse * 0.5f + flicker * 0.5f) * RangeSway;
	}
}