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
    private int initialMosquitoPopulation = 200;
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
        environment = new Environment(worldWidth, worldHeight,cellSize, this);
        simulationHandler = new SimulationHandler((initialHumanPopulation + initialBreedingSitePopulation + initialMosquitoPopulation) * 100, environment, parentNode);
        initialisePopulations();

        simulationHandler.startSimulation(populations);
        //calculate djikstra map for humans at full scale
        environment.HumanDijkstraMap = DijkstraCalculator(parent, false, 1);
        updateSubcells();
    }
    
    public void initialisePopulations()
    {
        Map humanSpawnMap = new Map();
        populations[2] = new breedingSites(initialBreedingSitePopulation, ref environment, parentNode);
        populations[0] = new HumanPopulation(initialHumanPopulation, ref environment, parentNode, ref humanSpawnMap);
        populations[1] = new MosquitoPopulation(initialMosquitoPopulation, ref environment, parentNode);        
    }

    public void updateSubcells()
    {
        //GD.Print("calculating where to enable subcells");
        Parallel.For(0,worldHeight/cellSize, i =>
            {
                Parallel.For(0,worldHeight/cellSize, j =>
                {
                    if (environment.grid[i,j].GetAllAgentsInCell().Count >= 10)
                    {
                        environment.grid[i,j].enableSubcells();
                    }
                    else
                    {
                        environment.grid[i,j].disableSubcells();
                    }
                });
            });
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

    public DjikstraMap DijkstraCalculator(Node2D parent, bool searchingForMosquitoes, int scale)
    {
        GD.Print("initialising Dijkstra calculation");
        
        //calculates the djikstra map for human population density, should be ran before mosquitoes spawned
        int MapCellSize = cellSize * scale;
        int NumCells = worldHeight / MapCellSize;
        DjikstraMap djikstraMap = new DjikstraMap(MapCellSize,worldHeight,worldHeight,scale);
        Color[] djikstraColors = new Color[NumCells * NumCells];
        Vector2[] djikstraPositions = new Vector2[NumCells * NumCells];
        MultiMeshinst djikstraMultiMesh = new MultiMeshinst(GD.Load<Mesh>("res://TestQuadMesh.tres"), NumCells * NumCells, NumCells * NumCells, parent);
        if(!searchingForMosquitoes)
        {
            for (int i = 0; i < NumCells; i++)
            {
                for (int j = 0; j < NumCells; j++)
                {
                    int localCount = 0;
                    for(int k = 0; k < scale; k++)
                    {
                        for(int l = 0; l < scale; l++)
                        {
                            if (environment.grid[i+k,j+l].humanPopulation > 0)
                            {
                                localCount += environment.grid[i+k,j+l].humanPopulation;
                            }
                        }
                    }
                    if(localCount != 0)
                    {
                        djikstraMap.map[i,j] = -(int)Math.Truncate(localCount/10d);
                    }
                }
            }
        }
        else
            {
                for (int i = 0; i < NumCells; i++)
                {
                    for (int j = 0; j < NumCells; j++)
                    {
                        int localCount = 0;
                        for(int k = 0; k < scale; k++)
                        {
                            for(int l = 0; l < scale; l++)
                            {
                                if (environment.grid[i+k,j+l].MaleMosquitoPopulation > 0)
                                {
                                    localCount += environment.grid[i+k,j+l].MaleMosquitoPopulation;
                                }                              
                            }
                        }
                        if(localCount != 0)
                        {
                            djikstraMap.map[i,j] = -(int)Math.Truncate((double)localCount/10d)^2;
                        }         
                    }
                }
            }
        bool updated = true;
        while(updated){
            updated = false;
            for (int i = 0; i < NumCells; i++)
            {
                for (int j = 0; j < NumCells; j++)
                {
                    if (djikstraMap.map[i,j] != int.MaxValue)
                    {
                        //update neigbhbors
                        for(int m = -1; m <= 1; m++)
                        {
                            for(int n = -1; n <= 1; n++)
                            {
                                if (m == 0 && n == 0) continue;
                                int neighborX = i + m;
                                int neighborY = j + n;
                                if (neighborX >= 0 && neighborX < NumCells && neighborY >= 0 && neighborY < NumCells)
                                {
                                        if (djikstraMap.map[neighborX, neighborY] > djikstraMap.map[i,j] + 1)
                                        {
                                            djikstraMap.map[neighborX, neighborY] = djikstraMap.map[i,j] + 1;
                                            updated = true;
                                        }
                                }
                            }
                        }
                    }
                }
            }
        
        }
        
        for (int i = 0; i < NumCells; i++)
        {
            for (int j = 0; j < NumCells; j++)
            {
                int index = i * NumCells + j;
                if (djikstraMap.map[i,j] == int.MaxValue)
                {
                    djikstraColors[index] = new Color(0,0,1);
                }
                else
                {
                    float intensity = 1f - Math.Min(1f, Math.Abs(djikstraMap.map[i,j]) / 70f);

                    float blueIntensity = Math.Min(1f, Math.Abs( djikstraMap.map[i,j]) / 1000f);
                    djikstraColors[index] = new Color(intensity, 0, 1);
                }
            }
        }
        
        for (int i = 0; i < NumCells; i++)
        {
            for (int j = 0; j < NumCells; j++)
            {
                int index = i * NumCells + j;
                djikstraPositions[index] = new Vector2(i*MapCellSize * 1f, j*MapCellSize * 1f);
            }
        }
        GD.Print("Djikstra Map Calculated");
        
        //if(searchingForMosquitoes){djikstraMultiMesh.UpdateTransform(NumCells * NumCells, djikstraPositions, djikstraColors);}
        return djikstraMap;

    }

}