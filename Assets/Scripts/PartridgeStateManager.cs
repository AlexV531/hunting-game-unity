using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class PartridgeStateManager : AnimalStateManager
{
    protected override void InitializeStates()
    {
        GrazingState = new AnimalGrazingState();
        MovingState = new AnimalMovingState();
        FleeingState = new AnimalFleeingState();
        ListeningState = new PartridgeListeningState();
        DeadState = new AnimalDeadState();
    }
}