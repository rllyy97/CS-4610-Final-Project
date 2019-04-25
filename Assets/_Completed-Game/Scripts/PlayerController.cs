using UnityEngine;

// Include the namespace required to use Unity UI
using UnityEngine.UI;
using UnityEngine.SceneManagement;

using System.Collections;

public class PlayerController : MonoBehaviour {

    // UI Variables
	public Text countText;
	public Text winText;
    public Slider speedBar;
    public Image windowShade;

	private Rigidbody rb;
    private Collider coll;
	private int count;

    // Jump Variables
    private Vector3 jump;
    public bool jumpLate;
    public float jumpScale = 2.0f;
    public float gravity = 2.0f;
    private float groundDistance;

    // Movement Variables
    public float speed;

    // Camera Control
    public Camera cam;
    public OrbitCameraController camController;
    public string rotateCameraXInput = "Mouse X";
    public string rotateCameraYInput = "Mouse Y";

    // Pause + Menu variables
    public bool paused = false;
    private Vector3 savedVelocity;
    private Vector3 savedAngularVelocity;

    // Initialization
    void Start ()
	{
		// Rigidbody and Collider
		rb = GetComponent<Rigidbody>();
        coll = GetComponent<Collider>();

        // Init UI
		count = 0;
		SetCountText();
		winText.text = "";
        windowShade.enabled = false;

        // Init jump variable
        jump = new Vector3(0.0f, 2.0f, 0.0f);
        groundDistance = coll.bounds.extents.y;

        // Handle Cursor
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
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

    }

    // Run once per frame (avoid user input here)
    void FixedUpdate ()
	{
        // Refuse player and camera movement
        if (paused) return;

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
   		if (other.gameObject.CompareTag ("Pick Up"))
		{
			other.gameObject.SetActive (false);
			count = count + 1;
            SetCountText ();
		}
	}

    // Physical Collisions
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Out")) OutOfBounds();
        else GroundCollide();
    }

    void SetCountText()
	{
		countText.text = count.ToString ();
        if (count >= 12) winText.text = "Win Text";
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

    }
    
    void OutOfBounds()
    {
        rb.constraints = RigidbodyConstraints.FreezeAll;
        rb.constraints = RigidbodyConstraints.None;
        rb.transform.position = new Vector3(0.0f, 1.0f, 0.0f);
        jumpLate = false;

        camController.mouseX = 0;
        camController.mouseY = 0;

    }

    void GroundCollide()
    {
        if (jumpLate && IsGrounded()) ForceJump();
        else jumpLate = false;
    }

    void ForceJump()
    {
        Vector3 jumpForce = jump * jumpScale;
        jumpForce.y -= rb.velocity.y * 0.8f;
        if (jumpForce.y < 0) jumpForce.y = 0;
        rb.AddForce(jumpForce, ForceMode.Impulse);
        jumpLate = false;

    }

}