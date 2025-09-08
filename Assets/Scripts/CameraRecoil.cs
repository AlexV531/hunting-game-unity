using UnityEngine;

public class CameraRecoil : MonoBehaviour
{
    public Transform recoilOffset; // assign RecoilOffsetTransform in Inspector
    public float recoilSpeed = 20f;
    public float recoilRecoverySpeed = 5f;

    private Vector2 currentRecoil;
    private Vector2 targetRecoil;

    void Update()
    {
        // Smoothly move current recoil toward target
        currentRecoil = Vector2.Lerp(currentRecoil, targetRecoil, Time.deltaTime * recoilSpeed);

        // Apply recoil as local rotation to the child transform
        if (recoilOffset != null)
            recoilOffset.localRotation = Quaternion.Euler(-currentRecoil.x, currentRecoil.y, 0f);

        // Slowly decay the target recoil
        targetRecoil = Vector2.Lerp(targetRecoil, Vector2.zero, Time.deltaTime * recoilRecoverySpeed);
    }

    public void AddRecoil(float pitch, float yaw)
    {
        targetRecoil += new Vector2(pitch, yaw);
    }
}
