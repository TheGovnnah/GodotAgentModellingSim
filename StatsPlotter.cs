using System;
using ScottPlot;
using Godot;
using DuckDB.NET.Data;
using System.ComponentModel.Design;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Generic;
using ScottPlot.Plottables;
using System.Reflection.Metadata;
using System.Linq;

public class StatsPlotter
{
    public int outputImageHeight = 400;
    public int outputImageWidth = 300;
    public Plot populationGraph;
    public Plot infectionGraph;
    public Plot actionsGraph;
    public TextureRect popOutputTexture;
    public TextureRect infectionOutputTexture;
    public TextureRect actionOutputTexture;
    DuckDBConnection duckDBConnection;
    DuckDBCommand command;
    public List<int> tick = new List<int>(10);
    public List<int> totalPop = new List<int>(10);
    public List<int> humanPop = new List<int>(10);
    public List<int> mosquitoPop = new List<int>(10);
    public List<int> maleMosquitoPop = new List<int>(10);
    public List<int> femaleMosquitoPop = new List<int>(10);
    public List<int> mosqutioLarvaePop = new List<int>(10);
    public List<int> breedingSites = new List<int>(10);
    public List<int> infectedHumanPop = new List<int>(10);
    public List<int> infectedMosqutioPop = new List<int>(10);
    public List<int> infectedThisTick = new List<int>(10);
    public List<int> movesThisTick = new List<int>(10);
    public List<int> BitesThisTick = new List<int>(10);

    public SignalXY totalPopSignal;
    public SignalXY humanPopSignal;
    public SignalXY mosquitoPopSignal;
    public SignalXY maleMosquitoPopSignal;
    public SignalXY femaleMosquitoPopSignal;
    public SignalXY mosqutioLarvaePopSignal;
    public SignalXY breedingSitesSignal;
    public SignalXY infectedHumanPopSignal;
    public SignalXY infectedMosqutioPopSignal;
    public SignalXY infectedThisTickSignal;
    public SignalXY movesThisTickSignal;
    public SignalXY BitesThisTickSignal;


    public StatsPlotter(Node node)
    {
        populationGraph = new Plot();
        infectionGraph = new Plot();
        actionsGraph = new Plot();
        populationGraph.Legend.BackgroundColor = ScottPlot.Colors.White.WithOpacity(0.0);
        infectionGraph.Legend.BackgroundColor = ScottPlot.Colors.White.WithOpacity(0.0);
        actionsGraph.Legend.BackgroundColor = ScottPlot.Colors.White.WithOpacity(0.0);

        popOutputTexture = node.GetNode<TextureRect>("UI/HUD/GraphContainer/PopulationGraph/TextureRect");
        infectionOutputTexture = node.GetNode<TextureRect>("UI/HUD/GraphContainer/InfectionGraph/TextureRect");
        actionOutputTexture = node.GetNode<TextureRect>("UI/HUD/GraphContainer/ActionsGraph/TextureRect");

        duckDBConnection = new DuckDBConnection("Data Source=SimulationRecord.db");
        duckDBConnection.Open();
        command = duckDBConnection.CreateCommand();

        totalPopSignal = populationGraph.Add.SignalXY(tick, totalPop);
        humanPopSignal = populationGraph.Add.SignalXY(tick, humanPop);
        mosquitoPopSignal = populationGraph.Add.SignalXY(tick, mosquitoPop);
        maleMosquitoPopSignal = populationGraph.Add.SignalXY(tick, maleMosquitoPop);
        femaleMosquitoPopSignal = populationGraph.Add.SignalXY(tick, femaleMosquitoPop);
        mosqutioLarvaePopSignal = populationGraph.Add.SignalXY(tick, mosqutioLarvaePop);
        breedingSitesSignal = populationGraph.Add.SignalXY(tick, breedingSites);

        infectedHumanPopSignal = infectionGraph.Add.SignalXY(tick, infectedHumanPop);
        infectedMosqutioPopSignal = infectionGraph.Add.SignalXY(tick, infectedMosqutioPop);
        infectedThisTickSignal = infectionGraph.Add.SignalXY(tick, infectedThisTick);

        movesThisTickSignal = actionsGraph.Add.SignalXY(tick, movesThisTick);
        BitesThisTickSignal = actionsGraph.Add.SignalXY(tick, BitesThisTick);

        totalPopSignal.LegendText = "Total Population";
        humanPopSignal.LegendText = "Humans";
        mosquitoPopSignal.LegendText = "Mosquitoes";
        maleMosquitoPopSignal.LegendText = "Male Mosquitoes";
        femaleMosquitoPopSignal.LegendText = "Female Mosquitoes";
        mosqutioLarvaePopSignal.LegendText = "Mosquito Larvae";
        breedingSitesSignal.LegendText = "Breeding Sites";
        infectedHumanPopSignal.LegendText = "Infected Humans";
        infectedMosqutioPopSignal.LegendText = "Infected Mosquitoes";
        infectedThisTickSignal.LegendText = "Agents Infected per tick";
        movesThisTickSignal.LegendText = "Moves made per tick";
        BitesThisTickSignal.LegendText = "Bites per tick";


    }

    public void updateGraph()
    {
        outputImageHeight = (int)popOutputTexture.GetRect().Size.Y;
        outputImageWidth = (int)popOutputTexture.GetRect().Size.X;
        if (tick.Count != 0)
        {
            int currentTick = tick[tick.Last()];
            string CommandText = $"SELECT * FROM simulation WHERE tick > {currentTick}";
            command.CommandText = CommandText;
            writeIntoArrays(command.ExecuteReader());
        }
        else
        {
            string CommandText = "SELECT * FROM simulation";
            command.CommandText = CommandText;
            writeIntoArrays(command.ExecuteReader());
        }
    }

    public void writeIntoArrays(DuckDBDataReader reader)
    {
        while (reader.Read())
        {
            int readTick = reader.GetInt32(0);
            tick.Insert(readTick, readTick);
            totalPop.Insert(readTick, reader.GetInt32(1));
            humanPop.Insert(readTick, reader.GetInt32(2));
            mosquitoPop.Insert(readTick, reader.GetInt32(3));
            maleMosquitoPop.Insert(readTick, reader.GetInt32(4));
            femaleMosquitoPop.Insert(readTick, reader.GetInt32(5));
            mosqutioLarvaePop.Insert(readTick, reader.GetInt32(6));
            breedingSites.Insert(readTick, reader.GetInt32(7));
            infectedHumanPop.Insert(readTick, reader.GetInt32(8));
            infectedMosqutioPop.Insert(readTick, reader.GetInt32(9));
            infectedThisTick.Insert(readTick, reader.GetInt32(10));
            movesThisTick.Insert(readTick, reader.GetInt32(11));
            BitesThisTick.Insert(readTick, reader.GetInt32(12));
        }
    }

    public void writeIntoArraysSystem(SimulationState simulationState)
    {
        int readTick = simulationState.tick;
        tick.Insert(readTick, readTick);
        totalPop.Insert(readTick, simulationState.totalPop);
        humanPop.Insert(readTick, simulationState.humanPop);
        mosquitoPop.Insert(readTick, simulationState.mosquitoPop);
        maleMosquitoPop.Insert(readTick, simulationState.maleMosquitoPop);
        femaleMosquitoPop.Insert(readTick, simulationState.femaleMosquitoPop);
        mosqutioLarvaePop.Insert(readTick, simulationState.mosqutioLarvaePop);
        breedingSites.Insert(readTick, simulationState.breedingSites);
        infectedHumanPop.Insert(readTick, simulationState.infectedHumanPop);
        infectedMosqutioPop.Insert(readTick, simulationState.infectedMosqutioPop);
        infectedThisTick.Insert(readTick, simulationState.infectedThisTick);
        movesThisTick.Insert(readTick, simulationState.movesThisTick);
        BitesThisTick.Insert(readTick, simulationState.BitesThisTick);
    }

    public void renderGraph()
    {
        var gdImage = new Godot.Image();
        byte[] buffer;

        populationGraph.Axes.AutoScale();
        var img = populationGraph.GetImage(outputImageWidth, outputImageHeight);
        buffer = img.GetImageBytes();
        gdImage.LoadPngFromBuffer(buffer);
        popOutputTexture.Texture = ImageTexture.CreateFromImage(gdImage);

        infectionGraph.Axes.AutoScale();
        img = infectionGraph.GetImage(outputImageWidth, outputImageHeight);
        buffer = img.GetImageBytes();
        gdImage.LoadPngFromBuffer(buffer);
        infectionOutputTexture.Texture = ImageTexture.CreateFromImage(gdImage);

        actionsGraph.Axes.AutoScale();
        img = actionsGraph.GetImage(outputImageWidth, outputImageHeight);
        buffer = img.GetImageBytes();
        gdImage.LoadPngFromBuffer(buffer);
        actionOutputTexture.Texture = ImageTexture.CreateFromImage(gdImage);
    }

}