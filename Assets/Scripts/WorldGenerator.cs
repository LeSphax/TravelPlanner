using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

public class WorldGenerator : MonoBehaviour
{

  const int StitchTilesKernel = 0;
  const int StitchHeightTilesKernel = 1;
  const int CalculateMeshHeightsKernel = 2;
  const int StitchLowResTilesKernel = 3;
  const int satelliteTileSize = 512;
  const int heightTileSize = 4096;

  public int resolution = 10;
  public int targetNumVertices;
  public float worldRadius = 100;
  public float heightMultiplier = 10;

  public int textureResolution = 5;

  // public Light fillLight;
  // public Light fillLight2;

  [Range(0, 1)]
  public float seaLevel;

  public bool initialized = false;
  public Material material;

  public ComputeShader meshCompute;

  public RenderTexture albedoMap;
  public RenderTexture heightMap;
  private CameraEdges cameraEdges;
  MeshFilter[] meshFilters;
  public int NUMBER_OF_TILES;

  void Awake()
  {
    cameraEdges = GetComponent<CameraEdges>();
    // Texture2D[] tiles = Resources.LoadAll<Texture2D>("SatelliteMap/Tiles_" + textureResolution);
    // tiles = tiles.OrderBy(tile => Int32.Parse(tile.name.Substring(5))).ToArray();
    NUMBER_OF_TILES = (int)Mathf.Pow(2, textureResolution);

    ComputeHelper.CreateRenderTexture(ref albedoMap, satelliteTileSize * NUMBER_OF_TILES, satelliteTileSize * NUMBER_OF_TILES, FilterMode.Bilinear, RenderTextureFormat.ARGBHalf, "Albedo Map");
    // Stitch(meshCompute, tiles, ref albedoMap, NUMBER_OF_TILES, NUMBER_OF_TILES, satelliteTileSize, StitchTilesKernel);
  }

  void Start()
  {

        Texture2D[] heightMapTiles = Resources.LoadAll<Texture2D>("HeightMap");

        heightMap = new RenderTexture(heightTileSize * 4, heightTileSize * 2, 0);
        heightMap.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_SNorm;
        heightMap.enableRandomWrite = true;

        heightMap.autoGenerateMips = false;
        heightMap.Create();
        heightMap.name = name;
        heightMap.wrapMode = TextureWrapMode.Clamp;
        heightMap.filterMode = FilterMode.Bilinear;

        Stitch(meshCompute, heightMapTiles, ref heightMap, 4, 2, heightTileSize, StitchHeightTilesKernel);


        meshFilters = new MeshFilter[600];
    for (int i = 0; i < 600; i++)
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
    material.SetVector("_left", new Vector2(0, 0));
    material.SetVector("_right", new Vector2(1, 1));

    initialized = true;
    //transform.position = new Vector3(0, 0, 63631.3125f);
    //transform.rotation = new Quaternion(-0.30855026841163638f, -0.038197193294763568f, -0.004406340420246124f, 0.9504305720329285f);
    //transform.localScale = Vector3.one * 63626.31f;
  }

  static void Stitch(ComputeShader meshCompute, Texture2D[] tiles, ref RenderTexture map, int width, int height, int tileSize, int kernel)
  {
    meshCompute.SetTexture(kernel, "Map", map);

    for (int i = 0; i < tiles.Length; i++)
    {
      meshCompute.SetTexture(kernel, kernel == StitchHeightTilesKernel ? "HeightTile" : "Tile", tiles[i]);
      meshCompute.SetInt("offsetX", (i / height) * tileSize);
      meshCompute.SetInt("offsetY", ((height - 1) - (i % height)) * tileSize);
      ComputeHelper.Dispatch(meshCompute, tileSize, tileSize, 1, kernel);
    }
  }

  public static void Stitch(ComputeShader meshCompute, (Texture2D tile, TilePart part)[] tiles, ref RenderTexture map, int width, int height, int tileSize)
  {
    Debug.Log("Stitch");
    meshCompute.SetTexture(StitchLowResTilesKernel, "Map", map);

    for (int i = 0; i < tiles.Length; i++)
    {
      meshCompute.SetTexture(StitchLowResTilesKernel, "Tile", tiles[i].tile);
      meshCompute.SetInt("offsetX", (i / height) * tileSize);
      meshCompute.SetInt("offsetY", ((height - 1) - (i % height)) * tileSize);
      var splitCount = tiles[i].part.sideSplitCount;
      var partSize = tileSize / splitCount;
      meshCompute.SetInt("sideSplitCount", splitCount);
      meshCompute.SetInt("logSideSplitCount", (int)Mathf.Log(splitCount, 2));
      meshCompute.SetInt("textureOffsetX", partSize * tiles[i].part.x);
      meshCompute.SetInt("textureOffsetY", partSize * tiles[i].part.y);
      if (splitCount == 8)
        Debug.Log($"{splitCount} {partSize} {52 % splitCount} {52 >> (splitCount / 2)}");
      ComputeHelper.Dispatch(meshCompute, partSize, partSize, splitCount * splitCount, StitchLowResTilesKernel);
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


  List<Mesh> frameMeshes = new List<Mesh>();
  void Update()
  {
    foreach (Mesh mesh in frameMeshes)
    {
      Destroy(mesh);
    }
    frameMeshes = new List<Mesh>();
    var allMeshData = CubeSphere.GenerateMeshes(resolution, transform, cameraEdges, targetNumVertices);
    for (int i = 0; i < allMeshData.Count; i++)
    {
      // Debug.Log(allMeshData[i].vertices.Length);
      frameMeshes.Add(Create(allMeshData[i].vertices, allMeshData[i].triangles, i));
    }
    material.SetTexture("_MainTex", albedoMap);
    material.SetTexture("_HeightMap", heightMap);
  }
}
