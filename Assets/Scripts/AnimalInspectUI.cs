using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AnimalInspectUI : MonoBehaviour
{
    public GameObject inspectScreen;
    public Transform modelTransform;
    public Transform hitDataContainer;
    public GameObject hitDataPrefab;
    public float rotationSpeed = 1f;
    public float sensitivity = 1f;
    public float exponent = 1.5f;
    public Button closeButton;
    private bool inspectOpen = true;
    private Vector2 lastMousePosition;
    private PlayerInputs inputs;
    private GameObject inspectTarget;
    
    private void Start()
    {
        closeButton.onClick.AddListener(CloseInspectScreen);

        CloseInspectScreen();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void OpenInspectScreen()
    {
        inspectScreen.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        inspectOpen = true;
    }

    public void OpenInspectScreen(GameObject inspectTarget, HitDataStrings[] hits)
    {
        // Remove old model
        foreach (Transform child in modelTransform)
        {
            Destroy(child.gameObject);
        }
        this.inspectTarget = null;
        // Add model
        GameObject targetClone = Instantiate(inspectTarget, modelTransform);
        targetClone.transform.localPosition = Vector3.zero;
        targetClone.transform.rotation = Quaternion.identity;
        this.inspectTarget = targetClone;
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
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        inspectOpen = true;
    }

    public void CloseInspectScreen()
    {
        inspectScreen.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        inspectOpen = false;
    }

    void Update()
    {
        if (inputs == null)
            return;

        if (!inspectOpen)
            return;

        if (inputs.leftMouseHeld)
        {
            Vector2 delta = inputs.dragInUI - lastMousePosition;

            float deltaX = Mathf.Sign(delta.x) * Mathf.Pow(Mathf.Abs(delta.x), exponent);
            float deltaY = Mathf.Sign(delta.y) * Mathf.Pow(Mathf.Abs(delta.y), exponent);

            // Horizontal rotation around up vector
            modelTransform.Rotate(Vector3.up, -deltaX * rotationSpeed * sensitivity * Time.deltaTime, Space.World);

            // Vertical rotation around right vector
            modelTransform.Rotate(Vector3.right, deltaY * rotationSpeed * sensitivity * Time.deltaTime, Space.World);
        }
        if (inputs.inspect)
        {
            if (inspectTarget != null && inspectTarget.GetComponent<ShaderSwitcher>() != null)
            {
                inspectTarget.GetComponent<ShaderSwitcher>().ToggleShader();
            }
            inputs.inspect = false;
        }

        lastMousePosition = inputs.dragInUI;
    }

    public bool IsInspectOpen()
    {
        return inspectOpen;
    }

    public void SetPlayerInput(PlayerInputs inputs) => this.inputs = inputs;
}