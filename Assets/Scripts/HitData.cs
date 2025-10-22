using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class HitData
{
    // Impact points
    public List<Vector3> intersectionPoints = new List<Vector3>();

    // List of internal organs hit
    public List<InternalHitData> internalsHit = new List<InternalHitData>();

    // Bullet properties
    public float bulletStrength = 1.0f;
    public float bulletBleed = 1.0f;
    public float bleedRate = 0.0f;
    public float healRate = 1.0f;

    // Affect the shot had
    public float initialDamageDone = 0f;
    public float bleedDamageDone = 0f;

    // Player who made the shot
    public FirstPersonController player;

    // Nested class for internal hits
    [System.Serializable]
    public class InternalHitData
    {
        public Internal internalPart; // Reference to the organ/body part hit
        public float hitWithPower = 0f; // How strong the hit was
        public float hitDist = 0f; // Distance traveled through this organ

        public InternalHitData(Internal internalPart, float hitWithPower)
        {
            this.internalPart = internalPart;
            this.hitWithPower = hitWithPower;
            hitDist = 0f;
        }
    }

    public void AddIntersectionPoint(Vector3 point)
    {
        intersectionPoints.Add(point);
    }

    public void AddInternalHitData(InternalHitData internalHitData)
    {
        internalsHit.Add(internalHitData);
    }

    public bool AddToHitDist(Internal internalPart, float hitDistToAdd)
    {
        foreach (InternalHitData internalHit in internalsHit)
        {
            if (internalHit.internalPart == internalPart)
            {
                internalHit.hitDist += hitDistToAdd;
                return true;
            }
        }
        return false;
    }
}