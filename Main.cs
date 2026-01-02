using Godot;
using System;

public partial class Main : Node2D
{
	public World MainWorld;
	// Called when the node enters the scene tree for the first time.
	//This is the main script that initialises parts of the simulation

	public override void _EnterTree()
	{
		GD.Print("Simulation Starting...");
		MainWorld = new World(this);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		MainWorld?.updateProcess();
	}
}
