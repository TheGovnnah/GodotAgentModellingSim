
public class Environment
{
    public int width;
    public int height;

    public int cellSize;

    public Cell[,] grid;

    public int cellsPerRow;
    public Environment(int width, int height, int cellSize)
    {
        this.width = width;
        this.height = height;
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