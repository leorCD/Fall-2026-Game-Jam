using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;


// yes i ripped a page out of fnaf
public abstract partial class Monk : CharacterBody3D
{
    protected StaticPlayerController Player;
    private Random _random = new Random();
    private bool _ready = false;
    private float TickInterval = 5f; // update every 5 ticks

    [ExportCategory("General Settings")]
    [Export] protected int Aggresiveness {get; set;} = 10; // scale from 0-20 like fnaf

    [ExportCategory("Monk Settings")]
    [Export] protected float HeightMeters { get; set; } = 3f;
    [Export] protected float MovementSpeed {get; set;} = 0.8f;

    [ExportGroup("Threat")]
    [Export] public float BuildRate { get; set; } = 0.1f;      // bar fraction per second while player is visible
    [Export] public float ArrivalThreat { get; set; } = 0.2f;  // one-time bump on reaching the door
    public float ActionMeter { get; private set; }
    public event Action<Monk, float> ActionMeterChanged;        // 0-1, handy for a UI bar or audio
    private bool _atDoor;

    public event Action<Monk> Attacking;

    protected Marker3D Marker_Spawn {get; private set;}
    protected Marker3D Marker_Doorway {get; private set;}
    protected Marker3D Marker_Away {get; private set;}

    protected Vector3 MoveDirection {get; private set;}
    protected Marker3D CurrentMarker {get; private set;}
    private Marker3D _targetMarker;
    private double _timer;
    public bool IsMoving
    {
        get => _targetMarker != null;
    }

    [ExportGroup("Textures")]
    protected Sprite3D Sprite1, Sprite2;
    [Export] protected Texture2D Idle;
    [Export] protected Texture2D Moving;


    public async Task Init(StaticPlayerController player, Marker3D spawn, Marker3D doorway, Marker3D away)
    {
        this.Player = player;
        Marker_Spawn = spawn;
        Marker_Doorway = doorway;
        Marker_Away = away;

        CurrentMarker = _random.Next(2) == 0 ? spawn : away;
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

            return; // stop early
        }

        // at door
        if (_atDoor)
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
        MoveDirection = (dest - GlobalPosition).Normalized();   // before moving

        this.GlobalPosition = GlobalPosition.MoveToward(dest, (float)delta * GetSpeed());
        if (this.GlobalPosition.DistanceTo(_targetMarker.GlobalPosition) > 0.01f) return; // safety check incase the distance between two poitns are negligable

        Marker3D from = CurrentMarker;
        CurrentMarker = _targetMarker;
        _targetMarker = null;

        OnArrived(from, CurrentMarker);

        if (CurrentMarker == Marker_Doorway)
        {
            _atDoor = true;
            Player.AddThreat(ArrivalThreat);
        }
        else
        {
            _atDoor = false;
        }
    }


    // --- shared, overridable defaults ---
    public bool IsAtDoor => _atDoor;
    public void Freeze() { _atDoor = false; _ready = false; }
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

        var next = available[_random.Next(0, available.Length)];
        return next;
    }
    public virtual void SendBack()
    {
        _atDoor = false;
        _targetMarker = Marker_Away; // walks away, doesn't teleport WWWWW
    }

    protected virtual void DoorProcess(double delta)
    {
        if (!Player.IsHiding)
            Player.AddThreat(BuildRate * (float)delta);
        else if (Player.Threat <= 0f)
            SendBack(); // player drained the bar, monk gives up
    }

    // protected virtual void Attack()
    // {
    //     _atDoor = false;
    //     _ready = false;           // freeze the monk
    //     Attacking?.Invoke(this);
    // }
}