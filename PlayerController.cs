using UnityEngine;

[RequireComponent(typeof(CharacterController))]
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
    private bool jumpQueued;

    private float coyoteTimer;
    private float jumpBufferTimer;

    private bool isCrouching;
    private float targetHeight;

    private float xRotation = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        targetHeight = standHeight;
        SyncControllerCenter(standHeight);

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            SetCursorState(false);
        else if (Input.GetMouseButtonDown(0))
            SetCursorState(true);

        if (Input.GetButtonDown("Jump"))
            jumpQueued = true;

        HandleMouseLook();
        HandleGroundCheck();
        HandleCrouch();
        HandleMovement();
        HandleJump();
        ApplyGravity();
    }

    void HandleMouseLook()
    {
        if (cameraTransform == null || Cursor.lockState != CursorLockMode.Locked) return;

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
        controller.height = currentHeight;
        SyncControllerCenter(currentHeight);
    }

    bool CanStandUp()
    {
        float castDistance = standHeight - controller.height;
        if (castDistance <= 0f)
            return true;

        Vector3 worldCenter = transform.TransformPoint(controller.center);
        float currentTop = worldCenter.y + controller.height * 0.5f - controller.radius;
        Vector3 origin = new Vector3(worldCenter.x, currentTop, worldCenter.z);
        float radius = Mathf.Max(0.01f, controller.radius * 0.95f);

        return !Physics.SphereCast(origin, radius, Vector3.up, out _, castDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
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
        if (jumpQueued)
        {
            jumpBufferTimer = jumpBufferTime;
            jumpQueued = false;
        }
        else
        {
            jumpBufferTimer -= Time.deltaTime;
        }

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

    void SyncControllerCenter(float height)
    {
        Vector3 center = controller.center;
        center.y = height * 0.5f;
        controller.center = center;
    }

    void SetCursorState(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
