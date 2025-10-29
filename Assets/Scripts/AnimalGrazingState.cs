using UnityEngine;

public class AnimalGrazingState : AnimalBaseState
{
    public float minWaitTime = 1f;
    public float maxWaitTime = 10f;
    private float timeTilMove;
    public override AlertnessLevel Alertness => AlertnessLevel.Calm;

    public override void EnterState(AnimalStateManager animal)
    {
        Debug.Log("Grazing state entered.");
        // Randomize a time until the animal moves
        timeTilMove = Random.Range(minWaitTime, maxWaitTime);
    }

    public override void UpdateState(AnimalStateManager animal)
    {
        timeTilMove -= Time.deltaTime;
        if (timeTilMove <= 0)
        {
            timeTilMove = 0;
            DoneGrazing(animal);
        }
    }

    public override void ExitState(AnimalStateManager animal)
    {

    }
    
    private void DoneGrazing(AnimalStateManager animal)
    {
        if (nextState != null)
        {
            AnimalAI animalAI = animal.GetComponent<AnimalAI>();
            Vector3 targetPosition;
            if (animalAI.herd != null)
            {
                targetPosition = animalAI.herd.GetRandomPointInRadius();
            }
            else // Fallback if no herd
            {
                Vector3 randomOffset = new Vector3(
                    Random.Range(0f, 7f),
                    0f,
                    Random.Range(0f, 7f)
                );
                targetPosition = animal.transform.position + randomOffset;
            }
            if (nextState is AnimalMovingState movingState)
            {
                movingState.AddTarget(targetPosition);
            }
            animal.ChangeState(nextState);
        }
    }
}
