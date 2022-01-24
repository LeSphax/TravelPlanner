using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Zoom : MonoBehaviour
{
    public Transform earthTransform;

    public float rotationSpeed = 100;
    private Vector3 previousRightPosition;
    private Vector3 previousLeftPosition; 
    // Start is called before the first frame update
    void Start()
    {
        
    }

    void rotateEarth(Vector3 position, Vector3 previousPosition) {
         Vector3 delta = position - previousPosition;
        Debug.Log(position.ToString("F4") + "   " + previousPosition.ToString("F4") + "    "  + delta.ToString("F4"));
        earthTransform.RotateAround(earthTransform.position, Vector3.up, -delta.x * rotationSpeed / earthTransform.localScale.x);
        earthTransform.RotateAround(earthTransform.position, Vector3.right, delta.y * rotationSpeed / earthTransform.localScale.x);
    }

    // Update is called once per frame
    void Update()
    {
        bool leftPressed = Input.GetAxis("Oculus_CrossPlatform_PrimaryIndexTrigger") > 0.8;
        bool rightPressed = Input.GetAxis("Oculus_CrossPlatform_SecondaryIndexTrigger") > 0.8;
        Vector3 leftPosition = OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch);
        Vector3 rightPosition = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch);

        if (leftPressed && rightPressed) {
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
        } else if (leftPressed) {
            Debug.Log("Left Pressed");
            rotateEarth(leftPosition, previousLeftPosition);
        } else if (rightPressed) {
            Debug.Log("Right Pressed");
            rotateEarth(rightPosition, previousRightPosition);
        } else if (Input.mouseScrollDelta.y != 0) {
            earthTransform.localScale = earthTransform.localScale * (1 + Input.mouseScrollDelta.y * 0.1f);
        }
        
        earthTransform.position = this.GetComponent<Transform>().position + new Vector3(0, 0, (earthTransform.localScale.z) + 5);
        previousLeftPosition = leftPosition;
        previousRightPosition = rightPosition;
    }
}
