using Godot;
using System.Collections.Generic;

public partial class AudioManager : Node
{
    public static AudioManager Instance { get; private set; }

    [Export] public int SfxPoolSize = 12;

    private readonly List<AudioStreamPlayer3D> _sfxPool = new();
    private AudioStreamPlayer3D _musicA, _musicB, _activeMusic;
    private readonly RandomNumberGenerator _rng = new();
    private readonly Dictionary<string, AudioStreamPlayer3D> _loops = new();

    public override void _Ready()
    {
        _rng.Randomize();

        for (int i = 0; i < SfxPoolSize; i++)
        {
            var p = new AudioStreamPlayer3D { Bus = "SFX" };
            AddChild(p);
            _sfxPool.Add(p);
        }

        _musicA = new AudioStreamPlayer3D { Bus = "Music" };
        _musicB = new AudioStreamPlayer3D { Bus = "Music" };
        AddChild(_musicA);
        AddChild(_musicB);
        _activeMusic = _musicA;
		
		Instance = this;
    }







    public void PlaySfx3D(AudioStream stream, Node3D parent, float pitchVariance = 0.08f, float volumeDb = 0f)
    {
        var player = new AudioStreamPlayer3D
        {
            Stream = stream,
            Bus = "SFX",
            PitchScale = 1f + _rng.RandfRange(-pitchVariance, pitchVariance),
            VolumeDb = volumeDb,
            PanningStrength = 3.0f,
            MaxDistance = 6.5f
        };

        parent.AddChild(player);
        player.Finished += player.QueueFree;
        player.Play();
    }

    // pooled, with slight pitch variation so repeats don't sound identical
    public void PlaySfx(AudioStream stream, float pitchVariance = 0.08f, float volumeDb = 0f)
    {
        var player = _sfxPool.Find(p => !p.Playing) ?? _sfxPool[0]; // steal oldest if full
        player.Stream = stream;
        player.PitchScale = 1f + _rng.RandfRange(-pitchVariance, pitchVariance);
        player.VolumeDb = volumeDb;
        player.Play();
    }

    // crossfade between two players so tension music never cuts abruptly
    public void PlayMusic(AudioStream stream, float fadeTime = 2f)
    {
        var next = _activeMusic == _musicA ? _musicB : _musicA;
        next.Stream = stream;
        next.VolumeDb = -80f;
        next.Play();

        var tween = CreateTween().SetParallel();
        tween.TweenProperty(next, "volume_db", 0f, fadeTime);
        tween.TweenProperty(_activeMusic, "volume_db", -80f, fadeTime);

        _activeMusic = next;
    }



    

    public void PlayLoop(string key, AudioStream stream, float volumeDb = 0f, float fadeTime = 0.5f, string bus = "SFX", Node3D parent = null)
    {
        if (_loops.ContainsKey(key)) return; // already running

        var player = new AudioStreamPlayer3D
        {
            Stream = stream,
            Bus = bus,
            VolumeDb = -80f,
        };

        if (parent != null)
        {
            parent.AddChild(player);
            player.PanningStrength = 3.0f;
            player.MaxDistance = 25f;
        }
        else
            AddChild(player);
        
        player.Play();
        _loops[key] = player;

        CreateTween().TweenProperty(player, "volume_db", volumeDb, fadeTime);
    }

    public void StopLoop(string key, float fadeTime = 0.5f)
    {
        if (!_loops.Remove(key, out var player)) return;

        var tween = CreateTween();
        tween.TweenProperty(player, "volume_db", -80f, fadeTime);
        tween.TweenCallback(Callable.From(player.QueueFree));
    }







    public void SetBusVolume(string bus, float linear)
    {
        int idx = AudioServer.GetBusIndex(bus);
        AudioServer.SetBusVolumeDb(idx, Mathf.LinearToDb(linear));
    }

    // pull ambience down for a scare, then bring it back
    public void DuckBus(string bus, float targetDb, float downTime, float holdTime, float upTime)
    {
        int idx = AudioServer.GetBusIndex(bus);
        float start = AudioServer.GetBusVolumeDb(idx);
        var tween = CreateTween();
        tween.TweenMethod(Callable.From<float>(v => AudioServer.SetBusVolumeDb(idx, v)), start, targetDb, downTime);
        tween.TweenInterval(holdTime);
        tween.TweenMethod(Callable.From<float>(v => AudioServer.SetBusVolumeDb(idx, v)), targetDb, start, upTime);
    }
}