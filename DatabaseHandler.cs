using DuckDB.NET.Data;

public class DatabaseHandler
{
    DuckDBConnection duckDBConnection;
    public DatabaseHandler()
    {
        duckDBConnection = new DuckDBConnection("Data Source=SimulationRecord.db");
        duckDBConnection.Open();

        var command = duckDBConnection.CreateCommand();
        command.CommandText = "CREATE TABLE simulation()"        
    }
}