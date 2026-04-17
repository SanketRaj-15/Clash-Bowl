using UnityEngine;

// This script adds a screen shake effect to the camera.
// Other scripts can call ScreenShake.Shake() to trigger it.
// The shake is subtle and short — just enough to feel impactful!
public class ScreenShake : MonoBehaviour
{
    // A static reference to this script so other scripts can 
    // access it easily without needing a direct reference.
    // "static" means there's only ONE instance shared across 
    // the entire game.
    //
    // Other scripts can call: ScreenShake.instance.TriggerShake()
    public static ScreenShake instance;

    // How long the shake lasts (in seconds).
    private float shakeDuration = 0f;

    // How intense the shake is (how far the camera moves).
    private float shakeMagnitude = 0.1f;

    // The camera's original position (we need this to return 
    // the camera to its correct position after shaking).
    private Vector3 originalPosition;

    // Whether we're currently shaking.
    private bool isShaking = false;

    void Awake()
    {
        // Set the static instance to this script.
        // Now any script can access ScreenShake.instance
        instance = this;
    }

    void Update()
    {
        // If we're currently shaking...
        if (isShaking)
        {
            // Reduce the remaining shake duration.
            shakeDuration -= Time.deltaTime;

            if (shakeDuration > 0)
            {
                // Generate a random offset for the camera position.
                // Random.insideUnitSphere gives a random point inside 
                // a sphere of radius 1.
                // We multiply by shakeMagnitude to control how far 
                // the camera moves.
                Vector3 shakeOffset = Random.insideUnitSphere * shakeMagnitude;

                // Apply the shake offset to the camera's LOCAL position.
                // We only shake X and Y (not Z — we don't want the 
                // camera to move forward/backward).
                transform.localPosition = new Vector3(
                    shakeOffset.x,
                    shakeOffset.y,
                    0f
                );
            }
            else
            {
                // Shake is over! Reset the camera's local position.
                isShaking = false;
                transform.localPosition = Vector3.zero;
            }
        }
    }

    // Public function that other scripts can call to trigger a shake.
    //
    // Parameters:
    //   duration = how long the shake lasts (seconds)
    //   magnitude = how intense the shake is (distance)
    public void TriggerShake(float duration = 0.15f, float magnitude = 0.1f)
    {
        shakeDuration = duration;
        shakeMagnitude = magnitude;
        isShaking = true;
    }
}