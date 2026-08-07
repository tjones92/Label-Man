// Scripts/Systems/Naming/NameDatabase.cs
//
// DEPRECATED. The hardcoded word arrays that used to live here were migrated to the tagged
// JSON lexicon at res://Data/Naming/lexicon.json and are now served by LabelMan.Naming.Lexicon.
// Word selection, weighting, and uniqueness now live in the Core (Systems/Naming/Core).
// This empty Resource shell is retained only so the [GlobalClass] registration / .uid stays
// valid; it can be deleted once no scene or resource references it.

using Godot;

[GlobalClass]
public partial class NameDatabase : Resource {
}
