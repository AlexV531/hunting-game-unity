using UnityEngine;

public abstract class AnimalBaseState
{
    public abstract void EnterState(AnimalStateManager animal);

    public abstract void UpdateState(AnimalStateManager animal);
}
