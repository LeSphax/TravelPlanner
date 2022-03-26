using UnityEngine;
using System.Collections.Generic;

public static class CubeSphere
{

  public static List<MeshData> GenerateMeshes(int resolution, int numSubdivisions, Transform transform, CameraEdges cameraEdges)
  {
    List<MeshData> meshes = new List<MeshData>();
    Vector3[] faceNormals = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };
    float faceCoveragePerSubFace = 1f / numSubdivisions;
    int meshIndex = 0;

    Vector3?[] viewportCorners = { (Vector3?)cameraEdges.topLeftIntersection, (Vector3?)cameraEdges.botLeftIntersection, (Vector3?)cameraEdges.botRightIntersection, (Vector3?)cameraEdges.topRightIntersection };
    Vector3? viewportCenter = cameraEdges.topLeftIntersection.HasValue && cameraEdges.botRightIntersection.HasValue ?
    ((Vector3)cameraEdges.topLeftIntersection.Value + ((Vector3)cameraEdges.botRightIntersection.Value - (Vector3)cameraEdges.topLeftIntersection.Value) / 2).normalized
    : (Vector3?)null;

    // var x = (long + PI) / (2 * PI);
    // var y = (1 - Math.log(Math.tan(lat) + 1 / Math.cos(lat)) / 2 * PI) * Math.pow(2, 0);

    foreach (Vector3 faceNormal in faceNormals)
    {
      Vector3 axisA = new Vector3(faceNormal.y, faceNormal.z, faceNormal.x);
      Vector3 axisB = Vector3.Cross(faceNormal, axisA);
      meshes.AddRange(CreateFaces(resolution, faceNormal, axisA, axisB, numSubdivisions, viewportCorners, viewportCenter, false, transform));
      meshIndex++;
    }

    return meshes;
  }

  static bool shouldRender(Vector3 normal, Vector3 axisA, Vector3 axisB, Vector3?[] viewportCorners, Vector3? viewportCenter)
  {
    Vector3[] corners = {
      PointOnCubeToPointOnSphere(normal + axisA + axisB),
      PointOnCubeToPointOnSphere(normal - axisA + axisB),
      PointOnCubeToPointOnSphere(normal + axisA - axisB),
      PointOnCubeToPointOnSphere(normal - axisA - axisB)
    };
    Vector3 sphereNormal = PointOnCubeToPointOnSphere(normal);
    // Dot product of normalized vectors is higher when they are more aligned.
    // If a position is more aligned than the corner of the face then it's inside.
    // I.E Its dot product is higher than the dot product of the corner.
    float threshold = Vector3.Dot(sphereNormal, corners[0].normalized);
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
        bool isCornerInside = Vector3.Dot(sphereNormal, coord.Value.normalized) > threshold;
        if (isCornerInside)
        {
          viewportHasOneCornerInside = true;
          break;
        }
      }
    }

    threshold = viewportCorners[0].HasValue && viewportCenter.HasValue ? Vector3.Dot(viewportCenter.Value, viewportCorners[0].Value.normalized) : Mathf.Infinity;
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

  static List<MeshData> CreateFaces(int resolution, Vector3 normal, Vector3 axisA, Vector3 axisB, int level, Vector3?[] viewportCorners, Vector3? viewportCenter, bool draw, Transform transform)
  {
    if (!shouldRender(normal, axisA, axisB, viewportCorners, viewportCenter))
    {
      return new List<MeshData>();
    }
    if (draw)
    {
      Vector3[] corners = {
        PointOnCubeToPointOnSphere(normal + axisA + axisB),
        PointOnCubeToPointOnSphere(normal - axisA + axisB),
        PointOnCubeToPointOnSphere(normal + axisA - axisB),
        PointOnCubeToPointOnSphere(normal - axisA - axisB)
      };

      foreach (var corner in corners)
      {
        Debug.Log(corner + "   " + transform.TransformPoint(corner) + "    " + (corner - transform.position));
        Debug.DrawLine(transform.position, transform.TransformPoint(corner), Color.red, 0.1f);
      }

      Debug.DrawLine(transform.position, transform.TransformPoint(PointOnCubeToPointOnSphere(normal)), Color.magenta, 0.1f);

    }
    var result = new List<MeshData>();
    if (level > 1)
    {
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
