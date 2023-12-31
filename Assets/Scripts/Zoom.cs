using UnityEngine;

public class Zoom : MonoBehaviour
{
  public Transform earthTransform;

  public float rotationSpeed = 100;
  private Vector3 previousRightPosition;
  private Vector3 previousLeftPosition;
  private Vector3 previousMousePosition;
  // Start is called before the first frame update
  void Awake()
  {
    call();
  }

  void rotateEarth(Vector3 position, Vector3 previousPosition)
  {
    Vector3 delta = position - previousPosition;
    // Debug.Log(position.ToString("F4") + "   " + previousPosition.ToString("F4") + "    "  + delta.ToString("F4"));
    earthTransform.RotateAround(earthTransform.position, Vector3.up, -delta.x * rotationSpeed / earthTransform.localScale.x);
    earthTransform.RotateAround(earthTransform.position, Vector3.right, delta.y * rotationSpeed / earthTransform.localScale.x);
  }

  void call()
  {
    bool leftPressed = Input.GetAxis("Oculus_CrossPlatform_PrimaryIndexTrigger") > 0.8;
    bool rightPressed = Input.GetAxis("Oculus_CrossPlatform_SecondaryIndexTrigger") > 0.8;
    Vector3 leftPosition = OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch);
    Vector3 rightPosition = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch);

    Vector3 mousePosition = Input.mousePosition / 1000;

    if (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(0))
    {
      previousMousePosition = mousePosition;
    }

    if (leftPressed && rightPressed)
    {
      Debug.Log(OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch));
      float previousDistance = Vector3.Distance(previousLeftPosition, previousRightPosition);
      float distance = Vector3.Distance(leftPosition, rightPosition);
      earthTransform.localScale = earthTransform.localScale * (1 - (previousDistance - distance));

      Vector3 previousDirection = previousRightPosition - previousLeftPosition;
      Vector3 projPreviousDirection = Vector3.ProjectOnPlane(previousDirection, Vector3.forward);
      Vector3 direction = rightPosition - leftPosition;
      Vector3 projDirection = Vector3.ProjectOnPlane(direction, Vector3.forward);

      float angle = Vector3.SignedAngle(previousDirection, direction, Vector3.forward);
      earthTransform.RotateAround(earthTransform.position, Vector3.forward, angle);
    }
    else if (leftPressed)
    {
      Debug.Log("Left Pressed");
      rotateEarth(leftPosition, previousLeftPosition);
    }
    else if (rightPressed)
    {
      Debug.Log("Right Pressed");
      rotateEarth(rightPosition, previousRightPosition);
    }
    else if (Input.mouseScrollDelta.y != 0)
    {
      earthTransform.localScale = earthTransform.localScale * (1 + Input.mouseScrollDelta.y * 0.1f);
    }
    else if (Input.GetMouseButton(1) || Input.GetMouseButton(0))
    {
      rotateEarth(mousePosition, previousMousePosition);
      previousMousePosition = mousePosition;
    }

    earthTransform.position = this.GetComponent<Transform>().position + new Vector3(0, 0, (earthTransform.localScale.z) + 5);
    previousLeftPosition = leftPosition;
    previousRightPosition = rightPosition;
  }

  private bool init = false;
  void Update()
  {
    call();
    if (Time.timeSinceLevelLoad > 0.5f && !init)
    {
      // earthTransform.localScale = earthTransform.localScale * 50.0f;
      init = true;
    }
  }
}
