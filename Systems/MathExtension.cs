using Godot;

public static class MathExtensions
{
	/// <summary>
	/// Clamps a float between 0 and 1.
	/// </summary>
	public static float Clamp01(this float value)
	{
		return Mathf.Clamp(value, 0f, 1f);
	}
}
