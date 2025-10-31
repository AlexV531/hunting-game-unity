using UnityEngine;

public class InspectRoom : MonoBehaviour
{
    public Transform inspectPoint;
    public Transform modelTransform;
    public float rotationSpeed = 10f;
    public float sensitivity = 1f;
    public float exponent = 1.5f;
    private Vector2 lastMousePosition;
    private GameObject inspectTarget;

    public GameObject ReplaceInspectTarget(GameObject inspectTarget)
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

        return targetClone;
    }

    public void UpdateModelRotation(PlayerInputs inputs)
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
        if (inputs.inspect)
        {
            Debug.Log("Hello 3");
            if (inspectTarget != null && inspectTarget.GetComponent<ShaderSwitcher>() != null)
            {
                inspectTarget.GetComponent<ShaderSwitcher>().ToggleShader();
            }
            inputs.inspect = false;
        }

        lastMousePosition = inputs.dragInUI;
    }
}
