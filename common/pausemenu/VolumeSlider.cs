using Godot;
using System.Collections.Generic;

public partial class VolumeSlider : HSlider
{
    [Export] public string BusName = "Master";
    [Export] public Label ValueLabel;
    [Export] public float DefaultPercent = 75f;
    [Export] public float MaxBoostDb = 6f;   // what 100% adds on top of normal

    // remembers settings across scene reloads
    private static readonly Dictionary<string, double> _saved = new();

    private int _bus;

    public override void _Ready()
    {
        _bus = AudioServer.GetBusIndex(BusName);
        if (_bus < 0) { GD.PushError($"No bus named {BusName}"); return; }

        MinValue = 0;
        MaxValue = 100;
        Step = 0.1;

        Value = _saved.TryGetValue(BusName, out var s) ? s : DefaultPercent;
        Apply(Value);

        ValueChanged += v =>
        {
            _saved[BusName] = v;
            Apply(v);
        };
    }

    private void Apply(double v)
    {
        float db;
        if (v <= 0)
            db = -80f;
        else if (v <= DefaultPercent)
            db = Mathf.LinearToDb((float)(v / DefaultPercent));   // 75 -> 0 dB
        else
            db = (float)((v - DefaultPercent) / (100f - DefaultPercent)) * MaxBoostDb; // 100 -> +MaxBoostDb

        AudioServer.SetBusMute(_bus, v <= 0);
        AudioServer.SetBusVolumeDb(_bus, db);

        if (ValueLabel != null)
            ValueLabel.Text = ((int)v).ToString() + "% Volume";
    }
}