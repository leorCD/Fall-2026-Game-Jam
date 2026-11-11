using Godot;
using System;

public partial class TallMonk : Monk
{
    [ExportGroup("Sound Effects")]
    [Export] private AudioStream footstepSFX;
    [Export] private AudioStream whisperingSFX;


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
        AudioManager.Instance.PlayLoop("tallmonk_footstep", footstepSFX, -5f, 0.1f, "SFX_Hallway", this);
    }
    private void StopFootstepSFX()
    {
        AudioManager.Instance.StopLoop("tallmonk_footstep");
    }


    protected override void OnArrived(Marker3D from, Marker3D to)
    {
        ChangeSprite(Idle);
        StopFootstepSFX();

        if (CurrentMarker == Marker_Doorway)
        {
            AudioManager.Instance.PlayLoop("tallmonk_whisper", whisperingSFX, 5f, 0.1f, "SFX_Hallway", this);
        }
    }


    protected override Marker3D RandomNextWaypoint()
    {
        var next = base.RandomNextWaypoint();

        if (next != Marker_Doorway)
        {
            AudioManager.Instance.StopLoop("tallmonk_whisper");
        }

        ChangeSprite(Moving);
        PlayFootstepSFX();

        return next;
    }
}
