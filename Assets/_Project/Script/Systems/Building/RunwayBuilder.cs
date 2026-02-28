using UnityEngine;
using UnityEngine.InputSystem; // 基于新输入系统的交互
using PP_RY.Core.Navigation;
using PP_RY.Systems.Navigation;

public class RunwayBuilder : MonoBehaviour
{
    [Header("Runway Settings")]
    public float runwayWidth = 15f;      // 跑道宽度 (X轴)
    public float runwayThickness = 0.5f; // 跑道厚度 (Y轴)
    public float minLength = 5.0f;       // 跑道最小长度

    [Header("Visuals")]
    public Material ghostMaterial;  // 建造中（幽灵模式）的材质
    public Material placedMaterial; // 建造完毕（实体模式）的材质

    // 建造状态机
    private enum BuildState { Idle, PlacingStart, PlacingEnd }
    private BuildState currentState = BuildState.Idle;

    private Vector3 startPoint;
    private GameObject currentGhostRunway;
    private string tooltip = ""; // 屏幕顶部的UI提示
    private string currentCategoryName = ""; // 当前正在建造的跑道等级

    public static RunwayBuilder Instance;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Update()
    {
        // 1. 根据状态处理点击
        if (currentState == BuildState.PlacingStart)
        {
            HandlePlacingStart();
        }
        else if (currentState == BuildState.PlacingEnd)
        {
            HandlePlacingEnd();
        }

        // 按 ESC 或 鼠标右键 随时取消建造
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CancelBuild();
        }
        
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            CancelBuild();
        }
    }

    // 给 UI 按钮调用的公开方法
    public void StartBuildingRunway(float width, string categoryName)
    {
        runwayWidth = width;
        currentCategoryName = categoryName;
        currentState = BuildState.PlacingStart;
        tooltip = $"建造跑道 (宽{width}m)：请点击海平面锚定【起点】。按 ESC 取消。";

        if (currentGhostRunway != null) Destroy(currentGhostRunway);
    }

    private void HandlePlacingStart()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector3? hitPos = GetMouseGroundPosition();
            if (hitPos.HasValue)
            {
                startPoint = hitPos.Value;
                currentState = BuildState.PlacingEnd;
                tooltip = "请移动鼠标拉伸跑道长度与角度，再次点击放置【终点】。";

                // 创建一个白模 Cube 作为跑道预览
                currentGhostRunway = GameObject.CreatePrimitive(PrimitiveType.Cube);
                currentGhostRunway.name = "GhostRunway";
                
                // 移除碰撞体，防止幽灵跑道挡住我们用来检测地面的射线
                Destroy(currentGhostRunway.GetComponent<Collider>());
                
                if (ghostMaterial != null)
                {
                    currentGhostRunway.GetComponent<Renderer>().material = ghostMaterial;
                }
            }
        }
    }

    private void HandlePlacingEnd()
    {
        Vector3? hitPos = GetMouseGroundPosition();
        if (hitPos.HasValue)
        {
            Vector3 currentMouseGroundPos = hitPos.Value;
            
            // 加入 Shift 键 45 度吸附检测
            if (Keyboard.current != null && Keyboard.current.shiftKey.isPressed)
            {
                currentMouseGroundPos = SnapTo45Degrees(startPoint, currentMouseGroundPos);
            }

            // 实时更新幽灵白模的大小、位置、旋转
            Vector3 direction = currentMouseGroundPos - startPoint;
            float totalLength = direction.magnitude;
            
            // 视觉实体向两端各延伸 width/2，所以总长 = 鼠标拉的长度 + width
            float visualTotalLength = totalLength + runwayWidth;
            
            UpdateRunwayTransform(currentGhostRunway.transform, startPoint, currentMouseGroundPos, visualTotalLength);

            // 当玩家再次点击左键时，确认建造
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                // 如果长度过短不让建，防止模型破面
                float length = Vector3.Distance(startPoint, currentMouseGroundPos);
                if (length >= minLength)
                {
                    FinalizeRunway(startPoint, currentMouseGroundPos);
                }
                else
                {
                    tooltip = "跑道太短了，请拉长一点！";
                }
            }
        }
    }

    // 强制角度吸附到最近的 45 度倍数，并根据跑道等级标准 (1=800m 2=1200m 3=1800m 4=2400m) 做长度强制吸附
    private Vector3 SnapTo45Degrees(Vector3 start, Vector3 end)
    {
        Vector3 direction = end - start;
        float distance = direction.magnitude;
        if (distance < 0.1f) return end;

        // --- 1. 角度吸附 ---
        // Atan2 返回弧度，转为角度。注意对应 Z 为前向 (Atan2(X, Z))
        float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        
        // 四舍五入到最近的 45 的倍数
        float snappedAngle = Mathf.Round(angle / 45f) * 45f;
        
        // --- 2. 长度吸附 (ICAO 规范) ---
        // 我们设定四个默认标准长度阶梯：800m, 1200m, 1800m, 2400m
        float[] standardLengths = new float[] { 800f, 1200f, 1800f, 2400f };
        float snappedDistance = distance;

        // 寻找离当前鼠标拖拽距离最近的“标准长度”
        float minDiff = float.MaxValue;
        foreach (float stdLen in standardLengths)
        {
            float diff = Mathf.Abs(distance - stdLen);
            if (diff < minDiff)
            {
                minDiff = diff;
                snappedDistance = stdLen;
            }
        }
        
        // 如果鼠标拉得比最大的 2400m 还要长很多（超过一个阈值，比如 3000），可以允许用户自由拉更长，或者强制锁死
        if (distance > 3000f) 
        {
            // 自由长跑道，例如每 500m 吸附一下
            snappedDistance = Mathf.Round(distance / 500f) * 500f;
        }

        // --- 3. 重组最终坐标 ---
        // 将吸附后的角度重新转回方向向量
        Vector3 snappedDirection = Quaternion.Euler(0, snappedAngle, 0) * Vector3.forward;
        
        // 使用吸附后的方向和吸附后的长度重新推算终点
        Vector3 newEnd = start + snappedDirection * snappedDistance;
        
        // 终点本身也网格化以防在 45 度时产生的浮点误差导致贴图错位
        newEnd.x = Mathf.Round(newEnd.x);
        newEnd.z = Mathf.Round(newEnd.z);
        
        return newEnd;
    }

    // 核心几何算法：根据起点和终点，计算长方体的 Transform
    // p1 和 p2 是玩家点击的确切节点圆心
    private void UpdateRunwayTransform(Transform runway, Vector3 p1, Vector3 p2, float visualTotalLength)
    {
        Vector3 direction = p2 - p1;
        float nodeDistance = direction.magnitude;

        // 如果节点距离极短，防止旋转和计算报错
        if (nodeDistance < 0.1f) return;

        // 1. 位置：中心点 (收缩后两个节点的中间)
        runway.position = p1 + (direction * 0.5f);
        
        // 由于地面高度是0，跑到厚度0.5，为了让跑道平躺在地上，我们需要把它的中心Y往上提一半厚度
        runway.position = new Vector3(runway.position.x, runwayThickness * 0.5f, runway.position.z);

        // 2. 缩放 (Scale)：
        // X 轴代表宽度，Y 轴代表高度/厚度，Z 轴代表我们实际想要的完整视觉长度
        runway.localScale = new Vector3(runwayWidth, runwayThickness, visualTotalLength);

        // 3. 旋转 (Rotation)：让它的 Z 轴面向终点方向
        // 因为地平线的 Y 相同，所以 direction 的 Y 是 0，LookRotation 只会在水平面旋转，完美契合任意角度
        runway.rotation = Quaternion.LookRotation(direction);
    }

    private void FinalizeRunway(Vector3 p1, Vector3 p2)
    {
        // 创建真正的实体跑道模型
        GameObject finalRunway = GameObject.CreatePrimitive(PrimitiveType.Cube);
        finalRunway.name = "Runway_New";
        
        Vector3 direction = p2 - p1;
        float totalLength = direction.magnitude;
        float visualTotalLength = totalLength + runwayWidth;

        UpdateRunwayTransform(finalRunway.transform, p1, p2, visualTotalLength);

        // 赋予实际材质
        if (placedMaterial != null)
        {
            finalRunway.GetComponent<Renderer>().material = placedMaterial;
        }

        // === 【新增】在放置完成的瞬间，直接生成底层的图论节点网络 并且注册到大脑管理器中 ===
        GenerateRunwayNetwork(p1, p2, runwayWidth);

        // 销毁预览版
        Destroy(currentGhostRunway);
        
        // 恢复闲置状态并重新打开菜单
        ExitBuildMode();
        tooltip = "跑道建造完成！网络节点已生成。";
    }

    // 建造真正的逻辑心脏：把视觉线段切片成数据网络
    private void GenerateRunwayNetwork(Vector3 start, Vector3 end, float width)
    {
        Vector3 direction = end - start;
        float totalLength = direction.magnitude;
        Vector3 normalizedDir = direction.normalized;

        // 这里暂且命名为 TEMP，日后跟进 UI 面板的输入赋值
        RunwayData runwayData = new RunwayData("RWY_TEMP", totalLength, width);

        // 每隔 100m 放置一个节点
        int numIntervals = Mathf.FloorToInt(totalLength / 100f);
        PathNode previousNode = null;

        for (int i = 0; i <= numIntervals; i++)
        {
            float currentDist = i * 100f;
            Vector3 nodePos = start + (normalizedDir * currentDist);
            
            // 判断节点的类型
            NodeType type = NodeType.RunwayCenterline;
            if (i == 0) type = NodeType.RunwayThreshold;

            PathNode currentNode = new PathNode($"Node_100m_{i}", nodePos, type);
            runwayData.centerlineNodes.Add(currentNode);

            if (i == 0) runwayData.thresholdNode = currentNode;

            // 从第二个点开始，和上面的点连结起来！打通任督二脉
            if (previousNode != null)
            {
                float dist = Vector3.Distance(previousNode.position, currentNode.position);
                // 跑道的中心线是双向通行的
                previousNode.AddEdge(currentNode, dist, EdgeType.RunwaySegment, false); 
            }

            previousNode = currentNode;
        }

        // 处理尾巴：如果总长度不是 100 的完美倍数（比如 1250m），我们在最后尽头再补一个终点节点
        float remainder = totalLength - (numIntervals * 100f);
        if (remainder > 1f) 
        {
            PathNode finalNode = new PathNode($"Node_End", end, NodeType.RunwayEnd);
            runwayData.centerlineNodes.Add(finalNode);
            runwayData.endNode = finalNode;
            
            float dist = Vector3.Distance(previousNode.position, finalNode.position);
            previousNode.AddEdge(finalNode, dist, EdgeType.RunwaySegment, false);
        }
        else
        {
            // 如果恰好是 100 的倍数（比如 1200m）那最后一个就是终点
            if (previousNode != null)
            {
                previousNode.type = NodeType.RunwayEnd;
                runwayData.endNode = previousNode;
            }
        }

        // 把这个完美的数据结构扔给单例管家！
        if (RunwayNetworkManager.Instance != null)
        {
            RunwayNetworkManager.Instance.RegisterRunway(runwayData);
        }
        else
        {
            Debug.LogWarning("未找到 RunwayNetworkManager，节点网络已抛弃。请在场景中挂载该管理器！");
        }
    }

    private void CancelBuild()
    {
        if (currentGhostRunway != null) Destroy(currentGhostRunway);
        ExitBuildMode();
        tooltip = "已取消建造。";
    }
    
    // 给外部脚本（如暂停菜单）调用的取消接口
    public void CancelBuildFromExternal()
    {
        if (currentState != BuildState.Idle)
        {
            CancelBuild();
        }
    }

    private void ExitBuildMode()
    {
        currentState = BuildState.Idle;
        
        // 如果想在建完之后立刻自动把跑道菜单唤出来，可以通过查找 FindManager 激活
        // 或者抛出一个事件。这里简单的做法是找到被隐藏的 Manager 重新开启（由于物体失活，FindObjectOfType 不一定管用，
        // 我们可以寻找叫 "Environment_Window" 或同名画布/父节点的物体，或者直接不管它，等玩家用 UI 快捷键重新唤出）。
    }

    // 将屏幕鼠标坐标转化为绝对的海平面(Y=0)世界坐标，并加上网格依附（自动四舍五入到整数）
    private Vector3? GetMouseGroundPosition()
    {
        if (Camera.main == null) return null;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);

        // 方案：最稳妥的数学平面相交法，因为海平面在 Y=0
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

        return null; // 比如鼠标仰视天空时
    }

    // 临时 GUI 用来显示文字提示
    private void OnGUI()
    {
        if (!string.IsNullOrEmpty(tooltip) && currentState != BuildState.Idle)
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 28;
            style.fontStyle = FontStyle.Bold;
            style.normal.textColor = Color.yellow;
            style.alignment = TextAnchor.MiddleCenter;

            // 在屏幕正上方居中绘制提示
            Rect rect = new Rect(0, 50, Screen.width, 100);
            GUI.Label(rect, tooltip, style);
        }
        else if (!string.IsNullOrEmpty(tooltip)) 
        {
            // 建造完成/取消后延时几秒的提示可以画在这里，或者直接被清空
            GUIStyle style = new GUIStyle();
            style.fontSize = 24;
            style.normal.textColor = Color.white;
            GUI.Label(new Rect(20, 100, 400, 100), tooltip, style);
        }

        // 绘制鼠标跟随的浮动提示（跑道等级与长度）
        if (currentState == BuildState.PlacingEnd)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            
            // Unity GUI 的 Y 轴是从上往下的（0 在顶部），而输入系统是从下往上的，需要翻转
            float guiY = Screen.height - mousePos.y; 

            Vector3? hitPos = GetMouseGroundPosition();
            if (hitPos.HasValue)
            {
                Vector3 endPos = hitPos.Value;
                
                if (Keyboard.current != null && Keyboard.current.shiftKey.isPressed)
                {
                    endPos = SnapTo45Degrees(startPoint, endPos);
                }
                
                float length = Vector3.Distance(startPoint, endPos);
                int lengthCategory = GetICAOLengthNumber(length);
                
                // 动态计算，比如 1A, 2C, 4F
                string floatText = $"{lengthCategory}{currentCategoryName}\n{length:F0}m";

                GUIStyle floatStyle = new GUIStyle();
                floatStyle.fontSize = 22;
                floatStyle.fontStyle = FontStyle.Bold;
                floatStyle.normal.textColor = Color.green;
                
                // 黑字描边用于视效增强
                GUIStyle shadowStyle = new GUIStyle(floatStyle);
                shadowStyle.normal.textColor = Color.black;

                // 绘制在鼠标偏右下的地方
                Rect shadowRect = new Rect(mousePos.x + 22, guiY + 22, 200, 80);
                Rect labelRect = new Rect(mousePos.x + 20, guiY + 20, 200, 80);

                GUI.Label(shadowRect, floatText, shadowStyle);
                GUI.Label(labelRect, floatText, floatStyle);
            }
        }
    }

    // 根据 ICAO 标准，将跑道长度换算为代号 1-4
    private int GetICAOLengthNumber(float length)
    {
        if (length < 800) return 1;
        if (length < 1200) return 2;
        if (length < 1800) return 3;
        return 4; // >= 1800m
    }
}
