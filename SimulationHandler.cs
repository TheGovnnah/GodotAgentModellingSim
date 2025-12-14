using System;
using Godot;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Threading;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;

public class SimulationHandler
{
    //top-level references
    protected Environment environment;

    //agent information
    public Agent[] agents { get;set; }
    public Vector2[] positionsA;
    public Color[] colorsA;
    public Vector2[] positionsB;
    public Color[] colorsB;
    public int numAgents = 0;

    //multithreading variables
    protected Task aiComputeTask;
    protected CellUpdate[] cellUpdates;
    private bool useBufferA = true;
    Vector2[] sourcePositions;
    Color[] sourceColors;

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
        cellUpdates = new CellUpdate[cacheSize];

        //initialise the free space stack
        for(int i = cacheSize -1; i >= numAgents; i--)
        {
            freeMultimeshSpaces.Push(i);
        }
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
    }

    public virtual void addAgent(Agent agent)
    {
        if(freeMultimeshSpaces.TryPop(out int output))
        {
            agents[output] = agent;
            agent.index = output;
            numAgents ++;
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
        if (aiComputeTask != null && !aiComputeTask.IsCompleted)
        {
            return; // Previous task is still running
        }

        Vector2[]targetPositions = useBufferA ? positionsA : positionsB;
        Color[] targetColors = useBufferA ? colorsA : colorsB;
        Array.Clear(cellUpdates, 0, cellUpdates.Length);
        aiComputeTask = Task.Run(() =>
        {
            Parallel.For(0, environment.cellsPerRow, i =>
            {
                Parallel.For(0,environment.cellsPerRow, j =>
                {
                    //if(!environment.grid[i,j].subcellsUsed)
                    {
                        Agent[] agentsInCell = environment.grid[i,j].GetAllAgentsInCell().ToArray();
                        foreach(Agent agent in agentsInCell)
                        {
                            agent.calculateAIStep();
                            if (!agent.agentActive)
                            {
                                removeAgent(agent.index);
                            }
                            var update = agent.returnCellUpdate();

                            if (update.HasValue)
                            {
                                cellUpdates[agent.index] =update.Value;
                            }
                            targetPositions[agent.index] = agent.position;
                            agent.updateColor();
                            targetColors[agent.index] = agent.color;
                        }
                    }
                    /*else
                    {
                        Parallel.For(0, 100, k =>
                        {
                            foreach(Agent agent in environment.grid[i,j].subCells[(int)Math.Truncate(k/10f),k%10].GetAllAgentsInCell())
                            {
                                agent.calculateAIStep();
                                if (!agent.agentActive)
                                {
                                    removeAgent(agent.index);
                                }
                                var update = agent.returnCellUpdate();

                                if (update.HasValue)
                                {
                                    cellUpdates[agent.index] =update.Value;
                                }
                                targetPositions[agent.index] = agent.position;
                                agent.updateColor();
                                targetColors[agent.index] = agent.color;
                            }
                        });
                    }*/
                });
            });
        });
    }

    public void updatePopulationRendering()
    {
        if (aiComputeTask != null && aiComputeTask.IsCompleted)
        {
            useBufferA = !useBufferA;
            sourcePositions = useBufferA ? positionsB : positionsA;
            sourceColors = useBufferA ? colorsB : colorsA;

            applyCellUpdates();
            multimesh.UpdateTransform(numAgents, sourcePositions, sourceColors);
            schedulePopulationAIsteps();
        }
    }

    public void applyCellUpdates()
    {
        foreach (var update in cellUpdates)
        {
            if(update.agent != null)
            {
                if (update.oldCell != update.newCell && update.oldCell != null)
                {
                    update.oldCell.removeAgentFromCell(update.agent);
                }
                update.newCell.addAgentToCell(update.agent);
                update.agent.currentCell = update.newCell;
                if(update.newCell.GetAllAgentsInCell().Count > 100)
                {
                    update.newCell.enableSubcells();
                }
                else
                {
                    update.newCell.disableSubcells();
                }
            }
        }
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