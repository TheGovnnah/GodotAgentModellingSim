using System.Diagnostics;
using Godot;
using System;
using System.Globalization;
using System.Threading.Tasks;
using System.IO;
public class World
{
    public Environment environment;
    public Population[] populations = new Population[3];

    private int worldHeight = 30000;
    public int worldWidth = 30000;
    public Node2D parentNode;
    public int cellSize = 60; 
    public int time = 0;

    public World(Node2D parent)
    {
        parentNode = parent;
        Map humanSpawnMap = new Map();
        // initialize the environment class
        environment = new Environment(worldWidth, worldHeight,cellSize, this);
        //Initialize populations here and add to populations array
        populations[2] = new breedingSites(10, ref environment, parent);
        populations[0] = new HumanPopulation(100, ref environment, parent, ref humanSpawnMap);
        environment.HumanDijkstraMap = DijkstraCalculator(parent, false, 1);
        populations[1] = new MosquitoPopulation(1000, ref environment, parent);
        

        int CellsWithHumans = 0;
        int totalPopulation = 0;
        foreach(Cell cell in environment.grid)
        {
            if (cell.agentsInCell.Count > 0)
            {
                CellsWithHumans++;
                totalPopulation += cell.agentsInCell.Count;
                if (cell.agentsInCell.Count > 20)
                {
                    cell.enableSubcells();
                    GD.Print($"Subcells enabled for cell at {cell.X}, {cell.Y} with {cell.GetAllAgentsInCell().Count} agents");
                }
            }
        }
        GD.Print("cells with humans: " + CellsWithHumans);
        GD.Print("total population: " + totalPopulation);
        

    }

    public void updateProcess()
    {
        foreach (Population pop in populations)
        {
            if(pop.GetType() != typeof(MosquitoPopulation)){
            pop.updatePopulationRendering();
            }
        }
        populations[1].updatePopulationRendering(); // mosquitos
        
        if(time % 100 == 0)
        {
            environment.MaleMosquitoDijkstraMap = DijkstraCalculator(parentNode, true, 2);
        }
        
        if(time % 100 == 0){
            Parallel.For(0,worldHeight/cellSize, i =>
            {
                for(int j = 0; j < worldHeight/cellSize; j++)
                {
                    if (environment.grid[i,j].GetAllAgentsInCell().Count >= 10)
                    {
                        environment.grid[i,j].enableSubcells();
                    }
                    else
                    {
                        environment.grid[i,j].disableSubcells();
                    }
                }
            });
        }
        //GD.Print("World Time: " + time +"minutes");
        time++;
    }

    public DjikstraMap DijkstraCalculator(Node2D parent, bool searchingForMosquitoes, int scale)
    {
        
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
                        djikstraMap.map[i,j] = -(int)Math.Truncate(localCount/10d)^2;
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
                            djikstraMap.map[i,j] = -(int)Math.Truncate((double)localCount)^2;
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