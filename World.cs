using Godot;
using System;

public partial class World : Node2D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		var player = GetNode<Player>("Player");
		var hud = GetNode<Hud>("HUD");
		
		player.ItemBChanged += hud.SetItemB;
		player.ItemAChanged += hud.SetItemA;
		
		player.HealthChanged += hud.SetHearts;
		player.RupeesChanged += hud.SetRupees;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
