using System.Collections.Generic;
using UnityEngine;

public class NoiseEvent
{
    public Vector3 position;
    public float loudness;
    public string name;

    public NoiseEvent(Vector3 pos, float loud, string name)
    {
        position = pos;
        loudness = loud;
        this.name = name;
    }
}

public interface INoiseListener
{
    float HearingThreshold { get; }
    void OnNoiseHeard(NoiseEvent noiseEvent, float perceivedVolume);
}

public class NoiseManager : MonoBehaviour
{
    private static readonly List<INoiseListener> listeners = new List<INoiseListener>();

    public static void RegisterListener(INoiseListener listener)
    {
        if (!listeners.Contains(listener))
            listeners.Add(listener);
    }

    public static void UnregisterListener(INoiseListener listener)
    {
        if (listeners.Contains(listener))
            listeners.Remove(listener);
    }

    public static void EmitNoise(NoiseEvent noiseEvent)
    {
        foreach (var listener in listeners)
        {
            // Assuming listener is also a MonoBehaviour with a transform
            var listenerTransform = (listener as MonoBehaviour)?.transform;
            if (listenerTransform == null)
                continue;

            float distance = Vector3.Distance(listenerTransform.position, noiseEvent.position);
            float perceivedVolume = noiseEvent.loudness / (distance + 1f);

            // Optional: Raycast check for occlusion
            // if (Physics.Raycast(noiseEvent.position, (listenerTransform.position - noiseEvent.position).normalized, out RaycastHit hit, distance)) { ... }

            if (perceivedVolume > listener.HearingThreshold)
            {
                listener.OnNoiseHeard(noiseEvent, perceivedVolume);
            }
        }
    }
}