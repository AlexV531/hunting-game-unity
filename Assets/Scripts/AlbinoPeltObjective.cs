using UnityEngine;

public class AlbinoPeltObjective : ObjectiveInteractable
{
    private int moneyReward = 200;

    protected override void Start()
    {
        numTargets = 1;
        base.Start();
    }

    protected override bool CheckSubmission()
    {
        foreach (var item in submissionBox.GetItems())
        {
            var def = ItemDatabase.Instance.GetItem(item.key);
            if (def != null && def.itemType == ItemType.AnimalPelt &&
                item.customData.description.ToString() == "Albino")
            {
                Debug.Log("SubmissionBox contains an albino pelt.");
                return true;
            }
        }

        Debug.Log("No albino pelt found in SubmissionBox.");
        return false;
    }

    protected override bool OnObjectiveComplete()
    {
        // Remove pelt from the submission and give player reward
        FirstPersonController.LocalPlayer.Money += moneyReward;
        ConsumePelts();
        Debug.Log("Objective complete: 10 pelts consumed. Reward granted.");
        return true;
    }

    protected void ConsumePelts()
    {
        // Iterate through submissionBox items
        for (int i = submissionBox.GetItems().Count - 1; i >= 0; i--)
        {
            var item = submissionBox.GetItems()[i];
            var def = ItemDatabase.Instance.GetItem(item.key);

            if (def != null && def.itemType == ItemType.AnimalPelt &&
                item.customData.description.ToString() == "Albino")
            {
                // Remove the albino pelt entirely
                submissionBox.GetItems().RemoveAt(i);
                Debug.Log("Albino pelt consumed.");
                return;
            }
        }
    }
}
