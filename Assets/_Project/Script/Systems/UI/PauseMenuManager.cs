using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using PP_RY.Systems.UI; // 引入 RunwayMenuManager 所在的命名空间

public class PauseMenuManager : MonoBehaviour
{
    [Header("UI 面板引用")]
    [Tooltip("原本的暂停主界面 (包含继续、设置、退出按钮)")]
    public GameObject pauseMainPanel; 
    [Tooltip("新建立的设置界面 (可以包含音量、画质等选项)")]
    public GameObject settingsPanel; 
    
    [Header("游戏中 UI 控制")]
    [Tooltip("存放所有游戏期间显示的 UI，例如时间轴、跑道菜单等。暂停时会自动隐藏。")]
    public GameObject inGameUIRoot;
    
    [Header("按钮引用")]
    public Button resumeButton;
    public Button returnToMenuButton; // 新增：返回主菜单按钮
    public Button openSettingsButton; // 打开设置
    public Button closeSettingsButton; // 关闭设置返回主界面
    public Button quitButton;

    [Header("输入设置")]
    [Tooltip("新输入系统中监听键盘 Escape 键的 Action")]
    public InputAction pauseAction = new InputAction("Pause", binding: "<Keyboard>/escape");

    [Header("场景设置")]
    [Tooltip("主菜单场景的名字")]
    public string mainMenuSceneName = "MainMenu_Scene";

    // 内部状态，记录当前是否处于暂停中
    private bool isPaused = false;

    private void Awake()
    {
        // 绑定按钮的点击事件
        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeGame);
            
        if (returnToMenuButton != null)
            returnToMenuButton.onClick.AddListener(ReturnToMainMenu); // 绑定返回主菜单事件
            
        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);

        // 绑定设置按钮的事件
        if (openSettingsButton != null)
            openSettingsButton.onClick.AddListener(ToggleSettings);
            
        if (closeSettingsButton != null)
            closeSettingsButton.onClick.AddListener(CloseSettings);
    }

    private void Start()
    {
        // 必须在 Start() 里隐藏，让子 UI 组件(特别是 TMPro)能在 Awake 时自我初始化
        if (pauseMainPanel != null) pauseMainPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    private void OnEnable()
    {
        pauseAction.Enable();
        // 当按下 Escape 键时触发 TogglePause 函数
        pauseAction.performed += _ => TogglePause();
    }

    private void OnDisable()
    {
        pauseAction.Disable();
        pauseAction.performed -= _ => TogglePause();
    }

    // 切换暂停状态
    public void TogglePause()
    {
        if (isPaused)
        {
            // 如果在设置界面按 ESC，先退回主菜单
            if (settingsPanel != null && settingsPanel.activeSelf)
            {
                CloseSettings();
            }
            else
            {
                ResumeGame();
            }
        }
        else
        {
            PauseGame();
        }
    }

    private void PauseGame()
    {
        isPaused = true;
        
        if (pauseMainPanel != null) pauseMainPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false); // 确保每次暂停出来都是主菜单
        
        // 游戏暂停时，如果有统一的局内 UI，直接把它隐藏了
        if (inGameUIRoot != null) inGameUIRoot.SetActive(false);

        // 游戏暂停时，强制打断可能正在进行的跑道/滑行道建造
        if (RunwayBuilder.Instance != null)
        {
            RunwayBuilder.Instance.CancelBuildFromExternal();
        }
        if (TaxiwayBuilder.Instance != null)
        {
            TaxiwayBuilder.Instance.CancelBuildFromExternal();
        }

        // 查找并隐藏可能开启着的跑道建造菜单 (既然有了 inGameUIRoot 这段可以作为保底，或者直接删掉，保留也不冲突)
        RunwayMenuManager runwayMenu = FindFirstObjectByType<RunwayMenuManager>();
        if (runwayMenu != null && runwayMenu.gameObject.activeSelf)
        {
            runwayMenu.gameObject.SetActive(false);
        }

        Time.timeScale = 0f;                   

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        isPaused = false;
        
        if (pauseMainPanel != null) pauseMainPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        
        // 游戏恢复时，如果之前它存在，重新开启局内 UI
        if (inGameUIRoot != null) inGameUIRoot.SetActive(true);
        
        Time.timeScale = 1f;                   

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // 切换设置界面的显示/隐藏（按一次打开，再按一次关闭）
    public void ToggleSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(!settingsPanel.activeSelf);
        }
    }

    // 显式关闭设置界面
    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    public void ReturnToMainMenu()
    {
        // 恢复时间刻度，防止切回主菜单后游戏处于假死状态
        Time.timeScale = 1f;

        // 解锁鼠标
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void QuitGame()
    {
        Debug.Log("Quit Game Clicked!"); // 在编辑器里测试时会打印这句话
        
        // 恢复时间刻度，防止退出后有系统残留问题
        Time.timeScale = 1f;
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // 如果在编辑器中运行，停止播放
        #else
        Application.Quit(); // 如果已经打包成客户端，直接退出程序
        #endif
    }
}
