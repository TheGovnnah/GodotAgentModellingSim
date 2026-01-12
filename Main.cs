using Godot;
using System;

public partial class Main : Node2D
{
	public World MainWorld;
	public DatabaseHandler databaseHandler;
	public StatsPlotter statsPlotter;
	// Called when the node enters the scene tree for the first time.
	//This is the main script that initialises parts of the simulation

	public override void _EnterTree()
	{
		GD.Print("Simulation Starting...");
		MainWorld = new World(this);
		databaseHandler = new DatabaseHandler(MainWorld.simulationState);
		statsPlotter = new StatsPlotter(this);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		MainWorld?.updateProcess();
		databaseHandler.updateBuffer();
		statsPlotter.writeIntoArraysSystem(MainWorld.simulationState);
		if(MainWorld.tick % 1000 == 0)
		{
			databaseHandler.writeBuffer();
		}
		if((MainWorld.tick - 100)% 1000 == 0)
		{
			statsPlotter.updateGraph();
			statsPlotter.renderGraph();
		}
		if(MainWorld.tick %10 == 0)
		{
			statsPlotter.renderGraph();
		} 
	}
}
