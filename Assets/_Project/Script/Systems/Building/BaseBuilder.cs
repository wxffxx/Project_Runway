using UnityEngine;
using UnityEngine.InputSystem;

public abstract class BaseBuilder : MonoBehaviour
{
    protected enum BuildState { Idle, PlacingStart, PlacingEnd }
    protected BuildState currentState = BuildState.Idle;

    protected Vector3 startPoint;
    protected string tooltip = "";
    protected string currentCategoryName = "";

    protected virtual void Update()
    {
        if (currentState == BuildState.PlacingStart)
        {
            HandlePlacingStart();
        }
        else if (currentState == BuildState.PlacingEnd)
        {
            HandlePlacingEnd();
        }

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CancelBuild();
        }
        
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            CancelBuild();
        }
    }

    protected abstract void HandlePlacingStart();
    protected abstract void HandlePlacingEnd();

    public virtual void CancelBuildFromExternal()
    {
        if (currentState != BuildState.Idle)
        {
            CancelBuild();
        }
    }

    protected virtual void CancelBuild()
    {
        ExitBuildMode();
        tooltip = "已取消建造。";
    }

    protected virtual void ExitBuildMode()
    {
        currentState = BuildState.Idle;
    }

    protected Vector3? GetMouseGroundPosition()
    {
        if (Camera.main == null) return null;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);

        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        if (groundPlane.Raycast(ray, out float enterDistance))
        {
            Vector3 rawPoint = ray.GetPoint(enterDistance);
            
            // 网格依附功能 (Grid Snapping) -> 强行将 X 和 Z 四舍五入到最近的整数 (1m一个格子)
            rawPoint.x = Mathf.Round(rawPoint.x);
            rawPoint.z = Mathf.Round(rawPoint.z);
            rawPoint.y = 0f; // 确保 Y 必须为绝对的 0

            return rawPoint;
        }

        return null;
    }

    protected virtual void OnGUI()
    {
        if (!string.IsNullOrEmpty(tooltip) && currentState != BuildState.Idle)
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 28;
            style.fontStyle = FontStyle.Bold;
            style.normal.textColor = Color.yellow;
            style.alignment = TextAnchor.MiddleCenter;

            Rect rect = new Rect(0, 50, Screen.width, 100);
            GUI.Label(rect, tooltip, style);
        }
        else if (!string.IsNullOrEmpty(tooltip)) 
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 24;
            style.normal.textColor = Color.white;
            GUI.Label(new Rect(20, 100, 400, 100), tooltip, style);
        }
        
        DrawFloatingTooltip();
    }

    protected abstract void DrawFloatingTooltip();
}
