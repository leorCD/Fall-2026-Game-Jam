using Godot;
using System;
using System.Threading.Tasks;

public partial class StartGame : StaticBody3D, IInteractable
{
    [Export] private StaticPlayerController Player;
    [Export] private MonkManager MonkManager;
    [Export] private CanvasLayer Shader;
    [Export] public AudioStream StartGameSFX;
    [Export] public Label ChipsDisplay;

    private bool _used;

    public override void _Ready()
    {
        if (Player == null) GD.PrintErr("Missing player");
        if (MonkManager == null) GD.PrintErr("Missing monk manager");

        this.Visible = true;
        Player.SlotMachine.Visible = false;
        ChipsDisplay.Visible = false;
        Shader.Visible = false;
        Player.Menu.Visible = true;

        Enable();
    }

    public void Interact(StaticPlayerController player)
    {
        if (_used) return;
        _used = true;

        this.Visible = false;

        Player.SlotMachine.Visible = true;
        Player.SlotMachine.TurnOn();

        ChipsDisplay.Visible = true;
        Shader.Visible = true;
        Player.Menu.Visible = false;

        MonkManager.EnableMonks();
        Player.Chips.AddChips(1000);
        AudioManager.Instance.PlaySfxClip(StartGameSFX, 0f, 1.35f, volumeDb: -10f, parent: this);

        _ = Disable();
    }

    private void Enable()
    {
        SetCollisionLayerValue(1, true);
    }

    private async Task Disable()
    {
        await ToSignal(GetTree().CreateTimer(2f), Timer.SignalName.Timeout);
        SetCollisionLayerValue(1, false);
    }
}