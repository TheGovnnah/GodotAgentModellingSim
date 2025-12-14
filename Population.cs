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
    protected int populationSize { get;set; }
    public Agent[] agents { get;set; }
    protected MultiMeshinst multimesh { get; }
    protected Environment environment;

    public Population(int popSize, ref Environment environment, Node2D parent)
    {
        populationSize = popSize;
        this.environment = environment;
        agents = new Agent[populationSize];
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
            agents[i + (j * popSize / 100)] = new Human(startPos + clusterPos, ref environment,i);
            }
        }
        agents[0].infected = true; // Infect first human for testing
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
                agents[i] = new femaleMosquito(startPos, ref environment,i);
            }
            else
            {
                agents[i] = new MaleMosquito(startPos, ref environment,i);
            }
        }
        agents[0].infected = true;
    }
}

public class breedingSites : Population
{
    public breedingSites(int popSize, ref Environment environment, Node2D parent) : base(popSize, ref environment, parent)
    {
        for (int i = 0; i < popSize; i++)
        {
            Vector2 startPos = new Vector2(GD.Randf() * environment.width, GD.Randf() * environment.height);
            agents[i] = new breedingSite(startPos, ref environment,i);
        }
    }
}
