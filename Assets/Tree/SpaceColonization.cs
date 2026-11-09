using System.Collections.Generic;
using UnityEngine;

public static class SpaceColonization
{
    public static int GrowTrunk(TreeSettings settings, TreeVariant variant, Vector3 groundNormal, System.Random random, TreeSkeleton skeleton)
    {
        List<TreeNode> nodes = skeleton.Nodes;
        nodes.Clear();

        Vector3 direction = (variant.TrunkTilt * groundNormal).normalized;
        Vector3 position = Vector3.zero;
        int step = 0;

        AddNode(nodes, position, -1, step);

        while (position.y < variant.TrunkHeight && nodes.Count < settings.MaxNodes)
        {
            step++;
            direction += TreeRandom.InUnitSphere(random) * settings.TrunkWobble * 0.5f;
            direction = Vector3.Slerp(direction.normalized, Vector3.up, settings.TrunkStraighten).normalized;
            position += direction * settings.SegmentLength;
            AddNode(nodes, position, nodes.Count - 1, step);
        }

        return step;
    }

    public static void SampleCrown(TreeSettings settings, TreeVariant variant, System.Random random, TreeSkeleton skeleton)
    {
        Vector3 trunkTip = skeleton.Nodes[skeleton.TrunkSteps].Position;
        Vector3 scale = new Vector3(variant.CrownRadius, variant.CrownRadius * variant.CrownHeightRatio, variant.CrownRadius);
        Vector3 center = trunkTip + Vector3.up * scale.y * 0.7f + variant.CrownOffset;

        skeleton.Attractors.Clear();
        skeleton.CrownCenter = center;
        skeleton.CrownRadius = Mathf.Max(scale.x, scale.y);

        int lobeCount = settings.CrownLobes;
        Vector3[] lobeCenters = new Vector3[lobeCount];
        Vector3[] lobeScales = new Vector3[lobeCount];

        for (int l = 0; l < lobeCount; l++)
        {
            lobeCenters[l] = center + Vector3.Scale(TreeRandom.InUnitSphere(random) * 0.5f, scale);
            lobeScales[l] = scale * TreeRandom.Range(random, 0.45f, 0.75f);
        }

        for (int i = 0; i < settings.AttractorCount; i++)
        {
            if (lobeCount <= 1 || TreeRandom.Value(random) < 0.35f)
            {
                skeleton.Attractors.Add(center + Vector3.Scale(TreeRandom.InUnitSphere(random), scale));
            }
            else
            {
                int l = random.Next(lobeCount);
                skeleton.Attractors.Add(lobeCenters[l] + Vector3.Scale(TreeRandom.InUnitSphere(random), lobeScales[l]));
            }
        }
    }

    public static void Colonize(TreeSettings settings, TreeVariant variant, System.Random random, TreeSkeleton skeleton)
    {
        List<TreeNode> nodes = skeleton.Nodes;
        List<Vector3> attractors = skeleton.Attractors;

        float killDistance = Mathf.Max(settings.KillDistance, settings.SegmentLength * 1.5f);
        float killSqr = killDistance * killDistance;
        float influenceSqr = settings.InfluenceRadius * settings.InfluenceRadius;

        int attractorCount = attractors.Count;
        int[] closest = new int[attractorCount];
        float[] closestSqr = new float[attractorCount];
        bool[] alive = new bool[attractorCount];

        for (int a = 0; a < attractorCount; a++)
        {
            closest[a] = -1;
            closestSqr[a] = float.MaxValue;
            alive[a] = true;
        }

        List<Vector3> pulls = new List<Vector3>();
        int firstNewNode = 0;
        int step = skeleton.LastStep;

        for (int iteration = 0; iteration < settings.MaxIterations; iteration++)
        {
            int nodeCount = nodes.Count;

            int aliveCount = 0;
            for (int a = 0; a < attractorCount; a++)
            {
                if (!alive[a])
                {
                    continue;
                }

                for (int n = firstNewNode; n < nodeCount; n++)
                {
                    float sqr = (attractors[a] - nodes[n].Position).sqrMagnitude;
                    if (sqr < closestSqr[a])
                    {
                        closestSqr[a] = sqr;
                        closest[a] = n;
                    }
                }

                if (closestSqr[a] < killSqr)
                {
                    alive[a] = false;
                    continue;
                }

                aliveCount++;
            }

            firstNewNode = nodeCount;

            if (aliveCount == 0)
            {
                break;
            }

            while (pulls.Count < nodeCount)
            {
                pulls.Add(Vector3.zero);
            }

            for (int n = 0; n < nodeCount; n++)
            {
                pulls[n] = Vector3.zero;
            }

            for (int a = 0; a < attractorCount; a++)
            {
                if (alive[a] && closestSqr[a] < influenceSqr)
                {
                    int n = closest[a];
                    pulls[n] += (attractors[a] - nodes[n].Position).normalized;
                }
            }

            step++;

            for (int n = 0; n < nodeCount; n++)
            {
                if (pulls[n] == Vector3.zero)
                {
                    continue;
                }

                Vector3 direction = pulls[n].normalized + Vector3.up * variant.Tropism + TreeRandom.InUnitSphere(random) * settings.BranchJitter;
                if (direction.sqrMagnitude < 0.0001f)
                {
                    continue;
                }

                Vector3 position = nodes[n].Position + direction.normalized * settings.SegmentLength;
                if (HasChildNear(nodes, n, position, settings.SegmentLength * 0.1f))
                {
                    continue;
                }

                AddNode(nodes, position, n, step);
            }

            if (nodes.Count == nodeCount || nodes.Count >= settings.MaxNodes)
            {
                break;
            }
        }
    }

    public static void AddNode(List<TreeNode> nodes, Vector3 position, int parent, int birthStep)
    {
        nodes.Add(new TreeNode(position, parent, birthStep));

        if (parent >= 0)
        {
            nodes[parent].Children.Add(nodes.Count - 1);
        }
    }

    private static bool HasChildNear(List<TreeNode> nodes, int parent, Vector3 position, float distance)
    {
        List<int> children = nodes[parent].Children;
        for (int i = 0; i < children.Count; i++)
        {
            if ((nodes[children[i]].Position - position).sqrMagnitude < distance * distance)
            {
                return true;
            }
        }

        return false;
    }
}
