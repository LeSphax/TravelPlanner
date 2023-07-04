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
  private float previousScale;

  private int[] minX = new int[2];
  private int minY = 0;
  private int[] maxX = new int[2];
  private int maxY = 0;
  private bool wrapped = false;

  private int[] previousMinX = new int[2];
  private int previousMinY;
  private int[] previousMaxX = new int[2];
  private int previousMaxY;

  private bool networkUpdated;
  private bool previousBotRightHasValue = false;
  public RenderTexture satelliteTexture;
  private Texture2D whiteTexture;

  private WorldGenerator world;
  private RenderTexture albedoMap;

  private CameraEdges cameraEdges;

  Dictionary<string, Texture2D> tiles = new Dictionary<string, Texture2D>();

  [Obsolete]
  async void DownloadImage(string hash)
  {
    if (!tiles.ContainsKey(hash))
    {
      string url = $"https://api.mapbox.com/styles/v1/mapbox/satellite-v9/tiles/{hash}?access_token=pk.eyJ1IjoibGVzcGhheDMxIiwiYSI6ImNsMWVqeDRqZjBsM2QzZG11ZnR0ZWVnb2sifQ.QXp_xN3F1W8R8ZrvXYHJgw";
      // string url = $"https://api.maptiler.com/tiles/satellite-v2/{hash}.jpg?key=aQbIOs34kku6WFUUdTnW";
      UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
      tiles[hash] = null;
      await request.SendWebRequest();
      if (request.isNetworkError || request.isHttpError)
      {
        Debug.Log($"{url} {request.error}");
      }
      else
      {
        Debug.Log($"Downloaded {hash}");
        if (Int32.Parse(hash.Split('/')[0]) <= precision && Int32.Parse(hash.Split('/')[0]) > precision - 4)
        {
          networkUpdated = true;
        }
        tiles[hash] = ((DownloadHandlerTexture)request.downloadHandler).texture;
        // byte[] bytes = tiles[hash].EncodeToPNG();
        // var dirPath = Application.dataPath + $"./Resources/{hash}.png";
        // Debug.Log($"{dirPath}");

        // Directory.CreateDirectory(Application.dataPath + $"./Resources/{hash.Split('/')[0]}/{hash.Split('/')[1]}");
        // System.IO.File.WriteAllBytes(dirPath, bytes);
      }
    }
  }

  int getXTile(double longitude, int tileSideAmount)
  {
    double xProportion = ((longitude + Mathd.PI) / Mathd.PI) / 2;
    float xTile = (int)Mathd.Floor(tileSideAmount * xProportion);
    return (int)Mathf.Clamp(xTile, 0, tileSideAmount - 1);
  }

  int getYTile(double latitude, int tileSideAmount)
  {
    double yProportion = (1 - Mathd.Log(Mathd.Tan(latitude) + 1 / Mathd.Cos(latitude)) / Mathd.PI) / 2;
    float yTile = (int)Mathd.Floor(tileSideAmount * yProportion);
    return (int)Mathf.Clamp(yTile, 0, tileSideAmount - 1);
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

  [Obsolete]
  void Awake()
  {
    //   defaultTexture = new Dictionary<int, Texture2D>{
    //     { 4, Texture2D.blackTexture },
    //     { 5, Texture2D.grayTexture },
    //     { 6, Texture2D.redTexture },
    //     { 7, Texture2D.whiteTexture },
    //     { 8, Texture2D.normalTexture },
    //     { 9, Texture2D.linearGrayTexture },
    //     { 10, Texture2D.blackTexture }
    // };
    for (int precision = 0; precision < 6; precision++)
      for (int i = 0; i < Mathf.Pow(2, precision); i++)
      {
        Texture2D[] level5Tiles = Resources.LoadAll<Texture2D>($"{precision}/{i}");
        level5Tiles = level5Tiles.OrderBy(tile => Int32.Parse(tile.name)).ToArray();
        int offset = 0;
        for (int j = 0; j < Mathf.Pow(2, precision); j++)
        {
          string hash = $"{precision}/{i}/{j}";
          if (j - offset < level5Tiles.Length && Int32.Parse(level5Tiles[j - offset].name) == j)
          {
            // Debug.Log($"{i}/{level5Tiles[j - offset].name}");
            tiles[hash] = level5Tiles[j - offset];
          }
          else
          {
            offset += 1;
            Debug.Log($"Not found {i}/{j}");
            DownloadImage(hash);
          }
        }
      }

    whiteTexture = new Texture2D(512, 512, TextureFormat.RGBAHalf, false);
    var fillColorArray = new Color[512 * 512];
    for (int i = 0; i < fillColorArray.Length; i++)
    {
      fillColorArray[i] = Color.white;
    }
    whiteTexture.SetPixels(fillColorArray);
    whiteTexture.Apply();
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
    public bool wrapped;

    public override string ToString()
    {
      return $"{minX[0]} {minX[1]} {maxX[0]} {maxX[1]} {minY} {maxY} {precision}";
    }
  }

  TileRanges? GetTileRanges(int precision)
  {
    Vector2d? botLeftIndices = getCoords(cameraEdges.botLeftIntersection);
    Vector2d? botRightIndices = getCoords(cameraEdges.botRightIntersection);
    Vector2d? topRightIndices = getCoords(cameraEdges.topRightIntersection);
    Vector2d? topLeftIndices = getCoords(cameraEdges.topLeftIntersection);
    Vector2d? topIndices = getCoords(cameraEdges.topIntersection);
    Vector2d? botIndices = getCoords(cameraEdges.botIntersection);

    if (botLeftIndices.HasValue && botRightIndices.HasValue && topRightIndices.HasValue && topLeftIndices.HasValue && topIndices.HasValue && botIndices.HasValue)
    {
      // Debug.Log($"Intersections {cameraEdges.botLeftIntersection} {cameraEdges.topLeftIntersection} {cameraEdges.topRightIntersection}");
      // Debug.Log($"Indices {wrapped} {botLeftIndices.Value.x} {botRightIndices.Value.x} {topRightIndices.Value.x} {topLeftIndices.Value.x}");
      double[] xArray = new double[] { botLeftIndices.Value.x, botRightIndices.Value.x, topRightIndices.Value.x, topLeftIndices.Value.x };
      double[] yArray = new double[] { botLeftIndices.Value.y, botRightIndices.Value.y, topRightIndices.Value.y, topLeftIndices.Value.y, topIndices.Value.y, botIndices.Value.y };
      Array.Sort(xArray);
      Array.Sort(yArray);
      Array.Reverse(yArray);

      int tileSideAmount = (int)Mathf.Pow(2, precision);

      TileRanges tr = new TileRanges();
      tr.precision = precision;
      tr.minX = new int[2];
      tr.minY = getYTile(yArray[0], tileSideAmount);
      tr.maxY = getYTile(yArray[5], tileSideAmount);

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
        tr.minX[0] = getXTile(xArray[0], tileSideAmount);
        tr.maxX[0] = getXTile(xArray[3], tileSideAmount);
      }


      tr.wrapped = false;
      // We wrapped around the origin
      if (!isAPoleVisible && tr.maxX[0] - tr.minX[0] > totalTiles / 2)
      {
        tr.minX[1] = 0;
        tr.maxX[1] = getXTile(xArray.Where(x => x < 0).Last(), tileSideAmount);
        tr.minX[0] = getXTile(xArray.Where(x => x > 0).First(), tileSideAmount);
        tr.maxX[0] = (int)totalTiles - 1;
        tr.wrapped = true;
      }

      // Debug.Log($"{(tr.maxX[0] - tr.minX[0])} {(tr.maxX[1] - tr.minX[1])} {(tr.maxY - tr.minY)} {world.NUMBER_OF_TILES}");
      if ((tr.maxX[0] - tr.minX[0]) >= world.NUMBER_OF_TILES / 4 || (tr.maxX[1] - tr.minX[1]) >= world.NUMBER_OF_TILES / 4 || (tr.maxY - tr.minY) >= world.NUMBER_OF_TILES / 4)
      {
        return GetTileRanges(precision - 1);
      }
      return tr;
    }
    else
    {
      TileRanges tr = new TileRanges();
      tr.precision = world.textureResolution;
      tr.minX = new int[2];
      tr.maxX = new int[2];
      tr.minX[0] = 0;
      tr.maxX[0] = (int)Mathf.Pow(2, tr.precision) - 1;
      tr.minY = 0;
      tr.maxY = (int)Mathf.Pow(2, tr.precision) - 1;
      tr.wrapped = false;
      return tr;
    }
  }

  [Obsolete]
  void Update()
  {
    Vector2d coords = getCoords(cameraEdges.centerIntersection).Value;
    bool somethingChanged = coords.y != previousLatitude || coords.x != previousLongitude || precision != previousPrecision || cameraEdges.botRightIntersection.HasValue != previousBotRightHasValue || world.transform.localScale.x != previousScale;
    previousBotRightHasValue = cameraEdges.botRightIntersection.HasValue;
    if (somethingChanged || networkUpdated)
    {

      TileRanges? tr = this.GetTileRanges(precision + 3);
      // Debug.Log($"Ranges {tr.ToString()} { cameraEdges.northPoleIsVisible || cameraEdges.southPoleIsVisible}");

      if (tr.HasValue)
      {
        minX = tr.Value.minX;
        maxX = tr.Value.maxX;
        minY = tr.Value.minY;
        maxY = tr.Value.maxY;
        precision = tr.Value.precision;
        wrapped = tr.Value.wrapped;

        for (int i = 0; i < 2; i++)
        {
          for (int x = minX[i]; x <= maxX[i]; x++)
          {
            for (int y = minY; y <= maxY; y++)
            {
              if (i == 0 || wrapped)
              {
                string hash = $"{precision}/{x}/{y}";
                DownloadImage(hash);
              }
            }
          }
        }
      }
    }
    previousLatitude = coords.y;
    previousLongitude = coords.x;
    previousPrecision = precision;
    previousScale = world.transform.localScale.x;

    if (minX[0] != previousMinX[0] || minX[1] != previousMinX[1] || minY != previousMinY || maxX[0] != previousMaxX[0] || maxX[1] != previousMaxX[1] || maxY != previousMaxY || networkUpdated)
    {
      int textureTileWidth = world.NUMBER_OF_TILES;
      int textureTileHeight = world.NUMBER_OF_TILES;
      // Debug.Log($"Values {minX[0]} {maxX[0]}  {minX[1]} {maxX[1]} {minY} {maxY} {textureTileWidth}");
      var tilesToDisplay = new (Texture2D texture, TilePart part)[textureTileWidth * textureTileHeight];


      for (int x = minX[0]; x <= maxX[0]; x++)
      {
        for (int y = minY; y <= maxY; y++)
        {
          var tile = getTileFromCoords(precision, x, y);
          if (tile != null)
            tilesToDisplay[(x - minX[0]) * textureTileHeight + (y - minY)] = tile.Value;
        }
      }

      if (wrapped)
      {
        for (int x = minX[1]; x <= maxX[1]; x++)
        {
          for (int y = minY; y <= maxY; y++)
          {
            var tile = getTileFromCoords(precision, x, y);
            if (tile != null)
              tilesToDisplay[(x - minX[1] + maxX[0] - minX[0] + 1) * textureTileHeight + (y - minY)] = tile.Value;
          }
        }
      }


      for (int y = 0; y < textureTileWidth * textureTileHeight; y++)
      {
        if (tilesToDisplay[y].texture == null)
        {
          tilesToDisplay[y] = (whiteTexture, TilePart.Default);
        }
      }

      WorldGenerator.Stitch(meshCompute, tilesToDisplay, ref albedoMap, textureTileWidth, textureTileHeight, 512);

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
  (Texture2D texture, TilePart part)? getTileFromCoords(int precision, int x, int y)
  {
    int originalPrecision = precision;
    int originalX = x;
    int originalY = y;
    string originalHash = $"{precision}/{x}/{y}";
    string hash = $"{precision}/{x}/{y}";
    while (!(tiles.ContainsKey(hash) && tiles[hash] != null))
    {
      precision -= 1;
      if (precision < 5)
      {
        return null;
      }
      x /= 2;
      y /= 2;
      hash = $"{precision}/{x}/{y}";
    }
    int precisionDifference = originalPrecision - precision;
    int sideSplitCount = (int)Mathf.Pow(2, precisionDifference);
    return (tiles[hash], new TilePart(sideSplitCount, originalX % sideSplitCount, (sideSplitCount - 1) - (originalY % sideSplitCount)));
  }
}



public struct TilePart
{
  public int sideSplitCount;
  public int x;
  public int y;

  public TilePart(int sideSplitCount, int x, int y)
  {
    this.sideSplitCount = sideSplitCount;
    this.x = x;
    this.y = y;
  }

  public static TilePart Default
  {
    get
    {
      return new TilePart(1, 0, 0);
    }
  }

}


// https://api.maptiler.com/maps/hybrid/static/-122.4271,37.8065,15/512x512.png?key=aQbIOs34kku6WFUUdTnW
