using Godot;
using System;
using System.Linq;


// yes i ripped a page out of fnaf
public abstract partial class Monk : CharacterBody3D
{
    protected StaticPlayerController Player;
    private Random _random = new Random();
    private bool _ready = false;

    [Export] public float HeightMeters { get; set; } = 3f;
    [Export] public float MovementSpeed {get; set;} = 0.5f;
    [Export] public int Aggresiveness {get; set;} = 20; // scale from 0-20 like fnaf
    [Export] public float TickInterval {get; set;} = 5f; // update every 5 ticks

    public event Action<Monk> ArrivedAtDoor;
    public event Action<Monk> Attacking;

    public Marker3D Marker_Spawn {get; private set;}
    public Marker3D Marker_Doorway {get; private set;}
    public Marker3D Marker_Away {get; private set;}

    protected Vector3 MoveDirection {get; private set;}
    protected Marker3D CurrentMarker {get; private set;}
    private Marker3D _targetMarker;
    private double _timer;
    public bool IsMoving
    {
        get => _targetMarker != null;
    }
    private double _pendingDwell;     // dwell queued for the marker we're walking to
    private double _dwellRemaining;   // dwell currently being served

    [ExportGroup("Textures")]
    protected Sprite3D Sprite1, Sprite2;
    [Export] public Texture2D Idle;
    [Export] public Texture2D Moving;


    public void Init(StaticPlayerController player, Marker3D spawn, Marker3D doorway, Marker3D away)
    {
        this.Player = player;
        Marker_Spawn = spawn;
        Marker_Doorway = doorway;
        Marker_Away = away;

        CurrentMarker = spawn;
        GlobalPosition = spawn.GlobalPosition;

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
            GD.PrintErr($"{this.Name}Missing sprite objects");
            return;
        }

        ChangeSprite(Idle);   // also applies the size on spawn
    }


    private void ApplySprite(Sprite3D sprite, Texture2D tex)
    {
        sprite.Texture = tex;
        sprite.PixelSize = HeightMeters / tex.GetHeight();
        sprite.Offset = new Vector2(0, tex.GetHeight() / 2f); // feet at the node origin
    }
    protected virtual void ChangeSprite(Texture2D tex)
    {
        if (tex == null) return;

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

            return; // stop early, no dwell or wandering
        }

        // dwelling
        if (_dwellRemaining > 0)
        {
            _dwellRemaining -= delta;
            DwellProcess(delta);
            if (_dwellRemaining <= 0) OnDwellEnded();

            return; // stop early, no wandering while dwelling
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
        MoveDirection = (dest - GlobalPosition).Normalized();   // before moving

        this.GlobalPosition = GlobalPosition.MoveToward(dest, (float)delta * GetSpeed());
        if (this.GlobalPosition.DistanceTo(_targetMarker.GlobalPosition) > 0.5f) return; // safety check incase the distance between two poitns are negligable

        Marker3D from = CurrentMarker;
        CurrentMarker = _targetMarker;
        _targetMarker = null;

        OnArrived(from, CurrentMarker);

        _dwellRemaining = _pendingDwell;
        _pendingDwell = 0;

        if (CurrentMarker == Marker_Doorway)
            ArrivedAtDoor?.Invoke(this);
    }

    protected void Dwell(double seconds) => _pendingDwell += seconds; // call this right after choosing a target: "when you get there, stay this long"


    // --- shared, overridable defaults ---
    protected virtual bool ShouldMove() => GD.RandRange(1, 20) <= Aggresiveness;
    protected virtual float GetSpeed() => MovementSpeed;
    protected virtual bool CanMove() => true; // freeze conditions
    protected virtual void OnArrived(Marker3D from, Marker3D to) // hook for when they arrive at a marker, which for now can only really be the doorway
    {
        // OLD : if (to == Marker_Away) this.QueueFree(); // call base.OnArrived last bc this is all it does
        GD.Print($"{this.Name} arrived at {CurrentMarker.Name}");
    }
    protected virtual void TransitionProcess(double delta) // hook for footsteps, flicker, etc.
    {
        if (Sprite1 == null || Sprite2 == null) return;
        if (Mathf.Abs(MoveDirection.Z) < 0.01f) return;   // no Z movement: keep the last facing

        bool flip = MoveDirection.Z < 0;
        Sprite1.FlipH = flip;
        Sprite2.FlipH = flip;
    }
    protected virtual Marker3D RandomNextWaypoint()
    {
        // OLD : this.CurrentMarker == Marker_Spawn ? Marker_Doorway : Marker_Away; // can be overriden but we only have 3 positions so this is fine
        Marker3D[] positions = {Marker_Spawn, Marker_Doorway, Marker_Away};

        var available = positions.Where(m => m != CurrentMarker && m != null).ToArray();
        if (available.Length == 0) return CurrentMarker;

        return available[_random.Next(0, available.Length)];
    } 
    protected virtual void OnAtDoorTick(double delta) {GD.Print($"{nameof(FastMonk)} is at the door");} // hook for when they reached the doorway
    public virtual void SendBack()
    {
        _pendingDwell = 0;
        _dwellRemaining = 0;
        _targetMarker = Marker_Away; // walks away, doesn't teleport WWWWW
    }

    protected virtual void DwellProcess(double delta) { }   // looming effects, etc.
    protected virtual void OnDwellEnded() // e.g. fire Attackin
    {
        GD.Print("Dwell ended");
    }
}