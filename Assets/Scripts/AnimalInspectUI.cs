using UnityEngine;
using UnityEngine.UI;

public class AnimalInspectUI : MonoBehaviour
{
    public GameObject inspectScreen;
    public Transform modelTransform;
    public float rotationSpeed = 1f;
    public float sensitivity = 1f;
    public float exponent = 1.5f;
    public Button closeButton;
    private bool inspectOpen = true;
    private Vector2 lastMousePosition;
    private PlayerInputs inputs;
    
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

        lastMousePosition = inputs.dragInUI;
    }

    public bool IsInspectOpen()
    {
        return inspectOpen;
    }

    public void SetPlayerInput(PlayerInputs inputs) => this.inputs = inputs;
}