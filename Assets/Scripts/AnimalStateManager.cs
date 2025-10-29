using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class AnimalStateManager : MonoBehaviour
{
    protected AnimalBaseState currentState;

    public AnimalBaseState GrazingState;
    public AnimalMovingState MovingState;
    public AnimalMovingState FleeingState;
    public AnimalBaseState ListeningState;
    public AnimalBaseState DeadState;

    protected virtual void InitializeStates()
    {
        GrazingState = new AnimalGrazingState();
        MovingState = new AnimalMovingState();
        FleeingState = new AnimalFleeingState();
        ListeningState = new AnimalListeningState();
        DeadState = new AnimalDeadState();
    }

    public void InitializeFSM()
    {
        InitializeStates();
        // Set up states
        GrazingState.SetNextState(MovingState);
        MovingState.SetNextState(GrazingState);
        FleeingState.SetNextState(GrazingState);
        ListeningState.SetNextState(GrazingState);
        // Initial state
        currentState = GrazingState;
        currentState.EnterState(this);
    }

    void Update()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            currentState.UpdateState(this);
        }
    }

    public void ChangeState(AnimalBaseState state)
    {
        currentState.ExitState(this);
        currentState = state;
        state.EnterState(this);
    }

    public AnimalBaseState GetCurrentState()
    {
        return currentState;
    }
}
