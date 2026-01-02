
using System;
using System.Collections.Generic;
using Godot;


public class Cell
{
	public int index; 
	public int generation = 0;
	public float cellsize = 60;
	//population in cell data
	public int humanPopulation = 0;
	public int MaleMosquitoPopulation = 0;
	public int FemaleMosquitoPopulation = 0;
	public int totalPopulation = 0;
	public bool subcellsUsed = false;
	public float subcellSize;
	public int subThreshold = 50;
	public HashSet<Agent> agentsInCell = new();
	public int X, Y;
	public Cell[,] subCells = new Cell[10,10]; 
	public Cell(int xIndex, int yIndex)
	{
		X = xIndex;
		Y = yIndex;
		subcellSize = cellsize/10;
	}
	public Cell(int xIndex, int yIndex, float cellsize)
	{
		X = xIndex;
		Y = yIndex;
		this.cellsize = cellsize;
		subcellSize = cellsize/10;
	}
	private Cell(int xIndex, int yIndex, float cellsize, int previousGeneration)
	{
		X = xIndex;

		Y = yIndex;
		this.cellsize = cellsize;
		subcellSize = cellsize/10;
		generation = previousGeneration +1;
	}

	public HashSet<Agent> GetAgentsInCell(ref Vector2 position)
	{
        if (!subcellsUsed)
        {
            return agentsInCell;
		}
		else
		{
			int subcellX = (int)Math.Truncate((position.X % cellsize) /subcellSize);
			int subcellY = (int)Math.Truncate((position.Y % cellsize) /subcellSize);
			return subCells[subcellX, subcellY].GetAgentsInCell(ref position);
        }
	}
	public HashSet<Agent> GetAllAgentsInCell()
	{
		if (!subcellsUsed)
		{
			return new HashSet<Agent>(agentsInCell);
		}
		else
		{
			HashSet<Agent> allAgents = new HashSet<Agent>();
			for (int i = 0; i < 10; i++)
			{
				for (int j = 0; j < 10; j++)
				{
					allAgents.UnionWith(subCells[i, j].GetAllAgentsInCell());
				}
			}
			return allAgents;
		}
	}
	public void addAgentToCell(Agent agent)
	{
		if (agentsInCell.Contains(agent))
		{
    		//GD.Print($"Warning: agent {agent.index} already exists in {X},{Y}, generation={generation}");
			return;
		}
		totalPopulation++;
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
			if(agentsInCell.Count >= subThreshold)
			{
				enableSubcells();
			}
		}
		else
		{
			int subcellX = (int)Math.Truncate((agent.position.X % cellsize) /subcellSize);
			int subcellY = (int)Math.Truncate((agent.position.Y % cellsize) /subcellSize);
			subCells[subcellX, subcellY].addAgentToCell(agent);
		}
		
		
	}
	public bool removeAgentFromCell(Agent agent)
	{
		bool agentRemoved = false;
		if (!subcellsUsed)
		{
			if (agentsInCell.Remove(agent))
			{
				agentRemoved = true;
				totalPopulation--;
				if(agent.GetType() == typeof(MaleMosquito))
					{
						MaleMosquitoPopulation--;
					}
				else if(agent.GetType() == typeof(Human))
				{
					humanPopulation--;
				}
			}
			if(agentsInCell.Count < subThreshold)
			{
				disableSubcells();
			}
		}
		else
		{
			int subcellX = (int)Math.Truncate((agent.position.X % cellsize) /subcellSize);
			int subcellY = (int)Math.Truncate((agent.position.Y % cellsize) /subcellSize);
			if(subCells[subcellX, subcellY].removeAgentFromCell(agent))
			{
				agentRemoved = true;
				totalPopulation--;
				if(agent.GetType() == typeof(MaleMosquito))
					{
						MaleMosquitoPopulation--;
					}
				else if(agent.GetType() == typeof(Human))
				{
					humanPopulation--;
				}
			}
		}
		return agentRemoved;
	}

	public void enableSubcells()
	{
		if (!subcellsUsed && generation <= 40)
		{
			int sumAgentsInCell = agentsInCell.Count;
			

			subcellsUsed = true;
			for (int i = 0; i < 10; i++)
			{
				for (int j = 0; j < 10; j++)
				{
					subCells[i, j] = new Cell(i,j, subcellSize,generation);
				}
			}
			foreach (var agent in agentsInCell)
			{
				int subcellX = (int)Math.Truncate((agent.position.X % cellsize) /subcellSize);
				int subcellY = (int)Math.Truncate((agent.position.Y % cellsize) /subcellSize);
				subCells[subcellX, subcellY].addAgentToCell(agent);

			}
			agentsInCell.Clear();
			GD.Print($"generation {generation+1} subcells enabled at {X},{Y}, with total population of {totalPopulation}, agents in cell: {agentsInCell.Count}");
		}
	}
	public void disableSubcells()
    {
        if (subcellsUsed)
        {
            subcellsUsed = false;
			agentsInCell = GetAllAgentsInCell();
			subCells = new Cell[10,10];
			GD.Print($"subcells disabled at {X},{Y}");
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

	public Cell CalculateSubcell(Vector2 position)
	{
		if (!subcellsUsed)
		{
			return this;
		}
		else
		{
			int subcellX = (int)Math.Truncate((position.X % cellsize) /subcellSize);
			int subcellY = (int)Math.Truncate((position.Y % cellsize) /subcellSize);
			return subCells[subcellX,subcellY].CalculateSubcell(position);
		}
	}

}
