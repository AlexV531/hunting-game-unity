using UnityEngine;

public class HerdNeeds
{
    public float eating = 30.5f;
    public float drinking = 100f;
    public float resting = 100f;
    
    // Drain rates per second
    public float eatingDrainRate = 2f;
    public float drinkingDrainRate = 3f;
    public float restingDrainRate = 1.5f;
    
    // Refill rates per second when at need zone
    public float refillRate = 10f;
    
    public void DrainNeeds(float deltaTime)
    {
        eating = Mathf.Max(0, eating - eatingDrainRate * deltaTime);
        drinking = Mathf.Max(0, drinking - drinkingDrainRate * deltaTime);
        resting = Mathf.Max(0, resting - restingDrainRate * deltaTime);
    }
    
    public void RefillNeed(NeedType needType, float deltaTime)
    {
        switch (needType)
        {
            case NeedType.Eating:
                eating = Mathf.Min(100, eating + refillRate * deltaTime);
                break;
            case NeedType.Drinking:
                drinking = Mathf.Min(100, drinking + refillRate * deltaTime);
                break;
            case NeedType.Resting:
                resting = Mathf.Min(100, resting + refillRate * deltaTime);
                break;
        }
    }
    
    public NeedType GetLowestNeed()
    {
        if (eating <= drinking && eating <= resting)
            return NeedType.Eating;
        if (drinking <= resting)
            return NeedType.Drinking;
        return NeedType.Resting;
    }
    
    public float GetNeedValue(NeedType needType)
    {
        return needType switch
        {
            NeedType.Eating => eating,
            NeedType.Drinking => drinking,
            NeedType.Resting => resting,
            _ => 0
        };
    }
}