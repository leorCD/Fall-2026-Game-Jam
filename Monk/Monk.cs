using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

// yes i ripped a page out of fnaf
public abstract partial class Monk : CharacterBody3D
{
    protected StaticPlayerController Player;
    private readonly Random _random = new Random();
    private bool _ready = false;
    private float TickInterval = 2f; // wander decision every 2 seconds

    [ExportCategory("General Settings")]
    [Export] public int Aggresiveness { get; set; } = 10; // scale from 0-20 like fnaf

    [ExportCategory("Monk Settings")]
    [Export] protected float HeightMeters { get; set; } = 3f;
    [Export] public float MovementSpeed { get; set; } = 0.8f;

    [ExportGroup("Threat")]
    [Export] public float BuildRate { get; set; } = 0.1f;      // bar fraction per second while player is visible
    [Export] public float ArrivalThreat { get; set; } = 0.3f;  // one-time bump on reaching the door

    [ExportGroup("Textures")]
    [Export] protected Texture2D Idle;
    [Export] protected Texture2D Moving;
    protected Sprite3D Sprite;

    public MonkMarkers Markers { get; private set; }
    protected Vector3 MoveDirection { get; private set; }
    public Marker3D CurrentMarker { get; private set; }

    protected Marker3D TargetMarker;
    private readonly Queue<Marker3D> _route = new(); // extra stops after TargetMarker
    private double _timer;
    private bool _engaged; // standing at a marker where DoorProcess runs (doorway, or inside)

    public bool IsMoving => TargetMarker != null;
    public bool IsEngaged => _engaged;

    [ExportGroup("Jumpscare")]
    [Export] protected float LookHeightFraction { get; set; } = 0.85f; // 0 = feet, 1 = top of the sprite
    [Export] protected float LookTime { get; set; } = 0.25f;           // how fast the camera snaps to the monk
    [Export] protected float LungeDistanceFromHead { get; set; } = 0.6f; // how close the camera ends up to the head
    
    [ExportGroup("Night")]
    [Export] public int StartHour { get; set; } = 0;






    public virtual void Init(StaticPlayerController player, MonkMarkers markers)
    {
        Player = player;
        Markers = markers;

        // spawn in one of the back corners, far from the doorway
        CurrentMarker = _random.Next(2) == 0 ? markers.TopLeft : markers.BottomLeft;
        GlobalPosition = CurrentMarker.GlobalPosition;

        _ready = true;
    }


    public override void _Ready()
    {
        foreach (Node child in GetChildren())
        {
            if (child is Sprite3D sprite)
            {
                if (Sprite == null)
                {
                    this.Sprite = sprite;
                    continue;
                }
            }
        }

        if (Sprite == null)
        {
            GD.PrintErr($"{Name} missing sprite object");
            return;
        }

        ChangeSprite(Idle); // also applies the size on spawn
    }





    private void ApplySprite(Sprite3D sprite, Texture2D tex)
    {
        sprite.Texture = tex;

        StandardMaterial3D mat = sprite.MaterialOverride as StandardMaterial3D;
        if (mat != null)
        {
            mat.AlbedoTexture = tex;
            mat.BacklightTexture = tex;
        }
        
        sprite.PixelSize = HeightMeters / tex.GetHeight();
        sprite.Offset = new Vector2(0, tex.GetHeight() / 2f); // feet at the node origin
    }
    protected virtual void ChangeSprite(Texture2D tex)
    {
        // GD.Print($"[{Name}] ChangeSprite -> {tex?.ResourcePath} | moving={IsMoving} | at {CurrentMarker?.Name}");

        if (tex == null || Sprite == null) return;

        ApplySprite(Sprite, tex);
    }
    





    public override void _Process(double delta)
    {
        if (!_ready) return;

        if (TargetMarker != null)
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

        _timer += delta;
        if (_timer >= TickInterval)
        {
            _timer = 0;
            if (ShouldMove())
                TargetMarker = RandomNextWaypoint(); // null if there's nowhere valid to go
        }
    }


    private void Step(double delta)
    {
        var dest = TargetMarker.GlobalPosition;
        MoveDirection = (dest - GlobalPosition).Normalized(); // before moving

        GlobalPosition = GlobalPosition.MoveToward(dest, (float)delta * GetSpeed());
        if (GlobalPosition.DistanceTo(dest) > 0.01f) return;

        Marker3D from = CurrentMarker;
        CurrentMarker = TargetMarker;
        TargetMarker = null;

        if (_route.Count > 0) // if a route exists, dequeue once one is reached
        {
            TargetMarker = _route.Dequeue();
            return;
        }

        OnArrived(from, CurrentMarker);

        if (CurrentMarker == Markers.Doorway)
        {
            _engaged = true;
            Player.AddThreat(ArrivalThreat);
        }
        else if (CurrentMarker == Markers.Inside || CurrentMarker == Markers.DeepInside || CurrentMarker == Markers.Window)
        {
            _engaged = true;
        }
    }


    // walk through the given markers in order (use this when a straight line would cut through the room)
    protected void StartRoute(params Marker3D[] route)
    {
        if (route.Length == 0) return; // early exit

        _engaged = false;
        _route.Clear(); // clear local route
        foreach (var m in route) // no wait time between points
            _route.Enqueue(m);
        TargetMarker = _route.Dequeue();
    }


    // walks to any marker along the hallway graph, one adjacent marker at a time
    protected void GoTo(Marker3D goal)
    {
        if (goal == null) return;

        var path = Markers.FindPath(CurrentMarker, goal);
        if (path.Count == 0) return; // already there, or unreachable

        StartRoute(path.ToArray());
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
        if (Sprite == null) return;
        if (Mathf.Abs(MoveDirection.Z) < 0.01f) return; // no Z movement: keep the last facing

        if (TargetMarker == Markers.BottomLeft)
        {
            Sprite.FlipH = false;
            return;
        }

        bool flip = MoveDirection.Z < 0;
        Sprite.FlipH = flip;
    }

    protected virtual Marker3D RandomNextWaypoint()
    {
        // only hops to an adjacent marker, so monks follow the hallway ring.
        // Inside/DeepInside/Window are intentionally excluded: only reachable through scripted GoTo calls
        var options = Markers.Neighbors(CurrentMarker)
            .Where(m => m != null && m != Markers.Inside && m != Markers.DeepInside && m != Markers.Window)
            .ToArray();

        if (options.Length == 0) return null;
        Marker3D next = options[_random.Next(options.Length)];
        return next;
    }

    public virtual void SendBack()
    {
        _engaged = false;
        _route.Clear();

        // walks away along the hallway, doesn't teleport WWWWW
        Marker3D retreat = _random.Next(2) == 0 ? Markers.TopLeft : Markers.BottomLeft;
        GoTo(retreat);
    }

    protected virtual void DoorProcess(double delta)
    {
        if (!Player.IsHiding)
            Player.AddThreat(BuildRate * (float)delta);
        else if (Player.Threat <= 0f)
            SendBack(); // player drained the bar, monk gives up
    }


    // forces the player's camera to look at the monk's head; override per monk for custom sound or animation
    public virtual void Jumpscare()
    {
        if (Player == null || Player.Camera == null) return;

        Vector3 lookTarget = GlobalPosition + Vector3.Up * (HeightMeters * LookHeightFraction);

        Camera3D cam = Player.Camera;
        Vector3 camPos = cam.GlobalPosition;
        Quaternion from = cam.GlobalTransform.Basis.GetRotationQuaternion();
        Basis toBasis = cam.GlobalTransform.LookingAt(lookTarget, Vector3.Up).Basis;
        Quaternion to = toBasis.GetRotationQuaternion();

        // after the turn
        Vector3 forward = -toBasis.Z;
        float distance = camPos.DistanceTo(lookTarget);
        float travel = Mathf.Max(0f, distance - LungeDistanceFromHead);
        Vector3 endPos = camPos + forward * travel;

        Player.IsJumpscared = true;

        var tween = CreateTween();

        // 1. snap to face the head
        tween.TweenMethod(Callable.From<float>(t =>
        {
            var basis = new Basis(from.Slerp(to, t));
            cam.GlobalTransform = new Transform3D(basis, camPos);
        }), 0f, 1f, LookTime)
            .SetTrans(Tween.TransitionType.Expo)
            .SetEase(Tween.EaseType.Out);

        // 2. then lunge forward along the camera's z-axis
        tween.TweenMethod(Callable.From<float>(t =>
        {
            cam.GlobalTransform = new Transform3D(new Basis(to), camPos.Lerp(endPos, t));
        }), 0f, 1f, 0.35f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.In); // starts slow, accelerates into their face
    }
}