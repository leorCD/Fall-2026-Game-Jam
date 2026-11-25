using Godot;
using System;

// Sprints down the hallway with loud footsteps and puts the torch out as she heads for the doorway.
// When she reaches the doorway she has a chance to come Inside. If she does, hide: she walks in and
// watches, and leaving the hiding spot while she's watching kills you.
public partial class FastMonk : Monk
{
    private Random _random = new Random();
    [Export(PropertyHint.Range, "0, 100")] private float ChanceToEnterRoom = 40; // rolled once when she reaches the doorway
    [Export] private float EnterDelaySeconds = 1f; // pause at the doorway before she walks in

    [ExportGroup("References")]
    [Export] private TorchObject _torch;

    [ExportGroup("Inside Watch")]
    [Export] private float WatchSeconds = 5f;
    private double _watchTimer;

    [ExportGroup("Sound Effects")]
    [Export] private AudioStream footstepSFX;


    private void PlayMainFootstepSFX()
    {
        AudioManager.Instance.PlayLoop("fastmonk_footstep", footstepSFX, 11f, 1f, 0.1f, "SFX_Hallway", this);
    }
    private void PlayDampFootstepSFX()
    {
        AudioManager.Instance.PlayLoop("fastmonk_footstep", footstepSFX, 11f, 1f, 0.1f, "SFX_HallwayDamp", this);
    }
    private void StopFootstepSFX()
    {
        AudioManager.Instance.StopLoop("fastmonk_footstep");
    }
    private void StartWalking(bool isSideHallway = false)
    {
        ChangeSprite(Moving);
        if (isSideHallway)
            PlayDampFootstepSFX();
        else
            PlayMainFootstepSFX();
    }


    private bool DecideToComeIn() // chance to enter the room
    {
        return GD.RandRange(1, 100) <= ChanceToEnterRoom;
    }

    private async void EnterRoomAfterDelay()
    {
        await ToSignal(GetTree().CreateTimer(EnterDelaySeconds), SceneTreeTimer.SignalName.Timeout);

        // skip if she was frozen or redirected while waiting
        if (!IsEngaged || CurrentMarker != Markers.Doorway) return;

        _watchTimer = 0;
        StartRoute(Markers.DeepInside);
        StartWalking();
    }


    protected async override void OnArrived(Marker3D from, Marker3D to)
    {
        ChangeSprite(Idle);
        StopFootstepSFX();

        // the torch going out on the way here was the warning, now she rolls to come in
        if (to == Markers.Doorway && DecideToComeIn())
            EnterRoomAfterDelay();

        if (to == Markers.DeepInside)
        {
            await ToSignal(GetTree().CreateTimer(1f), SceneTreeTimer.SignalName.Timeout);

            if (IsEngaged && CurrentMarker == Markers.DeepInside)
                Player.ChangeIndicatorText("Come out.", fontSize: 40);
        }
    }

    protected override Marker3D RandomNextWaypoint()
    {
        var next = base.RandomNextWaypoint();
        if (next == null) return null;

        StartWalking(isSideHallway: (next == Markers.TopLeft || next == Markers.BottomLeft));

        if (_torch != null)
        {
            if (next == Markers.Doorway || IsDoorwayPass(CurrentMarker, next))
                _torch.TurnOff();
            else
                _torch.TurnOn();
        }

        return next;
    }

    private bool IsDoorwayPass(Marker3D from, Marker3D to) =>
        (from == Markers.TopRight && to == Markers.BottomRight) ||
        (from == Markers.BottomRight && to == Markers.TopRight);


    protected override void DoorProcess(double delta)
    {
        if (CurrentMarker != Markers.DeepInside)
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
            Marker3D last = _random.Next(2) == 0 ? Markers.TopRight : Markers.BottomRight;

            StartRoute(Markers.Doorway, last); // leaves the way she came
            StartWalking();

            _torch?.TurnOn();
        }
    }
    
    public override void SendBack()
    {
        base.SendBack();
        _torch?.TurnOn(); // all-clear: she's leaving, same as when she walks out from inside
    }


    public override void Freeze()
    {
        base.Freeze();
        StopFootstepSFX();
    }
}