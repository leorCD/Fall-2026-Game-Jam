using Godot;
using System;

public partial class OldMonk : Monk
{
    [ExportGroup("Sound Effects")]
    [Export] private AudioStream footstepSFX;


    public override void _Ready()
    {
        base._Ready();

        if (footstepSFX == null)
        {
            GD.PrintErr("Empty sfx, no sound will play");
        }
    }


    protected override void DoorProcess(double delta)
	{
		if (Player.SlotMachine.isActive) // if the player gambles at any time while he's around
		{
			Player.AddThreat(1);
		}
	}


    private void PlayFootstepSFX()
    {
        AudioManager.Instance.PlayLoop("tallmonk_footstep", footstepSFX, -3f, 0.1f, "SFX_Hallway", this);
    }
    private void StopFootstepSFX()
    {
        AudioManager.Instance.StopLoop("tallmonk_footstep");
    }


    protected async override void OnArrived(Marker3D from, Marker3D to)
    {
        ChangeSprite(Idle);
        StopFootstepSFX();

        if (CurrentMarker == Marker_Doorway)
        {
            await ToSignal(GetTree().CreateTimer(5), Timer.SignalName.Timeout); // lingers for 5 sec in the doorway
            SendBack();
            Player.SetThreat(0);
        }
    }

    protected override Marker3D RandomNextWaypoint()
    {
        var next = base.RandomNextWaypoint();

        ChangeSprite(Moving);
        PlayFootstepSFX();

        return next;
    }
}
