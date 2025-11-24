using Godot;
using System;
using System.Linq;
using System.Threading;

public abstract class Agent
{

	public int speed = 30;
	public Vector2 position;
	public volatile bool infected;
	public Environment environment;
	public Cell currentCell;

	public float cellsPerRow;
	public Color color;
	protected int[] cellIndex = new int[2];

	public Agent(Vector2 startPos, ref Environment environment)
	{
		cellsPerRow = environment.cellSize;	
		position = startPos;
		this.environment = environment;
		currentCell = environment.grid[(int)Math.Truncate(position.X / cellsPerRow), (int)Math.Truncate(position.Y / cellsPerRow)];
		currentCell.agentsInCell.Add(this);
		calculateCellIndex();
	}
	public abstract void calculateAIStep();

	public abstract void updateColor();

	public CellUpdate? calculateCellIndex()
	{
		cellIndex[0] = (int)Math.Truncate(position.X / cellsPerRow);
		cellIndex[1] = (int)Math.Truncate(position.Y / cellsPerRow);
		if (currentCell != environment.grid[cellIndex[0], cellIndex[1]])
		{
			currentCell = environment.grid[cellIndex[0], cellIndex[1]];
			return new CellUpdate(currentCell, environment.grid[cellIndex[0], cellIndex[1]], this);
			
		}
		else
		{
			return null;
		}
	}

	public class Mosquito : Agent
	{
		int MosqutioAIstate = 0;
		
		public Mosquito(Vector2 startPos, ref Environment environment) : base(startPos, ref environment)
		{

		}

		public override void updateColor()
		{
			if (infected)
			{
				color = new Color(1, 0, 0); // Red for infected
			}
			else
			{
				color = new Color(0, 1, 0); // Green for healthy
			}
		}

		public override void calculateAIStep()
		{
			//Mosquito specific AI step calculations
			switch (MosqutioAIstate)
			{
				case 0:
					position.X += (GD.Randf() - 0.5f) * speed * 2; position.Y += (GD.Randf() - 0.5f) * speed * 2; if (position.X <= 0) position.X = 1; if (position.Y <= 0) position.Y = 1; if (position.X >= environment.width) position.X = environment.width - 10; if (position.Y >= environment.height) position.Y = environment.height -10;
					calculateCellIndex();
						foreach (Agent agent in currentCell.agentsInCell)
						{
							if (agent is Human)
						{
								float distance = position.DistanceTo(agent.position);
								if(distance <= 600)
								{
										if (infected && !agent.infected)
										{
											agent.infected = true;
											agent.updateColor();
											agent.calculateAIStep();
										}
										else if (!infected && agent.infected)
										{
											infected = true;
										}
						
								}

							}
						}
					break;
				case 1:
					//State 1 behavior
					break;
			}

		}
	}

	public class Human : Agent
	{

		public Human(Vector2 startPos, ref Environment environment) : base(startPos, ref environment)
		{
		}
		public override void updateColor()
		{
			if (infected)
			{
				color = new Color(1, 1, 0); // yellow for infected
			}
			else
			{
				color = new Color(0,0, 1); // blue for healthy
			}
		}
		public override void calculateAIStep()
		{
		}

	}
}
