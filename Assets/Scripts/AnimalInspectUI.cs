using UnityEngine;
using UnityEngine.UI;

public class AnimalInspectUI : UIMenu
{
    public GameObject inspectScreen;
    public InspectRoom inspectRoom;
    public Transform hitDataContainer;
    public GameObject hitDataPrefab;
    public Button closeButton;
    private bool inspectOpen = true;
    private PlayerInputs inputs;
    
    protected override void Start()
    {
        base.Start();

        closeButton.onClick.AddListener(CloseMenu);
    }

    public void OpenInspectScreen(GameObject inspectTarget, HitDataStrings[] hits)
    {
        // this.inspectTarget = inspectRoom.ReplaceInspectTarget(inspectTarget, new Vector3(0, -0.4f, -7));
        inspectRoom.ReplaceInspectTarget(inspectTarget, new Vector3(0, -0.4f, -7));
        // Remove old hit data entries
        foreach (Transform child in hitDataContainer)
        {
            Destroy(child.gameObject);
        }
        // Add hit data entries
        foreach (HitDataStrings hitDataStrings in hits)
        {
            GameObject hitDataEntryObj = Instantiate(hitDataPrefab, hitDataContainer);
            HitDataUIEntry hitDataEntry = hitDataEntryObj.GetComponent<HitDataUIEntry>();
            if (hitDataEntry != null)
            {
                hitDataEntry.WeaponAndPlayerText.text = hitDataStrings.string1.ToString();
                Debug.Log("String1: " + hitDataStrings.string1.ToString());
                hitDataEntry.DamageAndBleedText.text = hitDataStrings.string2.ToString();
                hitDataEntry.InternalsHitText.text = hitDataStrings.string3.ToString();
            }
        }

        OpenMenu();
    }

    void Update()
    {
        if (inspectOpen)
            inspectRoom.UpdateModelRotation(inputs);
    }

    public void SetPlayerInput(PlayerInputs inputs) => this.inputs = inputs;
}