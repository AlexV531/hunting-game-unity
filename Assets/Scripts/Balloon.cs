using UnityEngine;
using Unity.Netcode;

public class Balloon : NetworkBehaviour
{
    public float floatHeight = 50f;
    public float floatSpeed = 5f;
    public float moveSpeed = 2f;

    [SerializeField] private Transform tetherPoint;
    private Vector3 targetPosition;
    private bool isInitialized = false;
    private bool targetReached = false;
    private AttachHandler attachHandler;

    public void Initialize(Vector3 targetPosition, BalloonAttach attachTarget)
    {
        this.targetPosition = targetPosition;
        attachHandler = new AttachHandler(new Transform[] { tetherPoint });

        if (attachTarget != null)
            attachHandler.AttachTarget(attachTarget);

        isInitialized = true;
    }

    private void Update()
    {
        if (!IsSpawned) return;

        // Only simulate on server; clients receive updates via NetworkTransform
        if (IsServer && isInitialized)
        {
            Vector3 currentPosition = transform.position;

            // Compute upward floating
            if (currentPosition.y < floatHeight)
            {
                currentPosition.y = Mathf.MoveTowards(currentPosition.y, floatHeight, floatSpeed * Time.deltaTime);
            }

            if (targetReached)
                return;

            // Move toward target horizontally
            Vector3 horizontalTarget = new Vector3(targetPosition.x, currentPosition.y, targetPosition.z);
            currentPosition = Vector3.MoveTowards(currentPosition, horizontalTarget, moveSpeed * Time.deltaTime);

            transform.position = currentPosition;

            // Check if balloon has reached target
            if (transform.position.x - targetPosition.x <= 0.01 && transform.position.z - targetPosition.z <= 0.01)
            {
                attachHandler.ReleaseAll();
                targetReached = true;
            }
        }
    }

    public Transform GetTetherPoint()
    {
        return tetherPoint;
    }
}
