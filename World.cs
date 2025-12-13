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
        populations[0] = new HumanPopulation(10000, ref environment, parent, ref humanSpawnMap);
        environment.HumanDijkstraMap = DijkstraCalculator(parent, false);
        populations[1] = new MosquitoPopulation(20000, ref environment, parent);
        

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
            environment.MaleMosquitoDijkstraMap = DijkstraCalculator(parentNode, true);
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

    public int[,] DijkstraCalculator(Node2D parent, bool searchingForMosquitoes)
    {
        
        //calculates the djikstra map for human population density, should be ran before mosquitoes spawned
        int NumCells = worldHeight / cellSize;
        int[,] djikstrMap = new int [NumCells,NumCells];
        Color[] djikstraColors = new Color[NumCells * NumCells];
        Vector2[] djikstraPositions = new Vector2[NumCells * NumCells];
        MultiMeshinst djikstraMultiMesh = new MultiMeshinst(GD.Load<Mesh>("res://TestQuadMesh.tres"), NumCells * NumCells, NumCells * NumCells, parent);
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
        
        for (int i = 0; i < NumCells; i++)
        {
            for (int j = 0; j < NumCells; j++)
            {
                int index = i * NumCells + j;
                djikstraPositions[index] = new Vector2(i*cellSize * 1f, j*cellSize * 1f);
            }
        }
        if(!searchingForMosquitoes)
        {
            for (int i = 0; i < NumCells; i++)
            {
                for (int j = 0; j < NumCells; j++)
                {
                    if (environment.grid[i,j].humanPopulation > 0){
                    djikstrMap[i,j] = -(int)Math.Truncate(environment.grid[i,j].humanPopulation/10d)^2;
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
                    djikstrMap[i,j] = -(int)Math.Truncate(environment.grid[i,j].MaleMosquitoPopulation/10d);
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
                                            int index = neighborX * NumCells + neighborY;
                                            if (djikstrMap[i,j] == int.MaxValue)
                                            {
                                                djikstraColors[index] = new Color(0,0,0);
                                            }
                                            else
                                            {
                                                float intensity = 1f - Math.Min(1f, Math.Abs(djikstrMap[neighborX,neighborY]) / 70f);
                                                djikstraColors[index] = new Color(intensity, 0, 1);
                                            }
                                            djikstraPositions[index] = new Vector2(neighborX*cellSize * 1f, neighborY*cellSize * 1f);
                                            //djikstraMultiMesh.UpdateTransform(NumCells * NumCells, djikstraPositions, djikstraColors);
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
        
        for (int i = 0; i < NumCells; i++)
        {
            for (int j = 0; j < NumCells; j++)
            {
                int index = i * NumCells + j;
                djikstraPositions[index] = new Vector2(i*cellSize * 1f, j*cellSize * 1f);
            }
        }
        GD.Print("Djikstra Map Calculated");
        
        //if(searchingForMosquitoes){djikstraMultiMesh.UpdateTransform(NumCells * NumCells, djikstraPositions, djikstraColors);}
        return djikstrMap;

    }

}