using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class AnimalMovingState : AnimalBaseState
{
    [Header("Movement Settings")]
    public float navmeshSearchRadius = 2f;
    public float movingSpeed = 4.5f;

    [Header("Path Completion Settings")]
    [Tooltip("Additional distance buffer beyond stopping distance to consider arrived")]
    public float arrivalThreshold = 0.1f;

    [Tooltip("Minimum velocity squared magnitude to consider agent stopped")]
    public float stoppedVelocityThreshold = 0.01f;

    protected NavMeshAgent agent;
    protected readonly Queue<Vector3> targetQueue = new();
    private bool isWaitingForPath;

    public override AlertnessLevel Alertness => AlertnessLevel.Calm;

    public override void EnterState(AnimalStateManager animal)
    {
        agent = animal.GetComponent<NavMeshAgent>();

        if (agent == null)
        {
            Debug.LogError($"NavMeshAgent component missing on {animal.gameObject.name}");
            return;
        }

        agent.speed = movingSpeed;
        isWaitingForPath = false;

        // If targets were added before entering state, start moving immediately
        if (targetQueue.Count > 0 && !agent.hasPath)
        {
            SetNextTarget();
        }

        // Debug.Log($"{animal.gameObject.name}: Moving state entered with {targetQueue.Count} targets.");
    }

    public override void UpdateState(AnimalStateManager animal)
    {
        if (agent == null) return;

        // Currently calculating a path
        if (isWaitingForPath && agent.pathPending)
        {
            return; // Wait for path calculation to complete
        }

        // Path calculation just completed
        if (isWaitingForPath && !agent.pathPending)
        {
            isWaitingForPath = false;

            // Check if path is valid
            if (agent.pathStatus == NavMeshPathStatus.PathInvalid)
            {
                Debug.LogWarning("Invalid path calculated, trying next target");
                AdvanceToNextTarget(animal);
                return;
            }
        }

        // Currently following a valid path
        if (agent.hasPath && !agent.pathPending)
        {
            // Check if agent has arrived at destination
            if (HasReachedDestination())
            {
                AdvanceToNextTarget(animal);
            }
            return;
        }

        // No current path - check if we have queued targets
        if (!agent.hasPath && !isWaitingForPath && targetQueue.Count > 0)
        {
            SetNextTarget();
        }
        else if (!agent.hasPath && !isWaitingForPath && targetQueue.Count == 0)
        {
            // No path and no targets remaining
            OnTargetsDepleted(animal);
        }
    }

    public override void ExitState(AnimalStateManager animal)
    {
        if (agent != null)
        {
            agent.ResetPath();
        }

        isWaitingForPath = false;
    }

    public void ClearQueueAndAddTarget(Vector3 targetPosition)
    {
        ClearTargets();
        AddTarget(targetPosition);
    }

    public void AddTarget(Vector3 targetPosition)
    {
        if (NavMesh.SamplePosition(targetPosition, out var hit, navmeshSearchRadius, NavMesh.AllAreas))
        {
            targetQueue.Enqueue(hit.position);
        }
        else
        {
            Debug.LogWarning($"Target position {targetPosition} is not on or near the NavMesh (search radius: {navmeshSearchRadius})");
        }
    }

    public void AddTargets(IEnumerable<Vector3> targetPositions)
    {
        foreach (var target in targetPositions)
        {
            AddTarget(target);
        }
    }

    public void ClearTargets()
    {
        targetQueue.Clear();
        isWaitingForPath = false;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.ResetPath();
        }
    }

    public int RemainingTargetCount => targetQueue.Count;

    private bool HasReachedDestination()
    {
        if (!agent.hasPath) return true;

        // Check distance to destination
        float distanceThreshold = agent.stoppingDistance + arrivalThreshold;
        bool closeEnough = agent.remainingDistance <= distanceThreshold;

        // Check if agent has stopped moving
        bool hasStopped = agent.velocity.sqrMagnitude < stoppedVelocityThreshold;

        return closeEnough && hasStopped;
    }

    private void SetNextTarget()
    {
        if (targetQueue.Count == 0 || agent == null) return;

        Vector3 nextTarget = targetQueue.Dequeue();
        agent.SetDestination(nextTarget);
        isWaitingForPath = true;

        // Debug.Log($"Setting next target: {nextTarget}, remaining targets: {targetQueue.Count}");
    }

    private void AdvanceToNextTarget(AnimalStateManager animal)
    {
        agent.ResetPath();
        isWaitingForPath = false;

        if (targetQueue.Count > 0)
        {
            SetNextTarget();
        }
        else
        {
            OnTargetsDepleted(animal);
        }
    }

    protected virtual void OnTargetsDepleted(AnimalStateManager animal)
    {
        // Safety check - only transition if we're truly done
        if (agent.hasPath || isWaitingForPath || targetQueue.Count > 0)
        {
            Debug.LogWarning($"{animal.gameObject.name}: OnTargetsDepleted called prematurely, ignoring");
            return;
        }

        // Debug.Log($"{animal.gameObject.name}: All movement targets depleted");

        if (nextState != null)
        {
            animal.ChangeState(nextState);
        }
    }
}
