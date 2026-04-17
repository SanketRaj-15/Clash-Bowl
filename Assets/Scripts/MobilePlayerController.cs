// Unity core features
using UnityEngine;

// TextMeshPro for UI text
using TMPro;

// Scene management for restarting and loading levels
using UnityEngine.SceneManagement;

// FIXED MobilePlayerController — PROPER TILT CONTROLS! 📱
// Fixes the issue where tilting too far causes wrong movement.
// Uses calibrated neutral position and clamped tilt values.
public class MobilePlayerController : MonoBehaviour
{
    // =====================================================
    // PUBLIC VARIABLES
    // =====================================================

    // --- Movement ---

    // How fast the ball moves.
    public float speed = 10f;

    // How sensitive the tilt is.
    // Higher = more responsive to small tilts.
    public float tiltSensitivity = 2.0f;

    // The maximum tilt angle (in acceleration units) that we accept.
    // Values beyond this are clamped (limited).
    // 0.5 means we only use tilt from -0.5 to +0.5.
    // This PREVENTS the "wrong direction" bug when tilting too far!
    public float maxTiltAngle = 0.5f;

    // A dead zone — tilts smaller than this are ignored.
    // This prevents the ball from drifting when the phone is 
    // almost level (small sensor noise).
    // 0.05 means tilts less than 5% are treated as zero.
    public float deadZone = 0.05f;

    // --- UI References ---
    public TextMeshProUGUI scoreText;
    public GameObject winText;
    public int totalPickups = 12;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI bestTimeText;

    // --- Sound References ---
    public AudioClip pickupSound;
    public AudioClip winSound;

    // --- Level Progression ---
    public string nextLevelName = "";
    public float nextLevelDelay = 3f;

    // --- Effects ---
    // Reference to the FloatingText prefab for "+1" popups.
    public GameObject floatingTextPrefab;

    // =====================================================
    // PRIVATE VARIABLES
    // =====================================================

    private Rigidbody rb;
    private int score;
    private bool gameWon;
    private AudioSource audioSource;
    private float currentTime;
    private float bestTime;
    private float winTimer;

    // The "neutral" accelerometer reading when the game starts.
    // This is the tilt angle the player naturally holds their phone.
    // All movement is calculated RELATIVE to this starting position.
    // This means the player doesn't have to hold their phone 
    // perfectly flat — whatever angle they start at is "center."
    private Vector3 calibratedZero;

    // Whether calibration has been done.
    private bool isCalibrated;

    // =====================================================
    // UNITY LIFECYCLE FUNCTIONS
    // =====================================================

    void Start()
    {
        // Get components.
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();

        // Initialize game state.
        score = 0;
        gameWon = false;
        currentTime = 0f;
        winTimer = 0f;

        // Hide win text.
        winText.SetActive(false);

        // Display initial values.
        UpdateScoreText();
        UpdateTimerText();

        // Load best time for this level.
        string levelKey = "BestTime_" + SceneManager.GetActiveScene().name;
        bestTime = PlayerPrefs.GetFloat(levelKey, 0f);
        UpdateBestTimeText();

        // Prevent screen from sleeping during gameplay.
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        // CALIBRATE the accelerometer!
        // Save the current phone orientation as the "neutral" position.
        // This means wherever the player is holding their phone 
        // when the game starts = no movement.
        CalibrateAccelerometer();
    }

    void Update()
    {
        // Keyboard restart (for PC testing).
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartGame();
        }

        // Count time while playing.
        if (!gameWon)
        {
            currentTime += Time.deltaTime;
            UpdateTimerText();
        }

        // Handle next level loading after win.
        if (gameWon && nextLevelName != "")
        {
            winTimer += Time.deltaTime;
            if (winTimer >= nextLevelDelay)
            {
                SceneManager.LoadScene(nextLevelName);
            }
        }
    }

    void FixedUpdate()
    {
        if (!gameWon)
        {
            // =============================================
            // FIXED TILT CONTROLS! 📱
            // =============================================

            // Read the raw accelerometer values.
            Vector3 rawTilt = Input.acceleration;

            // Subtract the calibrated zero point.
            // This makes the starting phone position = no movement.
            // If the player starts holding the phone tilted 30°,
            // that 30° becomes the new "flat/center" position.
            Vector3 adjustedTilt = rawTilt - calibratedZero;

            // CLAMP the tilt values to prevent the "wrong direction" bug!
            // Mathf.Clamp(value, min, max) limits a number to a range.
            //
            // If maxTiltAngle is 0.5:
            //   A tilt of 0.3 stays as 0.3 ✅
            //   A tilt of 0.7 gets clamped to 0.5 ✅ (prevented!)
            //   A tilt of -0.8 gets clamped to -0.5 ✅ (prevented!)
            //
            // This means no matter HOW far the player tilts,
            // the ball never gets a "backwards" signal!
            float clampedX = Mathf.Clamp(adjustedTilt.x, -maxTiltAngle, maxTiltAngle);
            float clampedY = Mathf.Clamp(adjustedTilt.y, -maxTiltAngle, maxTiltAngle);

            // Apply DEAD ZONE — ignore very small tilts.
            // This prevents the ball from slowly drifting when 
            // the phone is almost level.
            if (Mathf.Abs(clampedX) < deadZone)
            {
                clampedX = 0f;
            }
            if (Mathf.Abs(clampedY) < deadZone)
            {
                clampedY = 0f;
            }

            // Create movement direction from the clamped tilt values.
            Vector3 movement = new Vector3(
                clampedX * tiltSensitivity,    // Left/Right
                0.0f,                           // No vertical (no flying!)
                clampedY * tiltSensitivity      // Forward/Backward
            );

            // Apply force to move the ball.
            rb.AddForce(movement * speed);
        }
    }

    // =====================================================
    // COLLISION HANDLING
    // =====================================================

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Pickup"))
        {
            // Hide the pickup.
            other.gameObject.SetActive(false);

            // Spawn floating "+1" text.
            if (floatingTextPrefab != null)
            {
                Instantiate(
                    floatingTextPrefab,
                    other.transform.position + Vector3.up * 0.5f,
                    Quaternion.identity
                );
            }

            // Increase score.
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

            // Screen shake.
            if (ScreenShake.instance != null)
            {
                ScreenShake.instance.TriggerShake(0.1f, 0.08f);
            }

            // Vibrate phone on Android! 📳
            // Handheld.Vibrate() makes the phone vibrate briefly.
            // This only works on actual mobile devices, not in the editor.
            #if UNITY_ANDROID
            Handheld.Vibrate();
            #endif

            // Check win condition.
            if (score >= totalPickups)
            {
                gameWon = true;
                winText.SetActive(true);

                if (winSound != null)
                {
                    audioSource.PlayOneShot(winSound);
                }

                // Save best time for this level.
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

    // Called by the Restart button.
    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Called by a "Recalibrate" button (optional).
    // If the player wants to change their neutral holding position,
    // they can tap this button while holding the phone at 
    // their preferred angle.
    public void RecalibrateControls()
    {
        CalibrateAccelerometer();
    }

    // =====================================================
    // PRIVATE FUNCTIONS
    // =====================================================

    // Saves the current accelerometer reading as the "zero" point.
    // All future tilt measurements are relative to this.
    void CalibrateAccelerometer()
    {
        // Read the current accelerometer values.
        calibratedZero = Input.acceleration;
        isCalibrated = true;
    }

    void UpdateScoreText()
    {
        scoreText.text = "Score: " + score.ToString();
    }

    void UpdateTimerText()
    {
        timerText.text = "Time: " + currentTime.ToString("F2");
    }

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