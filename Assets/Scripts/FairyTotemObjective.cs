using UnityEngine;

public class FairyTotemObjective : ObjectiveInteractable
{
    private ItemInstance fairyKey;

    protected override void Start()
    {
        numTargets = 1;
        requiresPlayerInventory = true;
        base.Start();

        fairyKey = new ItemInstance
        {
            key = 21,
            stackSize = 1,
        };
    }

    protected override bool CheckSubmission()
    {
        foreach (var item in submissionBox.GetItems())
        {
            if (item.key == 23)
            {
                Debug.Log("SubmissionBox contains a fairy totem.");
                return true;
            }
        }

        Debug.Log("No fairy totem found in SubmissionBox.");
        return false;
    }

    protected override bool OnObjectiveComplete()
    {
        // Remove 10 pelts from the submission box if player claims reward
        if (FirstPersonController.LocalPlayer.GetInventory().TryAddItem(fairyKey))
        {
            ConsumeItem();
            Debug.Log("Objective complete: 10 pelts consumed. Reward granted.");
            return true;
        }
        return false;
    }

    protected void ConsumeItem()
    {
        // Iterate through submissionBox items
        for (int i = submissionBox.GetItems().Count - 1; i >= 0; i--)
        {
            var item = submissionBox.GetItems()[i];
            if (item.key == 23)
            {
                submissionBox.GetItems().RemoveAt(i);
                Debug.Log("Fairy totem consumed.");
                return;
            }
        }
    }
}
