using UnityEngine;
using UnityEngine.AI;

public class AnimalFleeingState : AnimalMovingState
{
    public float fleeingSpeed = 10f;
    public override AlertnessLevel Alertness => AlertnessLevel.Panicked;

    public override void EnterState(AnimalStateManager animal)
    {
        Debug.Log("Fleeing state entered.");
        agent = animal.GetComponent<NavMeshAgent>();
        agent.speed = fleeingSpeed;
    }
}
