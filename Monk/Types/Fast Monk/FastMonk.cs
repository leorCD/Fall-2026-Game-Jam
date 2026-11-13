using Godot;
using System;

// Sprints down the hallway with loud footsteps. If you go from visible to hiding while she's at the
// doorway, she walks Inside and watches you. Leave the hiding spot while she's watching and you die.
public partial class FastMonk : Monk
{
    private Random _random = new Random();

    [ExportGroup("References")]
    [Export] private TorchObject _torch;

    [ExportGroup("Inside Watch")]
    [Export] private float WatchSeconds = 6f;
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
        StartRoute(Markers.Inside, Markers.DeepInside);
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
        
        if (next == Markers.Doorway)
        {
            _torch.TurnOff();
        }
        
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
            Marker3D last = _random.Next(2) == 0 ? Markers.Spawn : Markers.Away;

            StartRoute(Markers.Inside, Markers.Doorway, last); // leaves the way she came
            StartWalking();
        }
    }


    public override void Freeze()
    {
        base.Freeze();
        StopFootstepSFX();
    }
}