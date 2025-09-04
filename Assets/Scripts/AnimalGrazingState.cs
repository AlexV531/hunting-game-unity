using UnityEngine;

public class AnimalGrazingState : AnimalBaseState
{
    public override void EnterState(AnimalStateManager animal)
    {
        Debug.Log("Grazing state entered.");
    }

    public override void UpdateState(AnimalStateManager animal)
    {
        
    }
}
