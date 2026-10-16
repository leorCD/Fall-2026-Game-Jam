using Godot;
using System;

public partial class toggleenvironment : Node
{
	[Export] public Node3D renderedroom {get; set;}
	[Export] public Node3D unrenderedroom {get; set;}
	private bool RoomIsRendered = false;

    public override void _Ready()
    {
		if (renderedroom == null || unrenderedroom == null) return;

		renderedroom.Visible = RoomIsRendered;
		unrenderedroom.Visible = !RoomIsRendered;
    }


    public override void _Input(InputEvent @event)
    {
		if (renderedroom == null || unrenderedroom == null) return;

        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
		{
			if (keyEvent.Keycode == Key.Q)
			{
				RoomIsRendered = !RoomIsRendered;

				renderedroom.Visible = RoomIsRendered ? true : false;
				unrenderedroom.Visible = !RoomIsRendered ? true : false;
			}
		}
    }

}
