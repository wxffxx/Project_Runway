using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PauseMenuManager : MonoBehaviour
{
    [Header("UI 面板引用")]
    [Tooltip("原本的暂停主界面 (包含继续、设置、退出按钮)")]
    public GameObject pauseMainPanel; 
    [Tooltip("新建立的设置界面 (可以包含音量、画质等选项)")]
    public GameObject settingsPanel; 
    
    [Header("按钮引用")]
    public Button resumeButton;
    public Button openSettingsButton; // 打开设置
    public Button closeSettingsButton; // 关闭设置返回主界面
    public Button quitButton;

    [Header("输入设置")]
    [Tooltip("新输入系统中监听键盘 Escape 键的 Action")]
    public InputAction pauseAction = new InputAction("Pause", binding: "<Keyboard>/escape");

    // 内部状态，记录当前是否处于暂停中
    private bool isPaused = false;

    private void Awake()
    {
        // 自动隐藏菜单
        if (pauseMainPanel != null) pauseMainPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        // 绑定按钮的点击事件
        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeGame);
            
        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);

        // 绑定设置按钮的事件
        if (openSettingsButton != null)
            openSettingsButton.onClick.AddListener(ToggleSettings);
            
        if (closeSettingsButton != null)
            closeSettingsButton.onClick.AddListener(CloseSettings);
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
        
        Time.timeScale = 0f;                   

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        isPaused = false;
        
        if (pauseMainPanel != null) pauseMainPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        
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
