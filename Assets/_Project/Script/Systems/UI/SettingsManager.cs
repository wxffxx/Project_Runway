using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class SettingsManager : MonoBehaviour
{
    [Header("--- 标签页按钮 (Tabs) ---")]
    public Button tabDisplayBtn;
    public Button tabQualityBtn;
    public Button tabControlsBtn;
    public Button tabGameBtn;

    [Header("--- 子面板 (Panels) ---")]
    public GameObject displayPanel;
    public GameObject qualityPanel;
    public GameObject controlsPanel;
    public GameObject gamePanel;

    [Header("--- 显示设置 (Display) UI组件 ---")]
    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown windowModeDropdown;
    public TMP_Dropdown framerateDropdown;

    [Header("--- 控制设置 (Controls) UI组件 ---")]
    public Slider mouseSensitivitySlider;
    public TMP_Text mouseSensitivityValueText;
    public Toggle invertYToggle;

    [Header("--- 按键绑定 (Keybinds) 动态生成组件 ---")]
    public Transform keybindsContentParent; // Scroll View 的 Content 节点
    public GameObject keybindRowPrefab;     // 我们刚才写的 KeybindRow 预制体

    [Header("--- 游戏设置 (Game) UI组件 ---")]
    public Toggle showFPSToggle;

    // 核心数据缓存
    private readonly Vector2Int[] resolutions = new Vector2Int[]
    {
        new Vector2Int(1280, 720),
        new Vector2Int(1920, 1080),
        new Vector2Int(2560, 1440),
        new Vector2Int(3840, 2160)
    };
    private readonly int[] framerates = new int[] { 30, 60, 120, 144, -1 };

    private CameraMovement cameraMovement;
    private FPSCounter fpsCounter;

    private void Awake()
    {
        cameraMovement = FindFirstObjectByType<CameraMovement>();
        fpsCounter = FindFirstObjectByType<FPSCounter>();
    }

    private void Start()
    {
        // 1. 游戏启动一瞬间：不问缘由直接强制读取存档应用底层画面 (避免任何 UI 坑)
        ApplyStartupDisplaySettings();

        // 2. 绑定页面切换按钮
        if (tabDisplayBtn) tabDisplayBtn.onClick.AddListener(() => SwitchTab(displayPanel));
        if (tabQualityBtn) tabQualityBtn.onClick.AddListener(() => SwitchTab(qualityPanel));
        if (tabControlsBtn) tabControlsBtn.onClick.AddListener(() => SwitchTab(controlsPanel));
        if (tabGameBtn) tabGameBtn.onClick.AddListener(() => SwitchTab(gamePanel));

        // 3. 读取本地保存的用户自定义按键
        LoadAllKeybindOverrides();

        // 4. 悄悄初始化 UI 的文字和选项 (且绝不引发改变事件)
        InitUI();
        
        // 5. 动态生成按键绑定的 UI 列表
        InitKeybindsUI();

        // 4. 重置状态，默认打开显示面板
        SwitchTab(displayPanel);
    }

    // ==========================================
    // 强制画面初始化
    // ==========================================
    private void ApplyStartupDisplaySettings()
    {
        // 注意：换了新的保存 Key，避免被之前崩坏的注册表毒害
        int resIdx = PlayerPrefs.GetInt("Set_ResIdx", 1);    // 默认 1080p
        int modeIdx = PlayerPrefs.GetInt("Set_WinModeIdx", 2); // 默认 窗口化 (Windowed)
        int fpsIdx = PlayerPrefs.GetInt("Set_FpsIdx", 1);    // 默认 60帧

        Vector2Int res = resolutions[resIdx];
        FullScreenMode mode = GetModeFromIndex(modeIdx);
        
        Screen.fullScreenMode = mode;
        Screen.fullScreen = (mode != FullScreenMode.Windowed);
        Screen.SetResolution(res.x, res.y, mode);
        
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = framerates[fpsIdx];
    }

    private FullScreenMode GetModeFromIndex(int index)
    {
        if (index == 0) return FullScreenMode.ExclusiveFullScreen;
        if (index == 1) return FullScreenMode.FullScreenWindow;
        return FullScreenMode.Windowed;
    }

    // ==========================================
    // UI 文字赋值与绑定 (防闪退机制)
    // ==========================================
    private void InitUI()
    {
        // --- 显示 ---
        if (resolutionDropdown)
        {
            resolutionDropdown.ClearOptions();
            resolutionDropdown.AddOptions(new List<string> { "1280x720 (720p)", "1920x1080 (1080p)", "2560x1440 (2K)", "3840x2160 (4K)" });
            resolutionDropdown.SetValueWithoutNotify(PlayerPrefs.GetInt("Set_ResIdx", 1));
            // TMPro 不让强制刷新隐藏面板，所以干脆不掉用 RefreshShownValue，由它自身打开激活时自动渲染
            resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        }

        if (windowModeDropdown)
        {
            windowModeDropdown.ClearOptions();
            windowModeDropdown.AddOptions(new List<string> { "Exclusive Fullscreen", "Borderless Fullscreen", "Windowed" });
            windowModeDropdown.SetValueWithoutNotify(PlayerPrefs.GetInt("Set_WinModeIdx", 2));
            windowModeDropdown.onValueChanged.AddListener(OnWindowModeChanged);
        }

        if (framerateDropdown)
        {
            framerateDropdown.ClearOptions();
            framerateDropdown.AddOptions(new List<string> { "30 FPS", "60 FPS", "120 FPS", "144 FPS", "Unlimited" });
            framerateDropdown.SetValueWithoutNotify(PlayerPrefs.GetInt("Set_FpsIdx", 1));
            framerateDropdown.onValueChanged.AddListener(OnFramerateChanged);
        }

        // --- 控制 ---
        if (mouseSensitivitySlider)
        {
            float sens = PlayerPrefs.GetFloat("Set_MouseSens", 0.5f);
            mouseSensitivitySlider.SetValueWithoutNotify(sens);
            if (mouseSensitivityValueText) mouseSensitivityValueText.text = sens.ToString("0.00");
            if (cameraMovement) cameraMovement.mouseSensitivity = sens;

            mouseSensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        }

        if (invertYToggle)
        {
            bool invert = PlayerPrefs.GetInt("Set_InvertY", 0) == 1;
            // SetIsOnWithoutNotify 是 Toggle 官方防止触发 onValueChanged 的方案
            invertYToggle.SetIsOnWithoutNotify(invert);
            invertYToggle.onValueChanged.AddListener(OnInvertYChanged);
        }

        // --- 游戏 ---
        if (showFPSToggle)
        {
            bool showFPS = PlayerPrefs.GetInt("Set_ShowFPS", 0) == 1;
            showFPSToggle.SetIsOnWithoutNotify(showFPS);
            
            if (fpsCounter)
            {
                fpsCounter.enabled = showFPS;
                if (fpsCounter.fpsText) fpsCounter.fpsText.gameObject.SetActive(showFPS);
            }
            
            showFPSToggle.onValueChanged.AddListener(OnShowFPSChanged);
        }
    }

    // ==========================================
    // 动态生成按键绑定列表
    // ==========================================
    private void LoadAllKeybindOverrides()
    {
        if (cameraMovement == null) return;

        // 为所有的 Input Action 尝试加载他们之前存下的 Override 绑定
        LoadOverrideForAction(cameraMovement.moveAction);
        LoadOverrideForAction(cameraMovement.verticalAction);
        LoadOverrideForAction(cameraMovement.rotateAction);
        // 按需加载其他...
    }

    private void LoadOverrideForAction(InputAction action)
    {
        // Composite binding 包含了多个按键，需要遍历每一个绑定子节点
        for (int i = 0; i < action.bindings.Count; i++)
        {
            KeybindRow.LoadBindingOverride(action, i);
        }
    }

    private void InitKeybindsUI()
    {
        if (keybindsContentParent == null || keybindRowPrefab == null || cameraMovement == null) return;

        // 清空测试用的占位节点
        foreach (Transform child in keybindsContentParent)
        {
            Destroy(child.gameObject);
        }

        // 绑定 WASD 移动 (复合键，每个方向是一个 Binding)
        // bindings[0] 是 Composite 本身，[1] 是 Up, [2] 是 Down, [3] 是 Left, [4] 是 Right
        CreateKeybindRow(cameraMovement.moveAction, 1, "Forward");
        CreateKeybindRow(cameraMovement.moveAction, 2, "Backward");
        CreateKeybindRow(cameraMovement.moveAction, 3, "Left");
        CreateKeybindRow(cameraMovement.moveAction, 4, "Right");

        // 绑定 FC 上下升降 
        CreateKeybindRow(cameraMovement.verticalAction, 1, "Ascend");
        CreateKeybindRow(cameraMovement.verticalAction, 2, "Descend");

        // 绑定 QE 旋转
        CreateKeybindRow(cameraMovement.rotateAction, 1, "Rotate Left");
        CreateKeybindRow(cameraMovement.rotateAction, 2, "Rotate Right");
    }

    private void CreateKeybindRow(InputAction action, int bindingIndex, string displayName)
    {
        GameObject rowObj = Instantiate(keybindRowPrefab, keybindsContentParent);
        KeybindRow rowScript = rowObj.GetComponent<KeybindRow>();
        if (rowScript != null)
        {
            rowScript.Initialize(action, bindingIndex, displayName);
        }
    }

    private void SwitchTab(GameObject activePanel)
    {
        if (displayPanel) displayPanel.SetActive(false);
        if (qualityPanel) qualityPanel.SetActive(false);
        if (controlsPanel) controlsPanel.SetActive(false);
        if (gamePanel) gamePanel.SetActive(false);

        if (activePanel) activePanel.SetActive(true);
    }

    // ==========================================
    // UI 点击回调 (事件流)
    // ==========================================
    public void OnResolutionChanged(int index)
    {
        PlayerPrefs.SetInt("Set_ResIdx", index);
        PlayerPrefs.Save();
        ApplyScreenChange();
    }

    public void OnWindowModeChanged(int index)
    {
        PlayerPrefs.SetInt("Set_WinModeIdx", index);
        PlayerPrefs.Save();
        ApplyScreenChange();
    }

    private void ApplyScreenChange()
    {
        int resIdx = PlayerPrefs.GetInt("Set_ResIdx", 1);
        int modeIdx = PlayerPrefs.GetInt("Set_WinModeIdx", 2);

        Vector2Int res = resolutions[resIdx];
        FullScreenMode mode = GetModeFromIndex(modeIdx);

        Screen.fullScreenMode = mode;
        Screen.fullScreen = (mode != FullScreenMode.Windowed);
        Screen.SetResolution(res.x, res.y, mode);
    }

    public void OnFramerateChanged(int index)
    {
        PlayerPrefs.SetInt("Set_FpsIdx", index);
        PlayerPrefs.Save();
        
        Application.targetFrameRate = framerates[index];
    }

    public void OnSensitivityChanged(float value)
    {
        PlayerPrefs.SetFloat("Set_MouseSens", value);
        PlayerPrefs.Save();
        
        if (mouseSensitivityValueText) mouseSensitivityValueText.text = value.ToString("0.00");
        if (cameraMovement) cameraMovement.mouseSensitivity = value;
    }

    public void OnInvertYChanged(bool isOn)
    {
        PlayerPrefs.SetInt("Set_InvertY", isOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void OnShowFPSChanged(bool isOn)
    {
        PlayerPrefs.SetInt("Set_ShowFPS", isOn ? 1 : 0);
        PlayerPrefs.Save();
        
        if (fpsCounter)
        {
            fpsCounter.enabled = isOn;
            if (fpsCounter.fpsText) fpsCounter.fpsText.gameObject.SetActive(isOn);
        }
    }
}
