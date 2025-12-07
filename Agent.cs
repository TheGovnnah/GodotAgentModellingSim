using Godot;
using System;
using System.Data;
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

    public bool agentActive = true;

	public Agent(Vector2 startPos, ref Environment environment)
	{
		cellsPerRow = environment.cellsPerRow;	
		position = startPos;
		this.environment = environment;

		currentCell = environment.grid[(int)Math.Truncate(position.X / environment.cellSize), (int)Math.Truncate(position.Y / environment.cellSize)];
		currentCell.GetAgentsInCell(ref position).Add(this);
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

    public void death()
    {
        currentCell.GetAgentsInCell(ref position).Remove(this);
        agentActive = false;
    }
}    

    public class breedingSite : Agent
    {
        public int capacity;
        public breedingSite(Vector2 startPos, ref Environment environment) : base(startPos, ref environment)
        {
        }

        public override void updateColor()
        {
            color = new Color(0, 0, 0); //
        }
        public override void calculateAIStep()
        {
            //breeding sites are static
        }
    }
    
	public class Mosquito : Agent
	{
        Vector2 previousPosition = new Vector2();
		protected int MosqutioAIstate = 4;
        protected int previousAIstate = 4;
        protected bool fertilised = false;
		private Agent targetAgent = null;
		Vector2 movementTarget;
        protected int lifespan = 1440 * 152;//lifespan in minutes (1 day) * 152 days average lifespan
		public Mosquito(Vector2 startPos, ref Environment environment) : base(startPos, ref environment)
		{
            previousPosition = startPos;
		}
        
		public override void updateColor()
		{
            if (!agentActive)
            {
                color = new Color(0,0,0);
            }
			else if (infected)
			{
				color = new Color(1, 0, 0); // Red for infected
			}
			else if(!infected)
			{
				color = new Color(0, 1, 0); // Green for healthy
			}
            
            //color = new Color(cellIndex[0] / cellsPerRow, cellIndex[1] / cellsPerRow, 0);
		}

		public override void calculateAIStep()
        {
            //Mosquito specific AI step calculations
            lifespan--;
            if (lifespan <= 0)
            {
                death();
                MosqutioAIstate = 0;
            }

            switch (MosqutioAIstate)
            {
                case 0:
                    //death (does nothing aside from making the mosquito black)
                    color = new Color(0,0,0);
                    break;
                case 1:
                    //selecting a human target
                    float closestDistance = 999999f;
                    targetAgent = null;
                    foreach (Agent agent in currentCell.GetAgentsInCell(ref position))
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
                            previousAIstate = MosqutioAIstate;
                            MosqutioAIstate = 2;
                        }
                    }
                    if (targetAgent == null)
                    {
                        movementTarget = new Vector2(currentCell.X * environment.cellSize + GD.Randf() * environment.cellSize, currentCell.Y * environment.cellSize + GD.Randf() * environment.cellSize);
                        previousAIstate = MosqutioAIstate;
                        MosqutioAIstate = 5;
                    }
                    break;
                case 2:
                    //moving to target:
                    position = position.MoveToward(targetAgent.position, speed);
                    if (position.DistanceTo(targetAgent.position) < 5)
                    {
                        previousAIstate = MosqutioAIstate;
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
                    previousAIstate = MosqutioAIstate;
                    MosqutioAIstate = 6;
                    targetAgent.targeted = false;
                    break;
                case 4:
                    //navigate to humans based on djikstra map
                    movementTarget = selectDijkstraMove(environment.HumanDijkstraMap);
                    previousAIstate = MosqutioAIstate;
                    MosqutioAIstate = 5;
                    break;
                case 5:
                //move to movement target
                    position = position.MoveToward(movementTarget, speed);
                    
                    if (position.DistanceTo(movementTarget) < 5)
                    {
                        switch (previousAIstate)
                        {
                            case 4:
                                calculateCellIndex();
                                foreach (Agent agent in currentCell.GetAllAgentsInCell())
                                {
                                    if (agent is Human)
                                    {
                                        previousAIstate = MosqutioAIstate;
                                        MosqutioAIstate = 1;
                                        break;
                                    }
                                }
                                if (MosqutioAIstate != 1)
                                {
                                    previousAIstate = MosqutioAIstate;
                                    MosqutioAIstate = 4;
                                }
                                break;
                            case 6:
                                foreach(Agent agent in currentCell.GetAllAgentsInCell())
                                {
                                    if (agent is MaleMosquito)
                                    {
                                        previousAIstate = MosqutioAIstate;
                                        MosqutioAIstate = 8;
                                        break;
                                    }
                                }
                                if(MosqutioAIstate != 8)
                                {
                                    previousAIstate = MosqutioAIstate;
                                    MosqutioAIstate = 7;
                                }
                                break;
                        }
                        
                    }
                    break;
                case 6:
                //move to random position
                //select a random movement target
                    float targetCellX = GD.Randf() * environment.width ;
                    float targetCellY = GD.Randf() * environment.height;
                    movementTarget = new Vector2(targetCellX, targetCellY);
                    previousAIstate = MosqutioAIstate;
                    MosqutioAIstate = 5;
                    break;
                case 7:
                    //attempt to move to male mosquitoes via dikstra map
                    movementTarget = selectDijkstraMove(environment.MaleMosquitoDijkstraMap);
                    previousAIstate = MosqutioAIstate;
                    MosqutioAIstate = 5;
                    break;
                case 8:
                    //attempt to breed
                    calculateCellIndex();
                    foreach(Agent agent in currentCell.GetAllAgentsInCell())
                    {
                        if (agent is MaleMosquito maleMosquito && !maleMosquito.hasBreedingPartner)
                        {
                            fertilised = true;
                            previousAIstate = MosqutioAIstate;
                            MosqutioAIstate = 4;
                            break;
                        }
                    }
                    break;
                case 9:
                    //go to breeding site
                    break;
            }
        }

		public Vector2 selectDijkstraMove(int[,] dikstraMap )
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
                        if(dikstraMap[neighborX,neighborY] < dikstraMap[selectedX,selectedY])
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

public class MaleMosquito : Mosquito 
    {
        private Vector2 breedingSiteTarget;

        public bool hasBreedingPartner = false;
        public MaleMosquito(Vector2 startPos, ref Environment environment) : base(startPos, ref environment)
        {
            lifespan = 1440 * GD.RandRange(3, 7); //lifespan in minutes (1 day) * 152 days average lifespan
            breedingSiteTarget = startPos;
            MosqutioAIstate = 1;
            calculateCellIndex();
        }

        public override void updateColor()
        {
            color = new Color(0.5f, 0.5f, 0.5f); //
        }
        public override void calculateAIStep()
        {
            lifespan--;
            if (lifespan <= 0)
            {
                death();
            }

        switch (MosqutioAIstate)
        {
            case 0:
                //death case(does nothing)
                break;
            
            case 1:
                //wander close to breeding site
                position.X += (GD.Randf() - 0.5f) *2 * speed;
                position.Y += (GD.Randf() - 0.5f) *2 * speed;

                if (position.DistanceTo(breedingSiteTarget) >= 40)
                MosqutioAIstate = 2;
                break;
            case 2:
                position.MoveToward(breedingSiteTarget, speed);
                MosqutioAIstate = 1;
                break;
        }
            //male mosquitos linger around breeding sites & swarm (subcells needed probably)
            calculateCellIndex();

        }
        
    }
