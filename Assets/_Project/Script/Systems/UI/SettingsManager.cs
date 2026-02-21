using UnityEngine;
using UnityEngine.UI;
using TMPro; // 使用 TextMeshPro
using System.Collections.Generic;

public class SettingsManager : MonoBehaviour
{
    [Header("标签页按钮 (Tabs)")]
    public Button tabDisplayBtn;
    public Button tabQualityBtn;
    public Button tabControlsBtn;
    public Button tabGameBtn;

    [Header("子面板 (Panels)")]
    public GameObject displayPanel;
    public GameObject qualityPanel;
    public GameObject controlsPanel;
    public GameObject gamePanel;

    [Header("--- 显示设置 (Display) UI组件 ---")]
    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown framerateDropdown; // 新增：帧率下拉框
    public TMP_Dropdown windowModeDropdown; // 新增：窗口模式下拉框

    [Header("--- 控制设置 (Controls) UI组件 ---")]
    public Slider mouseSensitivitySlider;
    public TMP_Text mouseSensitivityValueText; 
    public Toggle invertYToggle;

    [Header("--- 游戏设置 (Game) UI组件 ---")]
    public Toggle showFPSToggle;

    // 内部数据缓存：固定的目标分辨率
    private readonly Vector2Int[] targetResolutions = new Vector2Int[]
    {
        new Vector2Int(1280, 720),  // 720p
        new Vector2Int(1920, 1080), // 1080p
        new Vector2Int(2560, 1440), // 2K
        new Vector2Int(3840, 2160)  // 4K
    };

    // 固定的目标帧率选项
    private readonly int[] targetFramerates = new int[] { 30, 60, 120, 144, -1 }; 

    private CameraMovement cameraMovement;
    private FPSCounter fpsCounterObj;

    private void Start()
    {
        cameraMovement = FindObjectOfType<CameraMovement>();
        fpsCounterObj = FindObjectOfType<FPSCounter>(); // 寻找场景中的 FPS 脚本

        SetupTabs();
        InitDisplaySettings();
        InitControlSettings();
        InitGameSettings(); // 新增：初始化游戏设置页

        OpenTab(displayPanel);
    }

    private void SetupTabs()
    {
        if (tabDisplayBtn != null) tabDisplayBtn.onClick.AddListener(() => OpenTab(displayPanel));
        if (tabQualityBtn != null) tabQualityBtn.onClick.AddListener(() => OpenTab(qualityPanel));
        if (tabControlsBtn != null) tabControlsBtn.onClick.AddListener(() => OpenTab(controlsPanel));
        if (tabGameBtn != null) tabGameBtn.onClick.AddListener(() => OpenTab(gamePanel));
    }

    private void OpenTab(GameObject targetPanel)
    {
        if (displayPanel != null) displayPanel.SetActive(false);
        if (qualityPanel != null) qualityPanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(false);
        if (gamePanel != null) gamePanel.SetActive(false);

        if (targetPanel != null) targetPanel.SetActive(true);
    }

    // ==========================================
    // 1. 显示设置 (Display Settings)
    // ==========================================
    private void InitDisplaySettings()
    {
        // A. 窗口模式设置 (独占全屏、无边框、窗口化)
        if (windowModeDropdown != null)
        {
            try {
                windowModeDropdown.ClearOptions();
                List<string> modeOptions = new List<string>()
                {
                    "独占全屏 (Exclusive)",
                    "无边框全屏 (Borderless)",
                    "窗口化 (Windowed)"
                };
                windowModeDropdown.AddOptions(modeOptions);

                int savedModeIndex = PlayerPrefs.GetInt("WindowModeIndex", 2); // 默认窗口化(2)
                windowModeDropdown.value = savedModeIndex;
                
                SetWindowMode(savedModeIndex);
                windowModeDropdown.onValueChanged.AddListener(SetWindowMode);
            } catch (System.Exception e) { Debug.LogError("Window Mode Init Error: " + e.Message); }
        }

        // B. 自定义分辨率设置
        if (resolutionDropdown != null)
        {
            try {
                resolutionDropdown.ClearOptions();
                List<string> options = new List<string>()
                {
                    "1280 x 720 (720p)",
                    "1920 x 1080 (1080p)",
                    "2560 x 1440 (2K)",
                    "3840 x 2160 (4K)"
                };
                resolutionDropdown.AddOptions(options);

                int savedResIndex = PlayerPrefs.GetInt("ResolutionIndex", 1);
                resolutionDropdown.value = savedResIndex;

                SetResolution(savedResIndex);
                resolutionDropdown.onValueChanged.AddListener(SetResolution);
            } catch (System.Exception e) { Debug.LogError("Resolution Init Error: " + e.Message); }
        }

        // C. 帧率 (FPS) 设置
        if (framerateDropdown != null)
        {
            try {
                framerateDropdown.ClearOptions();
                List<string> fpsOptions = new List<string>()
                {
                    "30 FPS",
                    "60 FPS",
                    "120 FPS",
                    "144 FPS",
                    "Unlimited"
                };
                framerateDropdown.AddOptions(fpsOptions);

                int savedFpsIndex = PlayerPrefs.GetInt("FramerateIndex", 1);
                framerateDropdown.value = savedFpsIndex;

                SetTargetFramerate(savedFpsIndex);
                framerateDropdown.onValueChanged.AddListener(SetTargetFramerate);
            } catch (System.Exception e) { Debug.LogError("FPS Init Error: " + e.Message); }
        }
    }

    public void SetWindowMode(int index)
    {
        FullScreenMode mode = FullScreenMode.ExclusiveFullScreen;
        if (index == 1) mode = FullScreenMode.FullScreenWindow;
        else if (index == 2) mode = FullScreenMode.Windowed;

        PlayerPrefs.SetInt("WindowModeIndex", index);
        PlayerPrefs.Save();

        // 重新应用分辨率使其在此模式下生效
        int savedResIndex = PlayerPrefs.GetInt("ResolutionIndex", 1);
        SetResolution(savedResIndex); 
    }

    public void SetResolution(int index)
    {
        Vector2Int res = targetResolutions[index];
        
        int modeIndex = PlayerPrefs.GetInt("WindowModeIndex", 2); // 默认窗口化(2)
        FullScreenMode mode = FullScreenMode.ExclusiveFullScreen;
        if (modeIndex == 1) mode = FullScreenMode.FullScreenWindow;
        else if (modeIndex == 2) mode = FullScreenMode.Windowed;

        // 强制使用对应的模式
        Screen.SetResolution(res.x, res.y, mode);
        
        PlayerPrefs.SetInt("ResolutionIndex", index);
        PlayerPrefs.Save();
    }

    public void SetTargetFramerate(int index)
    {
        int fps = targetFramerates[index];
        Application.targetFrameRate = fps;
        
        QualitySettings.vSyncCount = 0; 

        PlayerPrefs.SetInt("FramerateIndex", index);
        PlayerPrefs.Save();
    }

    // ==========================================
    // 3. 控制设置 (Control Settings)
    // ==========================================
    private void InitControlSettings()
    {
        // A. 鼠标灵敏度
        if (mouseSensitivitySlider != null)
        {
            float savedSens = PlayerPrefs.GetFloat("MouseSensitivity", 0.5f);
            mouseSensitivitySlider.value = savedSens;
            SetMouseSensitivity(savedSens);
            mouseSensitivitySlider.onValueChanged.AddListener(SetMouseSensitivity);
        }

        // B. 反转 Y 轴
        if (invertYToggle != null)
        {
            bool invertY = PlayerPrefs.GetInt("InvertYAxis", 0) == 1; // 默认不反转(0)
            invertYToggle.isOn = invertY;
            invertYToggle.onValueChanged.AddListener(SetInvertYAxis);
        }
    }

    public void SetMouseSensitivity(float sensitivity)
    {
        PlayerPrefs.SetFloat("MouseSensitivity", sensitivity);
        PlayerPrefs.Save();

        if (mouseSensitivityValueText != null)
        {
            mouseSensitivityValueText.text = sensitivity.ToString("0.00");
        }

        if (cameraMovement != null)
        {
            cameraMovement.mouseSensitivity = sensitivity;
        }
    }

    public void SetInvertYAxis(bool isInvert)
    {
        PlayerPrefs.SetInt("InvertYAxis", isInvert ? 1 : 0);
        PlayerPrefs.Save();
    }

    // ==========================================
    // 4. 游戏属性设置 (Game Settings)
    // ==========================================
    private void InitGameSettings()
    {
        if (showFPSToggle != null)
        {
            // 从保存配置里读取是否开启过 FPS 计数器 (默认 0：关闭)
            bool isShowFPS = PlayerPrefs.GetInt("ShowFPS", 0) == 1;
            showFPSToggle.isOn = isShowFPS;
            
            // 手动调用一次来设置隐藏/显示
            SetShowFPS(isShowFPS);

            showFPSToggle.onValueChanged.AddListener(SetShowFPS);
        }
    }

    public void SetShowFPS(bool isShowFPS)
    {
        PlayerPrefs.SetInt("ShowFPS", isShowFPS ? 1 : 0);
        PlayerPrefs.Save();

        // 找到场上的 FPS 脚本并开关它本身 (如果 disabled 它就不会运行 Update 和 OnGUI)
        if (fpsCounterObj != null)
        {
            fpsCounterObj.enabled = isShowFPS;
            
            // 如果它带有一个专门的 TextMeshPro UI 文字组件，把它对应的物体也关掉
            if (fpsCounterObj.fpsText != null)
            {
                fpsCounterObj.fpsText.gameObject.SetActive(isShowFPS);
            }
        }
    }

}
