using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class ProceduralOctagonPlaneBig : MonoBehaviour
{
    [Header("Size")]
    [Min(0.01f)]
    public float sideLength = 2f;

    [Header("Shape")]
    public bool makeHole = false;

    [Min(0.01f)]
    public float ringWidth = 0.4f;

    private const int Sides = 8;
    private const string MeshName = "Procedural Octagon Plane";

    private Mesh generatedMesh;

    private void OnEnable()
    {
        GenerateMesh();
    }

    private void OnValidate()
    {
        GenerateMesh();
    }

    [ContextMenu("Generate Mesh")]
    public void GenerateMesh()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        MeshCollider meshCollider = GetComponent<MeshCollider>();

        if (meshFilter == null || meshCollider == null)
            return;

        if (generatedMesh == null || meshFilter.sharedMesh != generatedMesh)
        {
            generatedMesh = new Mesh();
            generatedMesh.name = MeshName + " " + GetInstanceID();
            meshFilter.sharedMesh = generatedMesh;
        }

        Mesh mesh = generatedMesh;
        mesh.Clear();

        float angleStep = Mathf.PI * 2f / Sides;

        // Radius needed so every outer side is exactly sideLength meters
        float outerRadius = sideLength / (2f * Mathf.Sin(Mathf.PI / Sides));

        Vector3[] vertices;
        Vector2[] uvs;
        List<int> triangles = new List<int>();

        if (!makeHole)
        {
            // Filled octagon plane
            vertices = new Vector3[Sides + 1];
            uvs = new Vector2[Sides + 1];

            vertices[0] = Vector3.zero;

            for (int i = 0; i < Sides; i++)
            {
                float angle = i * angleStep + Mathf.PI / Sides;
                vertices[i + 1] = new Vector3(
                    Mathf.Cos(angle) * outerRadius,
                    0f,
                    Mathf.Sin(angle) * outerRadius
                );
            }

            for (int i = 0; i < Sides; i++)
            {
                int current = i + 1;
                int next = ((i + 1) % Sides) + 1;

                // Triangle order faces upward
                triangles.Add(0);
                triangles.Add(next);
                triangles.Add(current);
            }
        }
        else
        {
            // Hollow octagon ring, like your screenshot
            vertices = new Vector3[Sides * 2];
            uvs = new Vector2[Sides * 2];

            float outerApothem = outerRadius * Mathf.Cos(Mathf.PI / Sides);
            float maxRingWidth = Mathf.Max(0.01f, outerApothem - 0.01f);
            float safeRingWidth = Mathf.Clamp(ringWidth, 0.01f, maxRingWidth);

            float innerApothem = outerApothem - safeRingWidth;
            float innerRadius = innerApothem / Mathf.Cos(Mathf.PI / Sides);

            for (int i = 0; i < Sides; i++)
            {
                float angle = i * angleStep + Mathf.PI / Sides;

                vertices[i] = new Vector3(
                    Mathf.Cos(angle) * outerRadius,
                    0f,
                    Mathf.Sin(angle) * outerRadius
                );

                vertices[i + Sides] = new Vector3(
                    Mathf.Cos(angle) * innerRadius,
                    0f,
                    Mathf.Sin(angle) * innerRadius
                );
            }

            for (int i = 0; i < Sides; i++)
            {
                int next = (i + 1) % Sides;

                int outerCurrent = i;
                int outerNext = next;

                int innerCurrent = i + Sides;
                int innerNext = next + Sides;

                triangles.Add(outerCurrent);
                triangles.Add(innerNext);
                triangles.Add(outerNext);

                triangles.Add(outerCurrent);
                triangles.Add(innerCurrent);
                triangles.Add(innerNext);
            }
        }

        for (int i = 0; i < vertices.Length; i++)
        {
            uvs[i] = new Vector2(
                vertices[i].x / (outerRadius * 2f) + 0.5f,
                vertices[i].z / (outerRadius * 2f) + 0.5f
            );
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = mesh;
        meshCollider.convex = false;
    }
}
