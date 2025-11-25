using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AnimalInspectUI : UIMenu
{
    public InspectRoom inspectRoom;
    public Transform hitDataContainer;
    public GameObject hitDataPrefab;
    public Transform animalInfoContainer;
    public GameObject animalInfoPrefabOneLine;
    public GameObject animalInfoPrefabTwoLine;
    public TMP_Text animalInfoHeader;
    public Button closeButton;
    private PlayerInputs inputs;
    
    protected override void Start()
    {
        base.Start();

        closeButton.onClick.AddListener(CloseMenu);
    }

    public void OpenInspectScreen(GameObject inspectTarget, HitDataStrings[] hits, AnimalVariator variator)
    {
        // this.inspectTarget = inspectRoom.ReplaceInspectTarget(inspectTarget, new Vector3(0, -0.4f, -7));
        inspectRoom.ReplaceInspectTarget(inspectTarget, new Vector3(0, -0.4f, -7));
        // Remove old hit data entries
        foreach (Transform child in hitDataContainer)
        {
            Destroy(child.gameObject);
        }
        // Remove old animal info entries
        foreach (Transform child in animalInfoContainer)
        {
            Debug.Log("hello");
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
        // Add animal info entries, use AnimalVariator to find the sex, age, and quality of organs pelt and antlers
        if (variator != null)
        {
            if (variator.male)
            {
                animalInfoHeader.text = "Age: " + (variator.age * variator.averageAge).ToString("F1") + " Sex: Male";
                AddAnimalInfoEntry("Antlers", "Quality Unknown");
            }
            else
            {
                animalInfoHeader.text = "Age: " + (variator.age * variator.averageAge).ToString("F1") + " Sex: Female";
            }
            AddAnimalInfoEntry("Pelt", variator.pelt.Description.ToString(), "Quality Unknown");
        }

        OpenMenu();
    }

    void Update()
    {
        if (IsMenuOpen())
            inspectRoom.UpdateModelRotation(inputs);
    }

    public void AddAnimalInfoEntry(string title, string infoLine1)
    {
        GameObject animalInfoEntryObj = Instantiate(animalInfoPrefabOneLine, animalInfoContainer);
        AnimalInfoUIEnty animalInfoEntry = animalInfoEntryObj.GetComponent<AnimalInfoUIEnty>();
        if (animalInfoEntry != null)
        {
            animalInfoEntry.Title.text = title;
            animalInfoEntry.InfoLine1.text = infoLine1;
        }
    }

    public void AddAnimalInfoEntry(string title, string infoLine1, string infoLine2)
    {
        GameObject animalInfoEntryObj = Instantiate(animalInfoPrefabTwoLine, animalInfoContainer);
        AnimalInfoUIEnty animalInfoEntry = animalInfoEntryObj.GetComponent<AnimalInfoUIEnty>();
        if (animalInfoEntry != null)
        {
            animalInfoEntry.Title.text = title;
            animalInfoEntry.InfoLine1.text = infoLine1;
            animalInfoEntry.InfoLine2.text = infoLine2;
        }
    }

    public void SetPlayerInput(PlayerInputs inputs) => this.inputs = inputs;
}