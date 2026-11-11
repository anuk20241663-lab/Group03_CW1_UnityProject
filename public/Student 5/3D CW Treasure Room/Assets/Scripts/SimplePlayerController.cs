using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// A super simple First-Person Character Controller.
/// Attach this script to your player Cylinder GameObject.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class SimplePlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Movement speed in meters per second.")]
    public float moveSpeed = 5.0f;

    [Tooltip("Gravity acceleration.")]
    public float gravity = -9.81f;

    [Tooltip("Jump height in meters (set to 0 to disable jumping).")]
    public float jumpHeight = 1.2f;

    [Header("Look / Camera Settings")]
    [Tooltip("Reference to the camera. If left empty, it will automatically find the child camera.")]
    public Transform cameraTransform;

    [Tooltip("Mouse sensitivity multiplier.")]
    public float mouseSensitivity = 1.5f;

    [Tooltip("Minimum vertical look angle (looking down).")]
    public float minPitch = -85f;

    [Tooltip("Maximum vertical look angle (looking up).")]
    public float maxPitch = 85f;

    private CharacterController characterController;
    private float pitch = 0f;
    private float verticalVelocity = 0f;

    /// <summary>
    /// Set to true when interacting with UI (like a chest slider) to unlock the mouse and pause look rotation.
    /// </summary>
    public static bool isInteractingWithUI = false;

    void Awake()
    {
        characterController = GetComponent<CharacterController>();

        // Disable standard CapsuleCollider if present on the cylinder to prevent collision conflicts
        CapsuleCollider defaultCol = GetComponent<CapsuleCollider>();
        if (defaultCol != null)
        {
            defaultCol.enabled = false;
        }

        // Auto-detect camera in children if not manually linked
        if (cameraTransform == null)
        {
            Camera childCam = GetComponentInChildren<Camera>();
            if (childCam != null)
            {
                cameraTransform = childCam.transform;
            }
            else if (Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
        }
    }

    void Start()
    {
        // Lock and hide mouse cursor for seamless first-person looking
        SetCursorLock(true);
    }

    void Update()
    {
        HandleCursorToggle();
        HandleLook();
        HandleMovement();
    }

    /// <summary>
    /// Press Escape to unlock the cursor and left-click the game window to lock it back.
    /// </summary>
    void HandleCursorToggle()
    {
        if (isInteractingWithUI) return;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            SetCursorLock(false);
        }
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            SetCursorLock(true);
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetCursorLock(false);
        }
        if (Input.GetMouseButtonDown(0))
        {
            SetCursorLock(true);
        }
#endif
    }

    public void SetCursorLock(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    /// <summary>
    /// Call this from interaction scripts (like chest slider) to unlock the cursor and prevent camera movement.
    /// </summary>
    public static void SetInteractingWithUI(bool interacting)
    {
        isInteractingWithUI = interacting;
        Cursor.lockState = interacting ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = interacting;
    }

    /// <summary>
    /// Handles mouse look: horizontal turns the cylinder body, vertical tilts the camera.
    /// </summary>
    void HandleLook()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;

        float mouseX = 0f;
        float mouseY = 0f;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();
            mouseX = delta.x * mouseSensitivity * 0.1f;
            mouseY = delta.y * mouseSensitivity * 0.1f;
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * 2.0f;
        mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * 2.0f;
#endif

        // 1. Turn player body horizontally (yaw)
        transform.Rotate(Vector3.up * mouseX);

        // 2. Tilt camera vertically (pitch)
        if (cameraTransform != null)
        {
            pitch -= mouseY;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }

    /// <summary>
    /// Handles player movement using CharacterController with WASD and gravity.
    /// </summary>
    void HandleMovement()
    {
        float inputX = 0f;
        float inputZ = 0f;
        bool jumpPressed = false;

        if (!isInteractingWithUI)
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) inputZ += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) inputZ -= 1f;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) inputX -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) inputX += 1f;

                jumpPressed = Keyboard.current.spaceKey.wasPressedThisFrame;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            inputX = Input.GetAxisRaw("Horizontal");
            inputZ = Input.GetAxisRaw("Vertical");
            jumpPressed = Input.GetKeyDown(KeyCode.Space);
#endif
        }

        // Calculate move direction relative to where the cylinder is facing
        Vector3 move = (transform.right * inputX + transform.forward * inputZ);
        if (move.sqrMagnitude > 1f)
        {
            move.Normalize();
        }

        // Ground check & gravity
        if (characterController.isGrounded)
        {
            if (verticalVelocity < 0f)
            {
                verticalVelocity = -2f; // Slight downward push to keep firmly grounded on slopes
            }

            if (jumpPressed && jumpHeight > 0f)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        // Apply total velocity
        Vector3 velocity = (move * moveSpeed) + (Vector3.up * verticalVelocity);
        characterController.Move(velocity * Time.deltaTime);
    }
}
