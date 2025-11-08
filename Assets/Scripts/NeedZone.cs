using UnityEngine;

public enum NeedType
{
    Eating,
    Drinking,
    Resting
}

public class NeedZone : MonoBehaviour
{
    public NeedType needType;
    public float radius = 15f;
    public bool isOccupied = false;
    public Herd occupyingHerd = null;

    private void OnDrawGizmos()
    {
        Gizmos.color = isOccupied ? Color.red : GetNeedColor();
        Gizmos.DrawWireSphere(transform.position, radius);
    }

    private Color GetNeedColor()
    {
        return needType switch
        {
            NeedType.Eating => Color.green,
            NeedType.Drinking => Color.blue,
            NeedType.Resting => Color.yellow,
            _ => Color.white
        };
    }

    public bool TryOccupy(Herd herd)
    {
        if (isOccupied && occupyingHerd != herd)
            return false;
            
        isOccupied = true;
        occupyingHerd = herd;
        return true;
    }

    public void Release()
    {
        isOccupied = false;
        occupyingHerd = null;
    }
}