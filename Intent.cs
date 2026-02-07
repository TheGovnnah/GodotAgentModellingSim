using System;
using Godot;
using System.Collections.Generic;
using System.Numerics;
using System.Security.Cryptography.X509Certificates;
using System.Linq;

public interface IIntent
{
    public Agent IntentOwner {get; set;}
}
public interface IintentResolver
{
    Type IntentType {get;}
    void GenericResolve(IEnumerable<IIntent> intents, World world);

}
public abstract class IntentResolver<TIntent> : IintentResolver 
    where TIntent : IIntent
{
    public Type IntentType => typeof(TIntent);
    public void GenericResolve(IEnumerable<IIntent> intents, World world)
    {
        Resolve(world,intents.Cast<TIntent>());
    }
    public abstract void Resolve(World world,IEnumerable<TIntent> intents);
}

public struct AddIntent : IIntent
{
    public Agent IntentOwner {get;set;}
    public Agent AgentToAdd;
    public Func<Agent> factory;
    public AddIntent(Agent owner, Agent agent)
    {
        IntentOwner = owner;
        AgentToAdd = agent;
    }
    public AddIntent(Agent owner, Func<Agent> AdditionFunction)
    {
        IntentOwner = owner;
        factory = AdditionFunction;
    }
}
public class addAgentResolver : IntentResolver<AddIntent>
{
    private SimulationHandler simHandler;

    public addAgentResolver(SimulationHandler simHandler)
    {
        this.simHandler = simHandler;
    }
    public override void Resolve(World world,IEnumerable<AddIntent> intents)
    {
        foreach(var intent in intents)
        {
            var owner = intent.IntentOwner;
            Agent agent;
            if(intent.AgentToAdd != null)
            {
                agent = intent.AgentToAdd;
            }
            else
            {
                agent = intent.factory();
            }
            simHandler.addAgent(agent);
            
        }
    }
}

public struct breedIntent : IIntent
{
    public Agent IntentOwner {get; set;}
    public Agent BredAgent;

    public breedIntent(Agent owner, Agent target)
    {
        IntentOwner = owner;
        BredAgent = target;
    }
}
public class BreedingResolver : IntentResolver<breedIntent>
{
    public override void Resolve(World world,IEnumerable<breedIntent> intents)
    {
        foreach(var intent in intents)
        {
            var owner = intent.IntentOwner;
            var target = intent.BredAgent;
            
            target.targeted = false;
            owner.targetAgent = null;
            
        }
    }
}

public struct ExclusiveTargetIntent : IIntent
{
    public Agent IntentOwner {get;set;}
    public Agent TargetAgent;
    public ExclusiveTargetIntent(Agent owner, Agent agent)
    {
        IntentOwner = owner;
        TargetAgent = agent;
    }
}

public class ExclusiveTargetResolver : IntentResolver<ExclusiveTargetIntent>
{
    public override void Resolve(World world,IEnumerable<ExclusiveTargetIntent> intents)
    {
        foreach(var intent in intents)
        {
            var owner = intent.IntentOwner;
            var target = intent.TargetAgent;

            if (!target.targeted && target.agentActive)
            {
                target.targeted = true; 
                owner.targetAgent = target;
            }
        }
    }
}

public struct BiteIntent : IIntent
{
    public Agent IntentOwner {get;set;}
    public Agent TargetAgent;
    public BiteIntent(Agent owner, Agent agent)
    {
        IntentOwner = owner;
        TargetAgent = agent;
    }
}
public class BiteIntentResolver : IntentResolver<BiteIntent>
{
    public override void Resolve(World world,IEnumerable<BiteIntent> intents)
    {
        foreach(var intent in intents)
        {
            var owner = intent.IntentOwner;
            var target = intent.TargetAgent;
            
            target.targeted = false;
            owner.targetAgent = null;
            if(owner.infected)
            {
                if(!target.infected)
                {
                    target.infected = true;
                    world.simulationState.OnInfectionChange(target);
                }
            }
            else if (target.infected)
            {
                if(!owner.infected)
                {
                    owner.infected = true;
                    world.simulationState.OnInfectionChange(owner);
                }
            }
            world.simulationState.OnBite();
        }
    }
}

public struct updateCellIntent : IIntent
{
    public Agent IntentOwner {get;set;}
    public CellUpdate cellUpdate;
    public updateCellIntent(Agent owner, CellUpdate update)
    {
        IntentOwner = owner;
        cellUpdate = update;
    }
}
public class updateCellResolver : IntentResolver<updateCellIntent>
{
    public override void Resolve(World world,IEnumerable<updateCellIntent> intents)
    {
        foreach(var intent in intents)
        {
            intent.cellUpdate.oldCell.removeAgentFromCell(intent.IntentOwner);
            intent.cellUpdate.newCell.addAgentToCell(intent.IntentOwner);
            
            if(intent.cellUpdate.newCell.generation <= 0)
            {
                intent.IntentOwner.currentCell = intent.cellUpdate.newCell;
            }
            else
            {
                intent.IntentOwner.subcell = intent.cellUpdate.newCell;
            }
        }
    }
}

public struct updatePositionIntent : IIntent
{
    public Agent IntentOwner {get;set;}
    public Godot.Vector2 newPos;
    public updatePositionIntent(Agent owner, Godot.Vector2 pos)
    {
        IntentOwner = owner;
        newPos = pos;
    }
}

public class updatePositionResolver : IntentResolver<updatePositionIntent>
{
    public override void Resolve(World world,IEnumerable<updatePositionIntent> intents)
    {
        foreach(var intent in intents)
        {
            intent.IntentOwner.position = intent.newPos;
            world.simulationState.OnMove();
        }
    }
}

public struct updateAiStateIntent : IIntent
{
    public Agent IntentOwner {get;set;}
    public int newState;

    public updateAiStateIntent(Agent agent, int state)
    {
        IntentOwner = agent;
        newState = state;
    }
}

public class updateAiStateResolver : IntentResolver<updateAiStateIntent>
{
    public override void Resolve(World world,IEnumerable<updateAiStateIntent> intents)
    {
        foreach(var intent in intents)
        {
            intent.IntentOwner.AccessAIState(intent.newState);
        }
    }
}

public struct deactivateIntent : IIntent
{
    public Agent IntentOwner {get;set;}
    
    public Agent AgentToDeactivate {get;set;}
    public deactivateIntent(Agent owner, Agent target)
    {
        IntentOwner = owner;
        AgentToDeactivate = target;
    }
}

public class deactivateResolver : IntentResolver<deactivateIntent>
{
    SimulationHandler simulation;
    public deactivateResolver(SimulationHandler simulation)
    {
        this.simulation = simulation;
    }
    public override void Resolve(World world,IEnumerable<deactivateIntent> intents)
    {
        foreach(var intent in intents)
        {
            intent.AgentToDeactivate.agentActive = false;
            if(intent.AgentToDeactivate.targetAgent != null)
            {
                intent.AgentToDeactivate.targetAgent.targeted = false;
            }
            simulation.removeAgent(intent.AgentToDeactivate.index);
        }
    }
}

public struct updateMoveTargetIntent : IIntent
{
    public Agent IntentOwner {get;set;}
    public Godot.Vector2 target;

    public updateMoveTargetIntent(Agent owner, Godot.Vector2 moveTarget)
    {
        IntentOwner = owner;
        target = moveTarget;
    }
}

public class updateMoveTargetResolver : IntentResolver<updateMoveTargetIntent>
{
    public override void Resolve(World world,IEnumerable<updateMoveTargetIntent> intents)
    {
        foreach(var intent in intents)
        {
            intent.IntentOwner.movementTarget = intent.target;
        }   
    }
}

public struct UpdateTargetAgentIntent : IIntent
{
    public Agent IntentOwner {get;set;}
    public Agent target;

    public UpdateTargetAgentIntent(Agent owner, Agent newTarget)
    {
        IntentOwner = owner;
        target = newTarget;
    }
}

public class UpdateTargetAgentResolver : IntentResolver<UpdateTargetAgentIntent>
{
    public override void Resolve(World world,IEnumerable<UpdateTargetAgentIntent> intents)
    {
        foreach(var intent in intents)
        {
            intent.IntentOwner.targetAgent = intent.target;
        }   
    }
}

public struct RecoverIntent : IIntent
{
    public Agent IntentOwner {get;set;}
    public RecoverIntent(Agent owner)
    {
        IntentOwner = owner;
    }
}

public class RecoverResolver : IntentResolver<RecoverIntent>
{
    public override void Resolve(World world, IEnumerable<RecoverIntent> intents)
    {
        foreach(var intent in intents)
        {
            intent.IntentOwner.infected = false;
            world.simulationState.OnInfectionChange(intent.IntentOwner);
        }
    }
}