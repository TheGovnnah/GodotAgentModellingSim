using System;
using Godot;
public struct CellUpdate
{
    
    public Cell oldCell;
    public Cell newCell;
    public Agent agent;

    public CellUpdate(Cell oldCell, Cell newCell, Agent agent)
    {
        this.oldCell = oldCell;
        this.newCell = newCell;
        this.agent = agent;
    }
}

public struct DjikstraMap
{
    public int cellSize;
    public int height;
    public int width;
    public int cellsPerRow;
    public int[,] map;
    public int scale;

    public DjikstraMap(int cellSize, int height, int width, int scale)
    {
        this.cellSize = cellSize;
        this.width = width;
        this.height = height;
        this.scale = scale;
        cellsPerRow = width / cellSize;
        map = new int[cellsPerRow, cellsPerRow];
        for (int i = 0; i < cellsPerRow; i++)
        {
            for (int j = 0; j < cellsPerRow; j++)
            {
                map[i, j] = int.MaxValue;
            }
        }
    }
}
