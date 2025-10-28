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

    void Start()
    {
        InitializeStates();
        // Set up states
        MovingState.SetNextState(GrazingState);
        FleeingState.SetNextState(GrazingState);
        ListeningState.SetNextState(GrazingState);
        // Initial state
        currentState = GrazingState;
        currentState.EnterState(this);
    }

    void Update()
    {
        currentState.UpdateState(this);
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
