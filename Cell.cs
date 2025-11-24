
using System.Collections.Generic;


public class Cell
{
	public HashSet<Agent> agentsInCell = new();
	public int X, Y;
	public Cell(int xIndex, int yIndex)
	{
		X = xIndex;
		Y = yIndex;

	}


}
