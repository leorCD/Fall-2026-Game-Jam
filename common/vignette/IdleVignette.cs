using Godot;

public partial class IdleVignette : ColorRect
{
    [Export] public float SmoothSpeed = 4f;
    private float _target = 0f;
    private float _current = 0f;
    private ShaderMaterial _mat;

    public override void _Ready()
    {
        _mat = Material as ShaderMaterial;
    }

    public void SetTarget(float t) => _target = Mathf.Clamp(t, 0f, 1f);

    public override void _Process(double delta)
    {
        // smoothing makes the vignette ease open when a spin starts instead of snapping
        _current = Mathf.Lerp(_current, _target, 1f - Mathf.Exp(-SmoothSpeed * (float)delta));
        _mat?.SetShaderParameter("intensity", _current);
    }
}