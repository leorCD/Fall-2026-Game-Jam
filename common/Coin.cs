using Godot;
using System;

public partial class Coin : CsgCylinder3D, IInteractable
{
    [Export] private StaticPlayerController Player;
	[Export] private SlotMachineLogic SlotMachine;
	[Export] private MonkManager MonkManager;
    [Export] private CanvasLayer[] Canvas;
    [Export] private Sprite3D Title;


    public override void _Ready()
    {
        if (Player == null) GD.PrintErr("Missing player");
        if (SlotMachine == null) GD.PrintErr("Missing slot machine");
        if (MonkManager == null) GD.PrintErr("Missing monk manager");
        
		this.Visible = true;
		SlotMachine.Visible = false;
        ToggleCanvasLayers(false);
    }

    public void Interact(StaticPlayerController player)
    {
        if (SlotMachine == null || MonkManager == null) return;
		
        Title.Visible = false;
		this.Visible = false;

		SlotMachine.Visible = true;
        SlotMachine.TurnOn();

        MonkManager.EnableMonks();

        ToggleCanvasLayers(true);

        Player.Chips.AddChips(100);
    }







    private void ToggleCanvasLayers(bool visible)
    {
        foreach (CanvasLayer c in Canvas)
        {
            c.Visible = visible;
        }
    }

}
