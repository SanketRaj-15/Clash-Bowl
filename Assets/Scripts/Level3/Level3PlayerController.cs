using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

// =============================================================
// LEVEL 3 PLAYER CONTROLLER — COUNTDOWN TIMER! ⏰
// =============================================================
// Unique Feature: Player has a LIMITED TIME to collect all pickups!
// Timer counts DOWN from a set time limit.
// If time hits 0 = GAME OVER!
// Timer text changes color as time gets low:
//   Green (plenty of time) → Yellow (warning) → Red (critical!)
// Warning beeps play during the last 10 seconds.
// - ADDED: Keyboard controls for PC testing (WASD / Arrow Keys)
// - ADDED: Tilt smoothing for less jerky movement
// - ADDED: Auto platform detection (mobile vs PC)
// =============================================================
public class Level3PlayerController : MonoBehaviour
{
    // =====================================================
    // PUBLIC VARIABLES
    // =====================================================

    // --- Movement ---
    public float speed = 10f;
    public float tiltSensitivity = 2.0f;
    public float maxTiltAngle = 0.5f;
    public float deadZone = 0.05f;

    // How much the tilt input is smoothed out.
    // Higher = smoother but less responsive.
    // Lower = more responsive but jerkier.
    public float tiltSmoothing = 0.2f;

    // Speed multiplier for keyboard controls.
    // Adjust if keyboard movement feels too fast/slow
    // compared to tilt controls.
    public float keyboardSpeedMultiplier = 1.0f;

    // --- UI ---
    public TextMeshProUGUI scoreText;
    public GameObject winText;
    public int totalPickups = 15;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI bestTimeText;
    public GameObject gameOverText;

    // --- UNIQUE TO LEVEL 3 ---

    // How many seconds the player has to collect everything.
    // When this reaches 0: GAME OVER!
    public float timeLimit = 45f;

    // --- Sound ---
    public AudioClip pickupSound;
    public AudioClip winSound;
    public AudioClip gameOverSound;

    // Short beep that plays every second during the last 10 seconds.
    // Creates urgency! Download a short "beep" or "tick" sound.
    // Leave as None if you don't have one.
    public AudioClip warningBeep;

    // --- Level ---
    public string nextLevelName = "";
    public float nextLevelDelay = 3f;

    // --- Effects ---
    public GameObject floatingTextPrefab;

    // =====================================================
    // PRIVATE VARIABLES
    // =====================================================

    private Rigidbody rb;
    private int score;
    private bool gameWon;
    private bool gameOver;
    private AudioSource audioSource;
    private float currentTime;
    private float bestTime;
    private float delayTimer;
    private Vector3 calibratedZero;

    // Tracks when the next warning beep should play.
    private float nextWarningTime;

    // Whether we've entered the warning zone.
    private bool inWarningZone;

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
        Debug.Log("[Level3PlayerController] Start() called");

        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        Debug.Log("[Level3PlayerController] Rigidbody: " + (rb != null ? "Found" : "NULL") + " | AudioSource: " + (audioSource != null ? "Found" : "NULL"));

        score = 0;
        gameWon = false;
        gameOver = false;
        delayTimer = 0f;
        inWarningZone = false;
        nextWarningTime = 0f;
        smoothedTilt = Vector3.zero;
        Debug.Log("[Level3PlayerController] Game state initialized");

        // Detect if we're on a mobile device or PC.
        isMobileDevice = (Application.platform == RuntimePlatform.Android ||
                          Application.platform == RuntimePlatform.IPhonePlayer);
        Debug.Log("[Level3PlayerController] Platform: " + Application.platform + " | isMobileDevice: " + isMobileDevice);

        // If we're in the Unity Editor, check if Device Simulator is active.
        #if UNITY_EDITOR
        if (SystemInfo.deviceType == DeviceType.Handheld)
        {
            isMobileDevice = true;
            Debug.Log("[Level3PlayerController] Unity Editor detected handheld device simulation");
        }
        #endif

        // Timer starts at the time limit and counts DOWN!
        currentTime = timeLimit;
        Debug.Log("[Level3PlayerController] Timer set to: " + currentTime + " seconds");

        winText.SetActive(false);
        Debug.Log("[Level3PlayerController] Win text hidden");

        if (gameOverText != null)
        {
            gameOverText.SetActive(false);
            Debug.Log("[Level3PlayerController] Game Over text hidden");
        }
        else
        {
            Debug.Log("[Level3PlayerController] gameOverText is NULL — not assigned in Inspector");
        }

        UpdateScoreText();
        UpdateTimerText();

        string levelKey = "BestTime_" + SceneManager.GetActiveScene().name;
        bestTime = PlayerPrefs.GetFloat(levelKey, 0f);
        UpdateBestTimeText();
        Debug.Log("[Level3PlayerController] Best time loaded for " + levelKey + ": " + bestTime);

        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Debug.Log("[Level3PlayerController] Screen sleep disabled");

        CalibrateAccelerometer();
        Debug.Log("[Level3PlayerController] Accelerometer calibrated. Zero point: " + calibratedZero);
    }

    void Update()
    {
        // Keyboard restart.
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("[Level3PlayerController] R key pressed — restarting");
            RestartGame();
        }

        // Allow recalibration with C key (useful for testing).
        if (Input.GetKeyDown(KeyCode.C))
        {
            Debug.Log("[Level3PlayerController] C key pressed — recalibrating");
            RecalibrateControls();
        }

        // COUNTDOWN timer while game is active.
        if (!gameWon && !gameOver)
        {
            // Subtract time each frame.
            currentTime -= Time.deltaTime;
            Debug.Log("[Level3PlayerController] Timer counting down: " + currentTime.ToString("F2"));
            UpdateTimerText();

            // Enter warning zone at 10 seconds remaining.
            if (currentTime <= 10f && !inWarningZone)
            {
                inWarningZone = true;
                // Set the next beep time to the nearest whole second.
                nextWarningTime = Mathf.Floor(currentTime);
                Debug.Log("[Level3PlayerController] ENTERED WARNING ZONE! Next beep at: " + nextWarningTime);
            }

            // Play warning beep every second during last 10 seconds.
            if (inWarningZone && currentTime <= nextWarningTime)
            {
                Debug.Log("[Level3PlayerController] Warning beep triggered at: " + currentTime.ToString("F2"));
                if (warningBeep != null)
                {
                    audioSource.PlayOneShot(warningBeep);
                    Debug.Log("[Level3PlayerController] Warning beep sound played");
                }
                else
                {
                    Debug.Log("[Level3PlayerController] warningBeep is NULL — no beep sound played");
                }
                nextWarningTime -= 1f;
                Debug.Log("[Level3PlayerController] Next beep scheduled at: " + nextWarningTime);
            }

            // TIME'S UP! GAME OVER!
            if (currentTime <= 0f)
            {
                Debug.Log("[Level3PlayerController] TIME'S UP! GAME OVER!");

                currentTime = 0f;
                Debug.Log("[Level3PlayerController] Timer clamped to 0");

                UpdateTimerText();

                gameOver = true;
                delayTimer = 0f;
                Debug.Log("[Level3PlayerController] gameOver set to true, delayTimer reset");

                if (gameOverText != null)
                {
                    gameOverText.SetActive(true);
                    Debug.Log("[Level3PlayerController] Game Over text shown");
                }
                else
                {
                    Debug.Log("[Level3PlayerController] gameOverText is NULL — cannot show Game Over");
                }

                if (gameOverSound != null)
                {
                    audioSource.PlayOneShot(gameOverSound);
                    Debug.Log("[Level3PlayerController] Game Over sound played");
                }
                else
                {
                    Debug.Log("[Level3PlayerController] gameOverSound is NULL — no sound played");
                }

                // Stop the ball.
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                Debug.Log("[Level3PlayerController] Ball velocity and angular velocity set to zero");

                // Big shake.
                if (ScreenShake.instance != null)
                {
                    ScreenShake.instance.TriggerShake(0.3f, 0.2f);
                    Debug.Log("[Level3PlayerController] BIG screen shake triggered (0.3, 0.2)");
                }
                else
                {
                    Debug.Log("[Level3PlayerController] ScreenShake.instance is NULL — no shake");
                }
            }
        }

        // Auto-restart after game over (3 second delay).
        if (gameOver)
        {
            delayTimer += Time.deltaTime;
            Debug.Log("[Level3PlayerController] Death timer: " + delayTimer + " / 3.0");
            if (delayTimer >= 3f)
            {
                Debug.Log("[Level3PlayerController] Auto-restarting after death");
                RestartGame();
            }
        }

        // Load next level after win.
        if (gameWon && nextLevelName != "")
        {
            delayTimer += Time.deltaTime;
            Debug.Log("[Level3PlayerController] Win timer: " + delayTimer + " / " + nextLevelDelay);
            if (delayTimer >= nextLevelDelay)
            {
                Debug.Log("[Level3PlayerController] Loading next level: " + nextLevelName);
                SceneManager.LoadScene(nextLevelName);
            }
        }
    }

    void FixedUpdate()
    {
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
            Debug.Log("[Level3PlayerController] FixedUpdate() — Force applied: " + (movement * speed) + " | Velocity: " + rb.velocity + " | Speed: " + rb.velocity.magnitude);
        }
        else
        {
            Debug.Log("[Level3PlayerController] FixedUpdate() — Movement disabled (gameWon: " + gameWon + " | gameOver: " + gameOver + ")");
        }
    }

    // =====================================================
    // INPUT METHODS
    // =====================================================

    // Gets movement direction from phone tilt (mobile).
    private Vector3 GetTiltInput()
    {
        // Step 1: Read the raw accelerometer.
        Vector3 rawTilt = Input.acceleration;
        Debug.Log("[Level3PlayerController] GetTiltInput() — Raw accelerometer: " + rawTilt);

        // Step 2: Subtract the calibrated zero point.
        Vector3 adjustedTilt = rawTilt - calibratedZero;
        Debug.Log("[Level3PlayerController] GetTiltInput() — Adjusted tilt: " + adjustedTilt);

        // Step 3: SMOOTH the tilt values.
        smoothedTilt = Vector3.Lerp(smoothedTilt, adjustedTilt, tiltSmoothing);
        Debug.Log("[Level3PlayerController] GetTiltInput() — Smoothed tilt: " + smoothedTilt);

        // Step 4: CLAMP to safe range.
        float clampedX = Mathf.Clamp(smoothedTilt.x, -maxTiltAngle, maxTiltAngle);
        float clampedY = Mathf.Clamp(smoothedTilt.y, -maxTiltAngle, maxTiltAngle);
        Debug.Log("[Level3PlayerController] GetTiltInput() — Clamped X: " + clampedX + " | Clamped Y: " + clampedY);

        // Step 5: Apply dead zone.
        if (Mathf.Abs(clampedX) < deadZone) clampedX = 0f;
        if (Mathf.Abs(clampedY) < deadZone) clampedY = 0f;
        Debug.Log("[Level3PlayerController] GetTiltInput() — After dead zone — X: " + clampedX + " | Y: " + clampedY);

        // Step 6: Create movement direction.
        Vector3 movement = new Vector3(
            clampedX * tiltSensitivity,
            0.0f,
            clampedY * tiltSensitivity
        );
        Debug.Log("[Level3PlayerController] GetTiltInput() — Final movement: " + movement);

        return movement;
    }

    // Gets movement direction from keyboard (PC testing).
    private Vector3 GetKeyboardInput()
    {
        // Read WASD / Arrow Keys input.
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Debug.Log("[Level3PlayerController] GetKeyboardInput() — H: " + horizontal + " | V: " + vertical);

        // Create movement vector.
        Vector3 movement = new Vector3(
            horizontal * keyboardSpeedMultiplier,
            0.0f,
            vertical * keyboardSpeedMultiplier
        );

        // Prevent diagonal movement from being faster.
        movement = Vector3.ClampMagnitude(movement, 1.0f * keyboardSpeedMultiplier);
        Debug.Log("[Level3PlayerController] GetKeyboardInput() — Final movement: " + movement);

        return movement;
    }

    // =====================================================
    // COLLISION HANDLING
    // =====================================================

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("[Level3PlayerController] OnTriggerEnter() — Hit: " + other.gameObject.name + " | Tag: " + other.gameObject.tag);

        if (gameOver || gameWon)
        {
            Debug.Log("[Level3PlayerController] OnTriggerEnter() — Ignoring collision (gameOver: " + gameOver + " | gameWon: " + gameWon + ")");
            return;
        }

        if (other.gameObject.CompareTag("Pickup"))
        {
            Debug.Log("[Level3PlayerController] Good pickup collected: " + other.gameObject.name);

            other.gameObject.SetActive(false);
            Debug.Log("[Level3PlayerController] Pickup deactivated");

            if (floatingTextPrefab != null)
            {
                Instantiate(
                    floatingTextPrefab,
                    other.transform.position + Vector3.up * 0.5f,
                    Quaternion.identity
                );
                Debug.Log("[Level3PlayerController] Floating text spawned at: " + (other.transform.position + Vector3.up * 0.5f));
            }
            else
            {
                Debug.Log("[Level3PlayerController] floatingTextPrefab is NULL — no floating text spawned");
            }

            score += 1;
            UpdateScoreText();
            Debug.Log("[Level3PlayerController] Score updated to: " + score + " / " + totalPickups);

            if (pickupSound != null)
            {
                float originalPitch = audioSource.pitch;
                audioSource.pitch = Random.Range(0.85f, 1.15f);
                audioSource.PlayOneShot(pickupSound);
                audioSource.pitch = originalPitch;
                Debug.Log("[Level3PlayerController] Pickup sound played with pitch variation");
            }
            else
            {
                Debug.Log("[Level3PlayerController] pickupSound is NULL — no sound played");
            }

            if (ScreenShake.instance != null)
            {
                ScreenShake.instance.TriggerShake(0.1f, 0.08f);
                Debug.Log("[Level3PlayerController] Screen shake triggered (0.1, 0.08)");
            }
            else
            {
                Debug.Log("[Level3PlayerController] ScreenShake.instance is NULL — no shake");
            }

            #if UNITY_ANDROID
            Handheld.Vibrate();
            Debug.Log("[Level3PlayerController] Phone vibration triggered");
            #endif

            if (score >= totalPickups)
            {
                gameWon = true;
                delayTimer = 0f;
                winText.SetActive(true);
                Debug.Log("[Level3PlayerController] GAME WON! Time remaining: " + currentTime);

                if (winSound != null)
                {
                    audioSource.PlayOneShot(winSound);
                    Debug.Log("[Level3PlayerController] Win sound played");
                }
                else
                {
                    Debug.Log("[Level3PlayerController] winSound is NULL — no sound played");
                }

                // Best time = how fast you finished
                // (time limit minus remaining time = actual time spent).
                string levelKey = "BestTime_" + SceneManager.GetActiveScene().name;
                float completionTime = timeLimit - currentTime;
                Debug.Log("[Level3PlayerController] Completion time: " + completionTime + " | Saved best: " + bestTime);

                if (bestTime == 0f || completionTime < bestTime)
                {
                    bestTime = completionTime;
                    PlayerPrefs.SetFloat(levelKey, bestTime);
                    PlayerPrefs.Save();
                    UpdateBestTimeText();
                    Debug.Log("[Level3PlayerController] New best time saved: " + bestTime);
                }
                else
                {
                    Debug.Log("[Level3PlayerController] Current time (" + completionTime + ") did not beat best time (" + bestTime + ")");
                }
            }
        }
    }

    // =====================================================
    // PUBLIC FUNCTIONS
    // =====================================================

    public void RestartGame()
    {
        Debug.Log("[Level3PlayerController] RestartGame() called");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Called by a Recalibrate button (optional).
    // Resets the "center" tilt position to whatever angle
    // the player is currently holding the phone.
    public void RecalibrateControls()
    {
        Debug.Log("[Level3PlayerController] RecalibrateControls() called");
        CalibrateAccelerometer();
        Debug.Log("[Level3PlayerController] New calibrated zero: " + calibratedZero);
    }

    // =====================================================
    // PRIVATE FUNCTIONS
    // =====================================================

    void CalibrateAccelerometer()
    {
        calibratedZero = Input.acceleration;
        smoothedTilt = Vector3.zero;
        Debug.Log("[Level3PlayerController] CalibrateAccelerometer() — Zero set to: " + calibratedZero);
    }

    // Score shows collected / total format.
    void UpdateScoreText()
    {
        scoreText.text = "Score: " + score.ToString() + " / " + totalPickups.ToString();
        Debug.Log("[Level3PlayerController] UpdateScoreText() — " + scoreText.text);
    }

    // Timer changes color based on remaining time!
    void UpdateTimerText()
    {
        string timeString = currentTime.ToString("F1");

        if (currentTime <= 5f)
        {
            // CRITICAL! Less than 5 seconds — BRIGHT RED!
            timerText.color = new Color(1f, 0f, 0f, 1f);
            timerText.text = "TIME: " + timeString;
            Debug.Log("[Level3PlayerController] UpdateTimerText() — CRITICAL (RED): " + timeString);
        }
        else if (currentTime <= 10f)
        {
            // WARNING! Less than 10 seconds — ORANGE!
            timerText.color = new Color(1f, 0.5f, 0f, 1f);
            timerText.text = "TIME: " + timeString;
            Debug.Log("[Level3PlayerController] UpdateTimerText() — WARNING (ORANGE): " + timeString);
        }
        else if (currentTime <= 20f)
        {
            // Getting low! Less than 20 seconds — YELLOW!
            timerText.color = new Color(1f, 1f, 0f, 1f);
            timerText.text = "TIME: " + timeString;
            Debug.Log("[Level3PlayerController] UpdateTimerText() — LOW (YELLOW): " + timeString);
        }
        else
        {
            // Plenty of time — GREEN!
            timerText.color = new Color(0f, 1f, 0f, 1f);
            timerText.text = "TIME: " + timeString;
            Debug.Log("[Level3PlayerController] UpdateTimerText() — SAFE (GREEN): " + timeString);
        }
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
        Debug.Log("[Level3PlayerController] UpdateBestTimeText() — " + bestTimeText.text);
    }
}