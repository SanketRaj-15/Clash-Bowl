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
// - ADDED: Keyboard controls for PC testing (WASD / Arrow Keys)
// - ADDED: Tilt smoothing for less jerky movement
// - ADDED: Auto platform detection (mobile vs PC)
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

    // How much the tilt input is smoothed out.
    // Higher = smoother but less responsive.
    // Lower = more responsive but jerkier.
    public float tiltSmoothing = 0.2f;

    // Speed multiplier for keyboard controls.
    // Adjust if keyboard movement feels too fast/slow
    // compared to tilt controls.
    public float keyboardSpeedMultiplier = 1.0f;

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

    // Smoothed tilt input — prevents jerky movement from
    // noisy accelerometer readings.
    private Vector3 smoothedTilt;

    // Whether we're running on a mobile device or PC.
    // This is checked once at startup for efficiency.
    private bool isMobileDevice;

    // =====================================================
    // LIFECYCLE FUNCTIONS
    // =====================================================

    void Start()
    {
        Debug.Log("[Level2PlayerController] Start() called");

        // Find components on this object.
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        playerRenderer = GetComponent<MeshRenderer>();
        Debug.Log("[Level2PlayerController] Rigidbody: " + (rb != null ? "Found" : "NULL") + " | AudioSource: " + (audioSource != null ? "Found" : "NULL") + " | MeshRenderer: " + (playerRenderer != null ? "Found" : "NULL"));

        // Initialize game state.
        score = 0;
        gameWon = false;
        gameOver = false;
        currentTime = 0f;
        delayTimer = 0f;
        smoothedTilt = Vector3.zero;
        Debug.Log("[Level2PlayerController] Game state initialized");

        // Detect if we're on a mobile device or PC.
        isMobileDevice = (Application.platform == RuntimePlatform.Android ||
                          Application.platform == RuntimePlatform.IPhonePlayer);
        Debug.Log("[Level2PlayerController] Platform: " + Application.platform + " | isMobileDevice: " + isMobileDevice);

        // If we're in the Unity Editor, check if Device Simulator is active.
        #if UNITY_EDITOR
        if (SystemInfo.deviceType == DeviceType.Handheld)
        {
            isMobileDevice = true;
            Debug.Log("[Level2PlayerController] Unity Editor detected handheld device simulation");
        }
        #endif

        // Hide UI texts that shouldn't be visible at start.
        winText.SetActive(false);
        Debug.Log("[Level2PlayerController] Win text hidden");

        if (gameOverText != null)
        {
            gameOverText.SetActive(false);
            Debug.Log("[Level2PlayerController] Game Over text hidden");
        }
        else
        {
            Debug.Log("[Level2PlayerController] gameOverText is NULL — not assigned in Inspector");
        }

        // Display initial values.
        UpdateScoreText();
        UpdateTimerText();

        // Load this level's best time from saved data.
        string levelKey = "BestTime_" + SceneManager.GetActiveScene().name;
        bestTime = PlayerPrefs.GetFloat(levelKey, 0f);
        UpdateBestTimeText();
        Debug.Log("[Level2PlayerController] Best time loaded for " + levelKey + ": " + bestTime);

        // Prevent phone screen from sleeping.
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Debug.Log("[Level2PlayerController] Screen sleep disabled");

        // Calibrate the accelerometer.
        CalibrateAccelerometer();
        Debug.Log("[Level2PlayerController] Accelerometer calibrated. Zero point: " + calibratedZero);
    }

    void Update()
    {
        // Keyboard restart for PC testing.
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("[Level2PlayerController] R key pressed — restarting");
            RestartGame();
        }

        // Allow recalibration with C key (useful for testing).
        if (Input.GetKeyDown(KeyCode.C))
        {
            Debug.Log("[Level2PlayerController] C key pressed — recalibrating");
            RecalibrateControls();
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
            Debug.Log("[Level2PlayerController] Win timer: " + delayTimer + " / " + nextLevelDelay);
            if (delayTimer >= nextLevelDelay)
            {
                Debug.Log("[Level2PlayerController] Loading next level: " + nextLevelName);
                SceneManager.LoadScene(nextLevelName);
            }
        }

        // After dying: wait 2.5 seconds then auto-restart.
        if (gameOver)
        {
            delayTimer += Time.deltaTime;
            Debug.Log("[Level2PlayerController] Death timer: " + delayTimer + " / 2.5");
            if (delayTimer >= 2.5f)
            {
                Debug.Log("[Level2PlayerController] Auto-restarting after death");
                RestartGame();
            }
        }
    }

    void FixedUpdate()
    {
        // Only allow movement if game is active.
        if (!gameWon && !gameOver)
        {
            Vector3 movement;

            // Choose input method based on platform.
            if (isMobileDevice)
            {
                // =============================================
                // TILT CONTROLS (MOBILE)
                // =============================================
                movement = GetTiltInput();
            }
            else
            {
                // =============================================
                // KEYBOARD CONTROLS (PC TESTING)
                // =============================================
                movement = GetKeyboardInput();
            }

            // Apply force to push the ball.
            rb.AddForce(movement * speed);
            Debug.Log("[Level2PlayerController] FixedUpdate() — Force applied: " + (movement * speed) + " | Velocity: " + rb.velocity + " | Speed: " + rb.velocity.magnitude);
        }
        else
        {
            Debug.Log("[Level2PlayerController] FixedUpdate() — Movement disabled (gameWon: " + gameWon + " | gameOver: " + gameOver + ")");
        }
    }

    // =====================================================
    // INPUT METHODS
    // =====================================================

    // Gets movement direction from phone tilt (mobile).
    private Vector3 GetTiltInput()
    {
        // Step 1: Read the raw accelerometer (phone tilt sensor).
        Vector3 rawTilt = Input.acceleration;
        Debug.Log("[Level2PlayerController] GetTiltInput() — Raw accelerometer: " + rawTilt);

        // Step 2: Subtract the calibrated zero point.
        Vector3 adjustedTilt = rawTilt - calibratedZero;
        Debug.Log("[Level2PlayerController] GetTiltInput() — Adjusted tilt: " + adjustedTilt);

        // Step 3: SMOOTH the tilt values.
        // This prevents jerky movement from sensor noise.
        smoothedTilt = Vector3.Lerp(smoothedTilt, adjustedTilt, tiltSmoothing);
        Debug.Log("[Level2PlayerController] GetTiltInput() — Smoothed tilt: " + smoothedTilt);

        // Step 4: CLAMP to safe range (prevents wrong-direction bug).
        float clampedX = Mathf.Clamp(smoothedTilt.x, -maxTiltAngle, maxTiltAngle);
        float clampedY = Mathf.Clamp(smoothedTilt.y, -maxTiltAngle, maxTiltAngle);
        Debug.Log("[Level2PlayerController] GetTiltInput() — Clamped X: " + clampedX + " | Clamped Y: " + clampedY);

        // Step 5: Apply dead zone.
        if (Mathf.Abs(clampedX) < deadZone) clampedX = 0f;
        if (Mathf.Abs(clampedY) < deadZone) clampedY = 0f;
        Debug.Log("[Level2PlayerController] GetTiltInput() — After dead zone — X: " + clampedX + " | Y: " + clampedY);

        // Step 6: Create movement direction.
        Vector3 movement = new Vector3(
            clampedX * tiltSensitivity,
            0.0f,
            clampedY * tiltSensitivity
        );
        Debug.Log("[Level2PlayerController] GetTiltInput() — Final movement: " + movement);

        return movement;
    }

    // Gets movement direction from keyboard (PC testing).
    private Vector3 GetKeyboardInput()
    {
        // Read WASD / Arrow Keys input.
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Debug.Log("[Level2PlayerController] GetKeyboardInput() — H: " + horizontal + " | V: " + vertical);

        // Create movement vector.
        Vector3 movement = new Vector3(
            horizontal * keyboardSpeedMultiplier,
            0.0f,
            vertical * keyboardSpeedMultiplier
        );

        // Prevent diagonal movement from being faster.
        movement = Vector3.ClampMagnitude(movement, 1.0f * keyboardSpeedMultiplier);
        Debug.Log("[Level2PlayerController] GetKeyboardInput() — Final movement: " + movement);

        return movement;
    }

    // =====================================================
    // COLLISION HANDLING
    // =====================================================

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("[Level2PlayerController] OnTriggerEnter() — Hit: " + other.gameObject.name + " | Tag: " + other.gameObject.tag);

        // Ignore collisions if game is already over.
        if (gameOver || gameWon)
        {
            Debug.Log("[Level2PlayerController] OnTriggerEnter() — Ignoring collision (gameOver: " + gameOver + " | gameWon: " + gameWon + ")");
            return;
        }

        // ==================
        // GOOD PICKUP ✅
        // ==================
        if (other.gameObject.CompareTag("Pickup"))
        {
            Debug.Log("[Level2PlayerController] Good pickup collected: " + other.gameObject.name);

            // Hide the pickup.
            other.gameObject.SetActive(false);
            Debug.Log("[Level2PlayerController] Pickup deactivated");

            // Spawn floating "+1" text.
            if (floatingTextPrefab != null)
            {
                Instantiate(
                    floatingTextPrefab,
                    other.transform.position + Vector3.up * 0.5f,
                    Quaternion.identity
                );
                Debug.Log("[Level2PlayerController] Floating text spawned at: " + (other.transform.position + Vector3.up * 0.5f));
            }
            else
            {
                Debug.Log("[Level2PlayerController] floatingTextPrefab is NULL — no floating text spawned");
            }

            // Increase score.
            score += 1;
            UpdateScoreText();
            Debug.Log("[Level2PlayerController] Score updated to: " + score + " / " + totalPickups);

            // Play sound with pitch variation.
            if (pickupSound != null)
            {
                float originalPitch = audioSource.pitch;
                audioSource.pitch = Random.Range(0.85f, 1.15f);
                audioSource.PlayOneShot(pickupSound);
                audioSource.pitch = originalPitch;
                Debug.Log("[Level2PlayerController] Pickup sound played with pitch variation");
            }
            else
            {
                Debug.Log("[Level2PlayerController] pickupSound is NULL — no sound played");
            }

            // Screen shake.
            if (ScreenShake.instance != null)
            {
                ScreenShake.instance.TriggerShake(0.1f, 0.08f);
                Debug.Log("[Level2PlayerController] Screen shake triggered (0.1, 0.08)");
            }
            else
            {
                Debug.Log("[Level2PlayerController] ScreenShake.instance is NULL — no shake");
            }

            // Phone vibration.
            #if UNITY_ANDROID
            Handheld.Vibrate();
            Debug.Log("[Level2PlayerController] Phone vibration triggered");
            #endif

            // Check win condition.
            if (score >= totalPickups)
            {
                gameWon = true;
                delayTimer = 0f;
                winText.SetActive(true);
                Debug.Log("[Level2PlayerController] GAME WON! Time: " + currentTime);

                if (winSound != null)
                {
                    audioSource.PlayOneShot(winSound);
                    Debug.Log("[Level2PlayerController] Win sound played");
                }
                else
                {
                    Debug.Log("[Level2PlayerController] winSound is NULL — no sound played");
                }

                // Save best time.
                string levelKey = "BestTime_" + SceneManager.GetActiveScene().name;
                Debug.Log("[Level2PlayerController] Checking best time — current: " + currentTime + " | saved best: " + bestTime);
                if (bestTime == 0f || currentTime < bestTime)
                {
                    bestTime = currentTime;
                    PlayerPrefs.SetFloat(levelKey, bestTime);
                    PlayerPrefs.Save();
                    UpdateBestTimeText();
                    Debug.Log("[Level2PlayerController] New best time saved: " + bestTime);
                }
                else
                {
                    Debug.Log("[Level2PlayerController] Current time (" + currentTime + ") did not beat best time (" + bestTime + ")");
                }
            }
        }

        // ==================
        // ENEMY PICKUP ☠️
        // ==================
        if (other.gameObject.CompareTag("EnemyPickup"))
        {
            Debug.Log("[Level2PlayerController] ENEMY PICKUP HIT: " + other.gameObject.name + " — GAME OVER!");

            // GAME OVER!
            gameOver = true;
            delayTimer = 0f;
            Debug.Log("[Level2PlayerController] gameOver set to true, delayTimer reset");

            // Hide the enemy pickup that was touched.
            other.gameObject.SetActive(false);
            Debug.Log("[Level2PlayerController] Enemy pickup deactivated");

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
                Debug.Log("[Level2PlayerController] Explosion spawned at: " + transform.position);
            }
            else
            {
                Debug.Log("[Level2PlayerController] explosionPrefab is NULL — no explosion spawned");
            }

            // Play explosion sound.
            if (explosionSound != null)
            {
                audioSource.PlayOneShot(explosionSound);
                Debug.Log("[Level2PlayerController] Explosion sound played");
            }
            else
            {
                Debug.Log("[Level2PlayerController] explosionSound is NULL — no sound played");
            }

            // BIG screen shake (3x stronger than pickup shake!).
            if (ScreenShake.instance != null)
            {
                ScreenShake.instance.TriggerShake(0.3f, 0.3f);
                Debug.Log("[Level2PlayerController] BIG screen shake triggered (0.3, 0.3)");
            }
            else
            {
                Debug.Log("[Level2PlayerController] ScreenShake.instance is NULL — no shake");
            }

            // Phone vibration.
            #if UNITY_ANDROID
            Handheld.Vibrate();
            Debug.Log("[Level2PlayerController] Phone vibration triggered");
            #endif

            // HIDE the player ball (it "exploded"!).
            // Disabling the MeshRenderer makes the ball invisible
            // but the object still exists (so our script keeps running).
            playerRenderer.enabled = false;
            Debug.Log("[Level2PlayerController] Player renderer disabled (ball hidden)");

            // Stop all ball movement immediately.
            // Setting velocity and angular velocity to zero
            // makes the ball freeze in place.
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            Debug.Log("[Level2PlayerController] Ball velocity and angular velocity set to zero");

            // Disable the trail so no trail shows after explosion.
            TrailRenderer trail = GetComponent<TrailRenderer>();
            if (trail != null)
            {
                trail.enabled = false;
                Debug.Log("[Level2PlayerController] Trail renderer disabled");
            }
            else
            {
                Debug.Log("[Level2PlayerController] No TrailRenderer found on this object");
            }

            // Show the "Game Over!" text.
            if (gameOverText != null)
            {
                gameOverText.SetActive(true);
                Debug.Log("[Level2PlayerController] Game Over text shown");
            }
            else
            {
                Debug.Log("[Level2PlayerController] gameOverText is NULL — cannot show Game Over");
            }
        }
    }

    // =====================================================
    // PUBLIC FUNCTIONS
    // =====================================================

    // Called by the on-screen Restart button.
    public void RestartGame()
    {
        Debug.Log("[Level2PlayerController] RestartGame() called");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Called by a Recalibrate button (optional).
    // Resets the "center" tilt position to whatever angle
    // the player is currently holding the phone.
    public void RecalibrateControls()
    {
        Debug.Log("[Level2PlayerController] RecalibrateControls() called");
        CalibrateAccelerometer();
        Debug.Log("[Level2PlayerController] New calibrated zero: " + calibratedZero);
    }

    // =====================================================
    // PRIVATE FUNCTIONS
    // =====================================================

    void CalibrateAccelerometer()
    {
        calibratedZero = Input.acceleration;
        smoothedTilt = Vector3.zero;
        Debug.Log("[Level2PlayerController] CalibrateAccelerometer() — Zero set to: " + calibratedZero);
    }

    void UpdateScoreText()
    {
        scoreText.text = "Score: " + score.ToString();
        Debug.Log("[Level2PlayerController] UpdateScoreText() — " + scoreText.text);
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
        Debug.Log("[Level2PlayerController] UpdateBestTimeText() — " + bestTimeText.text);
    }
}