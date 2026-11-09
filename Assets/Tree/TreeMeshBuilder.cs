using UnityEngine;

public static class TreeMeshBuilder
{
    public static void Build(Mesh mesh, TreeSkeleton skeleton, GrowthState state, TreeSettings settings, MeshData data)
    {
        TreeShading shading = new TreeShading(settings, skeleton, state);

        data.Clear();
        TubeMeshBuilder.Build(data, skeleton, state, settings, shading);
        LeafMeshBuilder.Build(data, skeleton, state, settings, shading);
        data.Apply(mesh);
    }
}
