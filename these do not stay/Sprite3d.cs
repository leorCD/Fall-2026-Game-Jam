using Godot;
using System;

public partial class Sprite3d : Sprite3D
{
	private float speed = 7.5f;

	private bool _isActive = false;

    public async override void _Ready()
    {
        await ToSignal(GetTree().CreateTimer(50f), SceneTreeTimer.SignalName.Timeout);

		_isActive = true;
    }


	public override void _Process(double delta)
	{
		if (!_isActive) return;

		Vector3 newPos = this.Position;

		newPos.Z += (float)delta * speed;

		Position = newPos;
	}
}
