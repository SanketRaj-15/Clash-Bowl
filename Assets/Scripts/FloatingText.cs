// Unity core features
using UnityEngine;

// TextMeshPro for the text component
using TMPro;

// This script makes text float upward and fade away.
// Used for "+1" popups when collecting pickups.
// The object destroys itself after the animation completes.
public class FloatingText : MonoBehaviour
{
    // How fast the text floats upward.
    public float floatSpeed = 2f;

    // How long the text lasts before being destroyed (in seconds).
    public float lifetime = 1f;

    // How fast the text fades out.
    // Calculated automatically from lifetime.
    private float fadeSpeed;

    // Reference to the TextMeshPro component for changing color/alpha.
    private TextMeshPro textMesh;

    // The current color of the text (we'll modify the alpha to fade it).
    private Color textColor;

    void Start()
    {
        // Find the TextMeshPro component on this object.
        textMesh = GetComponent<TextMeshPro>();

        // Get the current text color.
        textColor = textMesh.color;

        // Calculate how fast we need to fade based on lifetime.
        // If lifetime is 1 second, fadeSpeed is 1 (fade fully in 1 second).
        // If lifetime is 2 seconds, fadeSpeed is 0.5 (fade fully in 2 seconds).
        fadeSpeed = 1f / lifetime;

        // Schedule this object to be destroyed after 'lifetime' seconds.
        // Destroy(gameObject, delay) is a built-in Unity function.
        // After the delay, the entire GameObject is removed from the scene.
        // This prevents floating text objects from piling up forever!
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        // Move the text upward every frame.
        // Vector3.up is (0, 1, 0) — straight up.
        // Multiplying by floatSpeed and Time.deltaTime gives smooth,
        // consistent upward movement.
        transform.Translate(Vector3.up * floatSpeed * Time.deltaTime);

        // Fade the text out gradually.
        // We reduce the alpha (transparency) value each frame.
        // Alpha goes from 1 (fully visible) to 0 (invisible).
        textColor.a -= fadeSpeed * Time.deltaTime;

        // Apply the modified color (with reduced alpha) to the text.
        textMesh.color = textColor;
    }
}