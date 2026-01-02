using System;
using Godot;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Threading;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.Design;
using System.Diagnostics;

public class SimulationHandler
{
    //top-level references
    public Environment environment;

    //agent information
    public Agent[] agents { get;set; }
    public Vector2[] positionsA;
    public Color[] colorsA;
    public Vector2[] positionsB;
    public Color[] colorsB;
    public int numAgents = 0;

    //multithreading variables
    protected Task aiComputeTask;
    protected AiUpdate[] aiUpdates;
    private bool useBufferA = true;
    Vector2[] sourcePositions;
    Color[] sourceColors;
    ConcurrentDictionary<Type, ConcurrentBag<IIntent>> intentsByType;
    List<IintentResolver> intentResolvers;

    //multimesh variables
    public ConcurrentStack<int> freeMultimeshSpaces = new ConcurrentStack<int>();
    protected MultiMeshinst multimesh { get; }
    protected int cacheSize; //defines the max possible number of agents in the simulation
    
    public SimulationHandler(int cacheSize, Environment environment, Node2D parent)
    {
        GD.Print("initialising simulation handler");
        //initialise variables
        this.cacheSize = cacheSize;
        this.environment = environment;

        //initialise the multimesh
        multimesh = new MultiMeshinst(GD.Load<Mesh>("res://TestQuadMesh.tres"), cacheSize, numAgents,parent);
        //initialise agents array
        agents = new Agent[cacheSize];
        //initialise the caches 
        positionsA = new Vector2[cacheSize];
        colorsA = new Color[cacheSize];
        positionsB = new Vector2[cacheSize];
        colorsB = new Color[cacheSize];
        aiUpdates = new AiUpdate[cacheSize];
        intentsByType = new ConcurrentDictionary<Type, ConcurrentBag<IIntent>>();

        //initialise the free space stack
        for(int i = cacheSize -1; i >= numAgents; i--)
        {
            freeMultimeshSpaces.Push(i);
        }

        //initialise intent handlers
        intentResolvers = new List<IintentResolver>{new addAgentResolver(this), new BreedingResolver(),new ExclusiveTargetResolver(),new BiteIntentResolver(), new updateCellResolver(), new updatePositionResolver(), new updateAiStateResolver(), new deactivateResolver(this), new updateMoveTargetResolver(), new UpdateTargetAgentResolver() };
    
    }
    public void addAgents(Agent[] agentsToAdd)
    {
        foreach(Agent agent in agentsToAdd)
        {
            addAgent(agent);
        }
    }
    public void removeAgent(int agentIndex)
    {
        freeMultimeshSpaces.Push(agentIndex);
        agents[agentIndex].currentCell.removeAgentFromCell(agents[agentIndex]);
        environment.world.simulationState.OnAgentRemoved(agents[agentIndex]);
        agents[agentIndex] = null;
    }

    public virtual void addAgent(Agent agent)
    {
        if(freeMultimeshSpaces.TryPop(out int output))
        {
            agents[output] = agent;
            agent.index = output;
            
            numAgents ++;
            environment.world.simulationState.OnAgentAdded(agent);

        }
        else
        {
            GD.Print("error in adding agent, trying again");
            System.Threading.Thread.Sleep((int)Math.Abs(GD.Randi() % 100));
            addAgent(agent);
        }
    }

    public void schedulePopulationAIsteps()
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        if (aiComputeTask != null && !aiComputeTask.IsCompleted)
        {
            return; // Previous task is still running
        }

        Vector2[]targetPositions = useBufferA ? positionsA : positionsB;
        Color[] targetColors = useBufferA ? colorsA : colorsB;
        var agentFrame = agents.Where(a => a != null && a.agentActive).ToArray();
        intentsByType = new ConcurrentDictionary<Type, ConcurrentBag<IIntent>>();
        aiComputeTask = Task.Run(() =>
        {
            Parallel.For(0, agentFrame.Length, i =>
            {
                Agent agent = agentFrame[i];
                agent.calculateAIStep();
                agent.updateColor();
                agent.returnCellUpdate();
                var localintents = agent.returnIntents();
                foreach(IIntent intent in localintents)
                {
                    intentsByType.AddOrUpdate(intent.GetType(), new ConcurrentBag<IIntent>{intent},(_, bag) => { bag.Add(intent); return bag; });
                }
                targetPositions[i] = agent.position;
                targetColors[i] = agent.color;
                agent.clearIntents();
            });
        });
        //GD.Print($"Frame took {stopwatch.Elapsed} s to compute");
        stopwatch.Stop();
    }

    public void updatePopulationRendering()
    {
        if (aiComputeTask != null && aiComputeTask.IsCompleted)
        {
            useBufferA = !useBufferA;
            sourcePositions = useBufferA ? positionsB : positionsA;
            sourceColors = useBufferA ? colorsB : colorsA;

            applyAiUpdates();
            multimesh.UpdateTransform(numAgents, sourcePositions, sourceColors);
            schedulePopulationAIsteps();
        }
    }

    public void applyAiUpdates()
    {
        environment.world.simulationState.resetCounters();
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        if(intentsByType.TryGetValue(intentResolvers[0].IntentType, out var intents))
        intentResolvers[0].GenericResolve(intents,environment.world);
        Parallel.ForEach(intentResolvers, intentResolver =>
        {
            if(!(intentResolver is addAgentResolver))
            if(intentsByType.TryGetValue(intentResolver.IntentType, out var intents))
            intentResolver.GenericResolve(intents, environment.world);
        });
        //GD.Print($"Apllying frame took {stopwatch.Elapsed} s to complete");
        stopwatch.Stop();
    }

    public void startSimulation(Population[] populations)
    {
        foreach(Population population in populations)
        {
            GD.Print($"adding {population.GetType()} to simulation handler");
            addAgents(population.agents);
        }
        GD.Print("Simulation starting...");
        schedulePopulationAIsteps();
    }

}