using Godot;

public partial class AutoFitLabel3D : Label3D
{
	[Export] public float MaxWidthMeters = 1.5f;
	[Export] public float DefaultPixelSize = 0.005f;

	public void SetDisplay(string newText)
	{
		Text = newText ?? string.Empty;

		if (string.IsNullOrEmpty(Text))
		{
			PixelSize = DefaultPixelSize;
			return;
		}

		// Use the assigned Font, or fall back to Godot's default font
		Font activeFont = Font ?? ThemeDB.FallbackFont;

		// Measures the widest line even if the string contains '\n' line breaks
		float textWidthPx = activeFont.GetMultilineStringSize(
			Text,
			HorizontalAlignment.Left,
			-1,
			FontSize
		).X;

		float currentWidthMeters = textWidthPx * DefaultPixelSize;

		if (currentWidthMeters > MaxWidthMeters && textWidthPx > 0)
		{
			PixelSize = MaxWidthMeters / textWidthPx;
		}
		else
		{
			PixelSize = DefaultPixelSize;
		}
	}

	// Optional overload so you can still pass numbers directly without .ToString()
	public void SetText(int number) => SetText(number.ToString());
	public void SetText(float number) => SetText(number.ToString());
}
