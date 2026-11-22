using Godot;

public partial class PauseScreen : CanvasLayer
{
    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        Visible = false;
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e.IsActionPressed("Escape"))
        {
            SetPaused(!GetTree().Paused);
            GetViewport().SetInputAsHandled();
        }
    }

    public void SetPaused(bool paused)
    {
        GetTree().Paused = paused;
        Visible = paused;
        Input.MouseMode = paused ? Input.MouseModeEnum.Visible : Input.MouseModeEnum.Confined;
    }


}