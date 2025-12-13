
using System;
using System.Collections.Generic;
using Godot;


public class Cell
{
	private int cellsize = 60;
	public int humanPopulation = 0;
	public int MaleMosquitoPopulation = 0;
	public int FemaleMosquitoPopulation = 0;
	public bool subcellsUsed = false;
	public HashSet<Agent> agentsInCell = new();
	public int X, Y;
	public Cell[,] subCells = new Cell[10,10]; 
	public Cell(int xIndex, int yIndex)
	{
		X = xIndex;
		Y = yIndex;

	}

	public HashSet<Agent> GetAgentsInCell(ref Vector2 position)
	{
        if (!subcellsUsed)
        {
            return agentsInCell;
		}
		else
		{
			int subcellX = (int)Math.Truncate((position.X - X*cellsize) /10);
			int subcellY = (int)Math.Truncate((position.Y - Y*cellsize) /10);
			return subCells[subcellX, subcellY].agentsInCell;
        }
	}
	public HashSet<Agent> GetAllAgentsInCell()
	{
		if (!subcellsUsed)
		{
			return agentsInCell;
		}
		else
		{
			HashSet<Agent> allAgents = new HashSet<Agent>();
			for (int i = 0; i < 10; i++)
			{
				for (int j = 0; j < 10; j++)
				{
					allAgents.UnionWith(subCells[i, j].agentsInCell);
				}
			}
			return allAgents;
		}
	}
	public void addAgentToCell(Agent agent)
	{
		if(agent.GetType() == typeof(MaleMosquito))
            {
				MaleMosquitoPopulation++;
            }
		else if(agent.GetType() == typeof(Human))
		{
			humanPopulation++;
		}
		if (!subcellsUsed)
		{
			agentsInCell.Add(agent);
		}
		else
		{
			int subcellX = (int)((agent.position.X % 1.0) * 10);
			int subcellY = (int)((agent.position.Y % 1.0) * 10);
			subCells[subcellX, subcellY].addAgentToCell(agent);
		}
	}
	public void removeAgentFromCell(Agent agent)
	{
		if(typeof(Agent) == typeof(MaleMosquito))
            {
				MaleMosquitoPopulation--;
            }
		else if(agent.GetType() == typeof(Human))
		{
			humanPopulation--;
		}
		if (!subcellsUsed)
		{
			agentsInCell.Remove(agent);
		}
		
		else
		{
			int subcellX = (int)((agent.position.X % 1.0) * 10);
			int subcellY = (int)((agent.position.Y % 1.0) * 10);
			subCells[subcellX, subcellY].removeAgentFromCell(agent);
		}
	}

	public void enableSubcells()
	{
		if (!subcellsUsed)
		{
			subcellsUsed = true;
			for (int i = 0; i < 10; i++)
			{
				for (int j = 0; j < 10; j++)
				{
					subCells[i, j] = new Cell(i,j);
				}
			}
			foreach (var agent in agentsInCell)
			{
				int subcellX = (int)((agent.position.X % 1.0) * 10);
				int subcellY = (int)((agent.position.Y % 1.0) * 10);
				subCells[subcellX, subcellY].addAgentToCell(agent);
			}
			agentsInCell.Clear();
		}
	}
	public void disableSubcells()
    {
        if (subcellsUsed)
        {
            subcellsUsed = false;
			agentsInCell = GetAllAgentsInCell();
			subCells = new Cell[10,10];
        }
    }

	public bool checkCellForAgents(Type agentType)
	{
		foreach (Agent agent in GetAllAgentsInCell())
		{
			if (agent.GetType() == agentType)
			{
				return true;
			}
		}
		return false;
	}

}
