using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI 面板")]
    [Tooltip("包含 开始游戏、设置、退出 按钮的主面板")]
    public GameObject mainButtonsPanel;
    [Tooltip("您的 SettingsPanel 预制体实例 (由 SettingsManager 控制)")]
    public GameObject settingsPanel;

    [Header("主界面按钮")]
    public Button startGameButton;
    public Button loadDemoButton; // 新增 LoadDemo 按钮
    public Button openSettingsButton;
    public Button quitGameButton;

    [Header("设置界面返回主界面按钮")]
    [Tooltip("放在 SettingsPanel 外部或内部，用来关闭设置界面")]
    public Button closeSettingsButton;

    [Header("场景设置")]
    [Tooltip("主游戏场景的名字 (例如: Level_01)")]
    public string gameSceneName = "GameScene";
    [Tooltip("Demo测试场景的名字")]
    public string demoSceneName = "DemoScene"; // 新增 Demo 场景名

    private void Awake()
    {
        // 初始状态下只显示主界面按钮，隐藏设置面板
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        // 绑定按钮事件
        if (startGameButton != null)
            startGameButton.onClick.AddListener(StartGame);

        if (loadDemoButton != null)
            loadDemoButton.onClick.AddListener(LoadDemo); // 绑定 LoadDemo

        if (openSettingsButton != null)
            openSettingsButton.onClick.AddListener(ToggleSettings); // 改为切换功能

        if (closeSettingsButton != null)
            closeSettingsButton.onClick.AddListener(CloseSettings);

        if (quitGameButton != null)
            quitGameButton.onClick.AddListener(QuitGame);
    }

    private void Start()
    {
        // 确保主菜单有鼠标并且不被锁定
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // 恢复时间流速（防止从暂定的游戏中切回来导致时间停止）
        Time.timeScale = 1f;
    }

    public void StartGame()
    {
        // 点击开始游戏后，加载您指定的主游戏场景
        SceneManager.LoadScene(gameSceneName);
    }

    public void LoadDemo()
    {
        // 加载 Demo 指定场景
        SceneManager.LoadScene(demoSceneName);
    }

    public void ToggleSettings()
    {
        // 按一下开启，再按一下关闭
        if (settingsPanel != null) 
            settingsPanel.SetActive(!settingsPanel.activeSelf);
    }

    public void CloseSettings()
    {
        // 关闭设置面板
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game Clicked!");

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}
