public class SimulationState
{
    //stores the current state of the simulation
    public int tick;

    public int totalPop;
    public int humanPop;
    public int mosquitoPop;
    public int maleMosquitoPop;
    public int femaleMosquitoPop;
    public int mosqutioLarvaePop;
    public int breedingSites;

    public int infectedHumanPop;
    public int infectedMosqutioPop;
    public int infectedThisTick;

    public int movesThisTick;
    public int BitesThisTick;

    public void resetCounters()
    {
        infectedThisTick = 0;
        movesThisTick = 0;
        BitesThisTick = 0;
    }

    public void OnAgentAdded(Agent agent)
    {
        totalPop++;
        if (agent is Human) humanPop++;
        else if (agent is Mosquito)
        {
            mosquitoPop++;
            if (agent is MaleMosquito) maleMosquitoPop++;
            else femaleMosquitoPop++;
        }
        else if (agent is mosqutioLarvae) mosqutioLarvaePop++;
        else if (agent is breedingSite) breedingSites++;
    }
    public void OnAgentRemoved(Agent agent)
    {
        totalPop--;
        if (agent is Human)
        {
            humanPop--;
            if (agent.infected) infectedHumanPop--;
        }
        else if (agent is Mosquito)
        {
            mosquitoPop--;
            if (agent is MaleMosquito) maleMosquitoPop--;
            else
            {
                femaleMosquitoPop--;
                if (agent.infected) infectedMosqutioPop--;
            }
        }
        else if (agent is mosqutioLarvae) mosqutioLarvaePop--;
        else if (agent is breedingSite) breedingSites--;



    }

    public void OnInfectionChange(Agent agent)
    {
        if (agent is Human)
        {
            infectedHumanPop += agent.infected ? 1 : -1;
        }
        if (agent is Mosquito)
        {
            infectedMosqutioPop += agent.infected ? 1 : -1;
        }
        if (agent.infected)
        {
            infectedThisTick++;
        }
    }

    public void OnMove()
    {
        movesThisTick++;
    }

    public void OnBite()
    {
        BitesThisTick++;
    }
}