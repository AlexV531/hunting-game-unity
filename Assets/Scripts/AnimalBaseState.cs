using UnityEngine;

public abstract class AnimalBaseState
{
    protected AnimalBaseState nextState;
    public abstract AlertnessLevel Alertness { get; }

    public abstract void EnterState(AnimalStateManager animal);

    public abstract void UpdateState(AnimalStateManager animal);

    public void SetNextState(AnimalBaseState next) => nextState = next;
}
