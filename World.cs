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

    public int cellSize = 60; 
    public int time = 0;

    public World(Node2D parent)
    {
        Map humanSpawnMap = new Map();
        // initialize the environment class
        environment = new Environment(worldWidth, worldHeight,cellSize);
        //Initialize populations here and add to populations array
        populations[0] = new HumanPopulation(10000, ref environment, parent, ref humanSpawnMap);
        environment.HumanDijkstraMap = DijkstraCalculator(parent, false);
        populations[1] = new MosquitoPopulation(10000, ref environment, parent);
        populations[2] = new MaleMosquitoPopulation(10000,ref environment, parent);
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
            pop.updatePopulationRendering();
        }
        time++;
        if(time % 10 == 0)
        {
            environment.MaleMosquitoDijkstraMap = DijkstraCalculator(null, true);
        }
        
        /*Parallel.For(0,worldHeight/cellSize, i =>
        {
            for(int j = 0; j < worldHeight/cellSize; j++)
            {
                if (environment.grid[i,j].GetAllAgentsInCell().Count >= 20)
                {
                    environment.grid[i,j].enableSubcells();
                }
                else
                {
                    environment.grid[i,j].disableSubcells();
                }
            }
        });*/
        //GD.Print("World Time: " + time +"minutes");
    }

    public int[,] DijkstraCalculator(Node2D parent, bool searchingForMosquitoes)
    {
        
        //calculates the djikstra map for human population density, should be ran before mosquitoes spawned
        int NumCells = worldHeight / cellSize;
        int[,] djikstrMap = new int [NumCells,NumCells];
        if(!searchingForMosquitoes)
        {
            for (int i = 0; i < NumCells; i++)
            {
                for (int j = 0; j < NumCells; j++)
                {
                    if (environment.grid[i,j].agentsInCell.Count > 0){
                    djikstrMap[i,j] = -(int)Math.Truncate(environment.grid[i,j].agentsInCell.Count/10d)^2;
                    }
                    else
                    {
                        djikstrMap[i,j] = int.MaxValue;
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
                    if (environment.grid[i,j].MaleMosquitoPopulation > 0){
                    djikstrMap[i,j] = -(int)Math.Truncate(environment.grid[i,j].MaleMosquitoPopulation/10d)^2;
                    }
                    else
                    {
                        djikstrMap[i,j] = int.MaxValue;
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
                    if (djikstrMap[i,j] != int.MaxValue)
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
                                    if (djikstrMap[neighborX, neighborY] > djikstrMap[i,j] + 1)
                                    {
                                        djikstrMap[neighborX, neighborY] = djikstrMap[i,j] + 1;
                                        updated = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        Color[] djikstraColors = new Color[NumCells * NumCells];
        for (int i = 0; i < NumCells; i++)
        {
            for (int j = 0; j < NumCells; j++)
            {
                int index = i * NumCells + j;
                if (djikstrMap[i,j] == int.MaxValue)
                {
                    djikstraColors[index] = new Color(0,0,0);
                }
                else
                {
                    float intensity = 1f - Math.Min(1f, Math.Abs(djikstrMap[i,j]) / 70f);

                    float blueIntensity = Math.Min(1f, Math.Abs( djikstrMap[i,j]) / 1000f);
                    djikstraColors[index] = new Color(intensity, 0, 1);
                }
            }
        }
        Vector2[] djikstraPositions = new Vector2[NumCells * NumCells];
        for (int i = 0; i < NumCells; i++)
        {
            for (int j = 0; j < NumCells; j++)
            {
                int index = i * NumCells + j;
                djikstraPositions[index] = new Vector2(i*cellSize * 1f, j*cellSize * 1f);
            }
        }
        GD.Print("Djikstra Map Calculated");
        //MultiMeshinst djikstraMultiMesh = new MultiMeshinst(GD.Load<Mesh>("res://TestQuadMesh.tres"), NumCells * NumCells, NumCells * NumCells, parent);
        //djikstraMultiMesh.UpdateTransform(NumCells * NumCells, djikstraPositions, djikstraColors);
        return djikstrMap;

    }

}