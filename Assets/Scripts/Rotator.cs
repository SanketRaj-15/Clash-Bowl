// This line lets us use Unity's core features
using UnityEngine;

// This script makes any object it's attached to rotate continuously.
// We'll attach it to our collectible pickup cubes.
public class Rotator : MonoBehaviour
{
    // Update() runs once every frame (about 60 times per second).
    // We use Update here (not FixedUpdate) because this is just
    // a visual effect, not physics.
    void Update()
    {
        // Rotate this object by 45 degrees per second on each axis.
        //
        // transform.Rotate() is a built-in Unity function that
        // rotates an object.
        //
        // The three numbers are rotation speeds for (X, Y, Z) axes:
        //   - 15 degrees per second on the X axis
        //   - 30 degrees per second on the Y axis  
        //   - 45 degrees per second on the Z axis
        //
        // Time.deltaTime makes the rotation smooth and consistent
        // regardless of the computer's speed.
        // Without Time.deltaTime, a fast computer would spin the cube
        // faster than a slow computer!
        //
        // Think of Time.deltaTime as "the time since the last frame."
        // Multiplying by it ensures consistent speed.
        transform.Rotate(
            15 * Time.deltaTime,
            30 * Time.deltaTime,
            45 * Time.deltaTime
        );
    }
}