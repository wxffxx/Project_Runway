using UnityEngine;
using TMPro; // 如果您使用 TextMeshPro

public class FPSCounter : MonoBehaviour
{
    // 如果您在 UI 里创建了一个 Text (TMP) 用于显示 FPS，拖到这里
    public TMP_Text fpsText;

    // 当不提供 fpsText 时，我们可以在左上角用老旧的 GUI 绘制
    public bool useLegacyGUI = true; 

    private float deltaTime = 0.0f;

    void Start()
    {
        // 如果有挂载TMP组件，防止它因为框太小而把后面的换行隐藏掉
        if (fpsText != null)
        {
            fpsText.textWrappingMode = TextWrappingModes.NoWrap;
        }
    }

    void Update()
    {
        // 计算每帧所花的时间的平滑平均值
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;

        // 如果您挂载了 TextMeshPro 的 UI 文本：
        if (fpsText != null)
        {
            float msec = deltaTime * 1000.0f;
            float fps = 1.0f / deltaTime;
            
            string camInfo = GetCameraInfo();
            // 改为一行显示，防止UI框太低导致下面的字被截断看不见
            fpsText.text = string.Format("{0:0.} FPS | {1}", fps, camInfo);
        }
        else
        {
            // 如果 TextMeshPro 组件丢了，强制开启旧版 GUI
            useLegacyGUI = true;
        }
    }

    private string GetCameraInfo()
    {
        // Camera.main 很多时候会抓错（比如抓到假死或没被激活的第一个MainCamera）
        // 所以我们尝试获取所有的相机中，真正在渲染的那一个（或者被玩家控制的那个）
        Camera activeCam = Camera.main; 

        // 如果 MainCamera 不在动，我们试着找找场景里其他激活的相机
        if (activeCam == null || !activeCam.gameObject.activeInHierarchy) 
        {
            activeCam = FindFirstObjectByType<Camera>();
        }

        if (activeCam == null) return "No Active Camera";

        Vector3 pos = activeCam.transform.position;
        string zoom = activeCam.orthographic ? 
            $"Size: {activeCam.orthographicSize:F2}" : 
            $"FOV: {activeCam.fieldOfView:F2}";
            
        return $"Pos: ({pos.x:F2}, {pos.y:F2}, {pos.z:F2}) | Zoom: {zoom}";
    }

    // 备用方案：如果没挂载 UI，直接用系统原生字体画在左上角
    void OnGUI()
    {
        if (fpsText != null || !useLegacyGUI) return;

        int w = Screen.width, h = Screen.height;

        GUIStyle style = new GUIStyle();
        Rect rect = new Rect(0, 0, w, h * 10 / 100);
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = h * 2 / 100;
        style.normal.textColor = Color.yellow;

        float msec = deltaTime * 1000.0f;
        float fps = 1.0f / deltaTime;
        
        string camInfo = GetCameraInfo();
        string text = string.Format("{0:0.0} ms ({1:0.} fps)\n{2}", msec, fps, camInfo);
        
        GUI.Label(rect, text, style);
    }
}
