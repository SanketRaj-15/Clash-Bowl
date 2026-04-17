// Unity core features
using UnityEngine;

// Input System for keyboard controls
using UnityEngine.InputSystem;

// TextMeshPro for UI text on screen
using TMPro;

// Scene management for restarting the game
using UnityEngine.SceneManagement;

// PlayerController — NOW WITH SOUND + TIMER! 🔊⏱️
// Handles: movement, pickups, score, win, restart, sounds, AND timer.
public class PlayerController : MonoBehaviour
{
    // =====================================================
    // PUBLIC VARIABLES (visible and editable in Inspector)
    // =====================================================

    // How fast the ball moves.
    public float speed = 10f;

    // --- UI References ---

    // Score text in the top-left.
    public TextMeshProUGUI scoreText;

    // Win text in the center (hidden until player wins).
    public GameObject winText;

    // How many pickups exist in the scene.
    public int totalPickups = 12;

    // NEW: Timer text in the top-right.
    // Shows the current elapsed time.
    public TextMeshProUGUI timerText;

    // NEW: Best time text below the timer.
    // Shows the player's fastest completion time.
    public TextMeshProUGUI bestTimeText;

    // --- Sound References ---

    // Sound that plays when collecting a pickup.
    public AudioClip pickupSound;

    // Sound that plays when winning.
    public AudioClip winSound;

    // =====================================================
    // PRIVATE VARIABLES
    // =====================================================

    // Rigidbody for physics-based movement.
    private Rigidbody rb;

    // Stores keyboard input direction.
    private Vector2 movementInput;

    // Current score (how many pickups collected).
    private int score;

    // Whether the game has been won.
    private bool gameWon;

    // AudioSource component (the "speaker").
    private AudioSource audioSource;

    // NEW: How many seconds have passed since the game started.
    // "float" because time has decimal places (like 12.345 seconds).
    // Starts at 0 and counts up every frame.
    private float currentTime;

    // NEW: The player's best (fastest) completion time.
    // Loaded from PlayerPrefs when the game starts.
    private float bestTime;

    // =====================================================
    // UNITY LIFECYCLE FUNCTIONS
    // =====================================================

    // Runs once when the game starts.
    void Start()
    {
        // Get components from this GameObject.
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();

        // Initialize game state.
        score = 0;
        gameWon = false;

        // NEW: Start the timer at 0 seconds.
        currentTime = 0f;

        // Hide the win text.
        winText.SetActive(false);

        // Display initial score.
        UpdateScoreText();

        // NEW: Load the best time from the player's computer.
        //
        // PlayerPrefs.GetFloat("BestTime", 0f) does this:
        //   1. Look for a saved value with the key "BestTime"
        //   2. If it exists, return that saved number
        //   3. If it does NOT exist (first time playing), return 0f
        //
        // The "0f" is the DEFAULT value — what to return if 
        // nothing has been saved yet.
        bestTime = PlayerPrefs.GetFloat("BestTime", 0f);

        // NEW: Display the best time on screen.
        // We call our custom function (defined at the bottom).
        UpdateBestTimeText();

        // NEW: Display the initial timer value (0.00).
        UpdateTimerText();
    }

    // Runs once per frame (about 60 times per second).
    void Update()
    {
        // Check for restart key (R).
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        // NEW: Update the timer ONLY if the game is still going.
        // Once the player wins, we stop the timer so they can
        // see their final time.
        if (!gameWon)
        {
            // Add the time since the last frame to our timer.
            // Time.deltaTime is the number of seconds since the last frame.
            //
            // Example at 60 FPS:
            //   Frame 1: currentTime = 0 + 0.0167 = 0.0167
            //   Frame 2: currentTime = 0.0167 + 0.0167 = 0.0334
            //   Frame 3: currentTime = 0.0334 + 0.0167 = 0.0501
            //   ... after 60 frames (~1 second):
            //   Frame 60: currentTime ≈ 1.00
            //
            // This creates a smooth, accurate stopwatch!
            currentTime += Time.deltaTime;

            // Update the timer display on screen every frame.
            UpdateTimerText();
        }
    }

    // Called by Input System when movement keys are pressed.
    void OnMove(InputValue value)
    {
        movementInput = value.Get<Vector2>();
    }

    // Fixed interval physics updates.
    void FixedUpdate()
    {
        if (!gameWon)
        {
            Vector3 movement = new Vector3(
                movementInput.x,
                0.0f,
                movementInput.y
            );
            rb.AddForce(movement * speed);
        }
    }

    // Called when the ball enters a Trigger collider.
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Pickup"))
        {
            // Hide the pickup.
            other.gameObject.SetActive(false);

            // Increase score.
            score += 1;
            UpdateScoreText();

            // Play pickup sound.
            if (pickupSound != null)
            {
                audioSource.PlayOneShot(pickupSound);
            }

            // Check win condition.
            if (score >= totalPickups)
            {
                // Mark game as won (this also stops the timer in Update).
                gameWon = true;

                // Show win text.
                winText.SetActive(true);

                // Play win sound.
                if (winSound != null)
                {
                    audioSource.PlayOneShot(winSound);
                }

                // NEW: Check if this is a new best time!
                //
                // We save a new best time if:
                //   1. bestTime is 0 (no previous best — first time winning)
                //   OR
                //   2. currentTime is LESS than bestTime (player was faster!)
                //
                // "||" means "OR" in C#.
                if (bestTime == 0f || currentTime < bestTime)
                {
                    // This IS a new best time! Update our variable.
                    bestTime = currentTime;

                    // Save the new best time to the player's computer.
                    //
                    // PlayerPrefs.SetFloat("BestTime", bestTime) does this:
                    //   Saves the number 'bestTime' with the key "BestTime".
                    //   Next time the game starts, we can retrieve it with
                    //   PlayerPrefs.GetFloat("BestTime").
                    //
                    // Think of it like a dictionary:
                    //   Key: "BestTime" → Value: 8.543
                    PlayerPrefs.SetFloat("BestTime", bestTime);

                    // PlayerPrefs.Save() writes the data to the hard drive
                    // immediately. Without this call, the data MIGHT be saved
                    // eventually, but calling Save() guarantees it.
                    // This way, even if the game crashes, the data is safe.
                    PlayerPrefs.Save();

                    // Update the best time display.
                    UpdateBestTimeText();

                    // Print a message to the Console for debugging.
                    Debug.Log("NEW BEST TIME: " + bestTime.ToString("F2") + " seconds!");
                }
                else
                {
                    // Not a new best time.
                    Debug.Log("Your time: " + currentTime.ToString("F2") + 
                              "s | Best: " + bestTime.ToString("F2") + "s");
                }
            }
        }
    }

    // =====================================================
    // CUSTOM FUNCTIONS
    // =====================================================

    // Updates the score text on screen.
    void UpdateScoreText()
    {
        scoreText.text = "Score: " + score.ToString();
    }

    // NEW: Updates the timer text on screen.
    void UpdateTimerText()
    {
        // currentTime.ToString("F2") formats the number to 2 decimal places.
        //
        // Examples:
        //   0.5      → "0.50"
        //   12.3456  → "12.35"
        //   100.1    → "100.10"
        //
        // "F2" means "Fixed-point, 2 decimal places"
        // "F3" would give 3 decimal places, "F1" would give 1, etc.
        timerText.text = "Time: " + currentTime.ToString("F2");
    }

    // NEW: Updates the best time text on screen.
    void UpdateBestTimeText()
    {
        // If bestTime is 0, it means no best time has been recorded yet
        // (the player has never won before). Show "--" instead of "0.00".
        if (bestTime == 0f)
        {
            bestTimeText.text = "Best: --";
        }
        else
        {
            // Show the best time with 2 decimal places, followed by "s" for seconds.
            // Example: "Best: 8.54s"
            bestTimeText.text = "Best: " + bestTime.ToString("F2") + "s";
        }
    }
}