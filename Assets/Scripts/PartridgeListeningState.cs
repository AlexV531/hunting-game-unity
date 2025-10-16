using UnityEngine;
using UnityEngine.AI;

public class PartridgeListeningState : AnimalMovingState //: AnimalTimerState
{
    [Header("Reaction Settings")]
    public float retreatDistance = 6f; // How far to move away
    public float sideOffsetAngle = 80f; // How much to offset to the side
    public float curiosityFactor = 0.8f; // 0 = direct flee, 1 = more lateral

    public float listeningMoveSpeed = 1.5f;
    private Animal actor;

    public override void EnterState(AnimalStateManager animal)
    {
        Debug.Log("Partridge listening state entered.");
        agent = animal.GetComponent<NavMeshAgent>();
        actor = animal.GetComponent<Animal>();
        agent.speed = listeningMoveSpeed;
    }

    public override void UpdateState(AnimalStateManager animal)
    {
        // If there is still a sound to hear, the partridge will move around nervously until it either panics or the sound disapates
        if (actor == null || actor.animalAI.MostRecentSoundPosition == null || actor.animalAI.soundHeard <= 0)
            animal.ChangeState(nextState);

        base.UpdateState(animal);
    }

    protected override void OnTargetsDepleted(AnimalStateManager animal)
    {
        if (nextState != null)
            AddTarget(GetEvasiveTarget(actor.transform.position, actor.animalAI.MostRecentSoundPosition));
    }

    public Vector3 GetEvasiveTarget(Vector3 currentPosition, Vector3 soundPosition)
    {
        // Direction away from the sound
        Vector3 fromSound = (currentPosition - soundPosition).normalized;
        if (fromSound.sqrMagnitude < 0.001f)
        {
            fromSound = Random.insideUnitSphere; // Fallback if on same position
        }

        // Randomly choose to veer left or right
        float sideSign = Random.value < 0.5f ? -1f : 1f;
        Quaternion sideRotation = Quaternion.AngleAxis(sideSign * sideOffsetAngle, Vector3.up);
        Vector3 sideDirection = sideRotation * fromSound;

        // Blend between direct retreat and side movement
        Vector3 finalDirection = Vector3.Lerp(fromSound, sideDirection, curiosityFactor).normalized;

        // Compute final target
        return currentPosition + finalDirection * retreatDistance;
    }
}