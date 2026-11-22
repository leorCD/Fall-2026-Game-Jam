using Godot;

public partial class ClockDisplay : Label3D
{
    [Export] public NightClock ClockLogic;

    public override void _Ready()
    {
        if (ClockLogic == null) { GD.PrintErr("[ClockDisplay] Missing clock"); return; }

        Text = ClockLogic.GetDisplayTime(0); // shows 12 AM before the night starts
        ClockLogic.HourChanged += OnHourChanged;
        ClockLogic.NightEnded += OnNightEnded;
    }

    private void OnHourChanged(int hour) => Text = ClockLogic.GetDisplayTime(hour);

    private void OnNightEnded() => Text = ClockLogic.GetDisplayTime(ClockLogic.HoursInNight);

    public override void _ExitTree()
    {
        if (ClockLogic == null) return;
        ClockLogic.HourChanged -= OnHourChanged;
        ClockLogic.NightEnded -= OnNightEnded;
    }
}