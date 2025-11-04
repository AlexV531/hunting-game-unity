using UnityEngine;

public class TenPeltObjective : ObjectiveInteractable
{
    private ItemInstance cauldronReward;
    
    protected override void Start()
    {
        numTargets = 10;
        base.Start();

        cauldronReward = new ItemInstance
        {
            key = 22,
            stackSize = 1,
        };
    }

    protected override bool CheckSubmission()
    {
        int peltCount = 0;

        foreach (var item in submissionBox.GetItems())
        {
            var def = ItemDatabase.Instance.GetItem(item.key);
            if (def != null && def.itemType == ItemType.AnimalPelt)
            {
                peltCount += item.stackSize;
            }
        }

        Debug.Log("SubmissionBox contains " + peltCount + " pelts.");
        return peltCount >= 10;
    }

    protected override bool OnObjectiveComplete()
    {
        // Remove 10 pelts from the submission box if player claims reward
        if (FirstPersonController.LocalPlayer.GetInventory().TryAddItem(cauldronReward))
        {
            ConsumePelts(10);
            Debug.Log("Objective complete: 10 pelts consumed. Reward granted.");
            return true;
        }
        return false;
    }

    protected void ConsumePelts(int amount)
    {
        int remaining = amount;

        // Iterate through submissionBox items
        for (int i = submissionBox.GetItems().Count - 1; i >= 0; i--)
        {
            var item = submissionBox.GetItems()[i];
            var def = ItemDatabase.Instance.GetItem(item.key);

            if (def != null && def.itemType == ItemType.AnimalPelt)
            {
                if (item.stackSize > remaining)
                {
                    // Reduce stack by remaining needed
                    ItemInstance updated = item;
                    updated.stackSize -= remaining;
                    submissionBox.GetItems()[i] = updated;
                    return;
                }
                else
                {
                    // Remove entire stack
                    remaining -= item.stackSize;
                    submissionBox.GetItems().RemoveAt(i);
                }

                if (remaining <= 0)
                    return;
            }
        }
    }
}