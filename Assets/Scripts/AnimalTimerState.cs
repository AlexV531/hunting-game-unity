using UnityEngine;

public class AnimalTimerState : AnimalBaseState
{
    private float waitTime = 10f;
    private float elapsedTime = 0f;
    private AnimalBaseState nextState;

    public AnimalTimerState(float defaultWaitTime = 10f)
    {
        waitTime = defaultWaitTime;
    }

    public void SetWaitTime(float newWaitTime)
    {
        waitTime = newWaitTime;
        elapsedTime = 0f; // Reset timer when wait time changes
    }

    public void SetNextState(AnimalBaseState nextState)
    {
        this.nextState = nextState;
    }

    public override void EnterState(AnimalStateManager animal)
    {
        Debug.Log("Timer state entered.");
        elapsedTime = 0f; // Reset timer when entering state
    }

    public override void UpdateState(AnimalStateManager animal)
    {
        // Increment timer
        elapsedTime += Time.deltaTime;

        // Check if timer has finished
        if (elapsedTime >= waitTime)
        {
            Debug.Log("Timer finished");
            if (nextState != null)
            {
                animal.ChangeState(nextState);
            }
        }
    }
}
