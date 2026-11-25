using Godot;
using System;

// Stands at the doorway whispering, then walks in to check the closet.
// Hiding in the closet is the wrong answer: if you're in there when he gets to it, you're caught.
// The right answer is to be out of the closet, with the room light off, and not act until he leaves.
// The whisper cutting out means he's leaving.
public partial class TallMonk : Monk
{
    [ExportGroup("Doorway")]
    [Export] private float DoorwayLingerSeconds = 2f;
    [Export(PropertyHint.Range, "0, 100")] private float ChanceToCheckCloset = 55; // rolled once after lingering

    [ExportGroup("Closet Check")]
    [Export] private float CheckSeconds = 5f;              // how long he stays at the closet
    [Export] private float LightOnThreatPerSecond = 0.5f;  // threat per second while the room light is on at the closet

    [ExportGroup("References")]
    [Export] private RoomLightSwitch _roomLight;
    [Export] private Closet _closet;

    [ExportGroup("Sound Effects")]
    [Export] private AudioStream footstepSFX;
    [Export] private AudioStream whisperingSFX;

    private double _checkTimer;
    private bool _wasForced;


    public override void _Ready()
    {
        base._Ready();

        if (footstepSFX == null || whisperingSFX == null)
        {
            GD.PrintErr("Empty sfx, some sounds will not play");
        }
        if (_roomLight == null)
        {
            GD.PrintErr("TallMonk has no room light: the closet check will not punish a lit room");
        }
    }

    public override void Init(StaticPlayerController player, MonkMarkers markers)
    {
        base.Init(player, markers);

        Player.Acted -= OnPlayerActed; // safe if Init runs twice
        Player.Acted += OnPlayerActed;
    }

    public override void _ExitTree()
    {
        if (Player != null) Player.Acted -= OnPlayerActed;
    }


    private bool LightIsOn => _roomLight != null && _roomLight.IsOn;

    // seen from the doorway: out of the closet with the light on
    private bool PlayerSeen => !Player.IsHiding && (_roomLight == null || _roomLight.IsOn);


    private void PlayMainFootstepSFX()
    {
        AudioManager.Instance.PlayLoop("tallmonk_footstep", footstepSFX, -5f, 1f, 0.1f, "SFX_Hallway", this);
    }
    private void PlayDampFootstepSFX()
    {
        AudioManager.Instance.PlayLoop("tallmonk_footstep", footstepSFX, -5f, 1f, 0.1f, "SFX_HallwayDamp", this);
    }
    private void StopFootstepSFX()
    {
        AudioManager.Instance.StopLoop("tallmonk_footstep");
    }
    private void StopWhisper()
    {
        AudioManager.Instance.StopLoop("tallmonk_whisper");
    }


    protected override async void OnArrived(Marker3D from, Marker3D to)
    {
        ChangeSprite(Idle);
        StopFootstepSFX();

        if (to == Markers.Doorway)
        {
            AudioManager.Instance.PlayLoop("tallmonk_whisper", whisperingSFX, 12f, 1f, 0.1f, "SFX_Hallway", this);

            await ToSignal(GetTree().CreateTimer(DoorwayLingerSeconds), SceneTreeTimer.SignalName.Timeout);

            // skip if he was frozen or redirected while waiting
            if (!IsEngaged || CurrentMarker != Markers.Doorway) return;

            if (GD.RandRange(1, 100) <= ChanceToCheckCloset)
            {
                ChangeSprite(Moving);
                PlayMainFootstepSFX();
                StartRoute(Markers.DeepInside); // straight line in, same as Fast Monk
            }
            else
            {
                SendBack();
            }
            return;
        }

        if (to == Markers.DeepInside)
        {
            _checkTimer = 0;
            _wasForced = Player.IsForcedToGamble;
            _closet?.interactor.SetOpen(true);
        }
    }


    protected override void DoorProcess(double delta)
    {
        if (CurrentMarker == Markers.DeepInside)
            ClosetProcess(delta);
        else
            DoorwayProcess(delta);
    }

    protected override void TransitionProcess(double delta)
    {
        base.TransitionProcess(delta);

        // on the walk in, a lit room still builds threat (only the inbound leg targets DeepInside)
        if (TargetMarker == Markers.DeepInside && PlayerSeen)
            Player.AddThreat(BuildRate * (float)delta, this, ThreatCause.Seen);
    }


    private void DoorwayProcess(double delta)
    {
        // hiding or a dark room keeps you safe from the doorway. the closet is where that stops working
        if (PlayerSeen)
            Player.AddThreat(BuildRate * (float)delta, this, ThreatCause.Seen);
    }

    private void ClosetProcess(double delta)
    {
        if (Player.IsHiding) // he opened the closet and you're in it
        {
            Player.AddThreat(1f, this, ThreatCause.FoundHiding);
            return;
        }

        if (LightIsOn)
            Player.AddThreat(LightOnThreatPerSecond * (float)delta, this, ThreatCause.LightOn);

        // a forced spin is a spin too
        bool forced = Player.IsForcedToGamble;
        if (forced && !_wasForced)
            Player.AddThreat(1f, this, ThreatCause.ForceGambled);
        _wasForced = forced;

        _checkTimer += delta;
        if (_checkTimer >= CheckSeconds)
            SendBack();
    }







    // any click while he's at the closet counts as moving, except turning the light off
    private void OnPlayerActed(IInteractable what)
    {
        if (!IsEngaged || CurrentMarker != Markers.DeepInside) return;
        if (what is RoomLightSwitch) return;

        bool spin = what is Betbutton b && b.Action == BetAction.Spin;
        Player.AddThreat(1f, this, spin ? ThreatCause.Gambled : ThreatCause.Moved);
    }


    protected override Marker3D RandomNextWaypoint()
    {
        var next = base.RandomNextWaypoint();
        if (next == null) return null;

        if (next != Markers.Doorway)
            StopWhisper();

        ChangeSprite(Moving);
        if (next == Markers.TopLeft || next == Markers.BottomLeft)
            PlayDampFootstepSFX();
        else
            PlayMainFootstepSFX();

        return next;
    }

    public override void SendBack()
    {
        StopWhisper(); // the whisper cutting out is the all-clear
        if (CurrentMarker == Markers.DeepInside)
            _closet?.interactor.SetOpen(false); // doors shut as he leaves
        base.SendBack();

        ChangeSprite(Moving);
        PlayMainFootstepSFX();
    }


    public override void Freeze()
    {
        base.Freeze();
        StopWhisper();
        StopFootstepSFX();
    }
}