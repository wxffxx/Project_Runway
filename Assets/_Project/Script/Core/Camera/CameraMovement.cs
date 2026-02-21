using UnityEngine;
using UnityEngine.InputSystem;

public class CameraMovement : MonoBehaviour
{
    [Header("Movement (移动)")]
    public float moveSpeed = 10.0f; // WASD 和 FC 的移动速度
    [Tooltip("新输入系统下模拟旧版 Axis 的加速度和减速度 (Sensitivity / Gravity)")]
    public float moveAccel = 10f;
    public float moveDecel = 10f;

    [Header("Rotation & Orbit (旋转与环绕)")]
    public float rotationSpeed = 80.0f; // QE 的环绕速度
    public float mouseSensitivity = 0.5f; // 鼠标灵敏度 (用于环绕)

    [Header("Mouse Controls (鼠标控制)")]
    [Tooltip("滚轮缩放的“灵敏度”或“力度” (新输入系统数值较大，默认设为 0.01 左右较合适)")]
    public float zoomSensitivity = 0.01f;
    [Tooltip("缩放速度的“衰减”速度。值越大，停止越快。")]
    public float zoomDamping = 5.0f;

    [Header("Default Focus Distance (默认焦点距离)")]
    [Tooltip("当射线未碰到任何物体时，环绕的默认焦点距离")]
    public float defaultFocusDistance = 15.0f;

    [Header("Movement Bounds (移动范围限制)")]
    public bool clampY = true;
    public float minY = 1f;
    public float maxY = 50f;
    public bool clampX = false;
    public float minX = -100f;
    public float maxX = 100f;
    public bool clampZ = false;
    public float minZ = -100f;
    public float maxZ = 100f;

    [Header("Input Actions (输入绑定)")]
    public InputAction moveAction = new InputAction("Move");
    public InputAction verticalAction = new InputAction("Vertical");
    public InputAction rotateAction = new InputAction("Rotate");
    public InputAction orbitBtnAction = new InputAction("OrbitBtn", binding: "<Mouse>/middleButton");
    public InputAction lookAction = new InputAction("Look", binding: "<Mouse>/delta");
    public InputAction zoomAction = new InputAction("Zoom", binding: "<Mouse>/scroll/y");

    // 内部变量
    private float zoomVelocity = 0.0f; 
    private Vector3 currentInput = Vector3.zero; // 用于追踪模拟的旧版输入轴值
    private Vector3 currentMoveVelocity = Vector3.zero;
    private Vector3 smoothedMoveDir = Vector3.zero;
    private bool isOrbiting = false;

    private void Awake()
    {
        // 自动设置默认的按键绑定，防止在面板没配置相关 Asset 时动不了
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

    private void OnEnable()
    {
        moveAction.Enable();
        verticalAction.Enable();
        rotateAction.Enable();
        orbitBtnAction.Enable();
        lookAction.Enable();
        zoomAction.Enable();

        orbitBtnAction.started += OnOrbitStarted;
        orbitBtnAction.canceled += OnOrbitCanceled;
    }

    private void OnDisable()
    {
        moveAction.Disable();
        verticalAction.Disable();
        rotateAction.Disable();
        orbitBtnAction.Disable();
        lookAction.Disable();
        zoomAction.Disable();

        orbitBtnAction.started -= OnOrbitStarted;
        orbitBtnAction.canceled -= OnOrbitCanceled;
    }

    private void OnOrbitStarted(InputAction.CallbackContext ctx)
    {
        isOrbiting = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnOrbitCanceled(InputAction.CallbackContext ctx)
    {
        isOrbiting = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        // 如果游戏暂停，直接返回，不处理任何输入
        if (Time.timeScale == 0f) 
        {
            // 如果在按住中键时暂停，清理一下环绕状态和鼠标锁定
            if (isOrbiting)
            {
                isOrbiting = false;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            return;
        }

        // 将输入采集与核心数值计算放在 Update 保证响应极速，不卡顿
        HandleMovementInput();
        HandleRotationInput();
        HandleOrbitInput();
        HandleZoomInput();
    }

    private void LateUpdate()
    {
        // 如果游戏暂停，不应用任何物理移动或缩放缓动
        if (Time.timeScale == 0f) return;

        // 1. 应用键盘移动 (基于模拟GetAxis的值)
        if (smoothedMoveDir.sqrMagnitude > 0.0001f)
        {
            transform.position += smoothedMoveDir * moveSpeed * Time.deltaTime;
        }

        // 2. 应用鼠标滚轮滑动平滑 (Damped Zoom)
        if (Mathf.Abs(zoomVelocity) > 0.001f)
        {
            float moveThisFrame = zoomVelocity * Time.deltaTime;
            transform.position += transform.forward * moveThisFrame;
            zoomVelocity = Mathf.Lerp(zoomVelocity, 0f, zoomDamping * Time.deltaTime);
        }
        else
        {
            zoomVelocity = 0f;
        }

        // 3. 应用位置限制 (Clamping)
        ApplyBounds();
    }

    private void HandleMovementInput()
    {
        // --- WASD 水平移动与 FC 垂直升降 ---
        Vector2 rawMove2D = moveAction.ReadValue<Vector2>();
        float rawMoveV = verticalAction.ReadValue<float>();

        // 模拟旧版 Input.GetAxis 的“回弹”和“死区”
        // 旧版输入系统中，当松开按键时，值会根据 Gravity 快速归零。
        // 使用 SmoothDamp 会导致向量在接近目标速度 0 时缓慢滑行或过冲，也就是您说的“停下来的瞬间回弹/卡顿(果冻感)”。
        
        // 我们改为直接对输入轴进行 Lerp 逼近，如果用户没有输入，则快速阻尼到 0
        float accelRate = moveAccel; // 相当于旧版的 Sensitivity
        float decelRate = moveDecel; // 相当于旧版的 Gravity

        // X轴平滑
        if (Mathf.Abs(rawMove2D.x) > 0.1f)
            currentInput.x = Mathf.MoveTowards(currentInput.x, rawMove2D.x, accelRate * Time.deltaTime);
        else
            currentInput.x = Mathf.MoveTowards(currentInput.x, 0f, decelRate * Time.deltaTime);

        // Y轴(前后)平滑
        if (Mathf.Abs(rawMove2D.y) > 0.1f)
            currentInput.y = Mathf.MoveTowards(currentInput.y, rawMove2D.y, accelRate * Time.deltaTime);
        else
            currentInput.y = Mathf.MoveTowards(currentInput.y, 0f, decelRate * Time.deltaTime);

        // Z轴(上下FC)平滑
        if (Mathf.Abs(rawMoveV) > 0.1f)
            currentInput.z = Mathf.MoveTowards(currentInput.z, rawMoveV, accelRate * Time.deltaTime);
        else
            currentInput.z = Mathf.MoveTowards(currentInput.z, 0f, decelRate * Time.deltaTime);


        Vector3 camForward = transform.forward;
        Vector3 camRight = transform.right;
        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        // 结合相机方向计算最终移动向量
        smoothedMoveDir = (camForward * currentInput.y + camRight * currentInput.x) + (Vector3.up * currentInput.z);
    }

    private void HandleRotationInput()
    {
        // --- QE 左右环绕 (基于焦点) ---
        float rotateInput = rotateAction.ReadValue<float>();
        if (rotateInput != 0f)
        {
            Vector3 focusPoint = GetFocusPoint();
            float angle = rotateInput * rotationSpeed * Time.deltaTime;
            transform.RotateAround(focusPoint, Vector3.up, angle);
        }
    }

    private void HandleOrbitInput()
    {
        // --- 鼠标中键 - 环绕视角 ---
        if (isOrbiting)
        {
            Vector3 focusPoint = GetFocusPoint();
            // 在新输入系统中，鼠标 delta 是按帧度量的真实像素移动量，无需再乘 Time.deltaTime
            Vector2 lookInput = lookAction.ReadValue<Vector2>();
            float mouseX = lookInput.x * mouseSensitivity;
            float mouseY = lookInput.y * mouseSensitivity;

            // 左右环绕 (Yaw) - 绕着世界Y轴
            transform.RotateAround(focusPoint, Vector3.up, mouseX);
            
            // 上下环绕 (Pitch) - 绕着摄像机本地的X轴 (使用 -mouseY 反转Y轴)
            transform.RotateAround(focusPoint, transform.right, -mouseY);

            // 修正Z轴，防止因为欧拉角计算误差导致相机倾斜
            Vector3 euler = transform.eulerAngles;
            euler.z = 0f;
            transform.eulerAngles = euler;
        }
    }

    private void HandleZoomInput()
    {
        // --- 鼠标滚轮缩放采集 ---
        float scroll = zoomAction.ReadValue<float>();
        if (scroll != 0f)
        {
            zoomVelocity += scroll * zoomSensitivity;
        }
    }

    private void ApplyBounds()
    {
        Vector3 currentPos = transform.position;
        if (clampY) currentPos.y = Mathf.Clamp(currentPos.y, minY, maxY);
        if (clampX) currentPos.x = Mathf.Clamp(currentPos.x, minX, maxX);
        if (clampZ) currentPos.z = Mathf.Clamp(currentPos.z, minZ, maxZ);
        transform.position = currentPos;
    }

    private Vector3 GetFocusPoint()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, Mathf.Infinity))
        {
            // 1. 射线碰到了物体：使用碰撞点作为旋转中心
            return hit.point;
        }
        else
        {
            // 2. 射线未碰到物体 (例如看向天空)：由于默认距离在摄像机前方创建一个“虚拟”旋转中心
            return transform.position + (transform.forward * defaultFocusDistance);
        }
    }
}