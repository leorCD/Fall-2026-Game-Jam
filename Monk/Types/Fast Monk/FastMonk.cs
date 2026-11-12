using Godot;
using System;

// Sprints down the hallway with loud footsteps. If you go from visible to hiding while she's at the
// doorway, she walks Inside and watches you. Leave the hiding spot while she's watching and you die.
public partial class FastMonk : Monk
{
    [ExportGroup("Inside Watch")]
    [Export] private float WatchSeconds = 6f;
    private double _watchTimer;

    [ExportGroup("Sound Effects")]
    [Export] private AudioStream footstepSFX;


    protected override float GetSpeed() => MovementSpeed * 2f;


    public override void Init(StaticPlayerController player, MonkMarkers markers)
    {
        base.Init(player, markers);

        Player.HidingChanged -= OnPlayerHidingChanged; // safe if Init runs twice
        Player.HidingChanged += OnPlayerHidingChanged;
    }

    public override void _ExitTree()
    {
        if (Player != null) Player.HidingChanged -= OnPlayerHidingChanged;
    }


    private void PlayFootstepSFX()
    {
        AudioManager.Instance.PlayLoop("fastmonk_footstep", footstepSFX, 2f, 0.1f, "SFX_Hallway", this); // loudest of the three
    }
    private void StopFootstepSFX()
    {
        AudioManager.Instance.StopLoop("fastmonk_footstep");
    }
    private void StartWalking()
    {
        ChangeSprite(Moving);
        PlayFootstepSFX();
    }


    private void OnPlayerHidingChanged(bool hiding)
    {
        if (!hiding || !IsEngaged || CurrentMarker != Markers.Doorway) return;

        _watchTimer = 0;
        GoTo(Markers.Inside);
        StartWalking();
    }


    protected override void OnArrived(Marker3D from, Marker3D to)
    {
        ChangeSprite(Idle);
        StopFootstepSFX();
    }

    protected override Marker3D RandomNextWaypoint()
    {
        var next = base.RandomNextWaypoint();
        StartWalking();
        return next;
    }


    protected override void DoorProcess(double delta)
    {
        if (CurrentMarker != Markers.Inside)
        {
            base.DoorProcess(delta);
            return;
        }

        if (!Player.IsHiding) // came out while she's watching
        {
            Player.AddThreat(1f);
            return;
        }

        _watchTimer += delta;
        if (_watchTimer >= WatchSeconds)
        {
            GoTo(Markers.Doorway, Markers.Away); // leaves the way she came
            StartWalking();
        }
    }


    public override void Freeze()
    {
        base.Freeze();
        StopFootstepSFX();
    }
}