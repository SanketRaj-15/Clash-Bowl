// Unity core features
using UnityEngine;

// This script makes an object "pulse" — smoothly growing bigger 
// and smaller, like a heartbeat or breathing effect.
// Combined with the Rotator script, pickups will spin AND pulse!
public class PulseEffect : MonoBehaviour
{
    // How much bigger/smaller the object gets from its original size.
    // 0.2 means it grows 20% bigger and 20% smaller.
    // Higher = more dramatic pulsing. Lower = subtle pulsing.
    public float pulseAmount = 0.2f;

    // How fast the pulsing happens.
    // Higher = faster heartbeat. Lower = slow breathing.
    public float pulseSpeed = 2f;

    // The original scale of the object.
    // We save this in Start() so we know what "normal size" is.
    private Vector3 originalScale;

    void Start()
    {
        // Remember the starting size of this object.
        // For our pickups, this is (0.5, 0.5, 0.5).
        originalScale = transform.localScale;
    }

    void Update()
    {
        // Mathf.Sin() creates a wave that goes from -1 to +1 smoothly.
        //
        // Time.time keeps increasing as the game runs.
        // Multiplying by pulseSpeed controls how fast the wave oscillates.
        //
        // Example with pulseSpeed = 2:
        //   Time 0.0:  Sin(0)    = 0.0   → normal size
        //   Time 0.4:  Sin(0.8)  = 0.7   → bigger
        //   Time 0.8:  Sin(1.6)  = 1.0   → maximum size!
        //   Time 1.2:  Sin(2.4)  = 0.7   → getting smaller
        //   Time 1.6:  Sin(3.2)  = 0.0   → back to normal
        //   Time 2.0:  Sin(4.0)  = -0.7  → smaller than normal
        //   Time 2.4:  Sin(4.8)  = -1.0  → minimum size!
        //   Then it cycles back up again...
        //
        // We multiply the wave by pulseAmount to control how much
        // the size actually changes.
        float pulse = Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;

        // Apply the pulse to the scale.
        // Vector3.one is (1, 1, 1).
        // We add the pulse value to all three axes equally
        // so the object grows/shrinks uniformly (not stretched).
        //
        // If originalScale is (0.5, 0.5, 0.5) and pulse is 0.1:
        //   New scale = (0.5, 0.5, 0.5) + (0.1, 0.1, 0.1) × 0.5
        //             = (0.55, 0.55, 0.55) → slightly bigger!
        transform.localScale = originalScale + (Vector3.one * pulse);
    }
}