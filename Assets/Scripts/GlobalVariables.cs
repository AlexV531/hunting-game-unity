using UnityEngine;

public class GlobalVariables : MonoBehaviour
{
    public static Vector3 debugTarget = Vector3.zero;
    public static Vector3 mapMin = new Vector3(0f, -100f, 0f);
    public static Vector3 mapMax = new Vector3(2000f, 500f, 2000f);
    public static Vector3 balloonTargetPosition = new Vector3(75f, 0f, 120f);
    public static float cameraFOV = 60f;
}
