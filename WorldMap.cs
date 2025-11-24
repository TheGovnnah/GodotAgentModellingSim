using System.IO;
using System.Security;
using System.Threading.Tasks;
using Godot;
using System;
using System.Numerics;

public class Map
{
    public int[,] humanSpawnLocations = new int[10000,10000];

    public int totalPop = 0;

    string locationsFile = "/home/francis/Documents/CodingProjects/ClearTextArray.txt";

    //upon initialisation, writes the 30/30m population data to the humanspawnlocations array, to be used by the program
    public Map()
    {
        loadMap();
    }
    public void loadMap()
    {
        using(StreamReader reader = File.OpenText(locationsFile))
        {
            string line;
            int i = 0;
            while((line = reader.ReadLine()) != null)
            {
                string[] stringLine = line.Split(',');
                for(int j =0; j < stringLine.Length; j++)
                {   
                    if(stringLine[j] != "0")
                    {
                            int humansInCell = (int)Math.Round(float.Parse(stringLine[j]));
                            humanSpawnLocations[i,j] = humansInCell;
                            totalPop += humansInCell;
                    }
                }
                i++;
            }
        }
        GD.Print($"Map loaded, total population {totalPop}");

    }
}
