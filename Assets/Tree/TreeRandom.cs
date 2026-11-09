using UnityEngine;

public static class TreeRandom
{
    public static float Value(System.Random random)
    {
        return (float)random.NextDouble();
    }

    public static float Range(System.Random random, float min, float max)
    {
        return min + (max - min) * Value(random);
    }

    public static Vector3 InUnitSphere(System.Random random)
    {
        while (true)
        {
            Vector3 point = new Vector3(Range(random, -1f, 1f), Range(random, -1f, 1f), Range(random, -1f, 1f));
            if (point.sqrMagnitude <= 1f)
            {
                return point;
            }
        }
    }

    public static Vector3 OnUnitSphere(System.Random random)
    {
        while (true)
        {
            Vector3 point = InUnitSphere(random);
            if (point.sqrMagnitude > 0.01f)
            {
                return point.normalized;
            }
        }
    }
}
