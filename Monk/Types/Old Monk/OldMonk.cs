using Godot;
using System;

// Stands at the doorway for a few seconds. Only kills you if you gamble in front of him.
// Set ArrivalThreat to 0 in the inspector so he doesn't add to the shared bar on arrival.
public partial class OldMonk : Monk
{
    [ExportGroup("Old Monk")]
    [Export] private float LingerSeconds = 5f;

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


    // no BuildRate and no give-up check: he only reacts to gambling
    protected override void DoorProcess(double delta)
    {
        if (Player.SlotMachine != null && Player.SlotMachine.isActive)
        {
            Player.AddThreat(1f);
        }
    }


    private void PlayFootstepSFX()
    {
        AudioManager.Instance.PlayLoop("oldmonk_footstep", footstepSFX, -4f, 0.1f, "SFX_Hallway", this);
    }
    private void StopFootstepSFX()
    {
        AudioManager.Instance.StopLoop("oldmonk_footstep");
    }


    protected override async void OnArrived(Marker3D from, Marker3D to)
    {
        ChangeSprite(Idle);
        StopFootstepSFX();

        if (to == Markers.Doorway)
        {
            await ToSignal(GetTree().CreateTimer(LingerSeconds), SceneTreeTimer.SignalName.Timeout);

            if (IsEngaged) // skip if the monk was frozen while waiting
                SendBack();
        }
    }

    protected override Marker3D RandomNextWaypoint()
    {
        var next = base.RandomNextWaypoint();

        ChangeSprite(Moving);
        PlayFootstepSFX();

        return next;
    }


    public override void Freeze()
    {
        base.Freeze();
        StopFootstepSFX();
    }
}