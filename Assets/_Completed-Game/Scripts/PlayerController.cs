using UnityEngine;

// Include the namespace required to use Unity UI
using UnityEngine.UI;

using System.Collections;

public class PlayerController : MonoBehaviour {
	
	// Create public variables for player speed, and for the Text UI game objects
	public float speed;

    // UI Variables
	public Text countText;
	public Text winText;
    public Slider speedBar;
    public Image windowShade;

	// Create private references to the rigidbody component on the player, and the count of pick up objects picked up so far
	private Rigidbody rb;
    private Collider coll;
	private int count;

    // Jump Variables
    private Vector3 jump;
    public bool jumpLate;
    public float jumpScale = 2.0f;
    public float gravity = 2.0f;
    private float groundDistance;


    // Camera Control
    public Camera cam;
    public OrbitCameraController camController;
    public string rotateCameraXInput = "Mouse X";
    public string rotateCameraYInput = "Mouse Y";

    // Pause + Menu variables
    public bool paused = false;
    private Vector3 savedVelocity;
    private Vector3 savedAngularVelocity;

    // At the start of the game..
    void Start ()
	{
		// Assign the Rigidbody component to our private rb variable
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

    bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, groundDistance + 0.2f);
    }

    bool IsAlmostGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, groundDistance + 2.0f);
    }

    // Run consistently (user input)
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (paused) Unpause();
            else Pause();
        }

        if (Input.GetKeyDown(KeyCode.Space) && IsGrounded())
        {
            rb.AddForce(jump * jumpScale, ForceMode.Impulse);
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

        // Get directions relative to camera
        var forward = cam.transform.forward;
        var right = cam.transform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();
        var desiredMoveDirection = forward * verticalAxis + right * horizontalAxis;


        // Create a Vector3 variable, and assign X and Z to feature our horizontal and vertical float variables above
        Vector3 movement = new Vector3 (desiredMoveDirection.x, 0.0f, desiredMoveDirection.z);

		// Add a physical force to our Player rigidbody using our 'movement' Vector3 above, 
		// multiplying it by 'speed' - our public player speed that appears in the inspector
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

    // Control Collisions
    void OnTriggerEnter(Collider other) 
	{
        // Control Pick Up Event
   		if (other.gameObject.CompareTag ("Pick Up"))
		{
			// Make the other game object (the pick up) inactive, to make it disappear
			other.gameObject.SetActive (false);

			// Add one to the score variable 'count'
			count = count + 1;

			// Run the 'SetCountText()' function
			SetCountText ();
		}
	}

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Out"))
        {
            OutOfBounds();
        }
    }

    // Create a standalone function that can update the 'countText' UI and check if the required amount to win has been achieved
    void SetCountText()
	{
		// Update the text field of our 'countText' variable
		countText.text = count.ToString ();

		// Check if our 'count' is equal to or exceeded 12
		if (count >= 12) 
		{
			// Set the text value of our 'winText'
			winText.text = "Win Text";
		}
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

    }

}