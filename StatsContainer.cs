using Godot;
using System;

public partial class StatsContainer : GridContainer
{
	private Main main;
	public SimulationState simulationState;
	private Label populationLabel;
	private Label timeLabel;
	private Label humanPopLabel;
	private Label MosquitoPopLabel;
	private Label MaleMosquitoPopLabel;
	private Label FemaleMosquitoPopLabel;
	private Label MosquitoLarvePopLabel;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		main = GetNode<Main>("/root/Main");
		populationLabel = GetNode<Label>("TotalPopLabel");
		timeLabel = GetNode<Label>("TimeLabel");
		humanPopLabel = GetNode<Label>("HumanPop");
		MosquitoPopLabel = GetNode<Label>("MosquitoPop");
		MaleMosquitoPopLabel = GetNode<Label>("MaleMosquitoPop");
		FemaleMosquitoPopLabel = GetNode<Label>("FemaleMosquitoPop");
		MosquitoLarvePopLabel = GetNode<Label>("MosquitoLarvePop");
		simulationState = main.MainWorld.simulationState;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		TimeSpan time = TimeSpan.FromMinutes(simulationState.tick);
		timeLabel.Text = $"simulation time:\n{time.Days} days\n{time.Hours} hours\n{time.Minutes} minutes";
		populationLabel.Text = $"total agents: {simulationState.totalPop}";
		humanPopLabel.Text = $"Human Population: {simulationState.humanPop}";
		MosquitoPopLabel.Text = $"Mosquito Population: {simulationState.mosquitoPop}";
		MaleMosquitoPopLabel.Text = $"Male Mosquito Population: {simulationState.maleMosquitoPop}";
		FemaleMosquitoPopLabel.Text = $"Female Mosquito Population: {simulationState.femaleMosquitoPop}";
		MosquitoLarvePopLabel.Text = $"Mosquito Larve Population: {simulationState.mosqutioLarvaePop}";
	}
}
