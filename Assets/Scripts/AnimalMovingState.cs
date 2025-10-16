using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class AnimalMovingState : AnimalBaseState
{
    [Header("Movement Settings")]
    public float navmeshSearchRadius = 2f;
    public float movingSpeed = 4.5f;

    protected NavMeshAgent agent;
    private readonly Queue<Vector3> targetQueue = new();

    public override AlertnessLevel Alertness => AlertnessLevel.Calm;

    public override void EnterState(AnimalStateManager animal)
    {
        agent = animal.GetComponent<NavMeshAgent>();
        agent.speed = movingSpeed;
        Debug.Log("Moving state entered.");
    }

    public override void UpdateState(AnimalStateManager animal)
    {
        // If the agent currently has a valid path and hasn't reached the destination
        if (agent.hasPath && !agent.pathPending)
        {
            if (agent.remainingDistance <= 0.1f)
                AdvanceToNextTarget(animal);

            return; // Wait until path is done before considering new targets
        }

        // No current path and nothing to do
        if (!agent.hasPath && targetQueue.Count == 0)
        {
            OnTargetsDepleted(animal);
            return;
        }

        // No current path, but more targets exist
        if (!agent.hasPath && targetQueue.Count > 0)
        {
            SetNextTarget();
        }
    }

    public void ClearQueueAndAddTarget(Vector3 targetPosition)
    {
        targetQueue.Clear();
        AddTarget(targetPosition);
    }

    public void AddTarget(Vector3 targetPosition)
    {
        if (NavMesh.SamplePosition(targetPosition, out var hit, navmeshSearchRadius, NavMesh.AllAreas))
            targetQueue.Enqueue(hit.position);
        else
            Debug.LogWarning("Target position is not on or near the NavMesh.");
    }

    public void ClearTargets()
    {
        targetQueue.Clear();
        if (agent != null)
            agent.ResetPath();
    }

    private void SetNextTarget()
    {
        if (targetQueue.Count == 0) return;

        agent.SetDestination(targetQueue.Dequeue());
    }

    private void AdvanceToNextTarget(AnimalStateManager animal)
    {
        agent.ResetPath(); // clear current path before setting next one
        if (targetQueue.Count > 0)
            SetNextTarget();
        else
            OnTargetsDepleted(animal);
    }

    protected virtual void OnTargetsDepleted(AnimalStateManager animal)
    {
        if (nextState != null)
            animal.ChangeState(nextState);
    }
}
