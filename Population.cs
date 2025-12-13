using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Collections;
using Godot.NativeInterop;
public abstract class Population
{
    //This class will manage populations of differet agent types
    // this calss also manages rendering of agents using multimeshinstances
    // finally this class with manage initialisations of agent populations
    // each population will have its own multimeshinstance

    //Population Data
    private int populationSize { get; }
    public Agent[] agents { get;set; }
    protected MultiMeshinst multimesh { get; }
    protected Environment environment;
    public Vector2[] positions;
    public Color[] colors;
    public Vector2[] positionsA;
    public Color[] colorsA;
    public Vector2[] positionsB;
    public Color[] colorsB;

    //Task tracking (BETA)
    protected readonly int batchSize = 500;
    protected Task aiComputeTask;
    protected CellUpdate[] cellUpdates;

    //tracks which buffer is currently in use
    private bool useBufferA = true;
    Vector2[] sourcePositions;
    Color[] sourceColors;

    public ConcurrentQueue<int> freeMultimeshSpaces = new ConcurrentQueue<int>();



    

    //protected System.Threading.ThreadLocal<List<CellUpdate>> cellUpdateBuffers = new System.Threading.ThreadLocal<List<CellUpdate>>(() => new List<CellUpdate>(5000), trackAllValues: true);


    public Population(int popSize, ref Environment environment, Node2D parent)
    {
        populationSize = popSize;
        this.environment = environment;
        agents = new Agent[populationSize * 2];
        positions = new Vector2[populationSize];
        colors = new Color[populationSize];
        multimesh = new MultiMeshinst(GD.Load<Mesh>("res://TestQuadMesh.tres"), populationSize * 2, populationSize, parent);

        positionsA = new Vector2[populationSize *2];
        colorsA = new Color[populationSize *2];
        positionsB = new Vector2[populationSize *2];
        colorsB = new Color[populationSize *2];
        cellUpdates = new CellUpdate[populationSize *2];
        
        
    }

    /*public Population(Agent[] agentsInPop, ref Environment environment, string fileLocationOFMesh, Node2D parent)
    {
        populationSize = agentsInPop.Length;
        this.environment = environment;
        agents = agentsInPop;
        
        multimesh = new MultiMeshinst(GD.Load<Mesh>(fileLocationOFMesh), populationSize * 2, populationSize, parent);
    }

    /*public void updatePopulationAIsteps()
    {
        
        Parallel.ForEach(Partitioner.Create(0, populationSize, batchSize), range =>
        {
            cellUpdateBuffers.Value.Clear();
            for (int i = range.Item1; i < range.Item2; i++)
            {
                agents[i].calculateAIStep();
                var update = agents[i].calculateCellIndex();
                if (update.HasValue)
                {
                    cellUpdateBuffers.Value.Add(update.Value);
                    agents[i].currentCell = update.Value.newCell;
                }
            }
        });
        foreach (var buffer in cellUpdateBuffers.Values)
        {
            foreach (var update in buffer)
            {
                if (update.oldCell != update.newCell)
                {
                    update.oldCell.agentsInCell.Remove(update.agent);
                    update.newCell.agentsInCell.Add(update.agent);
                }
            }
        }
        
        updatePopulationRendering();
    }*/

    public void applyCellUpdates()
    {
        foreach (var update in cellUpdates)
        {
            if(update.agent != null){
                if (update.oldCell != update.newCell && update.oldCell != null)
                {
                    update.oldCell.removeAgentFromCell(update.agent);
                }
                update.newCell.addAgentToCell(update.agent);
            }
            else
            {
                break;
            }
        }
    }
    public void schedulePopulationAIsteps()
    {
        if (aiComputeTask != null && !aiComputeTask.IsCompleted)
        {
            return; // Previous task is still running
        }

        Vector2[]targetPositions = useBufferA ? positionsA : positionsB;
        Color[] targetColors = useBufferA ? colorsA : colorsB;
        Array.Clear(cellUpdates, 0, cellUpdates.Length);
        aiComputeTask = Task.Run(() =>
        {
            Parallel.For(0, populationSize, i =>
            {
                agents[i].calculateAIStep();
                if (!agents[i].agentActive)
                {
                    removeAgent(i);
                }
                var update = agents[i].calculateCellIndex();
                if (update.HasValue)
                {
                    cellUpdates[i] =update.Value;
                }
                targetPositions[i] = agents[i].position;
                agents[i].updateColor();
                targetColors[i] = agents[i].color;
            });
        });
}

    public void updatePopulationRendering()
    {
       
        /*Parallel.For(0, populationSize, i =>
        {
            positions[i] = agents[i].position;
            agents[i].updateColor();
            colors[i] = agents[i].color;
        });
        multimesh.UpdateTransform(populationSize, positions, colors);*/

        if (aiComputeTask != null && aiComputeTask.IsCompleted)
        {
            useBufferA = !useBufferA;
            sourcePositions = useBufferA ? positionsB : positionsA;
            sourceColors = useBufferA ? colorsB : colorsA;

            applyCellUpdates();
            multimesh.UpdateTransform(populationSize, sourcePositions, sourceColors);
            schedulePopulationAIsteps();
        }
    }

    public void removeAgent(int agentIndex)
    {
        freeMultimeshSpaces.Append(agentIndex);
        agents[agentIndex].currentCell.removeAgentFromCell(agents[agentIndex]);
    }

    public virtual void addAgent(Agent agent)
    {
        if(freeMultimeshSpaces.TryDequeue(out int output))
        {

            agents[output] = agent;
        }
    }

}
public class HumanPopulation : Population
{
    Map spawnMap;
    public HumanPopulation(int popSize, ref Environment environment, Node2D parent, ref Map spawnMap) : base(popSize, ref environment, parent)
    { 
        /*this.spawnMap = spawnMap;
        int count = 0;
        for(int i =0; i < 1000; i++)
        {
            for(int j =0; j < 1000; j++)
            {
                for(int k = 0; k < spawnMap.humanSpawnLocations[i,j]; k++)
                {
                    if(spawnMap.humanSpawnLocations[i,j] != 0){
                    Vector2 startPos = new Vector2((GD.Randf()+ i)*30f, (GD.Randf()+j)*30f);
                    agents[count] = new Agent.Human(startPos,ref environment);
                    count++;
                    }
                }
            }
        }
        agents[0].infected = true; // Infect first human for testing
        schedulePopulationAIsteps();*/
        for(int j = 0; j < 100; j++)
        {
            Vector2 clusterPos = new Vector2(GD.Randf() * (environment.width- environment.cellSize), GD.Randf() * (environment.width - environment.cellSize));
        
            for (int i = 0; i < popSize /100; i++)
            {   

            Vector2 startPos = new Vector2(GD.Randf() * environment.cellSize, GD.Randf()* environment.cellSize);
            agents[i + (j * popSize / 100)] = new Human(startPos + clusterPos, ref environment);


            // AI compute section

            }
        }
        agents[0].infected = true; // Infect first human for testing
        schedulePopulationAIsteps();
    }
}

public class MosquitoPopulation : Population
{
    public MosquitoPopulation(int popSize, ref Environment environment, Node2D parent) : base(popSize, ref environment, parent)
    {
        for (int i = 0; i < popSize; i++)
        {
            Vector2 startPos = new Vector2(GD.Randf() * environment.width, GD.Randf() * environment.height);

            if(GD.Randf() < 0.5f)
            {
                agents[i] = new femaleMosquito(startPos, ref environment);
            }
            else
            {
                agents[i] = new MaleMosquito(startPos, ref environment);
            }
        }
        //for(int j =0; j <10; j++){
        //agents[j].infected = true; // Infect first mosquito for testing
        //}
        agents[0].infected = true;
        schedulePopulationAIsteps();
    }

}
public class femaleMosquitoPopulation : Population
{
    public femaleMosquitoPopulation(int popSize, ref Environment environment, Node2D parent) : base(popSize, ref environment, parent)
    {
        for (int i = 0; i < popSize; i++)
        {
            Vector2 startPos = new Vector2(GD.Randf() * environment.width, GD.Randf() * environment.height);
            agents[i] = new Mosquito(startPos, ref environment);
        }
        //for(int j =0; j <10; j++){
        //agents[j].infected = true; // Infect first mosquito for testing
        //}
        agents[0].infected = true;
        schedulePopulationAIsteps();
    }
}

public class MaleMosquitoPopulation : Population
{
    public MaleMosquitoPopulation(int popSize, ref Environment environment, Node2D parent) : base(popSize,ref environment, parent)
    {
        Vector2 breedingSitePos = new Vector2(GD.Randf() * (environment.width-100), GD.Randf() * (environment.height-100));
        for(int i = 0; i < popSize; i++)
        {
            Vector2 startPos = breedingSitePos + new Vector2(GD.Randf() * 100, GD.Randf() * 100);
            agents[i] = new MaleMosquito(startPos, ref environment);
        }
        schedulePopulationAIsteps();
    }
}

public class breedingSites : Population
{
    public breedingSites(int popSize, ref Environment environment, Node2D parent) : base(popSize, ref environment, parent)
    {
        for (int i = 0; i < popSize; i++)
        {
            Vector2 startPos = new Vector2(GD.Randf() * environment.width, GD.Randf() * environment.height);
            agents[i] = new breedingSite(startPos, ref environment);
        }
        schedulePopulationAIsteps();
    }
}