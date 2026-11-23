using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter))]
public class OceanSphere : MonoBehaviour
{
    private static readonly Vector3[] Faces =
    {
        Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back
    };

    [SerializeField] private Planet _planet;
    [SerializeField] private float _seaLevel = 0f;
    [SerializeField, Range(8, 256)] private int _resolution = 96;

    private Mesh _mesh;

    [ContextMenu("Regenerate")]
    public void Generate()
    {
        if (_planet == null)
        {
            _planet = GetComponentInParent<Planet>();
        }

        float scale = Mathf.Max(transform.lossyScale.x, 0.0001f);
        float worldRadius = scale * 0.5f;
        if (_planet != null)
        {
            worldRadius = _planet.Radius + _seaLevel;
        }

        float localRadius = worldRadius / scale;
        int n = _resolution;
        int side = n + 1;

        List<Vector3> vertices = new List<Vector3>(6 * side * side);
        List<Vector3> normals = new List<Vector3>(6 * side * side);
        List<int> triangles = new List<int>(6 * n * n * 6);

        for (int f = 0; f < Faces.Length; f++)
        {
            Vector3 normal = Faces[f];
            Vector3 axisA = new Vector3(normal.y, normal.z, normal.x);
            Vector3 axisB = Vector3.Cross(normal, axisA);
            int first = vertices.Count;

            for (int y = 0; y < side; y++)
            {
                for (int x = 0; x < side; x++)
                {
                    Vector3 cube = normal + (2f * x / n - 1f) * axisA + (2f * y / n - 1f) * axisB;
                    Vector3 direction = cube.normalized;

                    vertices.Add(direction * localRadius);
                    normals.Add(direction);
                }
            }

            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    int i = first + x + y * side;
                    triangles.Add(i);
                    triangles.Add(i + side + 1);
                    triangles.Add(i + side);

                    triangles.Add(i);
                    triangles.Add(i + 1);
                    triangles.Add(i + side + 1);
                }
            }
        }

        if (_mesh == null)
        {
            _mesh = new Mesh();
            _mesh.name = "Ocean";
            _mesh.hideFlags = HideFlags.DontSave;
        }

        _mesh.Clear();
        if (vertices.Count > 65535)
        {
            _mesh.indexFormat = IndexFormat.UInt32;
        }
        else
        {
            _mesh.indexFormat = IndexFormat.UInt16;
        }

        _mesh.SetVertices(vertices);
        _mesh.SetNormals(normals);
        _mesh.SetTriangles(triangles, 0);
        _mesh.bounds = new Bounds(Vector3.zero, Vector3.one * localRadius * 2.4f);

        GetComponent<MeshFilter>().sharedMesh = _mesh;
    }

    private void OnEnable()
    {
        Generate();
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall += DelayedGenerate;
#endif
    }

    private void DelayedGenerate()
    {
        if (this != null && isActiveAndEnabled)
        {
            Generate();
        }
    }

    private void OnDisable()
    {
        if (_mesh == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(_mesh);
        }
        else
        {
            DestroyImmediate(_mesh);
        }

        _mesh = null;
    }
}
