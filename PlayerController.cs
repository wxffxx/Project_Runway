using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float sprintSpeed = 10f;
    public float crouchSpeed = 3f;

    [Header("Jump")]
    public float jumpForce = 8f;
    public float gravity = -20f;
    public float coyoteTime = 0.15f;
    public float jumpBufferTime = 0.1f;

    [Header("Crouch")]
    public float crouchHeight = 1f;
    public float standHeight = 2f;
    public float crouchTransitionSpeed = 8f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 100f;
    public Transform cameraTransform;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;

    private float coyoteTimer;
    private float jumpBufferTimer;

    private bool isCrouching;
    private float targetHeight;

    private float xRotation = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        targetHeight = standHeight;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleMouseLook();
        HandleGroundCheck();
        HandleCrouch();
        HandleMovement();
        HandleJump();
        ApplyGravity();
    }

    void HandleMouseLook()
    {
        if (cameraTransform == null) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    void HandleGroundCheck()
    {
        isGrounded = controller.isGrounded;

        if (isGrounded)
        {
            coyoteTimer = coyoteTime;
            if (velocity.y < 0f)
                velocity.y = -2f;
        }
        else
        {
            coyoteTimer -= Time.deltaTime;
        }
    }

    void HandleCrouch()
    {
        bool crouchInput = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C);

        if (crouchInput)
        {
            isCrouching = true;
            targetHeight = crouchHeight;
        }
        else if (isCrouching)
        {
            if (CanStandUp())
            {
                isCrouching = false;
                targetHeight = standHeight;
            }
        }

        float currentHeight = Mathf.Lerp(controller.height, targetHeight, crouchTransitionSpeed * Time.deltaTime);
        Vector3 center = controller.center;
        center.y = currentHeight / 2f;
        controller.height = currentHeight;
        controller.center = center;
    }

    bool CanStandUp()
    {
        Vector3 origin = transform.position;
        return !Physics.Raycast(origin, Vector3.up, standHeight);
    }

    void HandleMovement()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        float currentSpeed;
        if (isCrouching)
            currentSpeed = crouchSpeed;
        else if (Input.GetKey(KeyCode.LeftShift) && isGrounded)
            currentSpeed = sprintSpeed;
        else
            currentSpeed = moveSpeed;

        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        controller.Move(move * currentSpeed * Time.deltaTime);
    }

    void HandleJump()
    {
        if (Input.GetButtonDown("Jump"))
            jumpBufferTimer = jumpBufferTime;
        else
            jumpBufferTimer -= Time.deltaTime;

        bool canJump = coyoteTimer > 0f && !isCrouching;

        if (jumpBufferTimer > 0f && canJump)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
        }
    }

    void ApplyGravity()
    {
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
