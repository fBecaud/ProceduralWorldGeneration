using System.Collections.Generic;
using UnityEngine;

public static class GrassMesh
{
    public static Mesh Create(int blades, int segments, int seed)
    {
        System.Random random = new System.Random(seed);
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<Vector4> tangents = new List<Vector4>();
        List<int> triangles = new List<int>();

        for (int b = 0; b < blades; b++)
        {
            float angle = ((float)b / blades + (float)random.NextDouble() * 0.2f) * Mathf.PI * 2f;
            Vector3 side = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            Vector3 forward = new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle));
            Vector3 center = forward * (0.3f + (float)random.NextDouble() * 0.5f);
            float height = 0.7f + (float)random.NextDouble() * 0.3f;
            int first = vertices.Count;

            for (int s = 0; s < segments; s++)
            {
                float t = (float)s / segments;
                float halfWidth = 0.5f * (1f - t);
                float y = t * height;

                vertices.Add(center - side * halfWidth + Vector3.up * y);
                vertices.Add(center + side * halfWidth + Vector3.up * y);
                normals.Add(forward);
                normals.Add(forward);
                uvs.Add(new Vector2(0f, y));
                uvs.Add(new Vector2(1f, y));
                tangents.Add(new Vector4(side.x, side.y, side.z, -halfWidth));
                tangents.Add(new Vector4(side.x, side.y, side.z, halfWidth));
            }

            vertices.Add(center + Vector3.up * height);
            normals.Add(forward);
            uvs.Add(new Vector2(0.5f, height));
            tangents.Add(new Vector4(side.x, side.y, side.z, 0f));

            for (int s = 0; s < segments - 1; s++)
            {
                int left = first + s * 2;
                triangles.Add(left);
                triangles.Add(left + 1);
                triangles.Add(left + 2);

                triangles.Add(left + 1);
                triangles.Add(left + 3);
                triangles.Add(left + 2);
            }

            int lastLeft = first + (segments - 1) * 2;
            triangles.Add(lastLeft);
            triangles.Add(lastLeft + 1);
            triangles.Add(vertices.Count - 1);
        }

        Mesh mesh = new Mesh();
        mesh.name = "Grass Clump";
        mesh.hideFlags = HideFlags.DontSave;
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTangents(tangents);
        mesh.SetTriangles(triangles, 0);
        mesh.bounds = new Bounds(Vector3.up * 0.5f, Vector3.one * 2f);
        return mesh;
    }
}
