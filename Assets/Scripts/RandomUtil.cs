using UnityEngine;

public static class RandomUtil
{
    public static bool RandomBool(System.Random rng)
    {
        return rng.NextDouble() < 0.5;
    }

    public static int RandomRangeInt(System.Random rng, int min, int max)
    {
        return rng.Next(min, max); // upper bound exclusive
    }

    public static float RandomRangeFloat(System.Random rng, float min, float max)
    {
        return (float)(rng.NextDouble() * (max - min) + min);
    }

    public static Vector3 RandomInsideUnitSphere(System.Random rng)
    {
        // Uniformly random vector inside a unit sphere
        float u = (float)rng.NextDouble();
        float v = (float)rng.NextDouble();
        float theta = 2f * Mathf.PI * u;
        float phi = Mathf.Acos(2f * v - 1f);
        float r = Mathf.Pow((float)rng.NextDouble(), 1f / 3f);
        float sinPhi = Mathf.Sin(phi);

        return new Vector3(
            r * sinPhi * Mathf.Cos(theta),
            r * sinPhi * Mathf.Sin(theta),
            r * Mathf.Cos(phi)
        );
    }
}