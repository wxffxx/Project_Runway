using UnityEngine;
using UnityEngine.InputSystem;
using PP_RY.Core.Navigation;
using PP_RY.Systems.Navigation;

public class TaxiwayBuilder : MonoBehaviour
{
    [Header("Builder Settings")]
    public float minLength = 5f; // 滑行道最小长度，比跑道宽容一些
    
    [Header("Materials")]
    [Tooltip("半透明预览材质 - 核心承重道面")]
    public Material ghostCoreMaterial; 
    [Tooltip("半透明预览材质 - 道肩底座")]
    public Material ghostShoulderMaterial;
    [Tooltip("实际建造后的材质 - 核心承重道面")]
    public Material placedCoreMaterial;
    [Tooltip("实际建造后的材质 - 道肩底座")]
    public Material placedShoulderMaterial;

    public static TaxiwayBuilder Instance;

    private enum BuildState { Idle, PlacingStart, PlacingEnd }
    private BuildState currentState = BuildState.Idle;

    private float currentCoreWidth;
    private float currentTotalWidth;
    private string currentCategoryName = "";

    private Vector3 startPoint;
    
    // 我们用两个物体组合表示滑行道：
    // ghostCoreObj 负责深灰色的中间道面，比跑道略低。
    // ghostShoulderObj 负责宽大泛黄/浅灰的道肩，比道面更低。
    private GameObject ghostCoreObj;
    private GameObject ghostShoulderObj;
    
    private string tooltip = "";

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Update()
    {
        if (currentState == BuildState.PlacingStart)
        {
            HandlePlacingStart();
        }
        else if (currentState == BuildState.PlacingEnd)
        {
            HandlePlacingEnd();
        }

        // 按 ESC 随时取消建造
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CancelBuild();
        }
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            CancelBuild();
        }
    }

    // 给 UI 调用的公开方法
    public void StartBuildingTaxiway(float coreWidth, float totalWidth, string categoryName)
    {
        currentCoreWidth = coreWidth;
        currentTotalWidth = totalWidth;
        currentCategoryName = categoryName;

        currentState = BuildState.PlacingStart;
        tooltip = $"建造滑行道 (Code {categoryName})：请点击海平面锚定起点。按 ESC 取消。";

        if (ghostCoreObj != null) Destroy(ghostCoreObj);
        if (ghostShoulderObj != null) Destroy(ghostShoulderObj);
        
        ghostCoreObj = null;
        ghostShoulderObj = null;
    }

    private void HandlePlacingStart()
    {
        Vector3? hitPos = GetMouseGroundPosition();
        if (hitPos.HasValue && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            startPoint = hitPos.Value;
            currentState = BuildState.PlacingEnd;
            tooltip = "请拖动鼠标确定长度与角度 (按住 Shift 吸附 45°)。再次点击左键完成。";

            // 创建幽灵白模(核心道面)
            ghostCoreObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ghostCoreObj.name = "Ghost_Taxiway_Core";
            Destroy(ghostCoreObj.GetComponent<BoxCollider>());
            if (ghostCoreMaterial != null) ghostCoreObj.GetComponent<Renderer>().material = ghostCoreMaterial;

            // 如果总宽度大于核心宽度，说明存在保护性道肩，创建双层底部模型
            if (currentTotalWidth > currentCoreWidth)
            {
                ghostShoulderObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ghostShoulderObj.name = "Ghost_Taxiway_Shoulder";
                Destroy(ghostShoulderObj.GetComponent<BoxCollider>());
                if (ghostShoulderMaterial != null) ghostShoulderObj.GetComponent<Renderer>().material = ghostShoulderMaterial;
            }
        }
    }

    private void HandlePlacingEnd()
    {
        Vector3? hitPos = GetMouseGroundPosition();
        if (hitPos.HasValue)
        {
            Vector3 endPos = hitPos.Value;
            
            // 加入 Shift 键强制吸附：45 度角度 + 100m 倍数长度
            if (Keyboard.current != null && Keyboard.current.shiftKey.isPressed)
            {
                endPos = SnapToAngleAndDistance(startPoint, endPos);
            }

            // 节点不收缩，视觉实体向两端延伸
            Vector3 direction = endPos - startPoint;
            float totalLength = direction.magnitude;
            
            // 核心道面的视觉长度 = 鼠标长度 + 核心宽度
            float visualCoreLength = totalLength + currentCoreWidth;
            // 保护性道肩的视觉长度 = 鼠标长度 + 道肩总宽度
            float visualShoulderLength = totalLength + currentTotalWidth;

            // 更新模型变换：
            // - yOffset 为 0.08f 确保不会与地面穿插，也比跑道低
            // - 道肩设置更低的 0.05f 形成立体阶梯感
            if (ghostCoreObj != null) UpdateTransform(ghostCoreObj.transform, startPoint, endPos, currentCoreWidth, 0.08f, visualCoreLength); 
            if (ghostShoulderObj != null) UpdateTransform(ghostShoulderObj.transform, startPoint, endPos, currentTotalWidth, 0.04f, visualShoulderLength); 

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                float length = Vector3.Distance(startPoint, endPos);
                if (length >= minLength)
                {
                    FinalizeTaxiway(startPoint, endPos);
                }
                else
                {
                    tooltip = "距离太近了，往远拖一点！";
                }
            }
        }
    }

    // 按住 Shift 时：强制角度吸附到最近的 45 度倍数，并强制将长度吸附为 100m 的倍数
    private Vector3 SnapToAngleAndDistance(Vector3 start, Vector3 end)
    {
        Vector3 direction = end - start;
        float distance = direction.magnitude;
        if (distance < 0.1f) return end;

        // 1. 角度吸附
        float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        float snappedAngle = Mathf.Round(angle / 45f) * 45f;
        
        // 2. 长度吸附 (100的倍数，确保最小长度不为0)
        float snappedDistance = Mathf.Round(distance / 100f) * 100f;
        if (snappedDistance < 100f) snappedDistance = 100f;
        
        Vector3 snappedDirection = Quaternion.Euler(0, snappedAngle, 0) * Vector3.forward;
        Vector3 newEnd = start + snappedDirection * snappedDistance;
        
        newEnd.x = Mathf.Round(newEnd.x);
        newEnd.z = Mathf.Round(newEnd.z);
        
        return newEnd;
    }

    private void UpdateTransform(Transform tf, Vector3 p1, Vector3 p2, float width, float yOffset, float visualTotalLength)
    {
        if (tf == null) return;

        Vector3 midpoint = (p1 + p2) / 2f;
        midpoint.y = yOffset;
        tf.position = midpoint;

        float distance = Vector3.Distance(p1, p2);
        tf.localScale = new Vector3(width, 0.1f, visualTotalLength);

        if (distance > 0.001f)
        {
            tf.rotation = Quaternion.LookRotation(p2 - p1);
        }
    }

    private void FinalizeTaxiway(Vector3 p1, Vector3 p2)
    {
        Vector3 direction = p2 - p1;
        float totalLength = direction.magnitude;

        float visualCoreLength = totalLength + currentCoreWidth;
        float visualShoulderLength = totalLength + currentTotalWidth;

        // 1. 生成道面实体
        GameObject finalCore = GameObject.CreatePrimitive(PrimitiveType.Cube);
        finalCore.name = $"Taxiway_Core_Code{currentCategoryName}";
        UpdateTransform(finalCore.transform, p1, p2, currentCoreWidth, 0.08f, visualCoreLength);
        if (placedCoreMaterial != null) finalCore.GetComponent<Renderer>().material = placedCoreMaterial;

        // 2. 生成道肩实体 (仅限 D, E, F 类等大宽度拥有道肩)
        if (currentTotalWidth > currentCoreWidth)
        {
            GameObject finalShoulder = GameObject.CreatePrimitive(PrimitiveType.Cube);
            finalShoulder.name = $"Taxiway_Shoulder_Code{currentCategoryName}";
            UpdateTransform(finalShoulder.transform, p1, p2, currentTotalWidth, 0.04f, visualShoulderLength);
            if (placedShoulderMaterial != null) finalShoulder.GetComponent<Renderer>().material = placedShoulderMaterial;
        }

        // 3. 将滑行道的首尾灌入全局图论网络
        GenerateTaxiwayNetwork(p1, p2);

        // 清理
        if (ghostCoreObj != null) Destroy(ghostCoreObj);
        if (ghostShoulderObj != null) Destroy(ghostShoulderObj);
        ghostCoreObj = null;
        ghostShoulderObj = null;
        
        ExitBuildMode();
        tooltip = $"滑行道组件 (Code {currentCategoryName}) 建造完成！";
    }

    // 在滑行道的起点和终点生成 Node
    private void GenerateTaxiwayNetwork(Vector3 start, Vector3 end)
    {
        PathNode startNode = new PathNode($"TWY_{currentCategoryName}_Start", start, NodeType.TaxiwayPoint);
        PathNode endNode = new PathNode($"TWY_{currentCategoryName}_End", end, NodeType.TaxiwayPoint);
        
        float dist = Vector3.Distance(start, end);
        // 双向连接滑行道
        startNode.AddEdge(endNode, dist, EdgeType.StandardTaxiway, false);

        // 为了与我们的网格大管家兼容，我先把它包装成一个 RunwayData 的外壳 (未来可以改名成 NetworkSegment之类的)
        RunwayData twyData = new RunwayData($"TWY_{currentCategoryName}_{System.Guid.NewGuid().ToString().Substring(0,5)}", dist, currentCoreWidth);
        twyData.thresholdNode = startNode;
        twyData.endNode = endNode;
        twyData.centerlineNodes.Add(startNode);
        twyData.centerlineNodes.Add(endNode);

        if (RunwayNetworkManager.Instance != null)
        {
            RunwayNetworkManager.Instance.RegisterRunway(twyData);
        }
    }

    public void CancelBuildFromExternal()
    {
        if (currentState != BuildState.Idle) CancelBuild();
    }

    private void CancelBuild()
    {
        if (ghostCoreObj != null) Destroy(ghostCoreObj);
        if (ghostShoulderObj != null) Destroy(ghostShoulderObj);
        ghostCoreObj = null;
        ghostShoulderObj = null;
        
        ExitBuildMode();
        tooltip = "已取消建造滑行道。";
    }

    private void ExitBuildMode()
    {
        currentState = BuildState.Idle;
    }

    // 强迫症网格依附：1米格子
    private Vector3? GetMouseGroundPosition()
    {
        if (Camera.main == null) return null;
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);

        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        if (groundPlane.Raycast(ray, out float enterDistance))
        {
            Vector3 rawPoint = ray.GetPoint(enterDistance);
            rawPoint.x = Mathf.Round(rawPoint.x);
            rawPoint.z = Mathf.Round(rawPoint.z);
            rawPoint.y = 0f;
            return rawPoint;
        }
        return null;
    }

    // 专属的浮动提示 UI
    private void OnGUI()
    {
        if (!string.IsNullOrEmpty(tooltip) && currentState != BuildState.Idle)
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 28;
            style.fontStyle = FontStyle.Bold;
            style.normal.textColor = Color.yellow;
            style.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(0, 50, Screen.width, 100), tooltip, style);
        }
        else if (!string.IsNullOrEmpty(tooltip)) 
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 24;
            style.normal.textColor = Color.white;
            GUI.Label(new Rect(20, 100, 400, 100), tooltip, style);
        }

        if (currentState == BuildState.PlacingEnd)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            float guiY = Screen.height - mousePos.y; 

            Vector3? hitPos = GetMouseGroundPosition();
            if (hitPos.HasValue)
            {
                Vector3 endPos = hitPos.Value;
                if (Keyboard.current != null && Keyboard.current.shiftKey.isPressed)
                {
                    endPos = SnapToAngleAndDistance(startPoint, endPos);
                }
                
                float length = Vector3.Distance(startPoint, endPos);
                // 专属的高管黄/橙色字体，显示总计含有道肩的提示信息
                string shoulderHint = (currentTotalWidth > currentCoreWidth) ? "\n(+巨幅道肩)" : "";
                string floatText = $"Code {currentCategoryName} Taxiway{shoulderHint}\n{length:F0}m";

                GUIStyle floatStyle = new GUIStyle();
                floatStyle.fontSize = 22;
                floatStyle.fontStyle = FontStyle.Bold;
                floatStyle.normal.textColor = new Color(1f, 0.6f, 0f); // 橙黄色，以区别于跑道的绿色
                
                GUIStyle shadowStyle = new GUIStyle(floatStyle);
                shadowStyle.normal.textColor = Color.black;

                Rect shadowRect = new Rect(mousePos.x + 22, guiY + 22, 200, 120);
                Rect labelRect = new Rect(mousePos.x + 20, guiY + 20, 200, 120);

                GUI.Label(shadowRect, floatText, shadowStyle);
                GUI.Label(labelRect, floatText, floatStyle);
            }
        }
    }
}
