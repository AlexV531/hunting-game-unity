using UnityEngine;

public class InspectRoom : MonoBehaviour
{
    public Transform inspectPoint;
    public Transform modelTransform;
    public float rotationSpeed = 1f;
    public float sensitivity = 1f;
    public float exponent = 1.5f;
    private Vector2 lastMousePosition;
    private GameObject inspectTarget;
    private bool wasMouseHeldLastFrame = false;

    public GameObject ReplaceInspectTarget(GameObject inspectTarget, Vector3 inspectPointOffset)
    {
        // Remove old model
        foreach (Transform child in modelTransform)
        {
            Destroy(child.gameObject);
        }
        this.inspectTarget = null;
        inspectPoint.transform.localPosition = Vector3.zero;
        // Add model
        inspectPoint.transform.localPosition = inspectPointOffset;
        GameObject targetClone = Instantiate(inspectTarget, modelTransform);
        targetClone.transform.localPosition = Vector3.zero;
        targetClone.transform.rotation = Quaternion.identity;
        this.inspectTarget = targetClone;
        

        return targetClone;
    }

    public void UpdateModelRotation(PlayerInputs inputs)
    {
        if (inputs == null)
            return;

        if (inputs.leftMouseHeld)
        {
            // Skip the first frame after clicking, to avoid large deltas
            if (!wasMouseHeldLastFrame)
            {
                lastMousePosition = inputs.dragInUI;
                wasMouseHeldLastFrame = true;
                return;
            }

            // Compute delta and ignore absurd spikes (e.g., from tabbing out)
            Vector2 delta = inputs.dragInUI - lastMousePosition;
            if (delta.sqrMagnitude > 10000f) // roughly >100px jump
            {
                delta = Vector2.zero;
            }

            float deltaX = Mathf.Sign(delta.x) * Mathf.Pow(Mathf.Abs(delta.x), exponent);
            float deltaY = Mathf.Sign(delta.y) * Mathf.Pow(Mathf.Abs(delta.y), exponent);

            // Horizontal rotation around up vector
            modelTransform.Rotate(Vector3.up, -deltaX * rotationSpeed * sensitivity * Time.deltaTime, Space.World);

            // Vertical rotation around right vector
            modelTransform.Rotate(Vector3.right, deltaY * rotationSpeed * sensitivity * Time.deltaTime, Space.World);

            lastMousePosition = inputs.dragInUI;
        }
        else
        {
            wasMouseHeldLastFrame = false;
        }

        if (inputs.inspect)
        {
            Debug.Log("Hello 3");
            if (inspectTarget != null)
            {
                var shaderSwitcher = inspectTarget.GetComponent<ShaderSwitcher>();
                if (shaderSwitcher != null)
                {
                    shaderSwitcher.ToggleShader();
                }
            }
            inputs.inspect = false;
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            // Reset to avoid massive delta when returning to the window
            lastMousePosition = Vector2.zero;
            wasMouseHeldLastFrame = false;
        }
    }
}
