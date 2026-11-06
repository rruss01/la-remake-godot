using Godot;
using System;

[GlobalClass]
public partial class ItemData : Resource
{
	[Export] public string DisplayName { get; set; } = "";
	[Export] public Texture2D Icon { get; set; }

}
