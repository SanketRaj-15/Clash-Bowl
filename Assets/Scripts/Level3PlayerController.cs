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

    // =====================================================
    // LIFECYCLE FUNCTIONS
    // =====================================================

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();

        score = 0;
        gameWon = false;
        gameOver = false;
        delayTimer = 0f;
        inWarningZone = false;
        nextWarningTime = 0f;

        // Timer starts at the time limit and counts DOWN!
        currentTime = timeLimit;

        winText.SetActive(false);
        if (gameOverText != null) gameOverText.SetActive(false);

        UpdateScoreText();
        UpdateTimerText();

        string levelKey = "BestTime_" + SceneManager.GetActiveScene().name;
        bestTime = PlayerPrefs.GetFloat(levelKey, 0f);
        UpdateBestTimeText();

        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        CalibrateAccelerometer();
    }

    void Update()
    {
        // Keyboard restart.
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartGame();
        }

        // COUNTDOWN timer while game is active.
        if (!gameWon && !gameOver)
        {
            // Subtract time each frame.
            currentTime -= Time.deltaTime;
            UpdateTimerText();

            // Enter warning zone at 10 seconds remaining.
            if (currentTime <= 10f && !inWarningZone)
            {
                inWarningZone = true;
                // Set the next beep time to the nearest whole second.
                nextWarningTime = Mathf.Floor(currentTime);
            }

            // Play warning beep every second during last 10 seconds.
            if (inWarningZone && currentTime <= nextWarningTime)
            {
                if (warningBeep != null)
                {
                    audioSource.PlayOneShot(warningBeep);
                }
                nextWarningTime -= 1f;
            }

            // TIME'S UP! GAME OVER!
            if (currentTime <= 0f)
            {
                currentTime = 0f;
                UpdateTimerText();
                gameOver = true;
                delayTimer = 0f;

                if (gameOverText != null)
                {
                    gameOverText.SetActive(true);
                }

                if (gameOverSound != null)
                {
                    audioSource.PlayOneShot(gameOverSound);
                }

                // Stop the ball.
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;

                // Big shake.
                if (ScreenShake.instance != null)
                {
                    ScreenShake.instance.TriggerShake(0.3f, 0.2f);
                }
            }
        }

        // Auto-restart after game over (3 second delay).
        if (gameOver)
        {
            delayTimer += Time.deltaTime;
            if (delayTimer >= 3f)
            {
                RestartGame();
            }
        }

        // Load next level after win.
        if (gameWon && nextLevelName != "")
        {
            delayTimer += Time.deltaTime;
            if (delayTimer >= nextLevelDelay)
            {
                SceneManager.LoadScene(nextLevelName);
            }
        }
    }

    void FixedUpdate()
    {
        if (!gameWon && !gameOver)
        {
            Vector3 rawTilt = Input.acceleration;
            Vector3 adjustedTilt = rawTilt - calibratedZero;

            float clampedX = Mathf.Clamp(adjustedTilt.x, -maxTiltAngle, maxTiltAngle);
            float clampedY = Mathf.Clamp(adjustedTilt.y, -maxTiltAngle, maxTiltAngle);

            if (Mathf.Abs(clampedX) < deadZone) clampedX = 0f;
            if (Mathf.Abs(clampedY) < deadZone) clampedY = 0f;

            Vector3 movement = new Vector3(
                clampedX * tiltSensitivity,
                0.0f,
                clampedY * tiltSensitivity
            );
            rb.AddForce(movement * speed);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (gameOver || gameWon) return;

        if (other.gameObject.CompareTag("Pickup"))
        {
            other.gameObject.SetActive(false);

            if (floatingTextPrefab != null)
            {
                Instantiate(
                    floatingTextPrefab,
                    other.transform.position + Vector3.up * 0.5f,
                    Quaternion.identity
                );
            }

            score += 1;
            UpdateScoreText();

            if (pickupSound != null)
            {
                float originalPitch = audioSource.pitch;
                audioSource.pitch = Random.Range(0.85f, 1.15f);
                audioSource.PlayOneShot(pickupSound);
                audioSource.pitch = originalPitch;
            }

            if (ScreenShake.instance != null)
            {
                ScreenShake.instance.TriggerShake(0.1f, 0.08f);
            }

            #if UNITY_ANDROID
            Handheld.Vibrate();
            #endif

            if (score >= totalPickups)
            {
                gameWon = true;
                delayTimer = 0f;
                winText.SetActive(true);

                if (winSound != null)
                {
                    audioSource.PlayOneShot(winSound);
                }

                // Best time = how fast you finished
                // (time limit minus remaining time = actual time spent).
                string levelKey = "BestTime_" + SceneManager.GetActiveScene().name;
                float completionTime = timeLimit - currentTime;
                if (bestTime == 0f || completionTime < bestTime)
                {
                    bestTime = completionTime;
                    PlayerPrefs.SetFloat(levelKey, bestTime);
                    PlayerPrefs.Save();
                    UpdateBestTimeText();
                }
            }
        }
    }

    // =====================================================
    // PUBLIC FUNCTIONS
    // =====================================================

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

    // Score shows collected / total format.
    void UpdateScoreText()
    {
        scoreText.text = "Score: " + score.ToString() + " / " + totalPickups.ToString();
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
        }
        else if (currentTime <= 10f)
        {
            // WARNING! Less than 10 seconds — ORANGE!
            timerText.color = new Color(1f, 0.5f, 0f, 1f);
            timerText.text = "TIME: " + timeString;
        }
        else if (currentTime <= 20f)
        {
            // Getting low! Less than 20 seconds — YELLOW!
            timerText.color = new Color(1f, 1f, 0f, 1f);
            timerText.text = "TIME: " + timeString;
        }
        else
        {
            // Plenty of time — GREEN!
            timerText.color = new Color(0f, 1f, 0f, 1f);
            timerText.text = "TIME: " + timeString;
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
    }
}
