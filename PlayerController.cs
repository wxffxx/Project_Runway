using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float sprintSpeed = 10f;
    public float crouchSpeed = 3f;
    [Range(0f, 1f)] public float airControlPercent = 0.5f;
    public bool sprintOnlyForward = true;

    [Header("Sprint Stamina")]
    public float maxStamina = 5f;
    public float staminaDrainPerSecond = 1f;
    public float staminaRecoveryPerSecond = 0.75f;
    public float sprintRecoveryDelay = 0.75f;

    [Header("Jump")]
    public float jumpForce = 8f;
    public float gravity = -20f;
    public float coyoteTime = 0.15f;
    public float jumpBufferTime = 0.1f;
    public float groundedGravity = -2f;
    [Range(0f, 1f)] public float jumpCutMultiplier = 0.5f;

    [Header("Crouch")]
    public float crouchHeight = 1f;
    public float standHeight = 2f;
    public float crouchTransitionSpeed = 8f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 100f;
    public Transform cameraTransform;

    [Header("Camera FOV")]
    public float fovDefault = 60f;
    public float fovSprint = 70f;
    public float fovTransitionSpeed = 8f;

    [Header("Head Bob")]
    public float bobFrequency = 2f;
    public float bobAmplitude = 0.05f;

    [Header("Landing")]
    public float landingDipAmount = 0.08f;
    public float landingDipSpeed = 12f;

    [Header("Double Jump")]
    public int maxJumpCount = 2;
    public float doubleJumpForce = 7f;

    [Header("Slide")]
    public float slideSpeed = 14f;
    public float slideDuration = 0.6f;
    public float slideFovBoost = 10f;

    private CharacterController controller;
    private Camera cam;
    private Vector3 velocity;
    private bool isGrounded;
    private bool jumpQueued;
    private bool wasGrounded;

    private float coyoteTimer;
    private float jumpBufferTimer;
    private float stamina;
    private float sprintRecoveryTimer;

    private bool isCrouching;
    private float targetHeight;

    private float xRotation = 0f;

    private float bobTimer;
    private float bobOffset;
    private float landingDip;
    private float cameraBaseY;

    private int jumpCount;
    private bool isSliding;
    private float slideTimer;
    private Vector3 slideDirection;

    /// <summary>Stamina as 0–1, useful for UI fill bars.</summary>
    public float StaminaNormalized => maxStamina > 0f ? stamina / maxStamina : 0f;
    public bool IsGrounded => isGrounded;
    public bool IsCrouching => isCrouching;
    public bool IsSliding => isSliding;

    void OnValidate()
    {
        crouchHeight = Mathf.Max(0.5f, crouchHeight);
        standHeight = Mathf.Max(crouchHeight, standHeight);
        moveSpeed = Mathf.Max(0f, moveSpeed);
        sprintSpeed = Mathf.Max(moveSpeed, sprintSpeed);
        crouchSpeed = Mathf.Clamp(crouchSpeed, 0f, moveSpeed);
        jumpForce = Mathf.Max(0f, jumpForce);
        crouchTransitionSpeed = Mathf.Max(0f, crouchTransitionSpeed);
        groundedGravity = Mathf.Min(groundedGravity, 0f);
        maxStamina = Mathf.Max(0f, maxStamina);
        staminaDrainPerSecond = Mathf.Max(0f, staminaDrainPerSecond);
        staminaRecoveryPerSecond = Mathf.Max(0f, staminaRecoveryPerSecond);
        sprintRecoveryDelay = Mathf.Max(0f, sprintRecoveryDelay);
        jumpCutMultiplier = Mathf.Clamp01(jumpCutMultiplier);
        fovSprint = Mathf.Max(fovDefault, fovSprint);
        bobAmplitude = Mathf.Max(0f, bobAmplitude);
        landingDipAmount = Mathf.Max(0f, landingDipAmount);
        maxJumpCount = Mathf.Max(1, maxJumpCount);
        doubleJumpForce = Mathf.Max(0f, doubleJumpForce);
        slideSpeed = Mathf.Max(moveSpeed, slideSpeed);
        slideDuration = Mathf.Max(0.1f, slideDuration);
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        stamina = maxStamina;
        targetHeight = standHeight;
        SyncControllerCenter(standHeight);

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (cameraTransform != null)
        {
            cam = cameraTransform.GetComponent<Camera>();
            cameraBaseY = cameraTransform.localPosition.y;
            if (cam != null)
                cam.fieldOfView = fovDefault;
        }

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
        HandleSlide();
        HandleMovement();
        HandleJump();
        UpdateStamina();
        ApplyGravity();
        HandleCameraEffects();
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
        wasGrounded = isGrounded;
        isGrounded = controller.isGrounded;

        if (isGrounded)
        {
            if (!wasGrounded)
            {
                float fallSpeed = Mathf.Abs(velocity.y);
                landingDip = landingDipAmount * Mathf.Clamp(fallSpeed / 10f, 0.3f, 1f);
                jumpQueued = false;
                jumpCount = 0;
            }

            coyoteTimer = coyoteTime;
            if (velocity.y < 0f)
                velocity.y = groundedGravity;
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

    void HandleSlide()
    {
        bool sprintInput = Input.GetKey(KeyCode.LeftShift) && isGrounded && stamina > 0f;
        bool crouchInput = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C);

        if (!isSliding && sprintInput && crouchInput && GetCurrentHorizontalSpeed() > moveSpeed + 0.5f)
        {
            isSliding = true;
            slideTimer = slideDuration;
            slideDirection = transform.forward;
        }

        if (isSliding)
        {
            slideTimer -= Time.deltaTime;
            float t = slideTimer / slideDuration;
            controller.Move(slideDirection * (slideSpeed * t * Time.deltaTime));

            if (slideTimer <= 0f || !isGrounded)
                isSliding = false;
        }
    }

    void HandleMovement()
    {
        if (isSliding) return;

        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");
        Vector3 inputDirection = new Vector3(moveX, 0f, moveZ);
        inputDirection = Vector3.ClampMagnitude(inputDirection, 1f);

        float currentSpeed = GetCurrentSpeed(moveZ);
        float controlPercent = isGrounded ? 1f : airControlPercent;

        Vector3 move = transform.right * inputDirection.x + transform.forward * inputDirection.z;
        controller.Move(move * (currentSpeed * controlPercent * Time.deltaTime));
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

        bool canJump = (coyoteTimer > 0f || jumpCount < maxJumpCount) && !isCrouching && !isSliding;

        if (jumpBufferTimer > 0f && canJump)
        {
            bool isFirstJump = coyoteTimer > 0f;
            float force = isFirstJump ? jumpForce : doubleJumpForce;
            velocity.y = Mathf.Sqrt(force * -2f * gravity);
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            jumpCount++;
        }

        if (Input.GetButtonUp("Jump") && velocity.y > 0f)
            velocity.y *= jumpCutMultiplier;
    }

    void ApplyGravity()
    {
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void HandleCameraEffects()
    {
        if (cameraTransform == null) return;

        // --- Sprint / Slide FOV ---
        if (cam != null)
        {
            bool isSprinting = !isCrouching && isGrounded && GetCurrentHorizontalSpeed() > moveSpeed + 0.5f;
            float targetFov = isSliding ? fovSprint + slideFovBoost
                            : isSprinting ? fovSprint
                            : fovDefault;
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, fovTransitionSpeed * Time.deltaTime);
        }

        // --- Head Bob ---
        bool isMovingOnGround = isGrounded &&
            new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).sqrMagnitude > 0.01f;

        if (isMovingOnGround)
        {
            float speedRatio = GetCurrentHorizontalSpeed() / moveSpeed;
            bobTimer += Time.deltaTime * bobFrequency * speedRatio;
            bobOffset = Mathf.Sin(bobTimer * 2f * Mathf.PI) * bobAmplitude;
        }
        else
        {
            bobTimer = 0f;
            bobOffset = Mathf.Lerp(bobOffset, 0f, Time.deltaTime * 8f);
        }

        // --- Landing Dip Recovery ---
        landingDip = Mathf.Lerp(landingDip, 0f, landingDipSpeed * Time.deltaTime);

        // Apply combined vertical offset to camera local position
        Vector3 localPos = cameraTransform.localPosition;
        localPos.y = cameraBaseY + bobOffset - landingDip;
        cameraTransform.localPosition = localPos;
    }

    void SyncControllerCenter(float height)
    {
        Vector3 center = controller.center;
        center.y = height * 0.5f;
        controller.center = center;
    }

    float GetCurrentSpeed(float moveZ)
    {
        if (isCrouching)
            return crouchSpeed;

        bool isTryingToSprint = Input.GetKey(KeyCode.LeftShift) && isGrounded && stamina > 0f;
        if (sprintOnlyForward)
            isTryingToSprint &= moveZ > 0.1f;

        return isTryingToSprint ? sprintSpeed : moveSpeed;
    }

    void UpdateStamina()
    {
        bool isMoving = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).sqrMagnitude > 0.01f;
        bool isSprinting = !isCrouching && isGrounded && isMoving && Mathf.Approximately(GetCurrentHorizontalSpeed(), sprintSpeed);

        if (isSprinting)
        {
            stamina = Mathf.Max(0f, stamina - staminaDrainPerSecond * Time.deltaTime);
            sprintRecoveryTimer = sprintRecoveryDelay;
            return;
        }

        if (sprintRecoveryTimer > 0f)
        {
            sprintRecoveryTimer -= Time.deltaTime;
            return;
        }

        stamina = Mathf.Min(maxStamina, stamina + staminaRecoveryPerSecond * Time.deltaTime);
    }

    float GetCurrentHorizontalSpeed()
    {
        Vector3 horizontalVelocity = controller.velocity;
        horizontalVelocity.y = 0f;
        return horizontalVelocity.magnitude;
    }

    void SetCursorState(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
