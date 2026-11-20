using Godot;
using System;

public partial class TorchObject : MeshInstance3D
{
    [Export] public TorchLight light;
    private Tween _lightTween;
    [Export] public MeshInstance3D fire;
    [Export] public AudioStreamPlayer3D sound;


    public async void TurnOff()
    {
        if (light == null) return;

        light.Active = false;

        _lightTween?.Kill();
        _lightTween = CreateTween();
        _lightTween.TweenProperty(light, "omni_range", 0f, 0.05f)
            .SetTrans(Tween.TransitionType.Circ)
            .SetEase(Tween.EaseType.Out);

        fire.Visible = false;
        sound.Playing = false;
    }

    public async void TurnOn()
    {
        _lightTween?.Kill();
        _lightTween = CreateTween();
        _lightTween.TweenProperty(light, "omni_range", 4f, 0.05f)
            .SetTrans(Tween.TransitionType.Circ)
            .SetEase(Tween.EaseType.Out);
            
        fire.Visible = true;
        sound.Playing = true;



        await ToSignal(_lightTween, Tween.SignalName.Finished);
        light.Active = true;
    }
}
