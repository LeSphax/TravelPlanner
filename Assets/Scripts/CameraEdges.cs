using UnityEngine;

public class CameraEdges : MonoBehaviour
{
  private Transform globeTransform;

  public float minLatitude = -Mathf.Infinity;
  public float maxLatitude = Mathf.Infinity;
  public float minLongitude = -Mathf.Infinity;
  public float maxLongitude = Mathf.Infinity;


  public Vector3d? topIntersection;
  public Vector3d? topLeftIntersection;
  public Vector3d? topRightIntersection;
  public Vector3d? botIntersection;
  public Vector3d? botRightIntersection;
  public Vector3d? botLeftIntersection;
  public Vector3d? centerIntersection;


  public bool southPoleIsVisible = false;
  public bool northPoleIsVisible = false;
  (double w, double x, double y, double z) QuaternionMult(double w, double x, double y, double z, double w2, double x2, double y2, double z2)
  {
    return (w2 * w - x2 * x - y2 * y - z2 * z,
            w2 * x + x2 * w - y2 * z + z2 * y,
            w2 * y + x2 * z + y2 * w - z2 * x,
            w2 * z - x2 * y + y2 * x + z2 * w);
  }

  Vector3d? getIntersection(Vector3d o, Vector3d d, float r, Matrix4x4 globeWorldToLocal, Color color, bool log = false, bool draw = true)
  {
    // Equation for the sphere X*X + Y*Y + Z*Z = R*R
    // Equations for the line (X,Y,Z) = o + T * d

    // Replacing the X,Y,Z in the first equation gives us a quadratic equation with the following solution:
    double a = d.x * d.x + d.y * d.y + d.z * d.z;
    double b = 2 * (o.x * d.x + o.y * d.y + o.z * d.z);
    double c = o.x * o.x + o.y * o.y + o.z * o.z - r * r;

    // Discriminant of the quadratic equation
    double delta = b * b - 4 * a * c;
    if (delta < 0) return null;

    double deltaRoot = Mathd.Sqrt(delta);
    // We only care about the closest point and deltaRoot is positive so we take the smaller T 
    double T = (-b - deltaRoot) / 2 * a;
    double T2 = (-b + deltaRoot) / 2 * a;

    Vector3d intersection = o + T * d;
    Vector3d i = intersection.normalized;

    // TODO: Create a double Quaternion.Inverse function for more precision.
    Quaternion inverseRotation = Quaternion.Inverse(globeTransform.localRotation);
    Quaternion conjugate = new Quaternion(-inverseRotation.x, -inverseRotation.y, -inverseRotation.z, inverseRotation.w);
    var mult1 = QuaternionMult(inverseRotation.w, inverseRotation.x, inverseRotation.y, inverseRotation.z, 0, i.x, i.y, i.z);
    var mult2 = QuaternionMult(mult1.w, mult1.x, mult1.y, mult1.z, conjugate.w, conjugate.x, conjugate.y, conjugate.z);
    Vector3d normalizedIntersection = new Vector3d(mult2.x, mult2.y, mult2.z);
    Vector3d normalizedIntersection2 = new Vector3d(Quaternion.Inverse(globeTransform.localRotation) * (Vector3)intersection.normalized);

    if (draw)
    {
      Debug.DrawLine(globeTransform.position, globeTransform.position + (Vector3)intersection.normalized * 100, color, 0.1f);
      // Debug.DrawLine((Vector3)o, globeTransform.position + (Vector3)intersection, color, 0.1f);
    }
    // Debug.DrawLine(globeTransform.position, globeTransform.position + normalizedIntersection * 10, color, 0.1f);
    double longitude_rad = Mathd.Atan2(normalizedIntersection.x, -normalizedIntersection.z);
    double latitude_rad = Mathd.Asin(normalizedIntersection.y);
    if (log)
      Debug.Log(latitude_rad + "  " + normalizedIntersection + "   " + intersection);
    return normalizedIntersection;
  }

  void getLimits()
  {
    Ray top = Camera.main.ViewportPointToRay(new Vector3(0.5f, 1, 0));
    Ray topLeft = Camera.main.ViewportPointToRay(new Vector3(0, 1, 0));
    Ray topRight = Camera.main.ViewportPointToRay(new Vector3(1, 1, 0));
    Ray bot = Camera.main.ViewportPointToRay(new Vector3(1, 0, 0));
    Ray botRight = Camera.main.ViewportPointToRay(new Vector3(1, 0, 0));
    Ray botLeft = Camera.main.ViewportPointToRay(new Vector3(0, 0, 0));
    Ray center = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
    float radius = globeTransform.localScale.x;

    topIntersection = getIntersection(new Vector3d(top.origin - globeTransform.position), new Vector3d(top.direction), radius, globeTransform.worldToLocalMatrix, Color.green, false, false);
    topLeftIntersection = getIntersection(new Vector3d(topLeft.origin - globeTransform.position), new Vector3d(topLeft.direction), radius, globeTransform.worldToLocalMatrix, Color.green);
    topRightIntersection = getIntersection(new Vector3d(topRight.origin - globeTransform.position), new Vector3d(topRight.direction), radius, globeTransform.worldToLocalMatrix, Color.yellow);
    botIntersection = getIntersection(new Vector3d(bot.origin - globeTransform.position), new Vector3d(bot.direction), radius, globeTransform.worldToLocalMatrix, Color.blue, false, false);
    botRightIntersection = getIntersection(new Vector3d(botRight.origin - globeTransform.position), new Vector3d(botRight.direction), radius, globeTransform.worldToLocalMatrix, Color.blue);
    botLeftIntersection = getIntersection(new Vector3d(botLeft.origin - globeTransform.position), new Vector3d(botLeft.direction), radius, globeTransform.worldToLocalMatrix, Color.cyan);
    centerIntersection = getIntersection(new Vector3d(center.origin - globeTransform.position), new Vector3d(center.direction), radius, globeTransform.worldToLocalMatrix, Color.grey);



    // minLatitude = Mathf.Min(
    //   topLeftIntersection.HasValue ? topLeftIntersection.Value.y : -Mathf.Infinity,
    //   topRightIntersection.HasValue ? topRightIntersection.Value.y : -Mathf.Infinity,
    //   botRightIntersection.HasValue ? botRightIntersection.Value.y : -Mathf.Infinity,
    //   botLeftIntersection.HasValue ? botLeftIntersection.Value.y : -Mathf.Infinity
    // );

    // maxLatitude = Mathf.Max(
    //   topLeftIntersection.HasValue ? topLeftIntersection.Value.y : Mathf.Infinity,
    //   topRightIntersection.HasValue ? topRightIntersection.Value.y : Mathf.Infinity,
    //   botRightIntersection.HasValue ? botRightIntersection.Value.y : Mathf.Infinity,
    //   botLeftIntersection.HasValue ? botLeftIntersection.Value.y : Mathf.Infinity
    // );

    // minLongitude = Mathf.Min(
    //   topLeftIntersection.HasValue ? topLeftIntersection.Value.x : -Mathf.Infinity,
    //   topRightIntersection.HasValue ? topRightIntersection.Value.x : -Mathf.Infinity,
    //   botRightIntersection.HasValue ? botRightIntersection.Value.x : -Mathf.Infinity,
    //   botLeftIntersection.HasValue ? botLeftIntersection.Value.x : -Mathf.Infinity
    // );

    // maxLongitude = Mathf.Max(
    //   topLeftIntersection.HasValue ? topLeftIntersection.Value.x : Mathf.Infinity,
    //   topRightIntersection.HasValue ? topRightIntersection.Value.x : Mathf.Infinity,
    //   botRightIntersection.HasValue ? botRightIntersection.Value.x : Mathf.Infinity,
    //   botLeftIntersection.HasValue ? botLeftIntersection.Value.x : Mathf.Infinity
    // );
  }

  bool isPoleVisible(Vector3 poleDirection)
  {
    float r = globeTransform.localScale.x;
    Vector3d polePosition = new Vector3d(globeTransform.localToWorldMatrix * poleDirection);
    Vector3d cameraPosition = new Vector3d(Camera.main.transform.position - globeTransform.position);

    Vector3d o = cameraPosition;
    Vector3d d = (polePosition - cameraPosition).normalized;

    double a = d.x * d.x + d.y * d.y + d.z * d.z;
    double b = 2 * (o.x * d.x + o.y * d.y + o.z * d.z);
    double c = o.x * o.x + o.y * o.y + o.z * o.z - r * r;

    // Discriminant of the quadratic equation
    double delta = b * b - 4 * a * c;

    if (delta > 0)
    {
      double deltaRoot = Mathd.Sqrt(delta);
      // We only care about the closest point and deltaRoot is positive so we take the smaller T 
      double T = (-b - deltaRoot) / 2 * a;
      double T2 = (-b + deltaRoot) / 2 * a;

      Vector3d intersection = o + T * d;
      Vector3d intersection2 = o + T2 * d;
      // Debug.Log($"Intersection {polePosition} {Vector3d.Distance(intersection, polePosition)} {Vector3d.Distance(intersection, polePosition)}");
      if (Vector3d.Distance(intersection, polePosition) < 0.0001f)
      {
        return true;
        // Debug.Log($"Pole is visible ! {Vector3.Distance(intersection, polePosition)} {Vector3.Distance(intersection2, polePosition)}");
      }
      else
      {
        // Debug.DrawLine(globeTransform.position, polePosition + globeTransform.position, Color.yellow, 0.1f);
        // Debug.DrawLine(globeTransform.position, globeTransform.position + intersection, Color.red, 0.1f);
        // Debug.DrawLine(globeTransform.position, globeTransform.position + intersection2, Color.blue, 0.1f);
        // Debug.Log($"Pole is not visible ! {intersection} {intersection2} {polePosition} {Vector3.Distance(intersection, polePosition)} {Vector3.Distance(intersection2, polePosition)}");
        return false;
      }
    }
    else
    {
      Debug.LogWarning("The pole ray should always intersect the sphere !");
      return false;
    }
  }

  void Start()
  {
    globeTransform = GameObject.FindGameObjectWithTag(Tags.WORLD).transform;
  }


  void Update()
  {
    if (FindObjectOfType<WorldGenerator>().initialized)
    {
      getLimits();
      southPoleIsVisible = isPoleVisible(Vector3.down);
      northPoleIsVisible = isPoleVisible(Vector3.up);
      // if (botLeftIntersection.HasValue)
      //   Debug.Log($"{botLeftIntersection} {Mathd.Atan2(botLeftIntersection.Value.x, -botLeftIntersection.Value.z) * Mathf.Rad2Deg} {Mathd.Asin(botLeftIntersection.Value.y) * Mathf.Rad2Deg}");
    }
  }
}
