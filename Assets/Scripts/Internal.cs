using UnityEngine;

public class Internal : MonoBehaviour
{
    [Header("Organ Stats")]
    [Tooltip("How much damage this organ can take")]
    public float strength = 10f;

    [Tooltip("Priority when calculating damage or hit order")]
    public float internalPriority = 0f;

    [Tooltip("Reference to the animal this organ belongs to")]
    public Animal animal;

    [Tooltip("How lethal a hit to this organ is")]
    public float lethality = 0f;

    [Tooltip("Factor affecting bleeding from this organ")]
    public float bleedFactor = 1f;

    [Tooltip("Amount of bullet power required to destroy this organ completely")]
    public float internalStrength = 1f;
}
