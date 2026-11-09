using UnityEngine;

public static class TreeHole
{
    public static bool Frame(TreeSkeleton skeleton, GrowthState state, out Vector3 center, out Vector3 axis, out Vector3 azimuth, out float radius)
    {
        center = Vector3.zero;
        axis = Vector3.up;
        azimuth = Vector3.forward;
        radius = 0f;

        if (!skeleton.HasHole || skeleton.HoleNode >= skeleton.Nodes.Count)
        {
            return false;
        }

        int n = skeleton.HoleNode;
        if (state.Progress[n] <= 0f || state.Radii[n] <= 0f)
        {
            return false;
        }

        TreeNode node = skeleton.Nodes[n];
        center = state.Positions[n];
        radius = state.Radii[n];

        if (node.Parent >= 0)
        {
            Vector3 delta = center - state.Positions[node.Parent];
            if (delta.sqrMagnitude > 0.000001f)
            {
                axis = delta.normalized;
            }
        }

        azimuth = Vector3.ProjectOnPlane(skeleton.HoleAzimuth, axis);
        if (azimuth.sqrMagnitude < 0.0001f)
        {
            azimuth = Vector3.Cross(axis, Vector3.right);
        }

        azimuth.Normalize();
        return true;
    }
}
