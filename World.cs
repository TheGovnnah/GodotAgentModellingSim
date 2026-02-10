using System.Diagnostics;
using Godot;
using System;
using System.Globalization;
using System.Threading.Tasks;
using System.IO;
public class World
{
    //intial variables *TO BE REPLACED WITH A CONFIG FILE*
    private int initialHumanPopulation = 2658;
    private int initialMosquitoPopulation = 500;
    private int initialBreedingSitePopulation = 10;
    private int worldHeight = 30000;
    public int worldWidth = 30000;
    public int cellSize = 60;
    //Godot/environment initialising variables
    public Node2D parentNode;
    public Environment environment;
    public Population[] populations = new Population[3];
    public SimulationHandler simulationHandler;
    //book-keeping variables

    public int tick = 0;
    public SimulationState simulationState = new SimulationState();


    public World(Node2D parent)
    {
        //initialise parent node, environment & simulation handler 
        parentNode = parent;
        environment = new Environment(worldWidth, worldHeight, cellSize, this);
        simulationHandler = new SimulationHandler(100000, environment, parentNode);
        initialisePopulations();

        simulationHandler.startSimulation(populations);
        //calculate djikstra map for humans at full scale
        environment.HumanDijkstraMap = DijkstraCalculator(1, typeof(Human));
    }

    public void initialisePopulations()
    {
        Map humanSpawnMap = new Map();
        populations[2] = new breedingSites(initialBreedingSitePopulation, ref environment, parentNode);
        populations[0] = new HumanPopulation(initialHumanPopulation, ref environment, parentNode, ref humanSpawnMap);
        populations[1] = new MosquitoPopulation(initialMosquitoPopulation, ref environment, parentNode);
    }

    public void updateProcess()
    {
        simulationState.tick = tick;
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        simulationHandler.updatePopulationRendering();
        stopwatch.Stop();
        //GD.Print($"Frame took {stopwatch.ElapsedMilliseconds} ms to complete");
        stopwatch.Reset();
        tick++;
    }

    public DjikstraMap DijkstraCalculator(int scale, Type agentType)
    {
        GD.Print("initialising Dijkstra calculation");
        int MapCellSize = cellSize * scale;
        int NumCells = worldHeight / MapCellSize;
        DjikstraMap djikstraMap = new DjikstraMap(MapCellSize, worldHeight, worldHeight, scale);

        for (int i = 0; i < NumCells; i++)
        {
            for (int j = 0; j < NumCells; j++)
            {
                int localCount = 0;
                for (int k = 0; k < scale; k++)
                {
                    for (int l = 0; l < scale; l++)
                    {
                        if (environment.grid[i + k, j + l].returnCount(agentType) > 0)
                        {
                            localCount += environment.grid[i + k, j + l].returnCount(agentType);
                        }
                    }
                }
                if (localCount != 0)
                {
                    djikstraMap.map[i, j] = -(int)Math.Truncate(localCount / 10d);
                }
            }
        }

        bool updated = true;
        while (updated)
        {
            updated = false;
            for (int i = 0; i < NumCells; i++)
            {
                for (int j = 0; j < NumCells; j++)
                {
                    if (djikstraMap.map[i, j] != int.MaxValue)
                    {
                        //update neigbhbors
                        for (int m = -1; m <= 1; m++)
                        {
                            for (int n = -1; n <= 1; n++)
                            {
                                if (m == 0 && n == 0) continue;
                                int neighborX = i + m;
                                int neighborY = j + n;
                                if (neighborX >= 0 && neighborX < NumCells && neighborY >= 0 && neighborY < NumCells)
                                {
                                    if (djikstraMap.map[neighborX, neighborY] > djikstraMap.map[i, j] + 1)
                                    {
                                        djikstraMap.map[neighborX, neighborY] = djikstraMap.map[i, j] + 1;
                                        updated = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }

        }

        GD.Print("Djikstra Map Calculated");

        return djikstraMap;

    }

}