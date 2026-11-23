using UnityEngine;

[System.Serializable]
public class TreeSettings
{
    [Header("Trunk")]
    [Range(0.2f, 20f)] public float TrunkHeight = 2f;
    [Range(0f, 1f)] public float TrunkStraighten = 0.15f;
    [Range(0f, 3f)] public float Flare = 1.6f;
    [Range(0f, 3f)] public float FlareHeight = 0.7f;

    [Header("Crown")]
    [Range(0.2f, 20f)] public float CrownRadius = 2f;
    [Range(0.2f, 3f)] public float CrownHeightRatio = 0.8f;
    [Range(1, 6)] public int CrownLobes = 3;
    [Range(10, 3000)] public int AttractorCount = 800;

    [Header("Space colonization")]
    [Range(0.02f, 2f)] public float SegmentLength = 0.15f;
    [Range(0.1f, 10f)] public float InfluenceRadius = 0.8f;
    [Range(0.05f, 5f)] public float KillDistance = 0.25f;
    [Range(-1f, 1f)] public float Tropism = 0.2f;
    [Range(0f, 1f)] public float Smoothing = 0.5f;
    [Range(10, 1000)] public int MaxIterations = 300;
    [Range(100, 20000)] public int MaxNodes = 5000;

    [Header("Variation")]
    [Range(0f, 1f)] public float Variation = 0.35f;
    [Range(0f, 30f)] public float TrunkLean = 10f;
    [Range(0f, 1f)] public float TrunkWobble = 0.15f;
    [Range(0f, 1f)] public float BranchJitter = 0.2f;

    [Header("Features")]
    [Range(0f, 1f)] public float ForkChance = 0.2f;
    [Range(0f, 1f)] public float LowBranchChance = 0.35f;
    [Range(0f, 1f)] public float HoleChance = 0.3f;

    [Header("Twigs")]
    [Range(0f, 1f)] public float TwigChance = 0.6f;
    [Range(1, 4)] public int TwigSegments = 2;
    [Range(0.2f, 1f)] public float TwigLength = 0.5f;
    [Range(0.005f, 0.2f)] public float TwigMaxRadius = 0.05f;
    [Range(3, 8)] public int TwigRadialSegments = 4;

    [Header("Thickness")]
    [Range(0.002f, 0.2f)] public float TipRadius = 0.015f;
    [Range(1.5f, 3f)] public float PipeExponent = 2.2f;

    [Header("Roots")]
    [Range(0, 12)] public int RootCount = 5;
    [Range(0.2f, 5f)] public float RootLength = 1.8f;
    [Range(0f, 2f)] public float RootDepth = 0.35f;
    [Range(0.1f, 1f)] public float RootRadius = 0.45f;
    [Range(0f, 1f)] public float RootWaves = 0.5f;
    [Range(0f, 1f)] public float RootWobble = 0.3f;

    [Header("Leaves")]
    [Range(0, 6)] public int LeavesPerNode = 2;
    [Range(0.02f, 2f)] public float LeafSize = 0.4f;
    [Range(0.005f, 0.2f)] public float LeafMaxRadius = 0.04f;
    [Range(0f, 2f)] public float LeafUpward = 0.5f;
    [Range(0f, 2f)] public float LeafOutward = 0.5f;
    [Range(0f, 1f)] public float LeafSpread = 0.6f;
    [Range(0f, 0.5f)] public float LeafFold = 0.2f;
    public bool LeafCross = true;

    [Header("Growth")]
    [Range(0.05f, 1f)] public float SaplingScale = 0.2f;
    [Range(1, 20)] public int LeafGrowSteps = 4;

    [Header("Colors")]
    public Color BarkDark = new Color(0.22f, 0.15f, 0.1f);
    public Color BarkLight = new Color(0.4f, 0.31f, 0.23f);
    public Color LeafColor = new Color(0.2f, 0.5f, 0.15f);
    public Color LeafColorVariation = new Color(0.45f, 0.62f, 0.2f);
    [Range(0f, 1f)] public float ColorNoise = 0.15f;
    [Range(0f, 1f)] public float Occlusion = 0.4f;
    [Range(0f, 0.5f)] public float SunGradient = 0.15f;

    [Header("Mesh")]
    [Range(3, 16)] public int RadialSegments = 10;
    [Range(1f, 3f)] public float BranchCollar = 1.8f;

    [Header("Performance")]
    [Range(0.01f, 10f)] public float FullDetailScale = 1f;
    [Range(0.1f, 1f)] public float MinDetail = 0.25f;
    public bool LeafShadows = false;

    public TreeSettings WithDetail(float detail)
    {
        TreeSettings scaled = (TreeSettings)MemberwiseClone();
        if (detail >= 1f)
        {
            return scaled;
        }

        float coarse = Mathf.Lerp(1.6f, 1f, detail);
        scaled.SegmentLength = SegmentLength * coarse;
        scaled.KillDistance = KillDistance * coarse;
        scaled.AttractorCount = Mathf.Max(60, Mathf.RoundToInt(AttractorCount * detail));
        scaled.TwigChance = TwigChance * detail;

        scaled.LeafCross = LeafCross && detail >= 0.6f;
        scaled.LeavesPerNode = Mathf.Max(Mathf.Min(LeavesPerNode, 1), Mathf.RoundToInt(LeavesPerNode * detail));

        int cardsBefore = LeavesPerNode * Cards(LeafCross);
        int cardsAfter = scaled.LeavesPerNode * Cards(scaled.LeafCross);
        if (cardsAfter > 0)
        {
            float ratio = (float)cardsBefore / cardsAfter;
            scaled.LeafSize = LeafSize * Mathf.Sqrt(ratio) * (1f + (1f - detail) * 0.3f);
        }

        scaled.TwigRadialSegments = Mathf.Max(3, Mathf.RoundToInt(TwigRadialSegments * Mathf.Lerp(0.75f, 1f, detail)));
        scaled.RadialSegments = Mathf.Max(scaled.TwigRadialSegments, Mathf.RoundToInt(RadialSegments * Mathf.Lerp(0.5f, 1f, detail)));

        return scaled;
    }

    private static int Cards(bool cross)
    {
        if (cross)
        {
            return 2;
        }

        return 1;
    }
}
