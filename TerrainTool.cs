using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class TerrainTool : EditorWindow
{
    private int terrainSize = 256; // Size of the terrain (e.g., 256x256 vertices for the full grid)
    private float terrainHeight = 50f; // Maximum height of the terrain
    private float noiseScale = 20f; // Scale of the Perlin noise
    private int detailLevel = 1; // Controls the actual resolution: higher value = lower resolution / fewer vertices

    private GameObject terrainGameObject;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;

    [MenuItem("Tools/Custom Terrain Tool")]
    public static void ShowWindow()
    {
        GetWindow<TerrainTool>("Custom Terrain Tool");
    }

    private void OnGUI()
    {
        GUILayout.Label("Terrain Generation Settings", EditorStyles.boldLabel);

        terrainSize = EditorGUILayout.IntField("Terrain Size (Vertices)", terrainSize);
        terrainHeight = EditorGUILayout.FloatField("Terrain Height", terrainHeight);
        noiseScale = EditorGUILayout.FloatField("Noise Scale", noiseScale);
        
        // Increased max value of the slider to 100
        detailLevel = EditorGUILayout.IntSlider("Detail Level (Higher = Less Detail)", detailLevel, 1, 100);

        if (GUILayout.Button("Generate Terrain"))
        {
            GenerateTerrain();
        }

        if (GUILayout.Button("Clear Terrain"))
        {
            ClearTerrain();
        }
    }

    void GenerateTerrain()
    {
        ClearTerrain(); // Clear any existing terrain first

        terrainGameObject = new GameObject("Custom Terrain");
        terrainGameObject.transform.position = Vector3.zero;

        meshFilter = terrainGameObject.AddComponent<MeshFilter>();
        meshRenderer = terrainGameObject.AddComponent<MeshRenderer>();
        meshCollider = terrainGameObject.AddComponent<MeshCollider>();

        // Set a default material (you might want a custom terrain shader)
        meshRenderer.sharedMaterial = new Material(Shader.Find("Standard")); 

        Mesh mesh = new Mesh();
        // Use UInt32 for index format if the mesh might exceed 65535 vertices
        // Max vertices for UInt16 is ~65k. For UInt32, it's over 4 billion.
        // A 256x256 terrain has (256+1)*(256+1) = 66049 vertices, so UInt32 is appropriate here.
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; 

        // Calculate the actual dimensions of the mesh based on terrainSize and detailLevel
        // A higher detailLevel means fewer vertices (larger step size between vertices)
        int width = terrainSize / detailLevel;
        int height = terrainSize / detailLevel;

        // Ensure we have at least a 1x1 grid for minimal terrain
        if (width < 1) width = 1;
        if (height < 1) height = 1;

        Vector3[] vertices = new Vector3[(width + 1) * (height + 1)];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[width * height * 6]; // 2 triangles per quad, 3 indices per triangle

        // --- Generate Vertices and UVs ---
        for (int z = 0; z <= height; z++)
        {
            for (int x = 0; x <= width; x++)
            {
                // Calculate height using Perlin noise
                float sampleX = (float)x / width * noiseScale;
                float sampleZ = (float)z / height * noiseScale;
                float y = Mathf.PerlinNoise(sampleX, sampleZ) * terrainHeight;

                // Position vertices based on the original terrainSize grid, scaled by detailLevel
                vertices[z * (width + 1) + x] = new Vector3(x * detailLevel, y, z * detailLevel);
                uvs[z * (width + 1) + x] = new Vector2((float)x / width, (float)z / height);
            }
        }

        // --- Generate Triangles ---
        int vert = 0;
        int tris = 0;
        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                // First triangle of the quad (bottom-left, top-left, bottom-right)
                triangles[tris + 0] = vert + 0;
                triangles[tris + 1] = vert + width + 1;
                triangles[tris + 2] = vert + 1;

                // Second triangle of the quad (bottom-right, top-left, top-right)
                triangles[tris + 3] = vert + 1;
                triangles[tris + 4] = vert + width + 1;
                triangles[tris + 5] = vert + width + 2;

                vert++; // Move to the next vertex in the current row
                tris += 6; // Move to the next set of 6 triangle indices
            }
            vert++; // Move to the first vertex of the next row (skip the last vertex of the current row)
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        
        mesh.RecalculateNormals(); // Important for correct lighting
        mesh.RecalculateBounds();  // Important for frustum culling and renderer visibility

        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh; // Add a collider for physics interactions
    }

    void ClearTerrain()
    {
        if (terrainGameObject != null)
        {
            // DestroyImmediate is used in Editor scripts to remove objects immediately
            // If you were doing this at runtime, you'd use Destroy()
            DestroyImmediate(terrainGameObject); 
            terrainGameObject = null;
        }
    }
}
