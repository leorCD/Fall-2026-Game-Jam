using Godot;
using System;

public partial class Closet : Area3D, IInteractable
{
	public event Action Interacted;

	public void Interact(StaticPlayerController player)
	{
		Interacted?.Invoke();
	}
}
