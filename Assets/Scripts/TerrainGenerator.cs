using System.Collections;
using System.Collections.Generic;
using TriangleNet;
using UnityEngine;
using TriangleNet.Topology;
using TriangleNet.Geometry;
using TriangleNet.Meshing;
using UnityEditor;


public enum ColorSetting
{
    Random,
    HeightGradient
}


[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter), typeof(MeshCollider))]
public class TerrainGenerator : MonoBehaviour
{
    [Header("General Settings")] public Material material;
    
    [Range(1, 1000)] public int sizeX;
    [Range(1, 1000)] public int sizeY;

    [Header("Point Distribution")] 
    
    [Range(4, 6000)]
    public int pointDensity;
    public bool randomPoints;
    [Range(10,150)] public float minDistancePerPoint = 10;
    [Range(5,50)] public int rejectionSamples = 30;

    [Header("Colors")] 
    
    public ColorSetting colorSetting;
    public Gradient heightGradient;

    [Header("Simple Perlin Noise")] 
    
    [Range(1f, 3000f)] public float heightScale = 50f;
    [Range(5f, 300f)] public float scale = 34;
    [Range(0.001f, 1.00f)] public float dampening = 0.21f;

    [Header("Layered Noise")] [Range(1, 15)]
    public int octaves = 1;

    [Range(0f, 1f)] public float persistence = 0.1f;
    [Range(1f, 10f)] public float lacunarity = 1.5f;
    public Vector2 offset;
    
    [HideInInspector] public int seed;
    
    private List<Vector2> poissonPoints = new List<Vector2>();
    private Polygon polygon;
    private TriangleNet.Mesh mesh;
    private UnityEngine.Mesh terrainMesh;
    private List<float> heights = new List<float>();

    private float minNoiseHeight;
    private float maxNoiseHeight;

    public void Initiate()
    {
        heights = new List<float>();
        polygon = new Polygon();

        if (randomPoints == true)
        {
            for (int i = 0; i < pointDensity; i++)
            {
                var x = Random.Range(.0f, sizeX);
                var y = Random.Range(.0f, sizeY);

                polygon.Add(new Vertex(x, y));
            }  
        }
        else
        {
            poissonPoints = PoissonDiscSampling.GeneratePoints(minDistancePerPoint, new Vector2(sizeX,sizeY), rejectionSamples);
            for (int i = 0; i < poissonPoints.Count; i++)
            {
                polygon.Add(new Vertex(poissonPoints[i].x,poissonPoints[i].y));
            }
        }

        ConstraintOptions constraints = new ConstraintOptions();
        constraints.ConformingDelaunay = true;

        mesh = polygon.Triangulate(constraints) as TriangleNet.Mesh;

        ShapeTerrain();
        GenerateMesh();
    }

    private void ShapeTerrain()
    {
        minNoiseHeight = float.PositiveInfinity;
        maxNoiseHeight = float.NegativeInfinity;

        for (int i = 0; i < mesh.vertices.Count; i++)
        {
            
            float amplitude = 1f;
            float frequency = 1f;
            float noiseHeight = 0f;

            for (int o = 0; o < octaves; o++)
            {
                float xValue = (float) mesh.vertices[i].x / scale * frequency;
                float yValue = (float) mesh.vertices[i].y / scale * frequency;

                float perlinValue = Mathf.PerlinNoise(xValue + offset.x + seed, yValue + offset.y + seed) * 2 - 1;
                perlinValue *= dampening;

                noiseHeight += perlinValue * amplitude;

                amplitude *= persistence;
                frequency *= lacunarity;
            }

            if (noiseHeight > maxNoiseHeight)
            {
                maxNoiseHeight = noiseHeight;
            }
            else if (noiseHeight < minNoiseHeight)
            {
                minNoiseHeight = noiseHeight;
            }

            noiseHeight = (noiseHeight < 0f) ? noiseHeight * heightScale/10f : noiseHeight * heightScale;
            
            heights.Add(noiseHeight); 
        }
        
    }

    private void GenerateMesh()
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<Color> colors = new List<Color>();
        List<int> triangles = new List<int>();
        
        IEnumerator<Triangle> triangleEnum = mesh.Triangles.GetEnumerator();

        for (int i = 0; i < mesh.Triangles.Count; i++)
        {
            if (!triangleEnum.MoveNext())
            {
                break;
            }
            
            Triangle currentTriangle = triangleEnum.Current;

            Vector3 v0 = new Vector3((float) currentTriangle.vertices[2].x,heights[currentTriangle.vertices[2].id],(float)currentTriangle.vertices[2].y);
            Vector3 v1 = new Vector3((float) currentTriangle.vertices[1].x,heights[currentTriangle.vertices[1].id],(float)currentTriangle.vertices[1].y);
            Vector3 v2 = new Vector3((float) currentTriangle.vertices[0].x,heights[currentTriangle.vertices[0].id],(float) currentTriangle.vertices[0].y);
            
            triangles.Add(vertices.Count);
            triangles.Add(vertices.Count + 1);
            triangles.Add(vertices.Count + 2);
            
            vertices.Add(v0);
            vertices.Add(v1);
            vertices.Add(v2);

            var normal = Vector3.Cross(v1 - v0, v2 - v0);

            var triangleColor = EvaluateColor(currentTriangle);
            
            for(int x = 0; x < 3; x++)
            {
                normals.Add(normal);
                uvs.Add(Vector3.zero);
                colors.Add(triangleColor);
            }
        }

        terrainMesh = new UnityEngine.Mesh();
        terrainMesh.vertices = vertices.ToArray();
        terrainMesh.uv = uvs.ToArray();
        terrainMesh.triangles = triangles.ToArray();
        terrainMesh.colors = colors.ToArray();
        terrainMesh.normals = normals.ToArray();

        transform.GetComponent<MeshFilter>().mesh = terrainMesh;
        transform.GetComponent<MeshCollider>().sharedMesh = terrainMesh;
        transform.GetComponent<MeshRenderer>().material = material;
        
    }

    private Color EvaluateColor(Triangle triangle)
    {
        var currentHeight = heights[triangle.vertices[0].id] + heights[triangle.vertices[1].id] + heights[triangle.vertices[2].id];
        currentHeight /= 3f;
        
        switch (colorSetting)
        {
            case ColorSetting.Random:

                return new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));

            case ColorSetting.HeightGradient:

                currentHeight = (currentHeight < 0f) ? currentHeight / heightScale * 10f : currentHeight / heightScale;
                
                var gradientVal = Mathf.InverseLerp (minNoiseHeight, maxNoiseHeight, currentHeight);
                return heightGradient.Evaluate(gradientVal);

        }

        return Color.magenta;
    }

    public void SaveMesh()
    {
        if (transform.GetComponent<MeshFilter>() != null)
        {
            var path = "Assets/GeneratedMesh" + seed.ToString() + ".asset";
            AssetDatabase.CreateAsset(transform.GetComponent<MeshFilter>().sharedMesh, path );
        }
    }
}
