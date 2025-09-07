using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class AnimalStateManager : MonoBehaviour
{
    AnimalBaseState currentState;
    public AnimalGrazingState GrazingState = new AnimalGrazingState();
    public AnimalMovingState MovingState = new AnimalMovingState();
    public AnimalFleeingState FleeingState = new AnimalFleeingState();
    public AnimalListeningState ListeningState = new AnimalListeningState();

    void Start()
    {
        // Set up states
        MovingState.agent = GetComponent<NavMeshAgent>();
        MovingState.SetNextState(GrazingState);
        FleeingState.agent = GetComponent<NavMeshAgent>();
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
        currentState = state;
        state.EnterState(this);
    }

    public AnimalBaseState GetCurrentState()
    {
        return currentState;
    }
}
