using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float sprintSpeed = 12f;
    public float crouchSpeed = 2.5f;
    public float sprintAcceleration = 10f;
    [Range(0f, 1f)] public float airControlPercent = 0.5f;
    public bool sprintOnlyForward = true;

    [Header("Sprint Stamina")]
    public float maxStamina = 8f;
    public float staminaDrainPerSecond = 1f;
    public float staminaRecoveryPerSecond = 0.75f;
    public float sprintRecoveryDelay = 0.75f;

    [Header("Jump")]
    public float jumpForce = 7f;
    public float gravity = -22f;
    public float coyoteTime = 0.15f;
    public float jumpBufferTime = 0.1f;
    public float groundedGravity = -2f;
    [Range(0f, 1f)] public float jumpCutMultiplier = 0.5f;

    [Header("Crouch")]
    public float crouchHeight = 1f;
    public float standHeight = 2f;
    public float crouchTransitionSpeed = 8f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 80f;
    public Transform cameraTransform;

    [Header("Camera FOV")]
    public float fovDefault = 60f;
    public float fovSprint = 70f;
    public float fovTransitionSpeed = 8f;

    [Header("Camera Tilt")]
    public float tiltAngle = 5f;
    public float tiltSpeed = 8f;

    [Header("Head Bob")]
    public float bobFrequency = 2f;
    public float bobAmplitude = 0.07f;

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

    [Header("Dash")]
    public KeyCode dashKey = KeyCode.LeftAlt;
    public float dashSpeed = 25f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 1.5f;
    public float dashStaminaCost = 1f;

    [Header("Health & Fall Damage")]
    public float maxHealth = 150f;
    public float fallDamageMinSpeed = 10f;
    public float fallDamageMaxSpeed = 25f;
    public float fallDamageMax = 50f;
    [Range(1f, 3f)] public float fallDamageExponent = 2f;

    [Header("Wall Run")]
    public float wallRunSpeed = 8f;
    public float wallRunGravity = -4f;
    public float wallRunDuration = 1.2f;
    public float wallRunDetectDistance = 0.65f;
    public float wallJumpForce = 7f;
    public float wallJumpSideForce = 5f;
    public float wallJumpCooldown = 0.4f;
    public float wallRunTiltAngle = 15f;
    public float wallRunFov = 75f;
    public LayerMask wallRunLayers = ~0;

    [Header("Footstep")]
    public float footstepWalkInterval = 0.5f;
    public float footstepSprintMultiplier = 0.6f;

    [Header("Health Regeneration")]
    public bool enableHealthRegen = false;
    public float healthRegenRate = 5f;
    public float healthRegenDelay = 5f;

    [Header("Stamina Exhaustion")]
    [Range(0f, 1f)] public float staminaExhaustionThreshold = 0.25f;
    [Range(0f, 1f)] public float exhaustionSpeedMultiplier = 0.65f;

    [Header("Events")]
    public UnityEvent onFootstep;
    public UnityEvent onJump;
    public UnityEvent onLand;
    public UnityEvent onDeath;
    public UnityEvent onDash;
    public UnityEvent onSlide;

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
    private float currentTilt = 0f;

    private float bobTimer;
    private float bobOffset;
    private float landingDip;
    private float cameraBaseY;

    private int jumpCount;
    private bool isSliding;
    private float slideTimer;
    private Vector3 slideDirection;

    private bool isDashing;
    private float dashTimer;
    private float dashCooldownTimer;
    private Vector3 dashDirection;

    private float health;
    private float peakFallSpeed;
    private float healthRegenTimer;

    private bool isExhausted;
    private float footstepTimer;
    private float smoothedSpeed;

    // Wall run state
    private bool isWallRunning;
    private float wallRunTimer;
    private Vector3 wallNormal;
    private bool wallOnLeft;
    private bool wallOnRight;
    private float wallJumpCooldownTimer;

    /// <summary>Stamina as 0–1, useful for UI fill bars.</summary>
    public float StaminaNormalized => maxStamina > 0f ? stamina / maxStamina : 0f;
    /// <summary>Health as 0–1, useful for UI fill bars.</summary>
    public float HealthNormalized => maxHealth > 0f ? health / maxHealth : 0f;
    public float Health => health;
    public bool IsGrounded => isGrounded;
    public bool IsCrouching => isCrouching;
    public bool IsSliding => isSliding;
    public bool IsDashing => isDashing;
    public bool IsWallRunning => isWallRunning;
    public bool IsDead => health <= 0f;
    public bool IsSprinting => !isCrouching && isGrounded && !isExhausted &&
        Input.GetKey(KeyCode.LeftShift) && stamina > 0f &&
        (!sprintOnlyForward || Input.GetAxisRaw("Vertical") > 0.1f);
    public bool IsExhausted => isExhausted;

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
        wallRunFov = Mathf.Max(fovSprint, wallRunFov);
        bobAmplitude = Mathf.Max(0f, bobAmplitude);
        landingDipAmount = Mathf.Max(0f, landingDipAmount);
        maxJumpCount = Mathf.Max(1, maxJumpCount);
        doubleJumpForce = Mathf.Max(0f, doubleJumpForce);
        slideSpeed = Mathf.Max(moveSpeed, slideSpeed);
        slideDuration = Mathf.Max(0.1f, slideDuration);
        dashSpeed = Mathf.Max(0f, dashSpeed);
        dashDuration = Mathf.Max(0.05f, dashDuration);
        dashCooldown = Mathf.Max(0f, dashCooldown);
        maxHealth = Mathf.Max(1f, maxHealth);
        fallDamageMaxSpeed = Mathf.Max(fallDamageMinSpeed, fallDamageMaxSpeed);
        footstepWalkInterval = Mathf.Max(0.1f, footstepWalkInterval);
        wallRunDuration = Mathf.Max(0.1f, wallRunDuration);
        wallRunDetectDistance = Mathf.Max(0.1f, wallRunDetectDistance);
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        stamina = maxStamina;
        health = maxHealth;
        targetHeight = standHeight;
        wallRunTimer = wallRunDuration;
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
        if (IsDead) return;

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
        HandleDash();
        HandleWallRun();
        HandleMovement();
        HandleJump();
        UpdateStamina();
        ApplyGravity();
        HandleCameraEffects();
        HandleFootsteps();
        HandleHealthRegen();
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

        if (!isGrounded && velocity.y < 0f)
            peakFallSpeed = Mathf.Max(peakFallSpeed, Mathf.Abs(velocity.y));

        if (isGrounded)
        {
            if (!wasGrounded)
            {
                float fallSpeed = Mathf.Abs(velocity.y);
                landingDip = landingDipAmount * Mathf.Clamp(fallSpeed / 10f, 0.3f, 1f);
                ApplyFallDamage(peakFallSpeed);
                if (!IsDead) onLand?.Invoke();
                peakFallSpeed = 0f;
                jumpQueued = false;
                jumpCount = 0;
            }

            wallRunTimer = wallRunDuration;
            coyoteTimer = coyoteTime;
            if (velocity.y < 0f)
                velocity.y = groundedGravity;
        }
        else
        {
            coyoteTimer -= Time.deltaTime;
        }
    }

    void ApplyFallDamage(float fallSpeed)
    {
        if (fallSpeed <= fallDamageMinSpeed) return;

        float t = Mathf.InverseLerp(fallDamageMinSpeed, fallDamageMaxSpeed, fallSpeed);
        float damage = Mathf.Pow(t, fallDamageExponent) * fallDamageMax;
        bool wasAlive = !IsDead;
        health = Mathf.Max(0f, health - damage);
        healthRegenTimer = healthRegenDelay;
        if (wasAlive && IsDead) onDeath?.Invoke();
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
            onSlide?.Invoke();
        }

        if (isSliding)
        {
            // Jump out of slide with a forward boost
            if (jumpBufferTimer > 0f)
            {
                isSliding = false;
                velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
                controller.Move(slideDirection * (slideSpeed * 0.4f * Time.deltaTime));
                jumpBufferTimer = 0f;
                jumpCount++;
                onJump?.Invoke();
                return;
            }

            slideTimer -= Time.deltaTime;
            float t = slideTimer / slideDuration;
            controller.Move(slideDirection * (slideSpeed * t * Time.deltaTime));

            if (slideTimer <= 0f || !isGrounded)
                isSliding = false;
        }
    }

    void HandleDash()
    {
        if (dashCooldownTimer > 0f)
            dashCooldownTimer -= Time.deltaTime;

        if (!isDashing && Input.GetKeyDown(dashKey) && !isCrouching && dashCooldownTimer <= 0f && stamina >= dashStaminaCost)
        {
            float moveX = Input.GetAxisRaw("Horizontal");
            float moveZ = Input.GetAxisRaw("Vertical");
            Vector3 input = new Vector3(moveX, 0f, moveZ);

            dashDirection = input.sqrMagnitude > 0.01f
                ? (transform.right * input.x + transform.forward * input.z).normalized
                : transform.forward;

            isDashing = true;
            dashTimer = dashDuration;
            stamina = Mathf.Max(0f, stamina - dashStaminaCost);
            dashCooldownTimer = dashCooldown;
            velocity.y = 0f;
            onDash?.Invoke();
        }

        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            controller.Move(dashDirection * (dashSpeed * Time.deltaTime));

            if (dashTimer <= 0f)
                isDashing = false;
        }
    }

    void HandleWallRun()
    {
        // Detect walls on left and right
        wallOnLeft = Physics.Raycast(transform.position, -transform.right, out RaycastHit hitLeft,
            wallRunDetectDistance, wallRunLayers, QueryTriggerInteraction.Ignore);
        wallOnRight = Physics.Raycast(transform.position, transform.right, out RaycastHit hitRight,
            wallRunDetectDistance, wallRunLayers, QueryTriggerInteraction.Ignore);

        wallNormal = wallOnLeft ? hitLeft.normal : (wallOnRight ? hitRight.normal : Vector3.up);

        bool wallDetected = wallOnLeft || wallOnRight;
        bool hasSpeed = GetCurrentHorizontalSpeed() > moveSpeed * 0.5f;
        bool canStart = !isGrounded && wallDetected && hasSpeed && wallRunTimer > 0f
                        && !isCrouching && !isDashing;

        if (canStart && !isWallRunning)
            isWallRunning = true;

        if (!isWallRunning) return;

        // Stop conditions
        if (!wallDetected || isGrounded || wallRunTimer <= 0f)
        {
            isWallRunning = false;
            return;
        }

        wallRunTimer -= Time.deltaTime;

        // Move forward along the wall surface
        Vector3 wallForward = Vector3.Cross(wallNormal, Vector3.up);
        if (Vector3.Dot(wallForward, transform.forward) < 0f)
            wallForward = -wallForward;

        controller.Move(wallForward * wallRunSpeed * Time.deltaTime);

        // Override vertical velocity to reduced wall gravity
        velocity.y = Mathf.Max(velocity.y + gravity * Time.deltaTime, wallRunGravity);
    }

    void HandleMovement()
    {
        if (isSliding || isDashing || isWallRunning) return;

        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");
        Vector3 inputDirection = new Vector3(moveX, 0f, moveZ);
        inputDirection = Vector3.ClampMagnitude(inputDirection, 1f);

        float targetSpeed = GetCurrentSpeed(moveZ);
        smoothedSpeed = Mathf.Lerp(smoothedSpeed, targetSpeed, sprintAcceleration * Time.deltaTime);
        float controlPercent = isGrounded ? 1f : airControlPercent;

        Vector3 move = transform.right * inputDirection.x + transform.forward * inputDirection.z;
        controller.Move(move * (smoothedSpeed * controlPercent * Time.deltaTime));
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

        if (wallJumpCooldownTimer > 0f)
            wallJumpCooldownTimer -= Time.deltaTime;

        // Wall jump — push away from wall and upward
        if (jumpBufferTimer > 0f && isWallRunning && wallJumpCooldownTimer <= 0f)
        {
            isWallRunning = false;
            wallRunTimer = 0f;
            velocity.y = Mathf.Sqrt(wallJumpForce * -2f * gravity);
            Vector3 wallPush = wallNormal * wallJumpSideForce;
            controller.Move(wallPush * Time.deltaTime);
            jumpBufferTimer = 0f;
            wallJumpCooldownTimer = wallJumpCooldown;
            onJump?.Invoke();
            return;
        }

        bool canJump = (coyoteTimer > 0f || jumpCount < maxJumpCount) && !isCrouching && !isSliding && !isDashing;

        if (jumpBufferTimer > 0f && canJump)
        {
            bool isFirstJump = coyoteTimer > 0f;
            float force = isFirstJump ? jumpForce : doubleJumpForce;
            velocity.y = Mathf.Sqrt(force * -2f * gravity);
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            jumpCount++;
            onJump?.Invoke();
        }

        if (Input.GetButtonUp("Jump") && velocity.y > 0f)
            velocity.y *= jumpCutMultiplier;
    }

    void ApplyGravity()
    {
        if (isDashing || isWallRunning) return;

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void HandleCameraEffects()
    {
        if (cameraTransform == null) return;

        // --- FOV ---
        if (cam != null)
        {
            bool isSprinting = !isCrouching && isGrounded && GetCurrentHorizontalSpeed() > moveSpeed + 0.5f;
            float targetFov = isWallRunning ? wallRunFov
                            : isDashing     ? fovSprint + slideFovBoost
                            : isSliding     ? fovSprint + slideFovBoost
                            : isSprinting   ? fovSprint
                            : fovDefault;
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, fovTransitionSpeed * Time.deltaTime);
        }

        // --- Camera Tilt (strafe lean + wall run lean) ---
        float strafeInput = Input.GetAxisRaw("Horizontal");
        float wallRunTilt = isWallRunning ? (wallOnLeft ? wallRunTiltAngle : -wallRunTiltAngle) : 0f;
        float targetTilt = isWallRunning ? wallRunTilt : -strafeInput * tiltAngle;
        currentTilt = Mathf.Lerp(currentTilt, targetTilt, tiltSpeed * Time.deltaTime);

        // --- Head Bob ---
        bool isMovingOnGround = isGrounded &&
            new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).sqrMagnitude > 0.01f;

        if (isMovingOnGround && !isSliding && !isDashing)
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

        Vector3 localPos = cameraTransform.localPosition;
        localPos.y = cameraBaseY + bobOffset - landingDip;
        cameraTransform.localPosition = localPos;

        Quaternion pitchRotation = Quaternion.Euler(xRotation, 0f, currentTilt);
        cameraTransform.localRotation = pitchRotation;
    }

    void HandleFootsteps()
    {
        bool isMovingOnGround = isGrounded && !isSliding && !isDashing &&
            new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).sqrMagnitude > 0.01f;

        if (!isMovingOnGround)
        {
            footstepTimer = footstepWalkInterval;
            return;
        }

        float sprintMult = GetCurrentHorizontalSpeed() > moveSpeed + 0.5f ? footstepSprintMultiplier : 1f;
        float interval = footstepWalkInterval * sprintMult;

        footstepTimer -= Time.deltaTime;
        if (footstepTimer <= 0f)
        {
            onFootstep?.Invoke();
            footstepTimer = interval;
        }
    }

    void HandleHealthRegen()
    {
        if (!enableHealthRegen || IsDead || health >= maxHealth) return;

        healthRegenTimer -= Time.deltaTime;
        if (healthRegenTimer <= 0f)
            health = Mathf.Min(maxHealth, health + healthRegenRate * Time.deltaTime);
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

        if (isExhausted)
            return moveSpeed * exhaustionSpeedMultiplier;

        bool isTryingToSprint = Input.GetKey(KeyCode.LeftShift) && isGrounded && stamina > 0f && !isExhausted;
        if (sprintOnlyForward)
            isTryingToSprint &= moveZ > 0.1f;

        return isTryingToSprint ? sprintSpeed : moveSpeed;
    }

    void UpdateStamina()
    {
        bool isMoving = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).sqrMagnitude > 0.01f;
        bool isTryingToSprint = Input.GetKey(KeyCode.LeftShift) && isGrounded && !isCrouching && isMoving && stamina > 0f && !isExhausted;
        if (sprintOnlyForward)
            isTryingToSprint &= Input.GetAxisRaw("Vertical") > 0.1f;

        if (isTryingToSprint)
        {
            stamina = Mathf.Max(0f, stamina - staminaDrainPerSecond * Time.deltaTime);
            sprintRecoveryTimer = sprintRecoveryDelay;
            if (stamina <= 0f)
                isExhausted = true;
            return;
        }

        if (sprintRecoveryTimer > 0f)
        {
            sprintRecoveryTimer -= Time.deltaTime;
            return;
        }

        stamina = Mathf.Min(maxStamina, stamina + staminaRecoveryPerSecond * Time.deltaTime);

        if (isExhausted && stamina >= maxStamina * staminaExhaustionThreshold)
            isExhausted = false;
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

    /// <summary>Restore health. Clamped to maxHealth.</summary>
    public void Heal(float amount) => health = Mathf.Min(maxHealth, health + amount);

    /// <summary>Deal damage. Returns true if this caused death.</summary>
    public bool TakeDamage(float amount)
    {
        bool wasAlive = !IsDead;
        health = Mathf.Max(0f, health - amount);
        healthRegenTimer = healthRegenDelay;
        if (wasAlive && IsDead) onDeath?.Invoke();
        return IsDead;
    }
}
