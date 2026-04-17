// Unity core features
using UnityEngine;

// TextMeshPro for UI text
using TMPro;

// Scene management for restarting and loading levels
using UnityEngine.SceneManagement;

// =============================================================
// FIXED MobilePlayerController — PROPER TILT CONTROLS!
// =============================================================
// Fixes:
// 1. Ball no longer goes wrong direction when tilted too far
// 2. Calibrated neutral position (whatever angle you hold = center)
// 3. Dead zone prevents drift when phone is level
// 4. Clamped values prevent accelerometer confusion
//
// Features:
// - Tilt movement
// - Pickup collection with sound + pitch variation
// - Score tracking
// - Timer (counts UP)
// - Best time saving (per level)
// - Win condition + next level loading
// - Screen shake on pickup
// - Floating "+1" text
// - Phone vibration on Android
// =============================================================
public class MobilePlayerController : MonoBehaviour
{
    // =====================================================
    // PUBLIC VARIABLES (editable in Inspector)
    // =====================================================

    // --- Movement ---

    // How fast the ball moves when tilted.
    public float speed = 10f;

    // How responsive the tilt is.
    // Higher number = small tilt causes big movement.
    // Lower number = need to tilt more for movement.
    public float tiltSensitivity = 2.0f;

    // The maximum tilt value we accept (range: 0 to 1).
    // Any tilt beyond this is ignored/clamped.
    // This PREVENTS the bug where tilting too far (past 90 degrees)
    // causes the ball to move in the WRONG direction!
    // 0.5 = about 30 degree max tilt. Safe and comfortable.
    public float maxTiltAngle = 0.5f;

    // Tilts smaller than this value are treated as zero.
    // Prevents the ball from slowly drifting when the phone
    // is almost level (sensors have tiny random noise).
    public float deadZone = 0.05f;

    // --- UI References ---

    // Score text in the top-left corner.
    public TextMeshProUGUI scoreText;

    // Win/Level Complete text (hidden until player wins).
    public GameObject winText;

    // How many pickups exist in this level.
    public int totalPickups = 12;

    // Timer text in the top-right corner.
    public TextMeshProUGUI timerText;

    // Best time text below the timer.
    public TextMeshProUGUI bestTimeText;

    // --- Sound References ---

    // Sound when collecting a good pickup.
    public AudioClip pickupSound;

    // Sound when winning/completing the level.
    public AudioClip winSound;

    // --- Level Progression ---

    // Name of the NEXT scene to load after winning.
    // Leave EMPTY for the final level (just shows "You Win!").
    // Set to "Level2" or "Level3" etc. for level transitions.
    public string nextLevelName = "";

    // Seconds to wait before loading the next level.
    public float nextLevelDelay = 3f;

    // --- Effects ---

    // Prefab for the floating "+1" text that appears when
    // collecting a pickup.
    public GameObject floatingTextPrefab;

    // =====================================================
    // PRIVATE VARIABLES (only this script uses these)
    // =====================================================

    // Physics component for moving the ball.
    private Rigidbody rb;

    // How many pickups the player has collected.
    private int score;

    // Whether the player has won this level.
    private bool gameWon;

    // Audio component for playing sounds.
    private AudioSource audioSource;

    // How many seconds have passed since the level started.
    private float currentTime;

    // The fastest completion time for this level.
    private float bestTime;

    // Timer for delay after winning before loading next level.
    private float winTimer;

    // The accelerometer reading when the game starts.
    // All tilt is measured RELATIVE to this starting position.
    // This means whatever angle the player holds their phone
    // at the start = "center" (no movement).
    private Vector3 calibratedZero;

    // =====================================================
    // UNITY LIFECYCLE FUNCTIONS
    // =====================================================

    // Runs once when the level starts.
    void Start()
    {
        // Find components on this GameObject.
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();

        // Set starting values.
        score = 0;
        gameWon = false;
        currentTime = 0f;
        winTimer = 0f;

        // Hide win text.
        winText.SetActive(false);

        // Show initial score and timer.
        UpdateScoreText();
        UpdateTimerText();

        // Load the best time saved on this device for THIS level.
        // Each level has its own key: "BestTime_MiniGame", "BestTime_Level2", etc.
        string levelKey = "BestTime_" + SceneManager.GetActiveScene().name;
        bestTime = PlayerPrefs.GetFloat(levelKey, 0f);
        UpdateBestTimeText();

        // Prevent the phone screen from going dark during gameplay.
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        // Save the current phone tilt as the "zero" / neutral position.
        // All future tilts are compared against this starting position.
        CalibrateAccelerometer();
    }

    // Runs every frame (about 60 times per second).
    void Update()
    {
        // Allow keyboard restart for PC testing.
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartGame();
        }

        // Count time upward while the game is still going.
        if (!gameWon)
        {
            currentTime += Time.deltaTime;
            UpdateTimerText();
        }

        // After winning, wait then load next level.
        if (gameWon && nextLevelName != "")
        {
            winTimer += Time.deltaTime;
            if (winTimer >= nextLevelDelay)
            {
                SceneManager.LoadScene(nextLevelName);
            }
        }
    }

    // Runs at fixed intervals for physics (50 times per second).
    void FixedUpdate()
    {
        // Only allow movement if the game is still active.
        if (!gameWon)
        {
            // =============================================
            // TILT CONTROLS — THE FIX IS HERE!
            // =============================================

            // Step 1: Read the raw accelerometer (phone tilt sensor).
            Vector3 rawTilt = Input.acceleration;

            // Step 2: Subtract the calibrated zero point.
            // This makes the player's starting angle = no movement.
            // If they started holding the phone at 30 degrees,
            // that 30 degrees becomes the new "flat/center".
            Vector3 adjustedTilt = rawTilt - calibratedZero;

            // Step 3: CLAMP the values to a safe range!
            // This is THE KEY FIX for the wrong-direction bug!
            //
            // Without clamping:
            //   Tilt 30° → value 0.5 → ball moves forward ✅
            //   Tilt 90° → value 1.0 → ball moves fast ✅
            //   Tilt 120° → value 0.87 → ball SLOWS DOWN ❌
            //   Tilt 150° → value 0.5 → ball goes BACKWARD ❌
            //
            // With clamping (max 0.5):
            //   Tilt 30° → value 0.5 → CLAMPED to 0.5 → correct ✅
            //   Tilt 90° → value 1.0 → CLAMPED to 0.5 → still correct ✅
            //   Tilt 120° → value 0.87 → CLAMPED to 0.5 → still correct ✅
            //   Tilt 150° → value 0.5 → CLAMPED to 0.5 → still correct ✅
            //
            // The ball NEVER gets a wrong signal, no matter how far you tilt!
            float clampedX = Mathf.Clamp(adjustedTilt.x, -maxTiltAngle, maxTiltAngle);
            float clampedY = Mathf.Clamp(adjustedTilt.y, -maxTiltAngle, maxTiltAngle);

            // Step 4: Apply dead zone — ignore very tiny tilts.
            // Phone sensors have small random noise even when held still.
            // Without dead zone, the ball slowly drifts on its own.
            // Mathf.Abs() gives the absolute value (removes negative sign)
            // so we check magnitude regardless of direction.
            if (Mathf.Abs(clampedX) < deadZone)
            {
                clampedX = 0f;
            }
            if (Mathf.Abs(clampedY) < deadZone)
            {
                clampedY = 0f;
            }

            // Step 5: Create the movement direction from clamped tilt.
            // X tilt → X movement (left/right)
            // Y tilt → Z movement (forward/backward)
            // Y axis (up/down) is always 0 — no flying!
            Vector3 movement = new Vector3(
                clampedX * tiltSensitivity,
                0.0f,
                clampedY * tiltSensitivity
            );

            // Step 6: Apply force to push the ball.
            rb.AddForce(movement * speed);
        }
    }

    // =====================================================
    // COLLISION HANDLING
    // =====================================================

    // Called when the ball enters a Trigger collider.
    void OnTriggerEnter(Collider other)
    {
        // Only process if the game is still active.
        if (gameWon) return;

        // Check if we touched a good pickup.
        if (other.gameObject.CompareTag("Pickup"))
        {
            // Hide the pickup.
            other.gameObject.SetActive(false);

            // Spawn floating "+1" text at the pickup's position.
            if (floatingTextPrefab != null)
            {
                Instantiate(
                    floatingTextPrefab,
                    other.transform.position + Vector3.up * 0.5f,
                    Quaternion.identity
                );
            }

            // Add 1 to score.
            score += 1;
            UpdateScoreText();

            // Play pickup sound with random pitch variation.
            if (pickupSound != null)
            {
                float originalPitch = audioSource.pitch;
                audioSource.pitch = Random.Range(0.85f, 1.15f);
                audioSource.PlayOneShot(pickupSound);
                audioSource.pitch = originalPitch;
            }

            // Trigger screen shake.
            if (ScreenShake.instance != null)
            {
                ScreenShake.instance.TriggerShake(0.1f, 0.08f);
            }

            // Vibrate phone on Android.
            #if UNITY_ANDROID
            Handheld.Vibrate();
            #endif

            // Check if all pickups collected = WIN!
            if (score >= totalPickups)
            {
                gameWon = true;
                winText.SetActive(true);

                if (winSound != null)
                {
                    audioSource.PlayOneShot(winSound);
                }

                // Save best time for this specific level.
                string levelKey = "BestTime_" + SceneManager.GetActiveScene().name;
                if (bestTime == 0f || currentTime < bestTime)
                {
                    bestTime = currentTime;
                    PlayerPrefs.SetFloat(levelKey, bestTime);
                    PlayerPrefs.Save();
                    UpdateBestTimeText();
                }
            }
        }
    }

    // =====================================================
    // PUBLIC FUNCTIONS (called by UI buttons)
    // =====================================================

    // Called by the on-screen Restart button.
    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Called by a Recalibrate button (optional).
    // Resets the "center" tilt position to whatever angle
    // the player is currently holding the phone.
    public void RecalibrateControls()
    {
        CalibrateAccelerometer();
    }

    // =====================================================
    // PRIVATE FUNCTIONS
    // =====================================================

    // Saves the current accelerometer reading as "zero."
    // All future tilts are measured relative to this.
    void CalibrateAccelerometer()
    {
        calibratedZero = Input.acceleration;
    }

    // Updates the score text on screen.
    void UpdateScoreText()
    {
        scoreText.text = "Score: " + score.ToString();
    }

    // Updates the timer text on screen.
    void UpdateTimerText()
    {
        timerText.text = "Time: " + currentTime.ToString("F2");
    }

    // Updates the best time text on screen.
    void UpdateBestTimeText()
    {
        if (bestTime == 0f)
        {
            bestTimeText.text = "Best: --";
        }
        else
        {
            bestTimeText.text = "Best: " + bestTime.ToString("F2") + "s";
        }
    }
}