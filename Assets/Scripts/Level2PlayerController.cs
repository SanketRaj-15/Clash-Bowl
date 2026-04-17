using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

// =============================================================
// LEVEL 2 PLAYER CONTROLLER — WITH ENEMY PICKUPS! ☠️
// =============================================================
// Same tilt controls as MobilePlayerController but adds:
// - Enemy pickup detection (tagged "EnemyPickup")
// - Explosion particle effect when touching enemy
// - Game Over state with auto-restart
// - Player ball disappears on death
// =============================================================
public class Level2PlayerController : MonoBehaviour
{
    // =====================================================
    // PUBLIC VARIABLES
    // =====================================================

    // --- Movement ---
    // How fast the ball moves.
    public float speed = 10f;

    // How responsive to tilting.
    public float tiltSensitivity = 2.0f;

    // Maximum tilt angle accepted (prevents wrong direction bug).
    public float maxTiltAngle = 0.5f;

    // Tiny tilts below this are ignored (prevents drift).
    public float deadZone = 0.05f;

    // --- UI References ---

    // Score display text.
    public TextMeshProUGUI scoreText;

    // Win/Level Complete text (hidden at start).
    public GameObject winText;

    // Number of GOOD pickups to collect to win.
    // Enemy pickups do NOT count toward this!
    public int totalPickups = 12;

    // Timer display text.
    public TextMeshProUGUI timerText;

    // Best time display text.
    public TextMeshProUGUI bestTimeText;

    // Game Over text (hidden at start).
    // Shown when the player touches an enemy pickup.
    public GameObject gameOverText;

    // --- Sound References ---

    // Sound when collecting a good pickup.
    public AudioClip pickupSound;

    // Sound when completing the level.
    public AudioClip winSound;

    // Sound when touching an enemy pickup (explosion!).
    public AudioClip explosionSound;

    // --- Level Progression ---

    // Next scene to load after winning.
    // Leave empty for the final level.
    public string nextLevelName = "";

    // Seconds to wait before loading next level.
    public float nextLevelDelay = 3f;

    // --- Effects ---

    // Floating "+1" text prefab.
    public GameObject floatingTextPrefab;

    // Explosion particle effect prefab.
    // Spawned at the player's position when they touch an enemy.
    public GameObject explosionPrefab;

    // =====================================================
    // PRIVATE VARIABLES
    // =====================================================

    // Physics component.
    private Rigidbody rb;

    // Number of good pickups collected.
    private int score;

    // Whether the player has won.
    private bool gameWon;

    // Whether the player has died (touched enemy).
    private bool gameOver;

    // Audio component.
    private AudioSource audioSource;

    // Seconds elapsed since level start.
    private float currentTime;

    // Fastest completion time saved on this device.
    private float bestTime;

    // Timer used for delays (after win or death).
    private float delayTimer;

    // Accelerometer neutral position.
    private Vector3 calibratedZero;

    // Reference to the ball's visible mesh.
    // Used to hide the ball when it "explodes."
    private MeshRenderer playerRenderer;

    // =====================================================
    // LIFECYCLE FUNCTIONS
    // =====================================================

    void Start()
    {
        // Find components on this object.
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        playerRenderer = GetComponent<MeshRenderer>();

        // Initialize game state.
        score = 0;
        gameWon = false;
        gameOver = false;
        currentTime = 0f;
        delayTimer = 0f;

        // Hide UI texts that shouldn't be visible at start.
        winText.SetActive(false);
        if (gameOverText != null)
        {
            gameOverText.SetActive(false);
        }

        // Display initial values.
        UpdateScoreText();
        UpdateTimerText();

        // Load this level's best time from saved data.
        string levelKey = "BestTime_" + SceneManager.GetActiveScene().name;
        bestTime = PlayerPrefs.GetFloat(levelKey, 0f);
        UpdateBestTimeText();

        // Prevent phone screen from sleeping.
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        // Calibrate the accelerometer.
        CalibrateAccelerometer();
    }

    void Update()
    {
        // Keyboard restart for PC testing.
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartGame();
        }

        // Count time only while game is active (not won, not dead).
        if (!gameWon && !gameOver)
        {
            currentTime += Time.deltaTime;
            UpdateTimerText();
        }

        // After winning: wait then load next level.
        if (gameWon && nextLevelName != "")
        {
            delayTimer += Time.deltaTime;
            if (delayTimer >= nextLevelDelay)
            {
                SceneManager.LoadScene(nextLevelName);
            }
        }

        // After dying: wait 2.5 seconds then auto-restart.
        if (gameOver)
        {
            delayTimer += Time.deltaTime;
            if (delayTimer >= 2.5f)
            {
                RestartGame();
            }
        }
    }

    void FixedUpdate()
    {
        // Only allow movement if game is active.
        if (!gameWon && !gameOver)
        {
            // Read and adjust tilt.
            Vector3 rawTilt = Input.acceleration;
            Vector3 adjustedTilt = rawTilt - calibratedZero;

            // Clamp to safe range.
            float clampedX = Mathf.Clamp(adjustedTilt.x, -maxTiltAngle, maxTiltAngle);
            float clampedY = Mathf.Clamp(adjustedTilt.y, -maxTiltAngle, maxTiltAngle);

            // Apply dead zone.
            if (Mathf.Abs(clampedX) < deadZone) clampedX = 0f;
            if (Mathf.Abs(clampedY) < deadZone) clampedY = 0f;

            // Create and apply movement.
            Vector3 movement = new Vector3(
                clampedX * tiltSensitivity,
                0.0f,
                clampedY * tiltSensitivity
            );
            rb.AddForce(movement * speed);
        }
    }

    // =====================================================
    // COLLISION HANDLING
    // =====================================================

    void OnTriggerEnter(Collider other)
    {
        // Ignore collisions if game is already over.
        if (gameOver || gameWon) return;

        // ==================
        // GOOD PICKUP ✅
        // ==================
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

            // Play sound with pitch variation.
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

            // Phone vibration.
            #if UNITY_ANDROID
            Handheld.Vibrate();
            #endif

            // Check win condition.
            if (score >= totalPickups)
            {
                gameWon = true;
                delayTimer = 0f;
                winText.SetActive(true);

                if (winSound != null)
                {
                    audioSource.PlayOneShot(winSound);
                }

                // Save best time.
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

        // ==================
        // ENEMY PICKUP ☠️
        // ==================
        if (other.gameObject.CompareTag("EnemyPickup"))
        {
            // GAME OVER!
            gameOver = true;
            delayTimer = 0f;

            // Hide the enemy pickup that was touched.
            other.gameObject.SetActive(false);

            // SPAWN EXPLOSION at the player's position!
            // Instantiate creates a copy of the explosion prefab.
            // The explosion prefab has "Stop Action = Destroy" so it
            // automatically cleans itself up after playing.
            if (explosionPrefab != null)
            {
                Instantiate(
                    explosionPrefab,
                    transform.position,
                    Quaternion.identity
                );
            }

            // Play explosion sound.
            if (explosionSound != null)
            {
                audioSource.PlayOneShot(explosionSound);
            }

            // BIG screen shake (3x stronger than pickup shake!).
            if (ScreenShake.instance != null)
            {
                ScreenShake.instance.TriggerShake(0.3f, 0.3f);
            }

            // Phone vibration.
            #if UNITY_ANDROID
            Handheld.Vibrate();
            #endif

            // HIDE the player ball (it "exploded"!).
            // Disabling the MeshRenderer makes the ball invisible
            // but the object still exists (so our script keeps running).
            playerRenderer.enabled = false;

            // Stop all ball movement immediately.
            // Setting velocity and angular velocity to zero
            // makes the ball freeze in place.
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // Disable the trail so no trail shows after explosion.
            TrailRenderer trail = GetComponent<TrailRenderer>();
            if (trail != null)
            {
                trail.enabled = false;
            }

            // Show the "Game Over!" text.
            if (gameOverText != null)
            {
                gameOverText.SetActive(true);
            }
        }
    }

    // =====================================================
    // PUBLIC FUNCTIONS
    // =====================================================

    // Called by the on-screen Restart button.
    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // =====================================================
    // PRIVATE FUNCTIONS
    // =====================================================

    void CalibrateAccelerometer()
    {
        calibratedZero = Input.acceleration;
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