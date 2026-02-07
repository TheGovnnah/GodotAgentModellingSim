using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Threading;

public abstract class Agent
{
    public int index {get; set;}
	public int speed = 30;
	public Vector2 position;
	public volatile bool infected;
	public Environment environment;
	public Cell currentCell;
	public bool targeted = false;
	public float cellsPerRow;
	public Color color;
	protected int[] cellIndex = new int[2];
    public Cell localCell;
    public Cell subcell;
    public bool agentActive = true;
    public Vector2 movementTarget;
    public Agent targetAgent;

    public ConcurrentBag<IIntent> intents;
    public virtual void updateAiState(int newState, Agent agent){}
    public virtual void updateAiState(int newState){}
    public virtual void AccessAIState(int newState){}
	public Agent(Vector2 startPos, ref Environment environment, int index)
	{
        this.index = index;
		cellsPerRow = environment.cellsPerRow;	
		position = startPos;
		this.environment = environment;
        intents = new ConcurrentBag<IIntent>();
        calculateCellIndex();
        subcell = localCell;
        currentCell = localCell;
	}
    
	public abstract void calculateAIStep();

	public abstract void updateColor();

    public IEnumerable<IIntent> returnIntents()
    {
        ConcurrentBag<IIntent> LocalIntent = intents;
        return LocalIntent;
    }
    public void clearIntents()
    {
        intents.Clear();
    }

	public void calculateCellIndex()
	{
		cellIndex[0] = (int)Math.Truncate(position.X / environment.cellSize);
		cellIndex[1] = (int)Math.Truncate(position.Y / environment.cellSize);
		if (currentCell != environment.grid[cellIndex[0], cellIndex[1]])
		{
            localCell = environment.grid[cellIndex[0], cellIndex[1]];
		}
        
	}
    public CellUpdate? returnCellUpdate()
    {
        cellIndex[0] = (int)Math.Truncate(position.X / environment.cellSize);
		cellIndex[1] = (int)Math.Truncate(position.Y / environment.cellSize);
		if (currentCell != environment.grid[cellIndex[0], cellIndex[1]])
		{
			Cell OldCell = currentCell;
            currentCell = environment.grid[cellIndex[0], cellIndex[1]];
			CellUpdate cellUpdate =  new CellUpdate(OldCell, environment.grid[cellIndex[0], cellIndex[1]], this);
            intents.Add(new updateCellIntent(this, cellUpdate));
            return cellUpdate;
            
		}
		else
		{
            updateSubcell();
			return null;
		}
    }
    public void moveAgent(Vector2 newPosition)
    {
            if(newPosition.X < 0 || newPosition.X >= environment.width || newPosition.Y < 0 || newPosition.Y >= environment.height)
            {
                position = new Vector2(Mathf.Clamp(newPosition.X, 0, environment.width - 0.1f), Mathf.Clamp(newPosition.Y, 0, environment.height - 0.1f));
                calculateCellIndex();
            }
            else
            {
                position = newPosition;
                calculateCellIndex();
            }
            intents.Add(new updatePositionIntent(this, position));
            
    }
    public bool updateSubcell()
    {
        calculateCellIndex();
        Cell calculatedSubcell = localCell.CalculateSubcell(position);
        if(calculatedSubcell != subcell && calculatedSubcell.subcellsUsed)
        {
            intents.Add(new updateCellIntent(this,new CellUpdate(subcell,calculatedSubcell,this)));
            return true;
        }
        return false;
    }

    public void death()
    {
        agentActive = false;
        intents.Add(new deactivateIntent(this,this));
    }
    public virtual Agent[] GetAgentsToSpawn()
    {
        return null;
    }
    
}    

    public class breedingSite : Agent
    {
        public int capacity;
        public breedingSite(Vector2 startPos, ref Environment environment, int index) : base(startPos, ref environment, index)
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
    
    public class femaleMosquito : Mosquito
    {
        public int breedingTimer;
        public femaleMosquito(Vector2 startPos, ref Environment environment, int index) : base(startPos, ref environment, index)
        {
            MosqutioAIstate = 7;
        }
        public override void calculateAIStep()
        {
            base.calculateAIStep();
            //Mosquito specific AI step calculations
            breedingTimer--;
            switch (MosqutioAIstate)
            {
                case 0:
                    //death (does nothing aside from making the mosquito black)
                    color = new Color(0,0,0);
                    break;
                case 1:
                    //selecting a human target
                    RequestExclusiveTarget(selectTargetAgent(typeof(Human)));
                    //if target found, move to target
                    if (targetAgent != null)
                    {
                        updateAiState(2);
                        break;
                    }
                else
                    {
                        //debug
                    }
                    break;
                case 2:
                    //moving to target:
                    if (moveToTarget(targetAgent.position))
                    {
                        updateAiState(3);
                    }
                    break;
                case 3:
                    //biting targetAgent
                    intents.Add(new BiteIntent(this, targetAgent));
                    updateAiState(10);
                    break;
                case 4:
                    //navigate to humans based on djikstra map
                    updateMovementTarget(selectDijkstraMove(environment.HumanDijkstraMap));
                    updateAiState(5);
                    break;
                case 5:
                //move to movement target
                    if (moveToTarget(movementTarget))
                    {
                        calculateCellIndex();
                        switch (previousAIstate)
                        {
                            case 4:
                                if (localCell.checkCellForAgents(typeof(Human)) && GD.Randf() > 0.5)
                                {
                                    updateAiState(1);
                                    break;
                                }
                                else
                                {
                                    updateAiState(4);
                                    break;
                                }
                            case 7:
                                if (localCell.checkCellForAgents(typeof(MaleMosquito)))
                                {
                                    updateAiState(8);
                                    break;
                                }
                                else
                                {
                                    updateAiState(7);
                                    break;
                                }
                            case 6:
                                updateAiState(4);
                                break;
                        }
                    }
                    break;
                case 6:
                //move to random position
                //select a random movement target
                    float targetCellX = GD.Randf() * (environment.width -1);
                    float targetCellY = GD.Randf() * (environment.height -1);
                    updateMovementTarget(new Vector2(targetCellX, targetCellY));
                    updateAiState(5);
                    break;
                case 7:
                    //attempt to move to male mosquitoes via dikstra map
                    updateMovementTarget(selectClosestFromList(environment.world.populations[2].agents.ToList()).position);
                    updateAiState(5); 
                    break;
                case 8:
                    //attempt to breed
                    if(targetAgent == null && localCell.MaleMosquitoPopulation != 0)
                    {
                        RequestExclusiveTarget(selectTargetAgent(typeof(MaleMosquito)));
                    }
                    else if(targetAgent != null)
                    {
                        updateAiState(9);
                    }
                    else
                    {   
                        updateAiState(7);
                        break;
                    }
                    break;      

                case 9:
                    //go to male mosquito
                    if(moveToTarget(targetAgent.position))
                    {
                        intents.Add(new breedIntent(this, targetAgent));
                        updateAiState(6);
                    }
                    break;
                case 10:
                    //select breeding site
                    fertilised = true;
                    breedingTimer = 1440 * 3; //takes 3 days for eggs to mature to be hatched
                    updateTargetAgent(selectClosestFromList(environment.world.populations[2].agents.ToList()));
                    updateAiState(11);
                    break;
                case 11:
                //move to breeding site
                if (moveToTarget(targetAgent.position))
                {
                    targetAgent.targeted = false;
                    fertilised = false;
                    updateAiState(12);
                }
                break;
                case 12:
                    //lay eggs
                    layEggs();
                    break;
            }
            
            if(oldPosition == position)
            {
                stuckCounter++;
            }
            else
            {
                stuckCounter = 0;
            }
            oldPosition = position;
            if(stuckCounter >= 100 && MosqutioAIstate!= 5 && MosqutioAIstate != 7 && MosqutioAIstate != 8)
            {
                //GD.Print("mosquito stuck");
            }
        }
        private void layEggs()
        {
            if(breedingTimer<= 0)
            {
                Mosquito agentToHatch;
                if(genotype != 0)
                {
                    agentToHatch = new MaleMosquito(position, ref environment, 0);
                    agentToHatch.genotype = genotype;
                }
                else
                {
                    if(GD.Randf() > 0.5)
                    {
                        agentToHatch = new MaleMosquito(position, ref environment, 0);
                    }
                    else
                    {
                        agentToHatch = new femaleMosquito(position, ref environment, 0);
                    }
                }
                for(int i = 0; i < 6; i++)
                {
                    Vector2 eggPos = position;
                    intents.Add(new AddIntent(this,new mosqutioLarvae(eggPos, ref environment,0,agentToHatch)));
                }
                updateAiState(7);
            }
        }
    }
	public class Mosquito : Agent
	{
        public Vector2 oldPosition;
        public int stuckCounter = 0;
		protected int MosqutioAIstate = 4;
        protected int previousAIstate = 4;
        protected bool fertilised = false;
        public int genotype = 00;
        protected int lifespan = 1440 * 152;//lifespan in minutes (1 day) * 152 days average lifespan
		public Mosquito(Vector2 startPos, ref Environment environment, int index) : base(startPos, ref environment, index)
		{
            oldPosition = startPos;
		}

		public override void updateColor()
		{
            if (!agentActive)
            {
                color = new Color(0,0,0);
            }
            if(stuckCounter >= 100)
            {
                color = new Color(1, 0, 1); // Magenta for stuck
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
            calculateCellIndex();
            //Mosquito specific AI step calculations
            lifespan--;
            if (lifespan <= 0)
            {
                death();
                MosqutioAIstate = 0;
                agentActive = false;
            }
            if(GD.Randf() < 0.00021)
            {
                death();
            }

        }
        //Returns the closest agent of the specified type within the current cell, null if none found
        public Agent selectTargetAgent(Type agentType)
        {

            calculateCellIndex();
            Agent selectedAgent = null;
            float closestDistance = 999999f;
            //Agent[] snapshot;
            //lock(localCell){HashSet<Agent> LocalCopy = new HashSet<Agent>(localCell.GetAgentsInCell(ref position));
            //lock(LocalCopy){snapshot = new Agent[LocalCopy.Count];LocalCopy.CopyTo(snapshot);}}
            foreach (Agent agent in localCell.GetAgentsInCell(ref position))
            {
                if(agent != null){
                if (agent.GetType() == agentType && !agent.targeted)
                {
                    float distance = position.DistanceSquaredTo(agent.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        selectedAgent = agent;
                    }
                }
                }
            }
            if (selectedAgent == null && localCell.subcellsUsed == true)
            {
                calculateCellIndex();
                foreach (Cell subcell in localCell.subCells)
                {
                    if(subcell.checkCellForAgents(agentType))
                    {
                        foreach (Agent agent in subcell.GetAllAgentsInCell())
                        {
                            if (agent.GetType() == agentType)
                            {
                                float distance = position.DistanceSquaredTo(agent.position);
                                if (distance <= closestDistance)
                                {
                                    closestDistance = distance;
                                    selectedAgent = agent;
                                }
                            }
                        }
                    }
                }
            }
            return selectedAgent;

        }
        public void updateMovementTarget(Vector2 target)
        {
            intents.Add(new updateMoveTargetIntent(this, target));
        }
        public void updateTargetAgent(Agent agent)
        {
            intents.Add(new UpdateTargetAgentIntent(this, agent));
        }
        public void RequestExclusiveTarget(Agent target)
        {
            if(target != null)
            {
                intents.Add(new ExclusiveTargetIntent(this, target));
            }
        }
        public Agent selectClosestFromList(System.Collections.Generic.List<Agent> agentList)
        {
            Agent selectedAgent = null;
            float closestDistance = 999999f;
            foreach (Agent agent in agentList)
            {
                if(agent != null)
                {
                    float distance = position.DistanceTo(agent.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        selectedAgent = agent;
                    }
                }
            }
            return selectedAgent;
        }
        //moves the agent towards the target position, returns true if within speed meters of target
        public bool moveToTarget(Vector2 target)
        {
            moveAgent(position.MoveToward(target, speed));
            if (position.DistanceTo(target) < (environment.cellSize /2)-1)
            {
                return true;

            }
            return false;
        }
        //selects the best move based on the provided dijkstra map, returns the centre of the cell to move to
		public Vector2 selectDijkstraMove(DjikstraMap djikstraMap )
        {
            calculateCellIndex();
            int selectedX = (int)Math.Truncate((double)(cellIndex[0] / djikstraMap.scale));
            int selectedY = (int)Math.Truncate((double)(cellIndex[1] / djikstraMap.scale));
            int originX = selectedX;
            int originY = selectedY;
            for(int i = -1; i <= 1; i++)
            {
                for(int j = -1; j <= 1; j++)
                {
                    if (i == 0 && j == 0) continue;
                    int neighborX = originX + i;
                    int neighborY = originY + j;
                    if (neighborX >= 0 && neighborX < djikstraMap.cellsPerRow && neighborY >= 0 && neighborY < djikstraMap.cellsPerRow)
                    {
                        if(djikstraMap.map[neighborX,neighborY] < djikstraMap.map[selectedX,selectedY])
                        {
                            selectedX = neighborX;
                            selectedY = neighborY;
                        }
                        if(djikstraMap.map[neighborX,neighborY] == djikstraMap.map[selectedX,selectedY])
                        {
                            //randomly select between equal options
                            if(GD.Randf() < 0.5f)
                            {
                                selectedX = neighborX;
                                selectedY = neighborY;
                            }
                        }
                        
                    }
                }
            }
            return new Vector2(selectedX * djikstraMap.cellSize + djikstraMap.cellSize*0.5f, selectedY * djikstraMap.cellSize + djikstraMap.cellSize*0.5f);
        }

        public override void updateAiState(int newState)
        {
            //previousAIstate = MosqutioAIstate;
            //MosqutioAIstate = newState;

            intents.Add(new updateAiStateIntent(this, newState));
            //testing
        }
    public override void AccessAIState(int newState)
    {
        previousAIstate = MosqutioAIstate;
        MosqutioAIstate = newState;
        
    }
	}
	public class Human : Agent
	{
        private int recoveryTimer = 0;
        private bool recovering = false;
		public Human(Vector2 startPos, ref Environment environment, int index) : base(startPos, ref environment, index)
        {
			calculateCellIndex();
            localCell.addAgentToCell(this);
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
            if (recovering)
            {
                recoveryTimer--;
                if(recoveryTimer <= 0)
                {
                    recovering = false;
                    intents.Add(new RecoverIntent());
                }
            }
            else if (infected)
            {
                recoveryTimer = 1440 * GD.RandRange(8,30);
                recovering = true;
            }
        }   

	}

public class MaleMosquito : Mosquito
{
    private Vector2 breedingSiteTarget;

    public MaleMosquito(Vector2 startPos, ref Environment environment, int index) : base(startPos, ref environment, index)
    {
        lifespan = 1440 * GD.RandRange(3, 7); //lifespan in minutes (1 day) * 152 days average lifespan
        breedingSiteTarget = selectClosestFromList(environment.world.populations[2].agents.ToList()).position;
        MosqutioAIstate = 1;
    }

    public override void updateColor()
    {
        color = new Color(0.5f, 0.5f, 0.5f); //
    }
    public override void calculateAIStep()
    {
        base.calculateAIStep();

        switch (MosqutioAIstate)
        {
            case 0:
                //death case(does nothing)
                break;

            case 1:
                //wander close to breeding site
                if (position.DistanceTo(breedingSiteTarget) >= 100)
                {
                    updateMovementTarget(breedingSiteTarget);
                    updateAiState(4);
                }
                else
                {
                    //select a random movement target near breeding site
                    float targetCellX = breedingSiteTarget.X + (GD.Randf() - 0.5f) * 1000;
                    float targetCellY = breedingSiteTarget.Y + (GD.Randf() - 0.5f) * 1000;
                    if(targetCellX >= environment.height || targetCellY >= environment.width || targetCellX <= 0 || targetCellY <= 0)
                    {
                        break;
                    }
                    updateMovementTarget(new Vector2(targetCellX, targetCellY));
                    updateAiState(4);
                }
                break;
            case 2:
                position.MoveToward(breedingSiteTarget, speed);
                MosqutioAIstate = 1;
                break;
            case 3:
                //updated breeding target from female
                if (moveToTarget(targetAgent.position))
                {
                    updateAiState(1);
                    targetAgent.targeted = false;
                }    
                break;
            case 4:
                //move to movement target
                if (moveToTarget(movementTarget))
                {
                    updateAiState(1);
                }
                break;
        }
        //male mosquitos linger around breeding sites & swarm (subcells needed probably)
        calculateCellIndex();

    }
    public override void updateAiState(int newState, Agent agent)
    {
        previousAIstate = MosqutioAIstate;
        MosqutioAIstate = newState;
        targetAgent = agent;
        intents.Add(new updateAiStateIntent(this,newState));
        intents.Add(new UpdateTargetAgentIntent(this, agent));
    }
}

public class mosqutioLarvae : Agent
{
    int timeToHatch;
    Agent agentToHatch;
    public mosqutioLarvae(Vector2 startpos, ref Environment environment, int index, Agent agentToHatch) : base(startpos, ref environment, index)
    {
        timeToHatch = 14400;
        this.agentToHatch = agentToHatch;
    }
    public override void updateColor()
    {
        color = new Color(timeToHatch/14400f,timeToHatch/28000f,0);
    }
    public override void calculateAIStep()
    {
        timeToHatch--;
        if(timeToHatch <= 0)
        {
            intents.Add(new AddIntent(this, agentToHatch));
            death();
        }
    }
}