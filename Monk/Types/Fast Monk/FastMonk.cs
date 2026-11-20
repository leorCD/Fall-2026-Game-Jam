using Godot;
using System;

// Sprints down the hallway with loud footsteps. If you go from visible to hiding while she's at the
// doorway, she walks Inside and watches you. Leave the hiding spot while she's watching and you die.
public partial class FastMonk : Monk
{
    private Random _random = new Random();
    [Export] private int ChanceToEnterRoom = 20;

    [ExportGroup("References")]
    [Export] private TorchObject _torch;

    [ExportGroup("Inside Watch")]
    [Export] private float WatchSeconds = 5f;
    private double _watchTimer;

    [ExportGroup("Sound Effects")]
    [Export] private AudioStream footstepSFX;


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


    private void PlayMainFootstepSFX()
    {
        AudioManager.Instance.PlayLoop("fastmonk_footstep", footstepSFX, 3f, 1f, 0.1f, "SFX_Hallway", this);
    }
    private void PlayDampFootstepSFX()
    {
        AudioManager.Instance.PlayLoop("fastmonk_footstep", footstepSFX, 3f, 1f, 0.1f, "SFX_HallwayDamp", this);
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


    private async void OnPlayerHidingChanged(bool hiding)
    {
        if (!hiding || !IsEngaged || CurrentMarker != Markers.Doorway) return;

        if (!DecideToComeIn()) return;

        await ToSignal(GetTree().CreateTimer(1f), Timer.SignalName.Timeout);

        _watchTimer = 0;
        StartRoute(Markers.DeepInside);
        StartWalking();
    }

    private bool DecideToComeIn() // 20% chance to enter the room
    {
        return GD.RandRange(1, 100) <= ChanceToEnterRoom;  
    }


    protected async override void OnArrived(Marker3D from, Marker3D to)
    {
        ChangeSprite(Idle);
        StopFootstepSFX();

        if (CurrentMarker == Markers.DeepInside)
        {
            await ToSignal(GetTree().CreateTimer(1f), Timer.SignalName.Timeout);
            Player.ChangeIndicatorText("Come out.");
        }
    }

    protected override Marker3D RandomNextWaypoint()
    {
        var next = base.RandomNextWaypoint();
        StartWalking(isSideHallway: (next == Markers.TopLeft || next == Markers.BottomLeft));
        
        if (next == Markers.Doorway)
            _torch.TurnOff();
        else
            _torch.TurnOn();
        
        return next;
    }


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

            _torch.TurnOn();
        }
    }


    public override void Freeze()
    {
        base.Freeze();
        StopFootstepSFX();
    }
}