using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;
using System;
using System.Linq;

public class WorldGenerator : MonoBehaviour
{

  const int StitchTilesKernel = 0;
  const int StitchHeightTilesKernel = 1;
  const int CalculateMeshHeightsKernel = 2;
  const int satelliteTileSize = 256;
  const int heightTileSize = 4096;

  public int resolution = 10;
  public int numSubdivisions;
  public float worldRadius = 100;
  public float heightMultiplier = 10;

  public int textureResolution = 5;

  // public Light fillLight;
  // public Light fillLight2;

  [Range(0, 1)]
  public float seaLevel;

  public static bool initialized = false;
  public Material material;

  public ComputeShader meshCompute;

  public RenderTexture albedoMap;
  public RenderTexture heightMap;
  private CameraEdges cameraEdges;
  public Texture2D tex;
  MeshFilter[] meshFilters;
  public int NUMBER_OF_TILES;

  void Awake()
  {
    cameraEdges = GetComponent<CameraEdges>();
    Texture2D[] tiles = Resources.LoadAll<Texture2D>("SatelliteMap/Tiles_" + textureResolution);
    tiles = tiles.OrderBy(tile => Int32.Parse(tile.name.Substring(5))).ToArray();
    NUMBER_OF_TILES = (int)Mathf.Sqrt(tiles.Length);

    ComputeHelper.CreateRenderTexture(ref albedoMap, satelliteTileSize * NUMBER_OF_TILES, satelliteTileSize * NUMBER_OF_TILES, FilterMode.Bilinear, RenderTextureFormat.ARGBHalf, "Albedo Map");
    Stitch(meshCompute, tiles, ref albedoMap, NUMBER_OF_TILES, NUMBER_OF_TILES, satelliteTileSize, StitchTilesKernel);
  }

  void Start()
  {

    Texture2D[] heightMapTiles = Resources.LoadAll<Texture2D>("HeightMap");

    heightMap = new RenderTexture(heightTileSize * 4, heightTileSize * 2, 0);
    heightMap.graphicsFormat = GraphicsFormat.R8G8B8A8_SNorm;
    heightMap.enableRandomWrite = true;

    heightMap.autoGenerateMips = false;
    heightMap.Create();
    heightMap.name = name;
    heightMap.wrapMode = TextureWrapMode.Clamp;
    heightMap.filterMode = FilterMode.Bilinear;
    // ComputeHelper.CreateRenderTexture(ref heightMap, heightTileSize * 4, heightTileSize * 2, FilterMode.Bilinear, RenderTextureFormat.ARGBHalf, "Height Map");
    Stitch(meshCompute, heightMapTiles, ref heightMap, 4, 2, heightTileSize, StitchHeightTilesKernel);


    meshFilters = new MeshFilter[6 * (int)Mathf.Pow(4, numSubdivisions)];
    for (int i = 0; i < 6 * Mathf.Pow(4, numSubdivisions); i++)
    {
      GameObject meshHolder = new GameObject("Cube Sphere Mesh" + i);
      meshHolder.transform.SetParent(transform, false);
      MeshFilter meshFilter = meshHolder.AddComponent<MeshFilter>();
      MeshRenderer renderer = meshHolder.AddComponent<MeshRenderer>();
      renderer.sharedMaterial = material;
      meshFilters[i] = meshFilter;
    }

    material.SetFloat("_bottom", 0);
    material.SetFloat("_top", 1);
    material.SetFloat("_left", 0);
    material.SetFloat("_right", 1);

    // tex = new Texture2D(tileSize * NUMBER_OF_TILES, tileSize * NUMBER_OF_TILES, TextureFormat.RGBAHalf, false);
    // RenderTexture.active = albedoMap;
    // tex.ReadPixels( new Rect(0, 0, albedoMap.width, albedoMap.height), 0, 0);
    // tex.Apply();
    // RenderTexture.active = null;

    // byte[] bytes = tex.EncodeToPNG();
    // var dirPath = Application.dataPath + "./TextureTest.png";
    // System.IO.File.WriteAllBytes(dirPath + "Image" + ".png", bytes);
    // CalculateNormalsTexture();
    initialized = true;
  }


  public static void Stitch(ComputeShader meshCompute, Texture2D[] tiles, ref RenderTexture map, int width, int height, int tileSize, int kernel)
  {
    meshCompute.SetTexture(kernel, "Map", map);

    for (int i = 0; i < tiles.Length; i++)
    {
      meshCompute.SetTexture(kernel, kernel == StitchTilesKernel ? "Tile" : "HeightTile", tiles[i]);
      meshCompute.SetInt("offsetX", (i / height) * tileSize);
      meshCompute.SetInt("offsetY", ((height - 1) - (i % height)) * tileSize);
      ComputeHelper.Dispatch(meshCompute, tileSize, tileSize, 1, kernel);
    }
  }



  Mesh Create(Vector3[] vertices, int[] triangles, int idx)
  {
    Mesh mesh = new Mesh();
    Vector3[] uvData = null;

    if (vertices.Length != 0)
    {
      ComputeBuffer vertexBuffer = new ComputeBuffer(vertices.Length, sizeof(float) * 3);
      vertexBuffer.SetData(vertices);

      ComputeBuffer uvBuffer = new ComputeBuffer(vertices.Length, sizeof(float) * 3);

      meshCompute.SetBuffer(CalculateMeshHeightsKernel, "VertexBuffer", vertexBuffer);
      meshCompute.SetBuffer(CalculateMeshHeightsKernel, "UVBuffer", uvBuffer);
      meshCompute.SetTexture(CalculateMeshHeightsKernel, "Map", heightMap);
      meshCompute.SetInt("numVertices", vertexBuffer.count);

      meshCompute.SetInt("mapWidth", heightMap.width);
      meshCompute.SetInt("mapHeight", heightMap.height);
      meshCompute.SetFloat("worldRadius", worldRadius);
      meshCompute.SetFloat("heightMultiplier", heightMultiplier);
      meshCompute.SetFloat("seaLevel", seaLevel);

      ComputeHelper.Dispatch(meshCompute, vertexBuffer.count, 1, 1, CalculateMeshHeightsKernel);
      vertexBuffer.GetData(vertices);

      uvData = new Vector3[uvBuffer.count];
      uvBuffer.GetData(uvData);

      vertexBuffer.Release();
      uvBuffer.Release();
    }

    // Create mesh and renderer object

    mesh.indexFormat = (vertices.Length < 1 << 16) ? IndexFormat.UInt16 : IndexFormat.UInt32;
    mesh.SetVertices(vertices);
    if (vertices.Length != 0) mesh.SetUVs(0, uvData);
    mesh.SetTriangles(triangles, 0, true);
    mesh.RecalculateNormals();

    meshFilters[idx].mesh = mesh;
    return mesh;
  }

  // void CalculateNormalsTexture()
  // {
  // 	meshCompute.SetTexture(CalculateNormalsKernel, "HeightMap", heightMap);
  // 	ComputeHelper.Dispatch(meshCompute, heightMap.width, heightMap.height, 1, CalculateNormalsKernel);
  // }


  List<Mesh> frameMeshes = new List<Mesh>();
  void Update()
  {
    foreach (Mesh mesh in frameMeshes)
    {
      Destroy(mesh);
    }
    frameMeshes = new List<Mesh>();
    var allMeshData = CubeSphere.GenerateMeshes(resolution, numSubdivisions, transform);
    for (int i = 0; i < allMeshData.Count; i++)
    {
      // Debug.Log(i + "  " + allMeshData.Count);
      frameMeshes.Add(Create(allMeshData[i].vertices, allMeshData[i].triangles, i));
    }
    material.SetTexture("_MainTex", albedoMap);
    material.SetTexture("_HeightMap", heightMap);
    // material.SetFloat("_SeaLevel", seaLevel);
    // material.SetVector("fillLightDir", fillLight.transform.forward);
    // material.SetColor("fillLightCol", new Color(fillLight.color.r, fillLight.color.g, fillLight.color.b, fillLight.intensity));
    // material.SetVector("fillLightDir2", fillLight2.transform.forward);
    // material.SetColor("fillLightCol2", new Color(fillLight2.color.r, fillLight2.color.g, fillLight2.color.b, fillLight2.intensity));
  }

  // public (float height, bool inOcean, int countryIndex) GetTerrainInfo(Coordinate coordinate)
  // {
  // 	//meshCompute.SetVector("probePos", pos.normalized);
  // 	ComputeBuffer heightRequestBuffer = new ComputeBuffer(1, sizeof(float) * 3);
  // 	meshCompute.SetVector("heightRequestCoord", coordinate.ToVector2());
  // 	meshCompute.SetBuffer(HeightRequestKernel, "HeightRequestBuffer", heightRequestBuffer);
  // 	meshCompute.SetTexture(HeightRequestKernel, "HeightMap", heightMap);
  // 	ComputeHelper.Dispatch(meshCompute, 1, 1, 1, HeightRequestKernel);

  // 	Vector3[] result = new Vector3[1];
  // 	heightRequestBuffer.GetData(result);
  // 	heightRequestBuffer.Release();

  // 	return (result[0].x, (int)result[0].y == 1, (int)result[0].z);

  // }


}
