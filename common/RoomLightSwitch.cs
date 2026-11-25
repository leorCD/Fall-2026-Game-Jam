using Godot;
using System;

// Click to turn the room light on or off. Tall Monk reads IsOn: a lit room means you're seen.
// Needs a CollisionShape3D on a layer included in the player's interaction Mask.
public partial class RoomLightSwitch : Area3D, IInteractable
{
	[Export] public Godot.Collections.Array<Light3D> Lights { get; set; } = new();
	[Export] public bool StartOn { get; set; } = true;
	[Export] private Light3D Offlight;

	public bool IsOn { get; private set; }
	public event Action<bool> Toggled;
	

	public override void _Ready()
	{
		SetOn(StartOn);
	}

	public void Interact(StaticPlayerController player)
	{
		SetOn(!IsOn);
		player.ChangeIndicatorText(IsOn ? "Light Off" : "Light On");
	}

	public void SetOn(bool on)
	{
		IsOn = on;

		foreach (var light in Lights)
		{
			if (light != null) light.Visible = on;
		}
		Offlight.Visible = !on;

		Toggled?.Invoke(on);
	}
}