using UnityEngine;
using UnityEngine.AI;

public class AnimalFleeingState : AnimalMovingState
{
    public float fleeingSpeed = 14f;
    public override AlertnessLevel Alertness => AlertnessLevel.Panicked;

    public override void EnterState(AnimalStateManager animal)
    {
        agent = animal.GetComponent<NavMeshAgent>();
        agent.speed = fleeingSpeed;
        Debug.Log("Fleeing state entered.");
        Debug.Log("Num targets: " + targetQueue.Count);
    }
}
