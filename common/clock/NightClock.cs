using Godot;
using System;

public partial class NightClock : Node
{
    [Export] public float RealSecondsPerHour = 96f;
    [Export] public int HoursInNight = 6;
    [Export] public Godot.Collections.Array<HourConfig> Hours;

    [ExportGroup("DEBUG")]
    [Export] public bool Showinits;

    [ExportGroup("Monks")]
    [Export] public TallMonk Tall;
    [Export] public OldMonk Old;
    [Export] public FastMonk Fast;


    public event Action<int> HourChanged;
    public event Action NightEnded;

    public float Elapsed { get; private set; }
    private int _lastHour = -1;
    private bool _running;

    public void StartNight() { Elapsed = 0; _lastHour = -1; _running = true; }
    public void Stop() => _running = false; // call on catch/death

    public override void _Process(double delta)
    {
        if (!_running) return;
        Elapsed += (float)delta;
        int hour = (int)(Elapsed / RealSecondsPerHour);

        if (hour != _lastHour)
        {
            _lastHour = hour;
            if (hour >= HoursInNight)
            {
                _running = false;
				NightEnded?.Invoke();
                return;
            }
			
            ApplyHour(hour);
			HourChanged?.Invoke(hour);
        }
    }

    // 0 -> "12 AM", 1 -> "1 AM", ... 6 -> "6 AM"
    public string GetDisplayTime(int hour) => $"{(hour == 0 ? 12 : hour)} AM";

    private void ApplyHour(int hour)
    {
        string time = $"{(hour == 0 ? 12 : hour)}:00 AM";

        if (Hours == null || hour >= Hours.Count || Hours[hour] == null)
        {
            GD.Print($"[Night] {time} | no config for hour {hour}, nothing applied");
            return;
        }
        var cfg = Hours[hour];


        // debug
        if (Showinits == true) GD.Print($"[Night] {time}");




        Bump(Tall, hour, cfg.TallThreatRate_Additive, cfg.OverallMoveSpeed, cfg.Aggressiveness_Additive);
        Bump(Old,  hour, cfg.OldThreatRate_Additive, cfg.OverallMoveSpeed, cfg.Aggressiveness_Additive);
        Bump(Fast, hour, cfg.FastThreatRate_Additive, cfg.OverallMoveSpeed, cfg.Aggressiveness_Additive);
    }

    
    private void Bump(Monk m, int hour, float threat, float speed, int aggressiveness)
    {
        if (m == null) return;

        if (hour < m.StartHour)
        {
            if (Showinits == true) GD.Print($"    {m.Name,-9} waiting (starts {m.StartHour}:00)");
            return;
        }

        m.BuildRate += threat;
        m.MovementSpeed += speed;
        m.Aggresiveness += aggressiveness;

        string tag = hour == m.StartHour ? "  <- enters" : "";
        if (Showinits == true) GD.Print($"    {m.Name,-9} BuildRate {m.BuildRate:0.000}  Speed {m.MovementSpeed:0.00}{tag}");
    }
}