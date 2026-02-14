using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

// 定义一个配置方案结构体，用于快速切换参数
[System.Serializable]
public struct CameraProfile
{
    public string profileName; // 方案名称 (e.g., "Outdoor", "Indoor")
    public float moveSpeed;
    public float moveSmoothing;
    public float rotationSpeed;
    public float zoomSensitivity;
    public Vector2 heightLimit;
    public float collisionRadius; // 碰撞检测半径
}

public class CameraMovementNewInput : MonoBehaviour
{
    // --- 1. 输入动作定义 ---
    [Header("Input Actions (输入绑定)")]
    public InputAction moveAction = new InputAction("Move", binding: "<Gamepad>/leftStick");
    public InputAction verticalAction = new InputAction("Vertical");
    public InputAction rotateAction = new InputAction("Rotate");
    public InputAction orbitActiveAction = new InputAction("OrbitActive", binding: "<Mouse>/middleButton");
    public InputAction lookAction = new InputAction("Look", binding: "<Mouse>/delta");
    public InputAction zoomAction = new InputAction("Zoom", binding: "<Mouse>/scroll/y");
    public InputAction cancelFollowAction = new InputAction("CancelFollow", binding: "<Keyboard>/escape");

    // --- 2. 参数配置系统 ---
    [Header("Profile System (参数方案)")]
    public List<CameraProfile> profiles = new List<CameraProfile>();
    public int defaultProfileIndex = 0;
    
    // 当前使用的运行时参数
    private CameraProfile currentSettings; 

    [Header("General Settings (通用设置)")]
    public float mouseSensitivity = 0.5f;
    public float defaultFocusDistance = 15.0f;
    public float zoomDamping = 5.0f;
    public bool enableBounds = true;

    [Header("Collision & Following (碰撞与跟随)")]
    public LayerMask collisionLayers; // 设置为 Ground, Buildings 等层级
    public float followSmoothTime = 0.1f; // 跟随目标的平滑时间
    
    // --- 内部状态 ---
    private float currentZoomVelocity = 0.0f;
    private bool isOrbiting = false;
    private Vector3 lockedOrbitCenter; // 旋转时的中心点
    
    // 移动相关变量 (保留了你的 Lerp 逻辑)
    private Vector3 currentMoveDir = Vector3.zero; 

    // 跟随系统状态
    private Transform followTarget;
    private Vector3 followOffset; // 锁定时的相对偏移量
    private Vector3 followVelocity; // SmoothDamp 引用变量

    private void Awake()
    {
        InitializeDefaultBindings();
        
        // 初始化默认配置方案
        if (profiles.Count > 0)
        {
            ApplyProfile(profiles[defaultProfileIndex]);
        }
        else
        {
            // 如果没有配置方案，创建一个默认的保底
            CameraProfile fallback = new CameraProfile
            {
                profileName = "Default",
                moveSpeed = 10f,
                moveSmoothing = 0.2f,
                rotationSpeed = 80f,
                zoomSensitivity = 0.05f,
                heightLimit = new Vector2(1f, 50f),
                collisionRadius = 0.5f
            };
            ApplyProfile(fallback);
        }
    }

    private void OnEnable()
    {
        moveAction.Enable();
        verticalAction.Enable();
        rotateAction.Enable();
        orbitActiveAction.Enable();
        lookAction.Enable();
        zoomAction.Enable();
        cancelFollowAction.Enable();

        orbitActiveAction.started += OnOrbitStarted;
        orbitActiveAction.canceled += OnOrbitCanceled;
        cancelFollowAction.performed += _ => ClearFollowTarget();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        verticalAction.Disable();
        rotateAction.Disable();
        orbitActiveAction.Disable();
        lookAction.Disable();
        zoomAction.Disable();
        cancelFollowAction.Disable();

        orbitActiveAction.started -= OnOrbitStarted;
        orbitActiveAction.canceled -= OnOrbitCanceled;
    }

    void Update()
    {
        // 如果正在跟随目标，逻辑会有所不同
        if (followTarget != null)
        {
            HandleFollowMovement();
        }
        else
        {
            HandleManualMovement();
        }

        HandleRotation();
        HandleOrbit();
        HandleZoomInput();
    }

    void LateUpdate()
    {
        if (followTarget == null)
        {
            ApplyZoomPhysics();
        }
        
        // 先应用边界，再应用碰撞，确保不会卡出边界外但在墙里
        ApplyBounds();
        ApplyCollisionCorrection(); 
    }

    // --- 功能模块：参数切换 ---
    public void ApplyProfile(CameraProfile profile)
    {
        currentSettings = profile;
        Debug.Log($"[Camera] Switched to profile: {profile.profileName}");
    }

    public void ApplyProfile(string profileName)
    {
        var p = profiles.Find(x => x.profileName == profileName);
        if (!string.IsNullOrEmpty(p.profileName))
        {
            ApplyProfile(p);
        }
    }

    // --- 功能模块：自动跟随 ---
    public void SetFollowTarget(Transform target)
    {
        followTarget = target;
        // 计算当前相机和目标的偏移量，保持相对位置
        followOffset = transform.position - target.position;
        // 如果偏移量太小（相机在目标内部），重置一个默认偏移
        if (followOffset.sqrMagnitude < 1f)
        {
            followOffset = -target.forward * 10f + Vector3.up * 5f;
        }
    }

    public void ClearFollowTarget()
    {
        followTarget = null;
        currentMoveDir = Vector3.zero; // 停止跟随时的惯性
    }

    // --- 核心逻辑：移动 ---
    
    // 1. 跟随模式下的移动
    void HandleFollowMovement()
    {
        // 任何手动的位移输入都会打断跟随
        Vector2 inputRaw = moveAction.ReadValue<Vector2>();
        if (inputRaw.sqrMagnitude > 0.01f || verticalAction.ReadValue<float>() != 0)
        {
            ClearFollowTarget();
            return;
        }

        if (followTarget == null) return;

        Vector3 targetPos = followTarget.position + followOffset;
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref followVelocity, followSmoothTime);
    }

    // 2. 手动模式下的移动 (保留了你修复回抽问题的 Lerp 逻辑)
    void HandleManualMovement()
    {
        Vector2 inputRaw = moveAction.ReadValue<Vector2>();
        float vInputRaw = verticalAction.ReadValue<float>();

        Vector3 forward = transform.forward;
        Vector3 right = transform.right;
        forward.y = 0;
        right.y = 0;
        
        Vector3 targetMoveDir = (forward.normalized * inputRaw.y + right.normalized * inputRaw.x);
        targetMoveDir += Vector3.up * vInputRaw;

        // 使用配置方案中的 smoothing
        float lerpSpeed = 1.0f / Mathf.Max(0.001f, currentSettings.moveSmoothing); 
        currentMoveDir = Vector3.Lerp(currentMoveDir, targetMoveDir, Time.deltaTime * lerpSpeed);

        if (currentMoveDir.sqrMagnitude < 0.001f && targetMoveDir == Vector3.zero)
        {
            currentMoveDir = Vector3.zero;
        }

        if (currentMoveDir.sqrMagnitude > 0.0001f)
        {
            // 使用配置方案中的 speed
            transform.position += currentMoveDir * currentSettings.moveSpeed * Time.deltaTime;
        }
    }

    // --- 核心逻辑：旋转与观察 ---
    void HandleRotation()
    {
        float rInput = rotateAction.ReadValue<float>();
        if (rInput != 0f)
        {
            // 如果在跟随模式，围绕目标旋转；否则围绕视线落点旋转
            Vector3 focus = (followTarget != null) ? followTarget.position : GetCurrentFocusPoint();
            
            float angle = rInput * currentSettings.rotationSpeed * Time.deltaTime;
            transform.RotateAround(focus, Vector3.up, angle);

            // 如果在跟随，旋转后需要更新偏移量，否则相机会弹回去
            if (followTarget != null)
            {
                followOffset = transform.position - followTarget.position;
            }
        }
    }

    private void OnOrbitStarted(InputAction.CallbackContext ctx)
    {
        isOrbiting = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // 确定旋转轴心
        if (followTarget != null)
            lockedOrbitCenter = followTarget.position;
        else
            lockedOrbitCenter = GetCurrentFocusPoint();
    }

    private void OnOrbitCanceled(InputAction.CallbackContext ctx)
    {
        isOrbiting = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void HandleOrbit()
    {
        if (isOrbiting)
        {
            Vector2 mouseDelta = lookAction.ReadValue<Vector2>();
            float mouseX = mouseDelta.x * mouseSensitivity; 
            float mouseY = mouseDelta.y * mouseSensitivity;

            // 围绕轴心旋转
            transform.RotateAround(lockedOrbitCenter, Vector3.up, mouseX);
            transform.RotateAround(lockedOrbitCenter, transform.right, -mouseY);

            // 修正：确保Z轴水平，防止相机歪斜
            Vector3 euler = transform.eulerAngles;
            euler.z = 0;
            transform.eulerAngles = euler;

            // 如果在跟随模式，轨道旋转需要更新偏移量
            if (followTarget != null)
            {
                followOffset = transform.position - followTarget.position;
            }
        }
    }

    // --- 核心逻辑：缩放 ---
    void HandleZoomInput()
    {
        float scrollValue = zoomAction.ReadValue<float>();
        if (Mathf.Abs(scrollValue) > 0.01f)
        {
            // 如果在跟随模式，直接缩进/拉远偏移量
            if (followTarget != null)
            {
                // 计算当前距离
                float currentDist = followOffset.magnitude;
                float targetDist = Mathf.Clamp(currentDist - scrollValue * currentSettings.zoomSensitivity * 10f, 2.0f, 100f);
                followOffset = followOffset.normalized * targetDist;
            }
            else
            {
                // 手动模式下保持物理惯性缩放
                currentZoomVelocity += scrollValue * currentSettings.zoomSensitivity;
            }
        }
    }

    void ApplyZoomPhysics()
    {
        if (Mathf.Abs(currentZoomVelocity) > 0.001f)
        {
            Vector3 proposedMove = transform.forward * currentZoomVelocity * Time.deltaTime;
            
            // 简单预判：如果缩放会导致碰撞，就停止缩放
            if (!Physics.CheckSphere(transform.position + proposedMove, currentSettings.collisionRadius, collisionLayers))
            {
                transform.position += proposedMove;
            }
            else
            {
                currentZoomVelocity = 0; // 撞墙停止
            }
            
            currentZoomVelocity = Mathf.Lerp(currentZoomVelocity, 0f, zoomDamping * Time.deltaTime);
        }
    }

    // --- 核心逻辑：限制与碰撞 ---
    void ApplyBounds()
    {
        if (!enableBounds) return;
        Vector3 pos = transform.position;
        pos.y = Mathf.Clamp(pos.y, currentSettings.heightLimit.x, currentSettings.heightLimit.y);
        transform.position = pos;
    }

    void ApplyCollisionCorrection()
    {
        // 使用球形重叠检测当前位置是否在碰撞体内
        if (Physics.CheckSphere(transform.position, currentSettings.collisionRadius, collisionLayers))
        {
            // 如果卡住了，尝试往反方向（通常是上方或后方）推
            // 这里使用一个简单的逻辑：找到最近的非碰撞点有点复杂，
            // 简单的做法是：向 Focus Point 的反方向推，或者直接向上推
            
            // 1. 获取逃逸向量：从碰撞中心向外
            Collider[] hits = Physics.OverlapSphere(transform.position, currentSettings.collisionRadius, collisionLayers);
            if (hits.Length > 0)
            {
                Collider col = hits[0];
                // 获取最近点
                Vector3 closestPoint = col.ClosestPoint(transform.position);
                Vector3 pushDir = (transform.position - closestPoint).normalized;
                
                // 防止 pushDir 为 0 (完全重合)
                if (pushDir == Vector3.zero) pushDir = Vector3.up;

                // 强制推离
                float pushDist = currentSettings.collisionRadius - Vector3.Distance(transform.position, closestPoint);
                transform.position += pushDir * (pushDist + 0.05f); // 额外增加一点缓冲
            }
        }
    }

    // --- 辅助函数 ---
    private Vector3 GetCurrentFocusPoint()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        // 使用 collisionLayers 确保视点落在合法的物体上，而不是穿过墙壁看外面
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, collisionLayers))
        {
            return hit.point;
        }
        return transform.position + transform.forward * defaultFocusDistance;
    }

    private void InitializeDefaultBindings()
    {
        if (moveAction.bindings.Count == 0)
        {
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
        }
        if (verticalAction.bindings.Count == 0)
        {
            verticalAction.AddCompositeBinding("1DAxis")
                .With("Positive", "<Keyboard>/f")
                .With("Negative", "<Keyboard>/c");
        }
        if (rotateAction.bindings.Count == 0)
        {
            rotateAction.AddCompositeBinding("1DAxis")
                .With("Positive", "<Keyboard>/e")
                .With("Negative", "<Keyboard>/q");
        }
    }
}
