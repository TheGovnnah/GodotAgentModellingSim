using Godot;
using System;

public partial class Main : Node2D
{
	World MainWorld;
	// Called when the node enters the scene tree for the first time.
	//This is the main script that initialises parts of the simulation

	public override void _EnterTree()
	{
		GD.Print("Simulation Starting...");
		MainWorld = new World(this);
	}
	public override void _Ready()
	{
		
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (MainWorld == null)
			return;

		MainWorld.updateProcess();
		/*Vector2[] TestingTransforms = new Vector2[10000];
		for (int i = 0; i < 10000; i++)
		{
			TestingTransforms[i] = new Vector2(GD.Randf()*1920, GD.Randf()*1080);
		}
		Color[] testingColors = new Color[10000];
		for (int i = 0; i < 10000; i++)
		{ 
			testingColors[i] = new Color(1, 0, 0);
		}

		MultiMeshinst multimeshtest = new MultiMeshinst(GD.Load<Mesh>("res://TestQuadMesh.tres"), 20000, 10000, this);
		multimeshtest.UpdateTransform(10000, TestingTransforms, testingColors);*/
  
	}
}
