using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public readonly record struct MonkMarkers(Marker3D Spawn, Marker3D Doorway, Marker3D Away, Marker3D Inside);

// yes i ripped a page out of fnaf
public abstract partial class Monk : CharacterBody3D
{
    protected StaticPlayerController Player;
    private readonly Random _random = new Random();
    private bool _ready = false;
    private float TickInterval = 5f; // wander decision every 5 seconds

    [ExportCategory("General Settings")]
    [Export] protected int Aggresiveness { get; set; } = 10; // scale from 0-20 like fnaf

    [ExportCategory("Monk Settings")]
    [Export] protected float HeightMeters { get; set; } = 3f;
    [Export] protected float MovementSpeed { get; set; } = 0.8f;

    [ExportGroup("Threat")]
    [Export] public float BuildRate { get; set; } = 0.1f;      // bar fraction per second while player is visible
    [Export] public float ArrivalThreat { get; set; } = 0.2f;  // one-time bump on reaching the door

    [ExportGroup("Textures")]
    [Export] protected Texture2D Idle;
    [Export] protected Texture2D Moving;
    protected Sprite3D Sprite1, Sprite2;

    protected MonkMarkers Markers { get; private set; }
    protected Vector3 MoveDirection { get; private set; }
    protected Marker3D CurrentMarker { get; private set; }

    private Marker3D _targetMarker;
    private readonly Queue<Marker3D> _route = new(); // extra stops after _targetMarker
    private double _timer;
    private bool _engaged; // standing at a marker where DoorProcess runs (doorway, or inside)

    public bool IsMoving => _targetMarker != null;
    public bool IsEngaged => _engaged;


    public virtual void Init(StaticPlayerController player, MonkMarkers markers)
    {
        Player = player;
        Markers = markers;

        CurrentMarker = _random.Next(2) == 0 ? markers.Spawn : markers.Away;
        GlobalPosition = CurrentMarker.GlobalPosition;

        _ready = true;
    }


    public override void _Ready()
    {
        foreach (Node child in GetChildren())
        {
            if (child is Sprite3D sprite)
            {
                if (Sprite1 == null)
                {
                    Sprite1 = sprite;
                    continue;
                }

                Sprite2 = sprite;
            }
        }

        if (Sprite1 == null || Sprite2 == null)
        {
            GD.PrintErr($"{Name} missing sprite objects");
            return;
        }

        ChangeSprite(Idle); // also applies the size on spawn
    }


    private void ApplySprite(Sprite3D sprite, Texture2D tex)
    {
        sprite.Texture = tex;
        sprite.PixelSize = HeightMeters / tex.GetHeight();
        sprite.Offset = new Vector2(0, tex.GetHeight() / 2f); // feet at the node origin
    }

    protected virtual void ChangeSprite(Texture2D tex)
    {
        if (tex == null || Sprite1 == null || Sprite2 == null) return;

        ApplySprite(Sprite1, tex);
        ApplySprite(Sprite2, tex);
    }


    public override void _Process(double delta)
    {
        if (!_ready) return;

        if (_targetMarker != null)
        {
            if (CanMove())
                Step(delta);

            TransitionProcess(delta);
            return;
        }

        if (_engaged)
        {
            DoorProcess(delta);
            return;
        }

        // wandering
        _timer += delta;
        if (_timer >= TickInterval)
        {
            _timer = 0;
            if (ShouldMove())
                _targetMarker = RandomNextWaypoint();
        }
    }


    private void Step(double delta)
    {
        var dest = _targetMarker.GlobalPosition;
        MoveDirection = (dest - GlobalPosition).Normalized(); // before moving

        GlobalPosition = GlobalPosition.MoveToward(dest, (float)delta * GetSpeed());
        if (GlobalPosition.DistanceTo(dest) > 0.01f) return;

        Marker3D from = CurrentMarker;
        CurrentMarker = _targetMarker;
        _targetMarker = null;

        if (_route.Count > 0) // passing through, not arriving
        {
            _targetMarker = _route.Dequeue();
            return;
        }

        OnArrived(from, CurrentMarker);

        if (CurrentMarker == Markers.Doorway)
        {
            _engaged = true;
            Player.AddThreat(ArrivalThreat);
        }
        else if (CurrentMarker == Markers.Inside)
        {
            _engaged = true;
        }
    }


    // walk through the given markers in order (use this when a straight line would cut through the room)
    protected void GoTo(params Marker3D[] route)
    {
        if (route.Length == 0) return;

        _engaged = false;
        _route.Clear();
        foreach (var m in route) _route.Enqueue(m);
        _targetMarker = _route.Dequeue();
    }


    // --- shared, overridable defaults ---
    public virtual void Freeze()
    {
        _engaged = false;
        _ready = false;
    }

    protected virtual bool ShouldMove() => GD.RandRange(1, 20) <= Aggresiveness;
    protected virtual float GetSpeed() => MovementSpeed;
    protected virtual bool CanMove() => true; // freeze conditions

    protected virtual void OnArrived(Marker3D from, Marker3D to) // hook for when they arrive at their final marker
    {
        GD.Print($"{Name} arrived at {to.Name}");
    }

    protected virtual void TransitionProcess(double delta) // hook for footsteps, flicker, etc.
    {
        if (Sprite1 == null || Sprite2 == null) return;
        if (Mathf.Abs(MoveDirection.Z) < 0.01f) return; // no Z movement: keep the last facing

        bool flip = MoveDirection.Z < 0;
        Sprite1.FlipH = flip;
        Sprite2.FlipH = flip;
    }

    protected virtual Marker3D RandomNextWaypoint()
    {
        // Inside is intentionally not here: it's only reachable through scripted GoTo calls
        Marker3D[] positions = { Markers.Spawn, Markers.Doorway, Markers.Away };

        var available = positions.Where(m => m != CurrentMarker && m != null).ToArray();
        if (available.Length == 0) return CurrentMarker;

        return available[_random.Next(available.Length)];
    }

    public virtual void SendBack()
    {
        _engaged = false;
        _route.Clear();
        _targetMarker = RandomNextWaypoint(); // walks away, doesn't teleport WWWWW
    }

    protected virtual void DoorProcess(double delta)
    {
        if (!Player.IsHiding)
            Player.AddThreat(BuildRate * (float)delta);
        else if (Player.Threat <= 0f)
            SendBack(); // player drained the bar, monk gives up
    }

    // lunges at the camera; override per monk for custom sound or animation
    public virtual void Jumpscare(Camera3D cam)
    {
        if (cam == null) return;

        Vector3 front = cam.GlobalPosition - cam.GlobalTransform.Basis.Z * 1.5f;
        CreateTween()
            .TweenProperty(this, "global_position", new Vector3(front.X, GlobalPosition.Y, front.Z), 0.4f)
            .SetTrans(Tween.TransitionType.Expo)
            .SetEase(Tween.EaseType.In);
    }
}