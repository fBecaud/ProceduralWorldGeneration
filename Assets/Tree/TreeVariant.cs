using UnityEngine;

public class TreeVariant
{
    public float TrunkHeight;
    public float CrownRadius;
    public float CrownHeightRatio;
    public Vector3 CrownOffset;
    public Quaternion TrunkTilt;
    public float Tropism;
    public float LeafShade;

    public static TreeVariant Create(TreeSettings settings, System.Random random)
    {
        float v = settings.Variation;
        TreeVariant variant = new TreeVariant();

        variant.TrunkHeight = settings.TrunkHeight * TreeRandom.Range(random, 1f - v * 0.5f, 1f + v * 0.5f);
        variant.CrownRadius = settings.CrownRadius * TreeRandom.Range(random, 1f - v * 0.4f, 1f + v * 0.4f);
        variant.CrownHeightRatio = settings.CrownHeightRatio * TreeRandom.Range(random, 1f - v * 0.4f, 1f + v * 0.4f);

        Vector3 offset = TreeRandom.InUnitSphere(random);
        offset.y = 0f;
        variant.CrownOffset = offset * variant.CrownRadius * v * 0.5f;

        Vector3 axis = Vector3.Cross(Vector3.up, TreeRandom.OnUnitSphere(random));
        if (axis.sqrMagnitude < 0.0001f)
        {
            axis = Vector3.right;
        }

        variant.TrunkTilt = Quaternion.AngleAxis(TreeRandom.Range(random, 0f, settings.TrunkLean), axis.normalized);
        variant.Tropism = settings.Tropism + TreeRandom.Range(random, -1f, 1f) * v * 0.3f;
        variant.LeafShade = TreeRandom.Value(random);

        return variant;
    }
}
