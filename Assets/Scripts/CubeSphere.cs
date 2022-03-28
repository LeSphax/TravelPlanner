using UnityEngine;
using System.Collections.Generic;

public static class CubeSphere
{

  private static int CFSCallCount = 0;
  private static int CFCallCount = 0;
  private static int SRCallCount = 0;
  private static float getRectangleCount(float proportionOfSphere, int splittingCount, int resolution)
  {
    float totalRectangleCount = 6 * Mathf.Pow(4, splittingCount) * resolution * resolution;
    // Debug.Log($"{splittingCount} {proportionOfSphere} {totalRectangleCount}");
    return totalRectangleCount * proportionOfSphere * proportionOfSphere;
  }

  public static List<MeshData> GenerateMeshes(int resolution, Transform transform, CameraEdges cameraEdges, int targetNumVertices)
  {
    List<MeshData> meshes = new List<MeshData>();
    Vector3d[] faceNormals = { Vector3d.up, Vector3d.down, Vector3d.left, Vector3d.right, Vector3d.forward, Vector3d.back };
    int meshIndex = 0;

    Vector3d?[] viewportCorners = { cameraEdges.topLeftIntersection, cameraEdges.botLeftIntersection, cameraEdges.botRightIntersection, cameraEdges.topRightIntersection };
    Vector3d? viewportCenter = cameraEdges.topLeftIntersection.HasValue && cameraEdges.botRightIntersection.HasValue ?
    (cameraEdges.topLeftIntersection.Value + (cameraEdges.botRightIntersection.Value - cameraEdges.topLeftIntersection.Value) / 2).normalized
    : (Vector3d?)null;

    // var x = (long + PI) / (2 * PI);
    // var y = (1 - Math.log(Math.tan(lat) + 1 / Math.cos(lat)) / 2 * PI) * Math.pow(2, 0);
    CFSCallCount = 0;
    CFCallCount = 0;
    SRCallCount = 0;
    int splittingCount = 1;
    if (cameraEdges.topRightIntersection.HasValue && cameraEdges.botLeftIntersection.HasValue)
    {
      double topRightLongitude = Mathd.Atan2(cameraEdges.topRightIntersection.Value.x, -cameraEdges.topRightIntersection.Value.z);
      double botLeftLongitude = Mathd.Atan2(cameraEdges.botLeftIntersection.Value.x, -cameraEdges.botLeftIntersection.Value.z);
      double longitudeProportion = Mathd.Abs(botLeftLongitude - topRightLongitude) / (2 * Mathf.PI);

      double topRightLatitude = Mathd.Asin(cameraEdges.topRightIntersection.Value.y);
      double botLeftLatitude = Mathd.Asin(cameraEdges.botLeftIntersection.Value.y);
      double latitudeProportion = Mathd.Abs(botLeftLatitude - topRightLatitude) / Mathf.PI;

      float proportionOfSphere = (float)Mathd.Max(longitudeProportion, latitudeProportion);

      float recCount = getRectangleCount(proportionOfSphere, splittingCount, resolution);
      while (recCount < targetNumVertices)
      {
        splittingCount++;
        recCount = getRectangleCount(proportionOfSphere, splittingCount, resolution);
      }
    }


    foreach (Vector3d faceNormal in faceNormals)
    {
      Vector3d axisA = new Vector3d(faceNormal.y, faceNormal.z, faceNormal.x);
      Vector3d axisB = Vector3d.Cross(faceNormal, axisA);
      meshes.AddRange(CreateFaces(resolution, faceNormal, axisA, axisB, splittingCount, viewportCorners, viewportCenter, false, transform));
      meshIndex++;
    }
    // Debug.Log($"{splittingCount}  Call count {SRCallCount} {CFCallCount} {CFSCallCount}");

    return meshes;
  }

  static bool shouldRender(Vector3d normal, Vector3d axisA, Vector3d axisB, Vector3d?[] viewportCorners, Vector3d? viewportCenter, bool log = false)
  {
    SRCallCount += 1;
    Vector3d[] corners = {
      PointOnCubeToPointOnSphere(normal + axisA * 1.1 + axisB * 1.1),
      PointOnCubeToPointOnSphere(normal - axisA * 1.1 + axisB * 1.1),
      PointOnCubeToPointOnSphere(normal + axisA * 1.1 - axisB * 1.1),
      PointOnCubeToPointOnSphere(normal - axisA * 1.1 - axisB * 1.1)
    };
    Vector3d sphereNormal = PointOnCubeToPointOnSphere(normal);
    // Dot product of normalized vectors is higher when they are more aligned.
    // If a position is more aligned than the corner of the face then it's inside.
    // I.E Its dot product is higher than the dot product of the corner.
    double threshold = Vector3d.Dot(sphereNormal, corners[0].normalized);
    // if (log)
    //   Debug.Log($"Thresh {threshold} {sphereNormal} {corners[0].normalized}");
    bool viewportHasOneCornerInside = false;
    foreach (Vector3d? coord in viewportCorners)
    {
      if (!coord.HasValue)
      {
        viewportHasOneCornerInside = true;
        break;
      }
      else
      {
        bool isCornerInside = Vector3d.Dot(sphereNormal, coord.Value.normalized) > threshold;

        if (isCornerInside)
        {
          if (log)
          {
            Debug.Log($"Thresh {threshold} {sphereNormal} {corners[0].normalized}");
            Debug.Log($"VCornerInside {sphereNormal} {coord.Value.normalized} {Vector3d.Dot(sphereNormal, coord.Value.normalized)} {isCornerInside}");
          }
          viewportHasOneCornerInside = true;
        }
      }
    }

    threshold = viewportCorners[0].HasValue && viewportCenter.HasValue ? Vector3d.Dot(viewportCenter.Value, viewportCorners[0].Value.normalized) : Mathd.Infinity;
    bool faceHasOneCornerInside = false || !viewportCenter.HasValue;
    // if (log)
    //   Debug.Log($"Thresh {threshold} {viewportCenter.Value} {viewportCorners[0].Value.normalized}");
    if (viewportCenter.HasValue)
    {
      foreach (Vector3d? coord in corners)
      {
        if (!coord.HasValue)
        {
          faceHasOneCornerInside = true;
          break;
        }
        else
        {
          // if (normal.x > 0)
          //   Debug.Log("Is viewport corner in face " + viewportCenter.Value + "   " + coord.Value + "   " + Vector3.Dot(viewportCenter.Value, coord.Value.normalized) + "   " + viewportCorners[0].Value.normalized + "   " + threshold);
          bool isCornerInside = Vector3d.Dot(viewportCenter.Value, coord.Value.normalized) > threshold;
          // if (log)
          //   Debug.Log($"CornerInside {viewportCenter.Value} {coord.Value.normalized} {Vector3d.Dot(viewportCenter.Value, coord.Value.normalized)} {isCornerInside}");
          if (isCornerInside)
          {
            faceHasOneCornerInside = true;
            break;
          }
        }
      }
    }
    // if (faceHasOneCornerInside || viewportHasOneCornerInside)
    //   Debug.Log($"{faceHasOneCornerInside} || {viewportHasOneCornerInside}");
    return faceHasOneCornerInside || viewportHasOneCornerInside;
  }

  static List<MeshData> CreateFaces(int resolution, Vector3d normal, Vector3d axisA, Vector3d axisB, int level, Vector3d?[] viewportCorners, Vector3d? viewportCenter, bool draw, Transform transform)
  {
    CFSCallCount += 1;

    if (!shouldRender(normal, axisA, axisB, viewportCorners, viewportCenter))
    {
      // Debug.Log($"Don't Render {level}");
      return new List<MeshData>();
    }
    // if (level == 3)
    // {
    //   Vector3d[] corners = {
    //     PointOnCubeToPointOnSphere(normal + axisA + axisB),
    //     PointOnCubeToPointOnSphere(normal - axisA + axisB),
    //     PointOnCubeToPointOnSphere(normal + axisA - axisB),
    //     PointOnCubeToPointOnSphere(normal - axisA - axisB)
    //   };

    //   foreach (var corner in corners)
    //   {
    //     Debug.Log(corner + "   " + transform.TransformPoint((Vector3)corner) + "    " + ((Vector3)corner - transform.position));
    //     Debug.DrawLine(transform.position, transform.TransformPoint((Vector3)corner).normalized * 100, Color.red, 0.1f);
    //   }

    //   Debug.DrawLine(transform.position, transform.TransformPoint((Vector3)PointOnCubeToPointOnSphere(normal)).normalized, Color.magenta, 0.1f);

    // }
    var result = new List<MeshData>();
    if (level > 1)
    {
      // Debug.Log($"Render sub level {level}");
      // if (normal.x > 0)
      //   Debug.Log(normal + "    " + faceHasOneCornerInside + "  " + viewportHasOneCornerInside);

      var newAxisA = axisA / 2;
      var newAxisB = axisB / 2;

      foreach (int x in new int[] { 1, -1 })
      {
        foreach (int y in new int[] { 1, -1 })
        {
          var newNormal = normal + x * newAxisA + y * newAxisB;
          result.AddRange(CreateFaces(resolution, newNormal, newAxisA, newAxisB, level - 1, viewportCorners, viewportCenter, draw, transform));
        }
      }
      return result;
    }
    else
    {
      return new List<MeshData> { CreateFace(resolution, normal, axisA, axisB) };
    }
  }


  static MeshData CreateFace(int resolution, Vector3d normal, Vector3d axisA, Vector3d axisB)
  {
    CFCallCount += 1;
    // Debug.Log("Create Face" + normal + "  " + axisA + "   " + axisB);
    int numVerts = resolution * resolution;
    int numTris = (resolution - 1) * (resolution - 1) * 6;

    int triIndex = 0;

    MeshData meshData = new MeshData(numVerts, numTris);
    float ty = 0;
    float dx = 1.0f / (resolution - 1);
    float dy = 1.0f / (resolution - 1);

    for (int y = 0; y < resolution; y++)
    {
      float tx = 0;

      for (int x = 0; x < resolution; x++)
      {
        int i = x + y * resolution;
        Vector3d pointOnUnitCube = normal + (tx - 0.5f) * 2 * axisA + (ty - 0.5f) * 2 * axisB;
        Vector3d pointOnUnitSphere = PointOnCubeToPointOnSphere(pointOnUnitCube);

        meshData.vertices[i] = (Vector3)pointOnUnitSphere;

        if (x != resolution - 1 && y != resolution - 1)
        {
          meshData.triangles[triIndex] = i;
          meshData.triangles[triIndex + 1] = i + resolution + 1;
          meshData.triangles[triIndex + 2] = i + resolution;

          meshData.triangles[triIndex + 3] = i;
          meshData.triangles[triIndex + 4] = i + 1;
          meshData.triangles[triIndex + 5] = i + resolution + 1;
          triIndex += 6;
        }
        tx += dx;
      }
      ty += dy;
    }
    return meshData;
  }

  // From http://mathproofs.blogspot.com/2005/07/mapping-cube-to-sphere.html
  static Vector3d PointOnCubeToPointOnSphere(Vector3d p)
  {
    double x2 = p.x * p.x / 2;
    double y2 = p.y * p.y / 2;
    double z2 = p.z * p.z / 2;
    double x = p.x * Mathd.Sqrt(1 - y2 - z2 + (p.y * p.y * p.z * p.z) / 3);
    double y = p.y * Mathd.Sqrt(1 - z2 - x2 + (p.x * p.x * p.z * p.z) / 3);
    double z = p.z * Mathd.Sqrt(1 - x2 - y2 + (p.x * p.x * p.y * p.y) / 3);
    return new Vector3d(x, y, z);

  }

  public struct MeshData
  {
    public readonly Vector3[] vertices;
    public readonly int[] triangles;

    public MeshData(int numVerts, int numTris)
    {
      vertices = new Vector3[numVerts];
      triangles = new int[numTris];
    }

    public override string ToString()
    {
      return string.Format("MeshData: {0} vertices, {1} triangles", vertices.Length, triangles.Length);
    }
  }
}
