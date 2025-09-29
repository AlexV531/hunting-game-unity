using UnityEngine;

public abstract class AnimalStoringInteractableBase : InteractableBase
{
    public Transform placementPoint;
    private Animal placedAnimal;
    
    public void SetPlacedAnimal(Animal animal)
    {
        placedAnimal = animal;
    }

    public Animal GetPlacedAnimal() => placedAnimal;

    public void ClearPlacedAnimal()
    {
        placedAnimal = null;
    }
}
