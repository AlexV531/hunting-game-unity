using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

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
        // only the server should process noises
        if (!NetworkManager.Singleton.IsServer)
            return;

        foreach (var listener in listeners)
        {
            var listenerTransform = (listener as MonoBehaviour)?.transform;
            if (listenerTransform == null)
                continue;

            float distance = Vector3.Distance(listenerTransform.position, noiseEvent.position);
            float perceivedVolume = noiseEvent.loudness / (distance + 1f);

            if (perceivedVolume > listener.HearingThreshold)
            {
                listener.OnNoiseHeard(noiseEvent, perceivedVolume);
            }
        }
    }
}