using Godot;
using System;

// Wanders the hallway and sometimes checks the window after reaching the left side.
// He no longer engages at the doorway, he just passes through it like any other hallway marker.
// At the window he reacts to the slot machine: every spin slams the window and bumps the shared threat bar.
public partial class OldMonk : Monk
{
    [ExportGroup("Window")]
    [Export(PropertyHint.Range, "0, 100")] private float WindowChance = 40; // chance to check the window after arriving on the left
    [Export] private float WindowLingerSeconds = 3f;
    [Export] private float WindowBangThreat = 0.3f;
    public event Action WallBanged;
    public bool BangedOnWindow; // debounce: one bang per spin

    [ExportGroup("Sound Effects")]
    [Export] private AudioStream footstepSFX;
    [Export] public AudioStream WindowBangSFX;


    public override void _Ready()
    {
        base._Ready();

        if (footstepSFX == null)
        {
            GD.PrintErr("Empty sfx, no sound will play");
        }
        
        EngagesAtDoorway = false;
    }


    // only runs while he's engaged, and he only engages at the window.
    // no BuildRate and no give-up check: he only reacts to gambling
    protected override void DoorProcess(double delta)
    {
        bool gambling = Player.SlotMachine != null && Player.SlotMachine.JustSpun;

        if (gambling && !BangedOnWindow) // once per spin, not every frame
        {
            BangedOnWindow = true;
            Bang();
        }
        else if (!gambling)
        {
            BangedOnWindow = false;
        }
    }

    private void Bang()
    {
        AudioManager.Instance.PlaySfx3D(WindowBangSFX, this, "SFX_Hallway", volumeDb: 8f);
        Player.AddThreat(WindowBangThreat);
        WallBanged?.Invoke();
    }


    private void PlayMainFootstepSFX()
    {
        AudioManager.Instance.PlayLoop("oldmonk_footstep", footstepSFX, 5f, 1f, 0.1f, "SFX_Hallway", this);
    }
    private void PlayDampFootstepSFX()
    {
        AudioManager.Instance.PlayLoop("oldmonk_footstep", footstepSFX, 5f, 1f, 0.1f, "SFX_HallwayDamp", this);
    }
    private void StopFootstepSFX()
    {
        AudioManager.Instance.StopLoop("oldmonk_footstep");
    }


    protected override async void OnArrived(Marker3D from, Marker3D to)
    {
        ChangeSprite(Idle);
        StopFootstepSFX();
        BangedOnWindow = false;

        if (to == Markers.Window)
        {
            await ToSignal(GetTree().CreateTimer(WindowLingerSeconds), SceneTreeTimer.SignalName.Timeout);

            // skip if the monk was frozen or redirected while waiting
            if (IsEngaged && CurrentMarker == Markers.Window)
                SendBack();
            return;
        }

        // arrived on the left side: roll for the window
        if ((to == Markers.TopLeft || to == Markers.BottomLeft) && GD.RandRange(1, 100) < WindowChance)
        {
            ChangeSprite(Moving);
            PlayDampFootstepSFX();
            GoTo(Markers.Window);
        }
    }

    protected override Marker3D RandomNextWaypoint()
    {
        var next = base.RandomNextWaypoint();
        if (next == null) return null;

        ChangeSprite(Moving);
        if (next == Markers.TopLeft || next == Markers.BottomLeft)
            PlayDampFootstepSFX();
        else
            PlayMainFootstepSFX();

        return next;
    }


    public override void Freeze()
    {
        base.Freeze();
        StopFootstepSFX();
    }
}