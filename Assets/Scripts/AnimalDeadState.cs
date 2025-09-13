using UnityEngine;

public class AnimalDeadState : AnimalBaseState
{
    public override AlertnessLevel Alertness => AlertnessLevel.Dead;

    public override void EnterState(AnimalStateManager animal)
    {
        Debug.Log("Dead state entered.");
    }

    public override void UpdateState(AnimalStateManager animal)
    {
        
    }
}
