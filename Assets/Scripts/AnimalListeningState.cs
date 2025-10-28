using UnityEngine;

public class AnimalListeningState : AnimalBaseState //: AnimalTimerState
{
    public float turnSpeed = 180f;
    private Animal actor;
    private AudioSource audio;
    public override AlertnessLevel Alertness => AlertnessLevel.Alert;

    public override void EnterState(AnimalStateManager animal)
    {
        Debug.Log("Listening state entered.");
        actor = animal.GetComponent<Animal>();
        audio = animal.GetComponent<AudioSource>();
        if (audio != null)
        {
            // if (Random.value < 0.3f)
            // {
                audio.Play();
            // }
        }
    }

    public override void UpdateState(AnimalStateManager animal)
    {
        // If there’s no recent sound, go to next state
        if (actor == null || actor.animalAI.MostRecentSoundPosition == null || actor.animalAI.soundHeard <= 0)
            animal.ChangeState(nextState);

        Vector3 soundPos = actor.animalAI.MostRecentSoundPosition;
        Vector3 direction = (soundPos - actor.transform.position).normalized;

        if (direction.magnitude == 0f)
            return;

        // Project direction onto the XZ plane
        Vector3 flatDir = new Vector3(direction.x, 0f, direction.z).normalized;
        if (flatDir.magnitude == 0f)
            return;

        // Determine current facing direction (Unity forward is +Z)
        Vector3 currentFacing = actor.transform.forward;
        currentFacing.y = 0f;
        currentFacing.Normalize();

        // Calculate signed angle between current facing and target
        float angleToTarget = Vector3.SignedAngle(currentFacing, flatDir, Vector3.up);

        // Clamp the rotation step based on turnSpeed and deltaTime
        float step = Mathf.Clamp(angleToTarget, -turnSpeed * Time.deltaTime, turnSpeed * Time.deltaTime);

        // Rotate the actor around the Y axis
        // Debug.Log("rotating to face sound");
        actor.transform.Rotate(Vector3.up, step);
    }

    public override void ExitState(AnimalStateManager animal)
    {

    }
}