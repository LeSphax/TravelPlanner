using UnityEngine;
using System.Collections.Generic;

public static class CubeSphere
{

  public static List<MeshData> GenerateMeshes(int resolution, int numSubdivisions)
  {
    List<MeshData> meshes = new List<MeshData>();
    Vector3[] faceNormals = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };
    float faceCoveragePerSubFace = 1f / numSubdivisions;
    int meshIndex = 0;

    Vector3?[] viewportCorners = { CameraEdges.topLeftIntersection, CameraEdges.botLeftIntersection, CameraEdges.botRightIntersection, CameraEdges.topRightIntersection };
    Vector3? viewportCenter = CameraEdges.topLeftIntersection.HasValue && CameraEdges.botRightIntersection.HasValue ? CameraEdges.topLeftIntersection.Value + (CameraEdges.botRightIntersection - CameraEdges.topLeftIntersection) / 2 : null;

    foreach (Vector3 faceNormal in faceNormals)
    {
      Vector3 axisA = new Vector3(faceNormal.y, faceNormal.z, faceNormal.x);
      Vector3 axisB = Vector3.Cross(faceNormal, axisA);
      meshes.AddRange(CreateFaces(resolution, faceNormal, axisA, axisB, numSubdivisions, viewportCorners, viewportCenter));
      meshIndex++;
    }

    return meshes;
  }

  static bool shouldRender(Vector3 normal, Vector3 axisA, Vector3 axisB, Vector3?[] viewportCorners, Vector3? viewportCenter)
  {
    Vector3[] corners = { normal + axisA + axisB, normal - axisA + axisB, normal + axisA - axisB, normal - axisA - axisB };
    // Dot product of normalized vectors is higher when they are more aligned.
    // If a position is more aligned than the corner of the face then it's inside.
    // I.E Its dot product is higher than the dot product of the corner.
    float threshold = Vector3.Dot(normal, corners[0].normalized) / 1.02f;
    bool viewportHasOneCornerInside = false;
    foreach (Vector3? coord in viewportCorners)
    {
      if (!coord.HasValue)
      {
        viewportHasOneCornerInside = true;
        break;
      }
      else
      {
        // if (normal.x > 0)
        //   Debug.Log("IsCorner in viewport " + normal + "   " + coord.Value + "   " + Vector3.Dot(normal, coord.Value.normalized) + " T " + corners[0].normalized + "   " + threshold);
        bool isCornerInside = Vector3.Dot(normal, coord.Value.normalized) > threshold;
        if (isCornerInside)
        {
          viewportHasOneCornerInside = true;
          break;
        }
      }
    }

    threshold = viewportCorners[0].HasValue && viewportCenter.HasValue ? Vector3.Dot(viewportCenter.Value, viewportCorners[0].Value.normalized) / 1.02f : Mathf.Infinity;
    bool faceHasOneCornerInside = false || !viewportCenter.HasValue;
    if (viewportCenter.HasValue)
    {
      foreach (Vector3? coord in corners)
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
          bool isCornerInside = Vector3.Dot(viewportCenter.Value, coord.Value.normalized) > threshold;
          if (isCornerInside)
          {
            faceHasOneCornerInside = true;
            break;
          }
        }
      }
    }
    return faceHasOneCornerInside || viewportHasOneCornerInside;
  }

  static List<MeshData> CreateFaces(int resolution, Vector3 normal, Vector3 axisA, Vector3 axisB, int level, Vector3?[] viewportCorners, Vector3? viewportCenter)
  {
    if (!shouldRender(normal, axisA, axisB, viewportCorners, viewportCenter))
    {
      return new List<MeshData>();

    }
    if (level > 1)
    {
      var result = new List<MeshData>();


      // if (normal.x > 0)
      //   Debug.Log(normal + "    " + faceHasOneCornerInside + "  " + viewportHasOneCornerInside);

      var newAxisA = axisA / 2;
      var newAxisB = axisB / 2;

      foreach (int x in new int[] { 1, -1 })
      {
        foreach (int y in new int[] { 1, -1 })
        {
          var newNormal = normal + x * newAxisA + y * newAxisB;
          result.AddRange(CreateFaces(resolution, newNormal, newAxisA, newAxisB, level - 1, viewportCorners, viewportCenter));
        }
      }
      return result;
    }
    else
    {
      return new List<MeshData> { CreateFace(resolution, normal, axisA, axisB) };
    }
  }


  static MeshData CreateFace(int resolution, Vector3 normal, Vector3 axisA, Vector3 axisB)
  {
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
        Vector3 pointOnUnitCube = normal + (tx - 0.5f) * 2 * axisA + (ty - 0.5f) * 2 * axisB;
        Vector3 pointOnUnitSphere = PointOnCubeToPointOnSphere(pointOnUnitCube);

        meshData.vertices[i] = pointOnUnitSphere;

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

  static MeshData CreateFace(int resolution, Vector3 normal, Vector2 startT, Vector2 endT, float minLatitude, float maxLatitude, float minLongitude, float maxLongitude)
  {
    // Debug.Log("Create Face  " + normal);
    int numVerts = resolution * resolution;
    int numTris = (resolution - 1) * (resolution - 1) * 6;
    int triIndex = 0;

    MeshData meshData = new MeshData(numVerts, numTris);

    Vector3 axisA = new Vector3(normal.y, normal.z, normal.x);
    Vector3 axisB = Vector3.Cross(normal, axisA);


    float ty = startT.y;
    float dx = (endT.x - startT.x) / (resolution - 1);
    float dy = (endT.y - startT.y) / (resolution - 1);

    for (int y = 0; y < resolution; y++)
    {
      float tx = startT.x;

      for (int x = 0; x < resolution; x++)
      {
        int i = x + y * resolution;
        Vector3 pointOnUnitCube = normal + (tx - 0.5f) * 2 * axisA + (ty - 0.5f) * 2 * axisB;
        Vector3 pointOnUnitSphere = PointOnCubeToPointOnSphere(pointOnUnitCube);

        // float longitude_rad = Mathf.Atan2(pointOnUnitSphere.x, -pointOnUnitSphere.z);
        // float latitude_rad = Mathf.Asin(pointOnUnitSphere.y);

        // if (pointOnUnitSphere.y < maxLatitude && pointOnUnitSphere.y > minLatitude && pointOnUnitSphere.x < maxLongitude && pointOnUnitSphere.x > minLongitude)
        // {

        meshData.vertices[i] = pointOnUnitSphere;

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
        // }
        tx += dx;
      }
      ty += dy;
    }
    return meshData;
  }

  // From http://mathproofs.blogspot.com/2005/07/mapping-cube-to-sphere.html
  static Vector3 PointOnCubeToPointOnSphere(Vector3 p)
  {
    float x2 = p.x * p.x / 2;
    float y2 = p.y * p.y / 2;
    float z2 = p.z * p.z / 2;
    float x = p.x * Mathf.Sqrt(1 - y2 - z2 + (p.y * p.y * p.z * p.z) / 3);
    float y = p.y * Mathf.Sqrt(1 - z2 - x2 + (p.x * p.x * p.z * p.z) / 3);
    float z = p.z * Mathf.Sqrt(1 - x2 - y2 + (p.x * p.x * p.y * p.y) / 3);
    return new Vector3(x, y, z);

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
