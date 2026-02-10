using Godot;
using System;
using System.Runtime.CompilerServices;

public partial class Camera2d : Camera2D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}
	float Lerp(float firstFloat, float secondFloat, float by)
	{
		return firstFloat * (1 - by) + secondFloat * by;
	}
	Vector2 Lerp(Vector2 firstVector, Vector2 secondVector, float by)
	{
		float retX = Lerp(firstVector.X, secondVector.X, by);
		float retY = Lerp(firstVector.Y, secondVector.Y, by);
		return new Vector2(retX, retY);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{

	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionType())
		{
			if (@event is InputEventMouseButton mbe && mbe.ButtonIndex == MouseButton.WheelUp)
			{
				Zoom = Zoom * 1.2f;
				GlobalPosition = Lerp(GlobalPosition, GetGlobalMousePosition(), 0.5f);
			}
			else if (@event is InputEventMouseButton mbe2 && mbe2.ButtonIndex == MouseButton.WheelDown)
			{
				Zoom = Zoom * 0.95f;
				GlobalPosition = Lerp(GlobalPosition, -GetLocalMousePosition(), 0.5f);
			}
		}
	}
}