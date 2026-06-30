using Godot;

public partial class FolderTabButton : Button
{
	[Export] public Color ActiveColor = new("d7b978");
	[Export] public Color InactiveColor = new("b99759");

	public void SetActive(bool active)
	{
		Disabled = active;
		Modulate = active ? ActiveColor : InactiveColor;
	}
}
