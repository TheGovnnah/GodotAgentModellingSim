using System.Diagnostics;
using Godot;
using System;
public class World
{
    public Environment environment;
    public Population[] populations = new Population[2];

    public int time = 0;

    public World(Node2D parent)
    {
        Map humanSpawnMap = new Map();
        // initialize the environment class
        environment = new Environment(300000, 300000,200);
        //Initialize populations here and add to populations array
        populations[0] = new HumanPopulation(1000, ref environment, parent, ref humanSpawnMap);

        populations[1] = new MosquitoPopulation(70000, ref environment, parent);

    }

    public void updateProcess()
    {
        foreach (Population pop in populations)
        {
            pop.updatePopulationRendering();
        }
        time++;
        //GD.Print("World Time: " + time +"minutes");
    }

}