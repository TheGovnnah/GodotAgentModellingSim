using System.Collections.Generic;
using System.Linq;
using DuckDB.NET.Data;
using Godot;

public class DatabaseHandler
{
    DuckDBConnection duckDBConnection;

    //the buffer that is written to before writing to the database, the key being the tick and the int[] being the various population counts 
    public Dictionary<int,int[]> buffer;
    public SimulationState SimulationState;
    public int[] frameStats;

    public DatabaseHandler(SimulationState simulationState)
    {
        this.SimulationState = simulationState;
        duckDBConnection = new DuckDBConnection("Data Source=SimulationRecord.db");
        duckDBConnection.Open();

        var command = duckDBConnection.CreateCommand();
        command.CommandText = "CREATE OR REPLACE TABLE simulation (tick INTEGER PRIMARY KEY, totalPop INTEGER, humanPop INTEGER, mosquitoPop INTEGER, maleMosquitoPop INTEGER, femaleMosquitoPop INTEGER, mosquitoLarvaePop INTEGER, breedingSites INTEGER, infectedHumanPop INTEGER, infectedMosquitoPop INTEGER, infectedThisTick INTEGER, movesThisTick INTEGER, bitesThisTick INTEGER)";
        var executeNonQuery = command.ExecuteNonQuery();
        buffer = new Dictionary<int, int[]>();

    }

    public void updateBuffer()
    {
        frameStats =
        [
            SimulationState.tick,
            SimulationState.totalPop,
            SimulationState.humanPop,
            SimulationState.mosquitoPop,
            SimulationState.maleMosquitoPop,
            SimulationState.femaleMosquitoPop,
            SimulationState.mosqutioLarvaePop,
            SimulationState.breedingSites,
            SimulationState.infectedHumanPop,
            SimulationState.infectedMosqutioPop,
            SimulationState.infectedThisTick,
            SimulationState.movesThisTick,
            SimulationState.BitesThisTick,
        ];
        buffer.Add(SimulationState.tick,frameStats);
    }

    public void writeBuffer()
    {
        string batchWriteCommand = "";
        foreach(KeyValuePair<int, int[]> frame in buffer)
        {
            string concatinatedFrame = "(";
            foreach(int value in frame.Value)
            {
                concatinatedFrame += $"{value}, ";
            }
            concatinatedFrame = concatinatedFrame.Remove(concatinatedFrame.Length-2);
            concatinatedFrame += ")";
            batchWriteCommand += $"INSERT INTO simulation VALUES {concatinatedFrame};";
        }
        var command = duckDBConnection.CreateCommand();
        command.CommandText = batchWriteCommand;
        var executeNonQuery = command.ExecuteNonQuery();
        buffer = new Dictionary<int, int[]>();
    }
}