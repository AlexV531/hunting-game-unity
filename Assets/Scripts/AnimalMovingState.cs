using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class AnimalMovingState : AnimalBaseState
{
    public float navmeshSearchRadius = 2.0f;
    public NavMeshAgent agent;
    private Queue<Vector3> targetQueue = new Queue<Vector3>();
    private bool hasTarget = false;

    public override void EnterState(AnimalStateManager animal)
    {
        Debug.Log("Moving state entered.");
    }

    public override void UpdateState(AnimalStateManager animal)
    {
        if (hasTarget)
        {
            CheckIfReachedTarget(animal);
        }
        else if (targetQueue.Count > 0)
        {
            SetNextTarget();
        }
        else // No more targets in queue
        {
            animal.ChangeState(animal.GrazingState);
        }
    }

    public void ClearQueueAndAddTarget(Vector3 targetPosition)
    {
        ClearTargets();
        AddTarget(targetPosition);
    }

    public void AddTarget(Vector3 targetPosition)
    {
        // Snap the target to the navmesh if possible
        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPosition, out hit, navmeshSearchRadius, NavMesh.AllAreas))
        {
            targetQueue.Enqueue(hit.position);
        }
        else
        {
            Debug.LogWarning("Target position is not on or near the NavMesh.");
        }
    }

    public void ClearTargets()
    {
        targetQueue.Clear();
        agent.SetDestination(agent.transform.position);
    }

    private void SetNextTarget()
    {
        if (targetQueue.Count == 0) return;

        Vector3 nextTarget = targetQueue.Dequeue();
        agent.SetDestination(nextTarget);
        hasTarget = true;
    }

    private void CheckIfReachedTarget(AnimalStateManager animal)
    {
        if (!agent.pathPending)
        {
            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                {
                    Debug.Log("Target reached.");

                    hasTarget = false;

                    // Automatically move to next target if queue is not empty
                    if (targetQueue.Count > 0)
                    {
                        SetNextTarget();
                    }
                    // No more targets in queue
                    else
                    {
                        animal.ChangeState(animal.GrazingState);
                    }
                }
            }
        }
    }
}
