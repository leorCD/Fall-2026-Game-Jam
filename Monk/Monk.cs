using Godot;
using System;
using System.Linq;


// yes i ripped a page out of fnaf
public abstract partial class Monk : Node3D
{
    private Random _random = new Random();
    private bool _ready = false;

    [Export] public float MovementSpeed {get;set;} = 0.5f;
    [Export] public int Aggresiveness {get;set;} = 20; // scale from 0-20 like fnaf
    [Export] public float TickInterval {get;set;} = 5f; // update every 5 ticks

    [ExportCategory("Textures")]
    [Export] public Texture idle;
    [Export] public Texture walk;

    public event Action<Monk> ArrivedAtDoor;
    public event Action<Monk> Attacking;

    public Marker3D Marker_Spawn {get;private set;}
    public Marker3D Marker_Doorway {get;private set;}
    public Marker3D Marker_Away {get;private set;}

    protected Marker3D CurrentMarker {get;private set;}
    private Marker3D _targetMarker;
    private double _timer;
    public bool IsMoving
    {
        get => _targetMarker != null;
    }



    public void Init(Marker3D spawn, Marker3D doorway, Marker3D away)
    {
        Marker_Spawn = spawn;
        Marker_Doorway = doorway;
        Marker_Away = away;

        CurrentMarker = spawn;
        GlobalPosition = spawn.GlobalPosition;

        _ready = true;
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

        _timer += delta;
        if (_timer >= TickInterval)
        {
            _timer = 0;
            if (ShouldMove())
                _targetMarker = NextWaypoint();
        }

    }

    private void Step(double delta)
    {
        this.GlobalPosition = GlobalPosition.MoveToward(_targetMarker.GlobalPosition, (float)delta * GetSpeed());
        if (this.GlobalPosition.DistanceTo(_targetMarker.GlobalPosition) > 0.5f) return; // safety check incase the distance between two poitns are negligable

        Marker3D from = CurrentMarker;
        CurrentMarker = _targetMarker;
        _targetMarker = null;

        OnArrived(from, CurrentMarker);

        if (CurrentMarker == Marker_Doorway)
            ArrivedAtDoor?.Invoke(this);
    }




    // --- shared, overridable defaults ---
    protected virtual bool ShouldMove() => GD.RandRange(1, 20) <= Aggresiveness;
    protected virtual float GetSpeed() => MovementSpeed;
    protected virtual bool CanMove() => true; // freeze conditions
    
    protected virtual void OnArrived(Marker3D from, Marker3D to) // hook for when they arrive at a marker, which for now can only really be the doorway
    {
        // if (to == Marker_Away) this.QueueFree(); // call base.OnArrived last bc this is all it does
        GD.Print(this.Name + " arrived at " + CurrentMarker.Name);
    }
    protected virtual void TransitionProcess(double delta) {} // hook for footsteps, flicker, etc.
    protected virtual Marker3D NextWaypoint()
    {
        // this.CurrentMarker == Marker_Spawn ? Marker_Doorway : Marker_Away; // can be overriden but we only have 3 positions so this is fine
        Marker3D[] positions = {Marker_Spawn, Marker_Doorway, Marker_Away};

        return positions[_random.Next(0, positions.Count())];
    } 
    protected virtual void OnAtDoorTick(double delta) {GD.Print($"{nameof(FastMonk)} is at the door");} // hook for when they reached the doorway

    public virtual void SendBack() => _targetMarker = Marker_Away; // walks away, doesn't teleport WWWWW
}