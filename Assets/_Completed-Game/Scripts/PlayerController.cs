using UnityEngine;

// Include the namespace required to use Unity UI
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Firebase;
using Firebase.Database;
using Firebase.Unity.Editor;

using System.Collections;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour {

    // UI Variables
	private Text countText;
    private int count;
    private Text winText;
    private Text timeText;
    private float time;
    private Slider speedBar;
    private Image windowShade;
    private InputField playerNameInput;
    private Button submitScoreButton;
    private Text leaderboardNames;
    private Text leaderboardTimes;
    private Text leaderboardNumbers;
    private Button restartButton;
    private Button menuButton;
    private Button exitButton;
    private Button resumeButton;

    // Sound Variables
    public AudioSource bounceSource;
    public AudioClip bounceSound;

    public AudioSource rollSource;
    public AudioClip rollSound;

    public AudioSource alertSource;
    public AudioClip clickSound;
    public AudioClip fallSound;
    public AudioClip pickupSound;
    public AudioClip winSound;
    public AudioClip uiUp;
    public AudioClip uiDown;
    public AudioClip hopSound;

    public AudioSource musicSource;
    public float musicVolume = 0.2f;

    private readonly float lowPitchRange = .6F;
    private readonly float highPitchRange = 1.0F;

    // Physical Components
    private Rigidbody rb;
    private Collider coll;

    // Jump Variables
    private Vector3 jump;
    public bool jumpLate;
    public float jumpScale = 2.0f;
    public float gravity = 2.0f;
    private float groundDistance;

    // Movement Variables
    public float speed;
    public float maxSpeed = 80;
    public bool rolling;

    // Camera Control
    public Camera cam;
    public OrbitCameraController camController;
    public string rotateCameraXInput = "Mouse X";
    public string rotateCameraYInput = "Mouse Y";
    public float minFOV = 60.0f;
    private float deltaFOV = 0.0f;

    // Pause + Menu variables
    public bool paused = false;
    public bool won = false;
    private bool timerRunning = true;
    private Vector3 savedVelocity;
    private Vector3 savedAngularVelocity;

    // Database Variables
    public Dictionary<string, float> leaderBoard = new Dictionary<string, float>();
    private const int MaxScores = 10;
    private string playerName = "testPlayer";
    private float finishTime = 100;
    const int kMaxLogSize = 16382;
    DependencyStatus dependencyStatus = DependencyStatus.UnavailableOther;
    protected bool isFirebaseInitialized = false;

    // Pickup Variables
    private int pickupMax;

    // Initialization
    void Start ()
	{
		// Init Components
		rb = GetComponent<Rigidbody>();
        coll = GetComponent<Collider>();
        pickupMax = GameObject.FindGameObjectsWithTag("Pick Up").Length;

        // Init UI
        countText = GameObject.Find("Pickup Count Text").GetComponent<Text>();
        timeText = GameObject.Find("Time Text").GetComponent<Text>();
        winText = GameObject.Find("Win Text").GetComponent<Text>();
        speedBar = GameObject.Find("Speed Bar").GetComponent<Slider>();
        windowShade = GameObject.Find("Window Shade").GetComponent<Image>();
        playerNameInput = GameObject.Find("Player Name Input").GetComponent<InputField>();
        submitScoreButton = GameObject.Find("Submit Score Button").GetComponent<Button>();
        leaderboardNames = GameObject.Find("Leaderboard Names").GetComponent<Text>();
        leaderboardTimes = GameObject.Find("Leaderboard Times").GetComponent<Text>();
        leaderboardNumbers = GameObject.Find("Leaderboard Numbers").GetComponent<Text>();

        restartButton = GameObject.Find("Restart Button").GetComponent<Button>();
        menuButton = GameObject.Find("Menu Button").GetComponent<Button>();
        exitButton = GameObject.Find("Exit Button").GetComponent<Button>();
        resumeButton = GameObject.Find("Resume Button").GetComponent<Button>();

        count = 0;
        countText.text = "0 / " + pickupMax;
        winText.text = "";
        windowShade.enabled = false;

        leaderboardNames.enabled = false;
        leaderboardTimes.enabled = false;
        leaderboardNumbers.enabled = false;
        playerNameInput.gameObject.SetActive(false);
        submitScoreButton.gameObject.SetActive(false);

        restartButton.gameObject.SetActive(false);
        menuButton.gameObject.SetActive(false);
        exitButton.gameObject.SetActive(false);
        resumeButton.gameObject.SetActive(false);

        // Init jump variable
        jump = new Vector3(0.0f, 2.0f, 0.0f);
        groundDistance = coll.bounds.extents.y;

        // Handle Cursor
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // Music
        musicSource.loop = true;
        musicSource.volume = musicVolume;
        musicSource.Play();

        // Firebase 
        leaderBoard.Clear();
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
            dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available) InitializeFirebase();
            else Debug.LogError("Could not resolve all Firebase dependencies: " + dependencyStatus);
        });
        

    }

    // Initialize Firebase (duh)
    protected virtual void InitializeFirebase()
    {
        FirebaseApp app = FirebaseApp.DefaultInstance;
        app.SetEditorDatabaseUrl("https://smemrmg2sf.firebaseio.com/");
        if (app.Options.DatabaseUrl != null) app.SetEditorDatabaseUrl(app.Options.DatabaseUrl);
        StartListener();
        isFirebaseInitialized = true;

        FirebaseDatabase.DefaultInstance.GetReference("leaderboard")
            .Child(SceneManager.GetActiveScene().name).OrderByChild("time").GetValueAsync().ContinueWith(task =>
            {
                if (task.IsCompleted)
                {
                    DataSnapshot snapshot = task.Result;
                    foreach (var child in snapshot.Children)
                        leaderBoard.Add(child.Child("playerName").Value.ToString(), (float) child.Child("time").Value);
                };
            });
        
    }

    protected void StartListener()
    {
        FirebaseDatabase.DefaultInstance.GetReference("leaderboard").Child(SceneManager.GetActiveScene().name).OrderByChild("time")
          .ValueChanged += (object sender2, ValueChangedEventArgs e2) => {
              if (e2.DatabaseError != null) { Debug.LogError(e2.DatabaseError.Message); return; }
              leaderBoard.Clear();
              if (e2.Snapshot != null && e2.Snapshot.ChildrenCount > 0)
                  foreach (var child in e2.Snapshot.Children)
                      leaderBoard.Add(child.Child("playerName").Value.ToString(), (float)child.Child("time").Value);
          };
    }

    TransactionResult AddScoreTransaction(MutableData mutableData)
    {
        List<object> leaders = mutableData.Value as List<object>;
        if (leaders == null) leaders = new List<object>();
        else if (mutableData.ChildrenCount >= MaxScores)
        {
            long maxTime = 0;
            object maxVal = null;
            foreach (var child in leaders)
            {
                if (!(child is Dictionary<string, object>)) continue;
                long childTime = (long)((Dictionary<string, object>)child)["time"];
                if (childTime > maxTime)
                {
                    maxTime = childTime;
                    maxVal = child;
                }
            }
            // Not fast enough
            if (maxTime < finishTime) return TransactionResult.Abort();
            // Kick lowest score
            leaders.Remove(maxVal);
        }

        // Insert new player record time
        Dictionary<string, object> newScoreMap = new Dictionary<string, object>();
        newScoreMap["time"] = finishTime;
        newScoreMap["playerName"] = playerName;
        leaders.Add(newScoreMap);

        mutableData.Value = leaders;
        return TransactionResult.Success(mutableData);
    }

    public void AddScore()
    {
        DatabaseReference reference = FirebaseDatabase.DefaultInstance.GetReference("leaderboard").Child(SceneManager.GetActiveScene().name);
        reference.RunTransaction(AddScoreTransaction);

    }

    // Checks if close enough to the ground to jump
    bool IsGrounded()
    {
        if (Physics.Raycast(transform.position, Vector3.down, groundDistance + 0.25f)) return true;
        if (Physics.Raycast(transform.position, Vector3.down + Vector3.left, groundDistance + 0.20f)) return true;
        if (Physics.Raycast(transform.position, Vector3.down + Vector3.right, groundDistance + 0.20f)) return true;
        if (Physics.Raycast(transform.position, Vector3.down + Vector3.forward, groundDistance + 0.20f)) return true;
        if (Physics.Raycast(transform.position, Vector3.down + Vector3.back, groundDistance + 0.20f)) return true;
        return false;
    }

    // Checks if close enough to the ground for a delayed jump
    bool IsAlmostGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, groundDistance + 2.0f);
    }

    // Run consistently (user input)
    void Update()
    {
        if (!won)
        {
            // ESCAPE
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (paused) Unpause();
                else Pause();
            }

            // SPACE
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (IsGrounded()) ForceJump();
                else if (IsAlmostGrounded() && rb.velocity.normalized.y < 0) jumpLate = true;
            }

            // R
            if (Input.GetKeyDown(KeyCode.R)) SceneManager.LoadScene(SceneManager.GetActiveScene().name);

            // Backspace
            if (Input.GetKeyDown(KeyCode.Backspace)) OutOfBounds();

            // Enter
            if (Input.GetKeyDown(KeyCode.Return)) Win();
        }
    }

    // Run once per frame (avoid user input here)
    void FixedUpdate ()
	{
        // Refuse player and camera movement
        if (paused) return;

        // Timer
        if (timerRunning)
        {
            time += Time.deltaTime;
            timeText.text = time.ToString("0.00") + " sec";
        }

        // Set some local float variables equal to the value of our Horizontal and Vertical Inputs
        float horizontalAxis = Input.GetAxis ("Horizontal");
		float verticalAxis = Input.GetAxis ("Vertical");

        // Setup Camera
        CameraInput();

        // Move relative to camera
        var forward = cam.transform.forward;
        var right = cam.transform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();
        var desiredMoveDirection = forward * verticalAxis + right * horizontalAxis;
        Vector3 movement = new Vector3 (desiredMoveDirection.x, 0.0f, desiredMoveDirection.z);
        var controlAcceleration = (maxSpeed - (Vector3.Dot(rb.velocity, movement.normalized))) / maxSpeed;
		rb.AddForce (movement * speed * controlAcceleration);

        // Gravity
        rb.AddForce(Vector3.down * gravity * rb.mass);

        speedBar.value = rb.velocity.magnitude;
        rollSource.volume = rb.velocity.magnitude / 25;

        // Dynamic FOV
        deltaFOV = (deltaFOV + rb.velocity.magnitude) / 2;
        cam.fieldOfView = minFOV + deltaFOV;
    }

    // Control Camera
    protected virtual void CameraInput()
    {
        var Y = Input.GetAxis(rotateCameraYInput);
        var X = Input.GetAxis(rotateCameraXInput);
        camController.RotateCamera(X, Y);
    }

    // Non-Rigid Collisions
    void OnTriggerEnter(Collider other) 
	{
        if (other.gameObject.CompareTag("Pick Up")) Pickup(other);
	}

    // Rigid Collisions
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Out")) OutOfBounds();
        else GroundCollide(collision.relativeVelocity.magnitude);
    }

    // STAY Collisions
    void OnCollisionStay(Collision collisionInfo)
    {
        if (rolling == false)
        {
            rolling = true;
            rollSource.loop = true;
            rollSource.Play();
        }
        
    }

    private void OnCollisionExit(Collision collision)
    {
        rolling = false;
        rollSource.Stop();
    }

    // Pickup Event
    void Pickup(Collider pickup)
	{
        pickup.gameObject.SetActive(false);
        count++;
        countText.text = count.ToString() + " / " + pickupMax;
        if (count >= pickupMax) Win();
        else alertSource.PlayOneShot(pickupSound);
	}

    // Out of Bounds
    void OutOfBounds()
    {
        rb.constraints = RigidbodyConstraints.FreezeAll;
        rb.constraints = RigidbodyConstraints.None;
        rb.transform.position = new Vector3(0.0f, 1.0f, 0.0f);
        jumpLate = false;

        camController.mouseX = 0;
        camController.mouseY = 0;

        alertSource.PlayOneShot(fallSound);

    }

    // Collision with Rigid object
    void GroundCollide(float rv)
    {
        bounceSource.pitch = Random.Range(lowPitchRange, highPitchRange);
        float hitVolume = rv / 40;
        bounceSource.PlayOneShot(bounceSound, hitVolume);
        if (jumpLate && IsGrounded()) ForceJump();
        else jumpLate = false;
    }

    // Jump on ground contact
    void ForceJump()
    {
        Vector3 jumpForce = jump * jumpScale;
        jumpForce.y -= rb.velocity.y * 0.8f;
        if (jumpForce.y < 0) jumpForce.y = 0;
        rb.AddForce(jumpForce, ForceMode.Impulse);
        jumpLate = false;
        alertSource.PlayOneShot(hopSound, 1.2f);

    }

    void Pause()
    {
        paused = true;

        // Freeze
        savedVelocity = rb.velocity;
        savedAngularVelocity = rb.angularVelocity;
        rb.constraints = RigidbodyConstraints.FreezeAll;

        // UI Changes
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        restartButton.gameObject.SetActive(true);
        menuButton.gameObject.SetActive(true);
        exitButton.gameObject.SetActive(true);
        resumeButton.gameObject.SetActive(true);

        windowShade.enabled = true;

        // Sound
        rolling = false;
        rollSource.volume = 0;
        rollSource.Stop();
        alertSource.PlayOneShot(uiDown);
        musicSource.volume = musicVolume / 2;

    }

    void Unpause()
    {
        paused = false;

        // Unfreeze
        rb.constraints = RigidbodyConstraints.None;
        rb.AddForce(savedVelocity, ForceMode.VelocityChange);
        rb.AddTorque(savedAngularVelocity, ForceMode.VelocityChange);

        // UI Changes
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        restartButton.gameObject.SetActive(false);
        menuButton.gameObject.SetActive(false);
        exitButton.gameObject.SetActive(false);
        resumeButton.gameObject.SetActive(false);

        windowShade.enabled = false;

        // Sound
        alertSource.PlayOneShot(uiUp);
        musicSource.volume = musicVolume;

    }

    void Win()
    {
        timerRunning = false;
        paused = true;
        won = true;

        // Freeze
        savedVelocity = rb.velocity;
        savedAngularVelocity = rb.angularVelocity;
        rb.constraints = RigidbodyConstraints.FreezeAll;

        // UI Changes
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        windowShade.enabled = true;
        winText.text = "Stage Complete!\nTime: " + timeText.text;
        speedBar.gameObject.SetActive(false);
        countText.enabled = false;
        timeText.enabled = false;

        // Sound
        rolling = false;
        rollSource.volume = 0;
        rollSource.Stop();
        alertSource.PlayOneShot(winSound);
        musicSource.volume = musicVolume / 2;

        finishTime = time;
        playerNameInput.gameObject.SetActive(true);
        playerNameInput.Select();
        playerNameInput.ActivateInputField();
        submitScoreButton.gameObject.SetActive(true);
        submitScoreButton.interactable = true;
        submitScoreButton.onClick.AddListener(delegate {
            if (playerNameInput.text != "")
            {
                playerName = playerNameInput.text;
                AddScore();
                playerNameInput.gameObject.SetActive(false);
                submitScoreButton.gameObject.SetActive(false);
                winText.enabled = false;

                leaderBoard.Add(playerName, finishTime);

                showLeaderboard();
            }
        });

        

        /*
        // Blink Time
        while (true)
        {
            timeText.enabled = true;
            yield return new WaitForSeconds(0.5f);
            timeText.enabled = false;
            yield return new WaitForSeconds(0.5f);

        }
        */

    }

    void showLeaderboard()
    {

        // alertSource.PlayOneShot(winSound);

        // Activate Leaderboard UI
        leaderboardNames.enabled = true;
        leaderboardTimes.enabled = true;
        leaderboardNumbers.enabled = true;

        var count = 1;
        // Add Numbers, Names, Times
        foreach (var child in leaderBoard)
        {
            leaderboardNames.text = leaderboardNames.text + "\n" + child.Key.ToString();
            leaderboardTimes.text = leaderboardTimes.text + "\n" + child.Value.ToString();
            leaderboardNumbers.text = leaderboardNumbers.text + "\n" + count + ".";
            count++;
        }

    }

    void hideLeaderboard()
    {
        // Hide Leaderboard UI
        leaderboardNames.enabled = false;
        leaderboardTimes.enabled = false;
        leaderboardNumbers.enabled = false;
    }

    // Pause Screen Button Calls
    public void ResumeClick() { Unpause(); }
    public void RestartClick() { SceneManager.LoadScene(SceneManager.GetActiveScene().name); }
    public void MenuClick() { SceneManager.LoadScene("Menu"); }
    public void ExitClick() { Application.Quit(); }

}