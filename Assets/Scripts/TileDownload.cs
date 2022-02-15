using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class TileDownload : MonoBehaviour
{

  public int precision = 2;

  public RawImage image;

  public ComputeShader meshCompute;

  private float previousLongitude;
  private float previousLatitude;
  private int previousPrecision;
  private float previousRequestTime;

  private int[] minX = new int[2];
  private int minY = 0;
  private int[] maxX = new int[2];
  private int maxY = 0;

  private bool wrapped;

  private int[] previousMinX = new int[2];
  private int previousMinY;
  private int[] previousMaxX = new int[2];
  private int previousMaxY;

  private bool networkUpdated;
  private bool previousBotRightHasValue = false;
  public RenderTexture satelliteTexture;
  private Texture2D pinkTexture;

  private WorldGenerator world;
  private RenderTexture albedoMap;

  Dictionary<string, Texture2D> tiles = new Dictionary<string, Texture2D>();

  private Dictionary<int, Texture2D> defaultTexture;

  Vector2 getTile(float longitude, float latitude, int precision)
  {
    float xProportion = ((longitude + Mathf.PI) / Mathf.PI) / 2;
    float yProportion = (1 - Mathf.Log(Mathf.Tan(latitude) + 1 / Mathf.Cos(latitude)) / Mathf.PI) / 2;

    // Debug.Log("Proportions " + xProportion + "   " + yProportion);
    float tileSideAmount = Mathf.Pow(2, precision);
    float xTile = (int)Mathf.Floor(tileSideAmount * xProportion);
    float yTile = (int)Mathf.Floor(tileSideAmount * yProportion);

    return new Vector2(xTile, yTile);
  }

  Vector2? getCoords(Vector3? intersection)
  {
    if (!intersection.HasValue) return null;
    float latitude_rad = Mathf.Asin(intersection.Value.y);
    float longitude_rad = Mathf.Atan2(intersection.Value.x, -intersection.Value.z);
    return new Vector2(longitude_rad, latitude_rad);
  }

  Vector2? getIndices(Vector3? intersection)
  {
    if (!intersection.HasValue) return null;
    Vector2? coords = this.getCoords(intersection);
    return this.getTile(coords.Value.x, coords.Value.y, precision);
  }

  void Awake()
  {
    defaultTexture = new Dictionary<int, Texture2D>{
      { 4, Texture2D.blackTexture },
      { 5, Texture2D.grayTexture },
      { 6, Texture2D.redTexture },
      { 7, Texture2D.whiteTexture },
      { 8, Texture2D.normalTexture },
      { 9, Texture2D.linearGrayTexture },
      { 10, Texture2D.blackTexture }
  };
    pinkTexture = new Texture2D(512, 512, TextureFormat.RGBAHalf, false);
    var fillColorArray = new Color[512 * 512];
    for (int i = 0; i < 256 * 512; i++)
    {
      fillColorArray[i] = Color.magenta;
    }

    for (int i = 256 * 512; i < 512 * 512; i++)
    {
      fillColorArray[i] = Color.blue;
    }
    pinkTexture.SetPixels(fillColorArray);
    pinkTexture.Apply();
    world = GetComponent<WorldGenerator>();

    // ComputeHelper.CreateRenderTexture(ref satelliteTexture, 2048, 2048, FilterMode.Bilinear, RenderTextureFormat.ARGBHalf, "Albedo Map");
    // image.texture = satelliteTexture;
  }
  void Start()
  {
    // ComputeHelper.CreateRenderTexture(ref albedoMap, 2048, 2048, FilterMode.Bilinear, RenderTextureFormat.ARGBHalf, "Albedo Map");
    albedoMap = world.albedoMap;
    image.texture = albedoMap;
  }

  [System.Obsolete]
  void Update()
  {
    float totalTiles = Mathf.Pow(2, precision);
    if (CameraEdges.topLeftIntersection.HasValue && CameraEdges.botRightIntersection.HasValue)
    {
      float distance = Vector3.Distance(CameraEdges.topLeftIntersection.Value, CameraEdges.botRightIntersection.Value);
      // Debug.Log("Distance " + distance);
    }

    if (CameraEdges.centerIntersection.HasValue)
    {
      Vector2 coords = getCoords(CameraEdges.centerIntersection).Value;
      bool somethingChanged = coords.y != previousLatitude || coords.x != previousLongitude || precision != previousPrecision || CameraEdges.botRightIntersection.HasValue != previousBotRightHasValue;
      previousBotRightHasValue = CameraEdges.botRightIntersection.HasValue;
      if (somethingChanged)
      {
        Vector2? botLeftIndices = getIndices(CameraEdges.botLeftIntersection);
        Vector2? botRightIndices = getIndices(CameraEdges.botRightIntersection);
        Vector2? topRightIndices = getIndices(CameraEdges.topRightIntersection);
        Vector2? topLeftIndices = getIndices(CameraEdges.topLeftIntersection);

        if (botLeftIndices.HasValue && botRightIndices.HasValue && topRightIndices.HasValue && topLeftIndices.HasValue)
        {
          minX[0] = (int)Mathf.Min(botLeftIndices.Value.x, botRightIndices.Value.x, topRightIndices.Value.x, topLeftIndices.Value.x);
          minY = (int)Mathf.Min(botLeftIndices.Value.y, botRightIndices.Value.y, topRightIndices.Value.y, topLeftIndices.Value.y);
          maxX[0] = (int)Mathf.Max(botLeftIndices.Value.x, botRightIndices.Value.x, topRightIndices.Value.x, topLeftIndices.Value.x);
          maxY = (int)Mathf.Max(botLeftIndices.Value.y, botRightIndices.Value.y, topRightIndices.Value.y, topLeftIndices.Value.y);

          Debug.Log($"Original {minX[0]} {maxX[0]}");

          wrapped = false;
          // We wrapped around the origin
          if (maxX[0] - minX[0] > totalTiles / 2)
          {
            minX[1] = 0;
            maxX[1] = minX[0];
            minX[0] = maxX[0];
            maxX[0] = (int)totalTiles - 1;
            wrapped = true;
          }


          for (int i = 0; i < 2; i++)
          {
            for (int x = minX[i]; x <= maxX[i]; x++)
            {
              for (int y = minY; y <= maxY; y++)
              {
                string hash = $"{precision}/{x}/{y}";
                string url = $"https://api.maptiler.com/tiles/satellite-v2/{precision}/{x}/{y}.jpg?key=aQbIOs34kku6WFUUdTnW";
                StartCoroutine(DownloadImage(hash, url));
              }
            }
          }
        }
        previousRequestTime = Time.timeSinceLevelLoad;
        previousLatitude = coords.y;
        previousLongitude = coords.x;
        previousPrecision = precision;
      }
    }


    if (CameraEdges.botLeftIntersection.HasValue && (minX[0] != previousMinX[0] || minX[1] != previousMinX[1] || minY != previousMinY || maxX[0] != previousMaxX[0] || maxX[1] != previousMaxX[1] || maxY != previousMaxY || networkUpdated))
    {
      int numberOfTiles = world.NUMBER_OF_TILES / 2;
      Debug.Log($"{minX[0]} {maxX[0]}  {minX[1]} {maxX[1]} {minY} {maxY} {numberOfTiles}");
      Texture2D[] tilesToDisplay = new Texture2D[numberOfTiles * numberOfTiles];


      for (int x = minX[0]; x < minX[0] + numberOfTiles; x++)
      {
        for (int y = minY; y < minY + numberOfTiles; y++)
        {
          string hash = $"{precision}/{x}/{y}";
          if (tiles.ContainsKey(hash) && tiles[hash] != null)
          {
            tilesToDisplay[(x - minX[0]) + (y - minY) * numberOfTiles] = tiles[hash];
          }
        }
      }

      if (wrapped)
      {
        for (int x = minX[1]; x < minX[1] + numberOfTiles; x++)
        {
          for (int y = minY; y < minY + numberOfTiles; y++)
          {
            string hash = $"{precision}/{x}/{y}";
            if (tiles.ContainsKey(hash) && tiles[hash] != null)
            {
              tilesToDisplay[(x - minX[1]) + (y - minY) * numberOfTiles] = tiles[hash];
            }
          }
        }
      }


      for (int y = 0; y < numberOfTiles * numberOfTiles; y++)
      {
        if (tilesToDisplay[y] == null)
        {
          tilesToDisplay[y] = pinkTexture;
        }
      }

      WorldGenerator.Stitch(meshCompute, tilesToDisplay, ref albedoMap, 512, 0);

      for (int i = 0; i < 2; i++)
      {
        previousMinX[i] = minX[i];
        previousMaxX[i] = maxX[i];

      }
      previousMinY = minY;

      previousMaxY = maxY;
      networkUpdated = false;

      float bottom = (totalTiles - (minY + numberOfTiles)) / totalTiles;
      float top = (totalTiles - minY) / totalTiles;
      for (int i = 0; i < 2; i++)
      {
        Vector2 left = new Vector2((minX[0]) / totalTiles, (minX[1]) / totalTiles);
        Vector2 right = new Vector2((minX[0] + numberOfTiles) / totalTiles, (minX[1] + numberOfTiles) / totalTiles);
        FindObjectOfType<WorldGenerator>().material.SetFloat("_bottom", bottom);
        FindObjectOfType<WorldGenerator>().material.SetFloat("_top", top);
        FindObjectOfType<WorldGenerator>().material.SetVector("_left", left);
        FindObjectOfType<WorldGenerator>().material.SetVector("_right", right);
      }
    }
  }

  [System.Obsolete]
  IEnumerator DownloadImage(string hash, string MediaUrl)
  {
    if (!tiles.ContainsKey(hash))
    {
      UnityWebRequest request = UnityWebRequestTexture.GetTexture(MediaUrl);
      tiles[hash] = defaultTexture.ContainsKey(precision) ? defaultTexture[precision] : Texture2D.redTexture;
      yield return request.SendWebRequest();
      if (request.isNetworkError || request.isHttpError)
      {
        Debug.Log(request.error);
        yield break;
      }
      else
      {
        networkUpdated = true;
        tiles[hash] = ((DownloadHandlerTexture)request.downloadHandler).texture;
      }
    }
  }
}


// https://api.maptiler.com/maps/hybrid/static/-122.4271,37.8065,15/512x512.png?key=aQbIOs34kku6WFUUdTnW
