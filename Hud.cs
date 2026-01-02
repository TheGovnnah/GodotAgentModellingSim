using Godot;
using System;

public partial class Hud : Control
{
	private Main main;
	private double timer;
	private Container StatsContainer;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		main = GetNode<Main>("/root/Main");
		StatsContainer = GetNode<Container>("UI/HUD/StatsContainer");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		timer += delta;

		if(timer < 0.25) return;
		timer = 0;

	}
}
