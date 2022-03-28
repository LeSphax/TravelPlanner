using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using System.Linq;

public class TileDownload : MonoBehaviour
{

  public int precision = 2;

  public RawImage image;

  public ComputeShader meshCompute;

  private double previousLongitude;
  private double previousLatitude;
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

  private CameraEdges cameraEdges;

  Dictionary<string, Texture2D> tiles = new Dictionary<string, Texture2D>();

  private Dictionary<int, Texture2D> defaultTexture;

  [Obsolete]
  async void DownloadImage(string hash, string MediaUrl)
  {
    if (!tiles.ContainsKey(hash))
    {
      UnityWebRequest request = UnityWebRequestTexture.GetTexture(MediaUrl);
      tiles[hash] = defaultTexture.ContainsKey(precision) ? defaultTexture[precision] : Texture2D.redTexture;
      await request.SendWebRequest();
      if (request.isNetworkError || request.isHttpError)
      {
        Debug.Log(request.error);
      }
      else
      {
        networkUpdated = true;
        tiles[hash] = ((DownloadHandlerTexture)request.downloadHandler).texture;
      }
    }
  }

  Vector2 getTile(double longitude, double latitude, int precision, bool log = false)
  {
    double xProportion = ((longitude + Mathd.PI) / Mathd.PI) / 2;
    double yProportion = (1 - Mathd.Log(Mathd.Tan(latitude) + 1 / Mathd.Cos(latitude)) / Mathd.PI) / 2;
    if (log)
      Debug.Log($"Props {xProportion} {yProportion}");

    // Debug.Log("Proportions " + xProportion + "   " + yProportion);
    double tileSideAmount = Mathf.Pow(2, precision);
    float xTile = (int)Mathd.Floor(tileSideAmount * xProportion);
    float yTile = (int)Mathd.Floor(tileSideAmount * yProportion);
    if (log)
      Debug.Log($"Coords {tileSideAmount * xProportion} {tileSideAmount * yProportion} {xTile} { yTile}");
    return new Vector2(xTile, yTile);
  }

  Vector2d? getCoords(Vector3d? intersection, bool log = false)
  {
    if (!intersection.HasValue) return null;
    double latitude_rad = Mathd.Asin(intersection.Value.y);
    double longitude_rad = Mathd.Atan2(intersection.Value.x, -intersection.Value.z);
    if (log)
      Debug.Log($"Lat {latitude_rad * Mathf.Rad2Deg} {longitude_rad * Mathf.Rad2Deg}");
    return new Vector2d(longitude_rad, latitude_rad);
  }

  Vector2? getIndices(Vector3d? intersection, int precision, bool log = false)
  {
    if (!intersection.HasValue) return null;
    Vector2d? coords = this.getCoords(intersection, log);
    // Debug.Log($" Coords {coords.Value.ToString()} {intersection.Value.ToString()}");
    return this.getTile(coords.Value.x, coords.Value.y, precision, log);
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
    cameraEdges = FindObjectOfType<CameraEdges>();
    // ComputeHelper.CreateRenderTexture(ref satelliteTexture, 2048, 2048, FilterMode.Bilinear, RenderTextureFormat.ARGBHalf, "Albedo Map");
    // image.texture = satelliteTexture;
  }
  void Start()
  {
    // ComputeHelper.CreateRenderTexture(ref albedoMap, 2048, 2048, FilterMode.Bilinear, RenderTextureFormat.ARGBHalf, "Albedo Map");
    albedoMap = world.albedoMap;
    image.texture = albedoMap;
  }

  struct TileRanges
  {
    public int precision;

    public int[] minX;
    public int minY;
    public int[] maxX;
    public int maxY;

    public override string ToString()
    {
      return $"{minX[0]} {minX[1]} {maxX[0]} {maxX[1]} {minY} {maxY} {precision}";
    }
  }

  TileRanges? GetTileRanges(int precision)
  {
    Vector2? botLeftIndices = getIndices(cameraEdges.botLeftIntersection, precision);
    Vector2? botRightIndices = getIndices(cameraEdges.botRightIntersection, precision);
    Vector2? topRightIndices = getIndices(cameraEdges.topRightIntersection, precision);
    Vector2? topLeftIndices = getIndices(cameraEdges.topLeftIntersection, precision);
    Vector2? topIndices = getIndices(cameraEdges.topIntersection, precision);
    Vector2? botIndices = getIndices(cameraEdges.botIntersection, precision);

    if (botLeftIndices.HasValue && botRightIndices.HasValue && topRightIndices.HasValue && topLeftIndices.HasValue && topIndices.HasValue && botIndices.HasValue)
    {
      // Debug.Log($"Intersections {cameraEdges.botLeftIntersection} {cameraEdges.topLeftIntersection} {cameraEdges.topRightIntersection}");
      // Debug.Log($"Indices {wrapped} {botLeftIndices.Value.x} {botRightIndices.Value.x} {topRightIndices.Value.x} {topLeftIndices.Value.x}");
      int[] xArray = new int[] { (int)botLeftIndices.Value.x, (int)botRightIndices.Value.x, (int)topRightIndices.Value.x, (int)topLeftIndices.Value.x };
      int[] yArray = new int[] { (int)botLeftIndices.Value.y, (int)botRightIndices.Value.y, (int)topRightIndices.Value.y, (int)topLeftIndices.Value.y, (int)topIndices.Value.y, (int)botIndices.Value.y };
      Array.Sort(xArray);
      Array.Sort(yArray);

      TileRanges tr = new TileRanges();
      tr.precision = precision;
      tr.minX = new int[2];
      tr.minY = yArray[0];
      tr.maxY = yArray[5];

      tr.maxX = new int[2];
      int totalTiles = (int)Mathf.Pow(2, precision);

      bool isAPoleVisible = cameraEdges.northPoleIsVisible || cameraEdges.southPoleIsVisible;

      if (isAPoleVisible)
      {
        tr.minX[0] = 0;
        tr.maxX[0] = totalTiles;
      }
      else
      {
        tr.minX[0] = xArray[0];
        tr.maxX[0] = xArray[3];
      }


      wrapped = false;
      // We wrapped around the origin
      if (!isAPoleVisible && tr.maxX[0] - tr.minX[0] > totalTiles / 2)
      {
        tr.minX[1] = 0;
        tr.maxX[1] = xArray.Where(x => x < totalTiles / 2).Last();
        tr.minX[0] = xArray.Where(x => x > totalTiles / 2).First();
        tr.maxX[0] = (int)totalTiles - 1;
        wrapped = true;
      }

      // Debug.Log($"{(tr.maxX[0] - tr.minX[0])} {(tr.maxX[1] - tr.minX[1])} {(tr.maxY - tr.minY)} {world.NUMBER_OF_TILES / 2}");
      if ((tr.maxX[0] - tr.minX[0]) >= world.NUMBER_OF_TILES / 2 || (tr.maxX[1] - tr.minX[1]) >= world.NUMBER_OF_TILES / 2 || (tr.maxY - tr.minY) >= world.NUMBER_OF_TILES / 2)
      {
        return GetTileRanges(precision - 1);
      }
      return tr;
    }
    else
    {
      return null;
    }
  }

  [Obsolete]
  void Update()
  {
    if (cameraEdges.centerIntersection.HasValue || networkUpdated)
    {
      Vector2d coords = getCoords(cameraEdges.centerIntersection).Value;
      bool somethingChanged = coords.y != previousLatitude || coords.x != previousLongitude || precision != previousPrecision || cameraEdges.botRightIntersection.HasValue != previousBotRightHasValue;
      previousBotRightHasValue = cameraEdges.botRightIntersection.HasValue;
      if (somethingChanged || networkUpdated)
      {

        TileRanges? tr = this.GetTileRanges(precision + 3);
        // Debug.Log($"Ranges {tr.ToString()}");

        if (tr.HasValue)
        {
          minX = tr.Value.minX;
          maxX = tr.Value.maxX;
          minY = tr.Value.minY;
          maxY = tr.Value.maxY;
          precision = tr.Value.precision;

          for (int i = 0; i < 2; i++)
          {
            for (int x = minX[i]; x <= maxX[i]; x++)
            {
              for (int y = minY; y <= maxY; y++)
              {
                string hash = $"{precision}/{x}/{y}";
                string url = $"https://api.maptiler.com/tiles/satellite-v2/{precision}/{x}/{y}.jpg?key=aQbIOs34kku6WFUUdTnW";
                DownloadImage(hash, url);
              }
            }
          }
        }
      }
      previousRequestTime = Time.timeSinceLevelLoad;
      previousLatitude = coords.y;
      previousLongitude = coords.x;
      previousPrecision = precision;

      if (cameraEdges.botLeftIntersection.HasValue && (minX[0] != previousMinX[0] || minX[1] != previousMinX[1] || minY != previousMinY || maxX[0] != previousMaxX[0] || maxX[1] != previousMaxX[1] || maxY != previousMaxY || networkUpdated))
      {
        int textureTileWidth = world.NUMBER_OF_TILES / 2;
        int textureTileHeight = world.NUMBER_OF_TILES / 2;
        // Debug.Log($"Values {minX[0]} {maxX[0]}  {minX[1]} {maxX[1]} {minY} {maxY} {textureTileWidth}");
        Texture2D[] tilesToDisplay = new Texture2D[textureTileWidth * textureTileHeight];


        for (int x = minX[0]; x <= maxX[0]; x++)
        {
          for (int y = minY; y <= maxY; y++)
          {
            string hash = $"{precision}/{x}/{y}";
            if (tiles.ContainsKey(hash) && tiles[hash] != null)
            {
              // Debug.Log($"Array {x} {y} {(x - minX[0]) * textureTileHeight + (y - minY)}");
              tilesToDisplay[(x - minX[0]) * textureTileHeight + (y - minY)] = tiles[hash];
            }
          }
        }

        if (wrapped)
        {
          for (int x = minX[1]; x <= maxX[1]; x++)
          {
            for (int y = minY; y <= maxY; y++)
            {
              string hash = $"{precision}/{x}/{y}";
              if (tiles.ContainsKey(hash) && tiles[hash] != null)
              {
                // Debug.Log($"Wrapped Array {(x - minX[1] + maxX[0] - minX[0]) * textureTileHeight + (y - minY)} {(y - minY)} {(x - minX[1] + maxX[0] - minX[0]) * textureTileHeight}");
                tilesToDisplay[(x - minX[1] + maxX[0] - minX[0] + 1) * textureTileHeight + (y - minY)] = tiles[hash];
              }
            }
          }
        }


        for (int y = 0; y < textureTileWidth * textureTileHeight; y++)
        {
          if (tilesToDisplay[y] == null)
          {
            tilesToDisplay[y] = pinkTexture;
          }
        }

        WorldGenerator.Stitch(meshCompute, tilesToDisplay, ref albedoMap, textureTileWidth, textureTileHeight, 512, 3);

        for (int i = 0; i < 2; i++)
        {
          previousMinX[i] = minX[i];
          previousMaxX[i] = maxX[i];

        }
        previousMinY = minY;

        previousMaxY = maxY;
        networkUpdated = false;

        float totalTiles = Mathf.Pow(2, precision);
        float bottom = (totalTiles - (minY + textureTileHeight)) / totalTiles;
        float top = (totalTiles - minY) / totalTiles;

        Vector2 left = new Vector2((minX[0]) / totalTiles, (minX[1]) / totalTiles);
        Vector2 right = new Vector2((minX[0] + textureTileWidth) / totalTiles, (minX[1] + textureTileWidth) / totalTiles);
        float offset = (maxX[0] - minX[0] + 1) / (float)textureTileWidth;
        world.material.SetFloat("_bottom", bottom);
        world.material.SetFloat("_top", top);
        world.material.SetFloat("_offset", offset);
        world.material.SetVector("_left", left);
        world.material.SetVector("_right", right);
      }
    }
  }
}



// https://api.maptiler.com/maps/hybrid/static/-122.4271,37.8065,15/512x512.png?key=aQbIOs34kku6WFUUdTnW
