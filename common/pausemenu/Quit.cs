using Godot;

public partial class Quit : Button
{
    public override void _Pressed()
    {
        QuitGame(GetTree());
    }

	public static void QuitGame(SceneTree tree)
	{
		tree.Quit();
	}
}
