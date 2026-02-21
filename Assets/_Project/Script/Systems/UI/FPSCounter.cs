using UnityEngine;
using TMPro; // 如果您使用 TextMeshPro

public class FPSCounter : MonoBehaviour
{
    // 如果您在 UI 里创建了一个 Text (TMP) 用于显示 FPS，拖到这里
    public TMP_Text fpsText;

    // 当不提供 fpsText 时，我们可以在左上角用老旧的 GUI 绘制
    public bool useLegacyGUI = true; 

    private float deltaTime = 0.0f;

    void Update()
    {
        // 计算每帧所花的时间的平滑平均值
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;

        // 如果您挂载了 TextMeshPro 的 UI 文本：
        if (fpsText != null)
        {
            float msec = deltaTime * 1000.0f;
            float fps = 1.0f / deltaTime;
            fpsText.text = string.Format("{0:0.} FPS", fps);
        }
    }

    // 备用方案：如果没挂载 UI，直接用系统原生字体画在左上角
    void OnGUI()
    {
        if (fpsText != null || !useLegacyGUI) return;

        int w = Screen.width, h = Screen.height;

        GUIStyle style = new GUIStyle();
        Rect rect = new Rect(0, 0, w, h * 2 / 100);
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = h * 2 / 100;
        style.normal.textColor = Color.yellow;

        float msec = deltaTime * 1000.0f;
        float fps = 1.0f / deltaTime;
        string text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);
        
        GUI.Label(rect, text, style);
    }
}
