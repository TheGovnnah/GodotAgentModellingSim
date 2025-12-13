using Godot;
using System.Collections.Generic;
using System.Numerics;

public class Environment
{
    public World world;
    public int width;
    public int height;

    public int cellSize;

    public Cell[,] grid;

    public DjikstraMap HumanDijkstraMap;

    public DjikstraMap MaleMosquitoDijkstraMap;
    public int cellsPerRow;
    public Environment(int width, int height, int cellSize, World world)
    {
        this.cellSize = cellSize;
        this.width = width;
        this.height = height;
        this.world = world;
        cellsPerRow = width / cellSize;
        grid = new Cell[cellsPerRow, cellsPerRow];
        for (int i = 0; i < cellsPerRow; i++)
        {
            for (int j = 0; j < cellsPerRow; j++)
            {
                grid[i, j] = new Cell(i, j);
            }
        }
    }
}