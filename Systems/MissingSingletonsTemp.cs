// Scripts/Systems/MissingSingletonsTemp.cs
using System.Collections.Generic;
using Godot;

public partial class Rolodex : Node {
	public static Rolodex Instance { get; private set; }
	public override void _EnterTree() { if (Instance == null) Instance = this; }
	public void AdvanceDay() { /* Stub */ }
}
