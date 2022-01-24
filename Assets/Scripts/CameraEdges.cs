using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class CameraEdges : MonoBehaviour
{
  private Transform globeTransform;

  public float minLatitude = -Mathf.Infinity;
  public float maxLatitude = Mathf.Infinity;
  public float minLongitude = -Mathf.Infinity;
  public float maxLongitude = Mathf.Infinity;

  Vector3? getIntersection(Vector3 o, Vector3 d, float r)
  {
    // Debug.Log(o);
    // Debug.Log(d);
    // Debug.Log(r);
    // Equation for the sphere X*X + Y*Y + Z*Z = R*R
    // Equations for the line (X,Y,Z) = o + T * d

    // Replacing the X,Y,Z in the first equation gives us a quadratic equation with the following solution:
    float a = d.x * d.x + d.y * d.y + d.z * d.z;
    float b = 2 * (o.x * d.x + o.y * d.y + o.z * d.z);
    float c = o.x * o.x + o.y * o.y + o.z * o.z - r * r;

    // Discriminant of the quadratic equation
    float delta = b * b - 4 * a * c;
    if (delta < 0) return null;

    float deltaRoot = Mathf.Sqrt(delta);
    // We only care about the closest point and deltaRoot is positive so we take the smaller T 
    float T = (-b - deltaRoot) / 2 * a;
    float T2 = (-b + deltaRoot) / 2 * a;

    Vector3 intersection = o + T * d;
    Vector3 intersection2 = o + T2 * d;
    Debug.DrawLine(globeTransform.position, globeTransform.position + intersection, Color.green, 0.1f);
    Debug.DrawLine(globeTransform.position, globeTransform.position + intersection2, Color.yellow, 0.1f);
    Vector3 normalizedIntersection = intersection.normalized;
    // float longitude_rad = Mathf.Atan2(normalizedIntersection.x, -normalizedIntersection.z);
    // float latitude_rad = -Mathf.Asin(normalizedIntersection.y);
    return intersection;
  }

  void getLimits()
  {
    Ray topLeft = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0, 0));
    Ray topRight = Camera.main.ViewportPointToRay(new Vector3(1, 0.5f, 0));
    Ray botRight = Camera.main.ViewportPointToRay(new Vector3(0.5f, 1, 0));
    Ray botLeft = Camera.main.ViewportPointToRay(new Vector3(0, 0.5f, 0));
    Debug.DrawRay(topLeft.origin, 10 * topLeft.direction, Color.red, 0.1f);
    Debug.DrawRay(topRight.origin, 10 * topRight.direction, Color.red, 0.1f);
    Debug.DrawRay(botRight.origin, 10 * botRight.direction, Color.red, 0.1f);
    Debug.DrawRay(botLeft.origin, 10 * botLeft.direction, Color.red, 0.1f);

    float radius = globeTransform.localScale.x;

    Vector2? topLeftIntersection = getIntersection(topLeft.origin - globeTransform.position, topLeft.direction, radius);
    Vector2? topRightIntersection = getIntersection(topRight.origin - globeTransform.position, topRight.direction, radius);
    Vector2? botRightIntersection = getIntersection(botRight.origin - globeTransform.position, botRight.direction, radius);
    Vector2? botLeftIntersection = getIntersection(botLeft.origin - globeTransform.position, botLeft.direction, radius);

    minLatitude = botRightIntersection.HasValue ? 1.5f * botRightIntersection.Value.y : -Mathf.Infinity;
    maxLatitude = topLeftIntersection.HasValue ? 1.5f * topLeftIntersection.Value.y : Mathf.Infinity;
    minLongitude = botLeftIntersection.HasValue ? 1.5f * botLeftIntersection.Value.x : -Mathf.Infinity;
    maxLongitude = topRightIntersection.HasValue ? 1.5f * topRightIntersection.Value.x : Mathf.Infinity;

  }

  void Start()
  {
    globeTransform = GameObject.FindGameObjectWithTag(Tags.WORLD).transform;
    getLimits();
  }


  void Update()
  {
    getLimits();
  }
}
