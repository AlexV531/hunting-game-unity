using System.Collections.Generic;
using UnityEngine;

public class ObjectiveInteractable : InteractableBase
{
    public Inventory submissionBox = new Inventory();
    public ItemSpawner itemSpawner;
    public string title;
    public string description;
    protected int numTargets; // Number of items needed for submission
    protected bool requiresPlayerInventory = false;

    protected virtual void Start()
    {
        submissionBox.SetCapacity(numTargets);
    }

    protected virtual bool CheckSubmission()
    {
        Debug.Log("Default Check: SubmissionBox contains " + submissionBox.GetItems().Count + " items.");
        return true; // Example default condition
    }

    protected virtual bool OnObjectiveComplete()
    {
        Debug.Log("Objective complete");
        return true;
    }

    public override void Interact(FirstPersonController player)
    {
        if (player.GetCarriedWorldItem() != null)
        {
            WorldItem itemToStore = player.GetCarriedWorldItem();

            if (submissionBox.TryAddItem(itemToStore.GetItemData()))
            {
                player.DropWorldItemServerRpc();
                itemToStore.DespawnItemServerRpc();
            }
        }
        else
        {
            var objectiveMenu = player.GetObjectiveMenu();
            objectiveMenu.OpenObjectiveMenu(player.GetInventory(), submissionBox, title, description, numTargets, requiresPlayerInventory);
            objectiveMenu.inventoryPanel.OnItemTooLarge = (item) =>
            {
                itemSpawner.DropItem(item, itemSpawner.transform.position, Vector3.zero);
            };
            objectiveMenu.OnItemSubmitted = CheckSubmission;
            objectiveMenu.OnObjectiveComplete = OnObjectiveComplete;
        }
    }

    public override string GetPrompt(FirstPersonController player)
    {
        if (player.GetCarriedWorldItem() != null)
        {
            return "Press \"e\" to submit carried item";
        }
        else
        {
            return "Press \"e\" to see objective";
        }
    }
}