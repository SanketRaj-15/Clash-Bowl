using UnityEngine;

// This script makes an object always face the camera.
// Also called a "billboard" effect.
// Used on floating text so it's always readable from any angle.
public class FaceCamera : MonoBehaviour
{
    // Reference to the main camera.
    private Camera mainCamera;

    void Start()
    {
        // Find the main camera in the scene.
        // Camera.main finds the camera tagged as "MainCamera"
        // (which our Main Camera is by default).
        mainCamera = Camera.main;
    }

    void LateUpdate()
    {
        // Make this object face the same direction as the camera.
        // This ensures the text is always readable.
        //
        // transform.forward is the direction this object is facing.
        // mainCamera.transform.forward is the direction the camera 
        // is looking.
        // By setting them equal, the text always faces toward 
        // the camera.
        transform.forward = mainCamera.transform.forward;
    }
}