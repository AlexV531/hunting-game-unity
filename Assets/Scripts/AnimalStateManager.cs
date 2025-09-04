using UnityEngine;
using UnityEngine.AI;

public class AnimalStateManager : MonoBehaviour
{
    AnimalBaseState currentState;
    public AnimalGrazingState GrazingState = new AnimalGrazingState();
    public AnimalMovingState MovingState = new AnimalMovingState();

    void Start()
    {
        // Set up states
        MovingState.agent = GetComponent<NavMeshAgent>();
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
}
