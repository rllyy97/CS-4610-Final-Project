using UnityEngine;

// Include the namespace required to use Unity UI
using UnityEngine.UI;
using UnityEngine.SceneManagement;

using System.Collections;

public class PlayerController : MonoBehaviour {

    // UI Variables
	public Text countText;
    private int count;
    public Text winText;
    public Text timeText;
    private float time;
    public Slider speedBar;
    public Image windowShade;

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
    private bool timerRunning = true;
    private Vector3 savedVelocity;
    private Vector3 savedAngularVelocity;

    // Pickup Variables
    public int pickupMax;

    // Initialization
    void Start ()
	{
		// Init Components
		rb = GetComponent<Rigidbody>();
        coll = GetComponent<Collider>();

        // Init UI
        count = 0;
        countText.text = "0";
		winText.text = "";
        windowShade.enabled = false;

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

    }

    // Checks if close enough to the ground to jump
    bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, groundDistance + 0.25f);
    }

    // Checks if close enough to the ground for a delayed jump
    bool IsAlmostGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, groundDistance + 2.0f);
    }

    // Run consistently (user input)
    void Update()
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
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        // Backspace
        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            OutOfBounds();
        }

        // Enter
        if (Input.GetKeyDown(KeyCode.Return))
        {
            Win();
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
		rb.AddForce (movement * speed);

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
        countText.text = count.ToString ();
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
        winText.text = "Paused";
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
        winText.text = "";
        windowShade.enabled = false;

        // Sound
        alertSource.PlayOneShot(uiUp);
        musicSource.volume = musicVolume;
    }

    void Win()
    {
        timerRunning = false;
        winText.text = "Win Text";
        alertSource.PlayOneShot(winSound);

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

}