using UnityEngine;

public class AnimalGrazingState : AnimalBaseState
{
    public override AlertnessLevel Alertness => AlertnessLevel.Calm;

    public override void EnterState(AnimalStateManager animal)
    {
        Debug.Log("Grazing state entered.");
    }

    public override void UpdateState(AnimalStateManager animal)
    {

    }
    
    public override void ExitState(AnimalStateManager animal)
    {

    }
}
