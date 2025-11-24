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
	public bool targeted = false;
	public float cellsPerRow;
	public Color color;
	protected int[] cellIndex = new int[2];

	public Agent(Vector2 startPos, ref Environment environment)
	{
		cellsPerRow = environment.cellsPerRow;	
		position = startPos;
		this.environment = environment;
		currentCell = environment.grid[(int)Math.Truncate(position.X / environment.cellSize), (int)Math.Truncate(position.Y / environment.cellSize)];
		currentCell.agentsInCell.Add(this);
		calculateCellIndex();
		
	}
	public abstract void calculateAIStep();

	public abstract void updateColor();

	public CellUpdate? calculateCellIndex()
	{
		cellIndex[0] = (int)Math.Truncate(position.X / environment.cellSize);
		cellIndex[1] = (int)Math.Truncate(position.Y / environment.cellSize);
		if (currentCell != environment.grid[cellIndex[0], cellIndex[1]])
		{
			Cell OldCell = currentCell;
			currentCell = environment.grid[cellIndex[0], cellIndex[1]];
			return new CellUpdate(OldCell, environment.grid[cellIndex[0], cellIndex[1]], this);
			
		}
		else
		{
			return null;
		}
	}

	public class Mosquito : Agent
	{
		private int MosqutioAIstate = 4;
		private Agent targetAgent = null;
		Vector2 movementTarget;
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
                    position.X += (GD.Randf() - 0.5f) * speed * 2; position.Y += (GD.Randf() - 0.5f) * speed * 2; if (position.X <= 0) position.X = 1; if (position.Y <= 0) position.Y = 1; if (position.X >= environment.width) position.X = environment.width -1; if (position.Y >= environment.height ) position.Y = environment.height -1;
                    calculateCellIndex();
                        foreach (Agent agent in currentCell.agentsInCell)
                        {
                            if (agent is Human)
                        {
                            MosqutioAIstate = 1;
                            break;
                                

                            }
                        }
                    break;
                case 1:
                    //selecting a human target
                    float closestDistance = 999999f;
                    targetAgent = null;
                    foreach (Agent agent in currentCell.agentsInCell)
                    {
                        if (agent is Human)
                        {
                            float distance = position.DistanceTo(agent.position);
                            if(distance < closestDistance && !agent.targeted && GD.Randf() > 0.75f)
                            {
                                closestDistance = distance;
                            }
                            targetAgent = agent;
                            agent.targeted = true;
                            MosqutioAIstate = 2;
                        }
                    }
                    break;
                case 2:
                    //moving to target:
                    position = position.MoveToward(targetAgent.position, speed);
                    if (position.DistanceTo(targetAgent.position) < 5)
                    {
                        MosqutioAIstate = 3;
                    }
                    break;
                case 3:
                    //biting targetAgent
                    if (infected && !targetAgent.infected)
                                {
                                    targetAgent.infected = true;
                                    targetAgent.updateColor();
                                    targetAgent.calculateAIStep();
                                    break;
                                    }
                                else if (!infected && targetAgent.infected)
                                {
                                    infected = true;
                                    break;
                                }
                    MosqutioAIstate = 6;
                    targetAgent.targeted = false;
                    break;
                case 4:
                    //navigate based on djikstra map
                    movementTarget = selectDijkstraMove();
                    MosqutioAIstate = 5;
                    break;
                case 5:
                //move to movement target
                    position = position.MoveToward(movementTarget, speed);
                    
                    if (position.DistanceTo(movementTarget) < 5)
                    {
                        calculateCellIndex();
                        foreach (Agent agent in currentCell.agentsInCell)
                        {
                            if (agent is Human)
                            {

                                MosqutioAIstate = 1;
                                break;
                            }
                        }
                        if (MosqutioAIstate != 1)
                        {
                            MosqutioAIstate = 4;
                        }
                    }
                    break;
                case 6:
                //move to random position
                //select a random movement target
                    float targetCellX = GD.Randf() * environment.width ;
                    float targetCellY = GD.Randf() * environment.height;
                    movementTarget = new Vector2(targetCellX, targetCellY);
                    MosqutioAIstate = 5;
                    break;
            }

        }

		public Vector2 selectDijkstraMove()
        {
            int selectedX = cellIndex[0];
            int selectedY = cellIndex[1];
            for(int i = -1; i <= 1; i++)
            {
                for(int j = -1; j <= 1; j++)
                {
                    if (i == 0 && j == 0) continue;
                    int neighborX = cellIndex[0] + i;
                    int neighborY = cellIndex[1] + j;
                    if (neighborX >= 0 && neighborX < cellsPerRow && neighborY >= 0 && neighborY < cellsPerRow)
                    {
                        if(environment.dijkstraMap[neighborX,neighborY] < environment.dijkstraMap[selectedX,selectedY])
                        {
                            selectedX = neighborX;
                            selectedY = neighborY;
                        }
                        
                    }
                }
            }
            return new Vector2(selectedX * environment.cellSize + 15, selectedY * environment.cellSize + 15);
        }
	}

	public class Human : Agent
	{

		public Human(Vector2 startPos, ref Environment environment) : base(startPos, ref environment)
        {
			calculateCellIndex();
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
