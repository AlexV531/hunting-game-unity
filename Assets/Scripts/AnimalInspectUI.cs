using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AnimalInspectUI : MonoBehaviour
{
    public GameObject inspectScreen;
    public InspectRoom inspectRoom;
    public Transform hitDataContainer;
    public GameObject hitDataPrefab;
    public Button closeButton;
    private bool inspectOpen = true;
    private PlayerInputs inputs;
    
    private void Start()
    {
        closeButton.onClick.AddListener(CloseInspectScreen);

        CloseInspectScreen();
        CursorManager.SetCursorActive(true);
    }

    public void OpenInspectScreen()
    {
        inspectScreen.SetActive(true);
        CursorManager.SetCursorActive(true);
        inspectOpen = true;
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
        inspectScreen.SetActive(true);
        CursorManager.SetCursorActive(true);
        inspectOpen = true;
    }

    public void CloseInspectScreen()
    {
        inspectScreen.SetActive(false);
        CursorManager.SetCursorActive(false);
        inspectOpen = false;
    }

    void Update()
    {
        if (inspectOpen)
            inspectRoom.UpdateModelRotation(inputs);
    }

    public bool IsInspectOpen()
    {
        return inspectOpen;
    }

    public void SetPlayerInput(PlayerInputs inputs) => this.inputs = inputs;

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && IsInspectOpen())
        {
            CursorManager.SetCursorActive(true);
        }
    }
}