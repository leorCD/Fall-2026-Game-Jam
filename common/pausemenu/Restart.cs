using Godot;

public partial class Restart : Button
{
    public override void _Pressed()
    {
        RestartScene(GetTree());
    }

    public static void RestartScene(SceneTree tree)
    {
        AudioManager.Instance.ResetBuses();
        tree.ReloadCurrentScene();

        tree.Paused = false;
        Input.MouseMode = Input.MouseModeEnum.Confined;
    }
}