using UnityEngine;
using TMPro;

// =============================================================
// LevelLabelFade.cs
// Shows the level label at the start, then fades it out
// and hides it after a set duration.
// Attach this to the LevelLabel GameObject.
// =============================================================
public class LevelLabelFade : MonoBehaviour
{
    // =====================================================
    // PUBLIC VARIABLES (editable in Inspector)
    // =====================================================

    // How long the label stays fully visible (in seconds).
    // After this time, it starts fading out.
    public float displayDuration = 1.5f;

    // How long the fade-out animation takes (in seconds).
    // 0.5 = quick fade, 1.0 = slow fade.
    public float fadeDuration = 0.5f;

    // Optional: Should the label scale up when it first appears?
    // This creates a nice "pop in" effect.
    public bool useScaleAnimation = true;

    // How fast the scale animation plays.
    // Higher = faster pop-in.
    public float scaleSpeed = 5f;

    // =====================================================
    // PRIVATE VARIABLES
    // =====================================================

    // Reference to the TextMeshPro component on this object.
    private TextMeshProUGUI labelText;

    // Reference to the CanvasGroup (handles alpha/transparency).
    private CanvasGroup canvasGroup;

    // Timer tracking how long the label has been visible.
    private float timer;

    // Whether we're currently in the fading-out phase.
    private bool isFading;

    // Whether the whole process is complete (label fully hidden).
    private bool isComplete;

    // The starting scale for the pop-in animation.
    private Vector3 targetScale;

    // =====================================================
    // LIFECYCLE FUNCTIONS
    // =====================================================

    void Start()
    {
        Debug.Log("[LevelLabelFade] Start() called on: " + gameObject.name);

        // Get the TextMeshPro component.
        labelText = GetComponent<TextMeshProUGUI>();
        Debug.Log("[LevelLabelFade] TextMeshProUGUI: " + (labelText != null ? "Found" : "NULL"));

        // Get or add a CanvasGroup component.
        // CanvasGroup lets us fade the ENTIRE label (text + background)
        // by changing a single "alpha" value from 1 (visible) to 0 (invisible).
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            Debug.Log("[LevelLabelFade] CanvasGroup added automatically");
        }
        else
        {
            Debug.Log("[LevelLabelFade] CanvasGroup: Found");
        }

        // Initialize state.
        timer = 0f;
        isFading = false;
        isComplete = false;
        Debug.Log("[LevelLabelFade] State initialized — displayDuration: " + displayDuration + " | fadeDuration: " + fadeDuration);

        // Make sure the label is fully visible at the start.
        canvasGroup.alpha = 1f;
        gameObject.SetActive(true);
        Debug.Log("[LevelLabelFade] Label set to fully visible");

        // Store the target scale and start small for pop-in effect.
        targetScale = transform.localScale;
        if (useScaleAnimation)
        {
            transform.localScale = Vector3.zero;
            Debug.Log("[LevelLabelFade] Scale animation enabled — starting from zero");
        }
    }

    void Update()
    {
        // If the process is already complete, do nothing.
        if (isComplete) return;

        // =============================================
        // PHASE 1: POP-IN ANIMATION (optional)
        // =============================================
        if (useScaleAnimation && transform.localScale != targetScale)
        {
            // Smoothly scale up from 0 to target scale.
            transform.localScale = Vector3.Lerp(
                transform.localScale,
                targetScale,
                Time.deltaTime * scaleSpeed
            );
            Debug.Log("[LevelLabelFade] Scaling up: " + transform.localScale);

            // Snap to target when close enough to prevent endless lerping.
            if (Vector3.Distance(transform.localScale, targetScale) < 0.01f)
            {
                transform.localScale = targetScale;
                Debug.Log("[LevelLabelFade] Scale animation complete");
            }
        }

        // =============================================
        // PHASE 2: DISPLAY (stay visible)
        // =============================================
        if (!isFading)
        {
            // Count up the timer.
            timer += Time.deltaTime;

            // When display duration is reached, start fading.
            if (timer >= displayDuration)
            {
                isFading = true;
                timer = 0f;
                Debug.Log("[LevelLabelFade] Display time finished — starting fade out");
            }
        }

        // =============================================
        // PHASE 3: FADE OUT
        // =============================================
        if (isFading)
        {
            // Count up the timer.
            timer += Time.deltaTime;

            // Calculate fade progress (0 = just started, 1 = fully faded).
            float fadeProgress = timer / fadeDuration;
            Debug.Log("[LevelLabelFade] Fading out — progress: " + fadeProgress.ToString("F2"));

            // Set the alpha (transparency).
            // 1 - fadeProgress makes it go from 1 (visible) to 0 (invisible).
            canvasGroup.alpha = 1f - fadeProgress;

            // Optional: Also scale down slightly while fading.
            if (useScaleAnimation)
            {
                float scaleMultiplier = 1f - (fadeProgress * 0.2f);
                transform.localScale = targetScale * scaleMultiplier;
            }

            // When fade is complete, hide the object entirely.
            if (fadeProgress >= 1f)
            {
                canvasGroup.alpha = 0f;
                gameObject.SetActive(false);
                isComplete = true;
                Debug.Log("[LevelLabelFade] Fade complete — label hidden");
            }
        }
    }
}