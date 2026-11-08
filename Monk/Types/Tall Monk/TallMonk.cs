using Godot;
using System;

public partial class TallMonk : Monk
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




    private void PlayFootstepSFX()
    {
        AudioManager.Instance.PlayLoop("tallmonk_footstep", footstepSFX, -5f, 0.1f, "SFX", this);
    }

    private void StopFootstepSFX()
    {
        AudioManager.Instance.StopLoop("tallmonk_footstep");
    }


    protected override void OnArrived(Marker3D from, Marker3D to)
    {
        ChangeSprite(Idle);
        StopFootstepSFX();
    }


    protected override Marker3D RandomNextWaypoint()
    {
        var next = base.RandomNextWaypoint();
        if (next == Marker_Doorway) Dwell(12);

        ChangeSprite(Moving);
        PlayFootstepSFX();

        return next;
    }
}
