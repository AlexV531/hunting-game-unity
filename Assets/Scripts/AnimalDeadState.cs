using System;
using UnityEngine;
using UnityEngine.AI;

public class AnimalDeadState : AnimalBaseState
{
    private Vector3 velocity = Vector3.zero;
    private float gravity = 9.81f;
    private bool isGrounded = false;
    private Vector3 lastPosition;
    private LayerMask groundLayers;
    private BalloonAttach actorAttach;
    public override AlertnessLevel Alertness => AlertnessLevel.Dead;

    public override void EnterState(AnimalStateManager animal)
    {
        Debug.Log("Dead state entered.");

        isGrounded = false;
        velocity = Vector3.zero;
        lastPosition = animal.transform.position;
        actorAttach = animal.GetComponent<Animal>().GetBalloonAttach();
        groundLayers = LayerMask.GetMask("Terrain", "Default");
    }

    public override void UpdateState(AnimalStateManager animal)
    {
        // Check if the corpse has been moved externally
        if (Vector3.Distance(animal.transform.position, lastPosition) > 0.1f)
        {
            isGrounded = false; // allow gravity to act again
        }

        if (isGrounded || actorAttach.IsAttached()) return;

        // Check for ground collision
        RaycastHit hit;
        if (Physics.Raycast(animal.transform.position, Vector3.down, out hit, Math.Abs(animal.GetComponent<Animal>().bottom.localPosition.y), groundLayers))
        {
            animal.transform.position = hit.point - animal.GetComponent<Animal>().bottom.localPosition / 2;
            velocity = Vector3.zero;
            isGrounded = true;
        }
        else // Apply gravity
        {
            velocity.y -= gravity * Time.deltaTime;
            animal.transform.position += velocity * Time.deltaTime;
        }

        lastPosition = animal.transform.position;
    }
}
