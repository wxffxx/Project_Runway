using UnityEngine;
using UnityEngine.InputSystem;
using PP_RY.Core.Navigation;
using PP_RY.Systems.Navigation;

namespace PP_RY.Systems.Building
{
    public class GateBuilder : BaseBuilder
    {
        [Header("Materials")]
        public Material ghostGateMaterial;
        public Material ghostPushbackMaterial;
        public Material placedGateMaterial;

        [Header("Gate Configuration")]
        public GateSize currentGateSize = GateSize.Medium;
        public float snapRadius = 30f; // 推出路线终点自动吸附到周围滑行道节点的最大距离
        
        public int currentFloorLayer = 0;
        public const float FLOOR_HEIGHT = 6f; // 与 Terminal 层高保持一致

        public static GateBuilder Instance;

        private GameObject ghostGateObj; // 机位底座与廊桥预览
        private GameObject ghostPushbackLine; // 推出路线预览 (简单用一个拉长的 Cube 或者 LineRenderer，这里用 Cube 模拟黄线)

        private Vector3 gateNodePos;
        private PathNode snappedPushbackNode; // 如果在拖拽时吸附到了滑行道，这里存下那个节点

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void StartBuildingGate(GateSize size)
        {
            currentGateSize = size;
            currentState = BuildState.PlacingStart;
            UpdateFloorTooltip();

            if (ghostGateObj != null) Destroy(ghostGateObj);
            if (ghostPushbackLine != null) Destroy(ghostPushbackLine);
            ghostGateObj = null;
            ghostPushbackLine = null;
            snappedPushbackNode = null;
        }

        protected override void Update()
        {
            base.Update();
            
            if (currentState != BuildState.Idle && Keyboard.current != null)
            {
                if (Keyboard.current.pageUpKey.wasPressedThisFrame)
                {
                    currentFloorLayer++;
                    UpdateFloorTooltip();
                }
                else if (Keyboard.current.pageDownKey.wasPressedThisFrame)
                {
                    if (currentFloorLayer > 0) currentFloorLayer--;
                    UpdateFloorTooltip();
                }
            }
        }

        private void UpdateFloorTooltip()
        {
            if (currentState == BuildState.PlacingStart)
            {
                tooltip = $"建造停机位 (楼层: {currentFloorLayer}F) 高度 {currentFloorLayer * FLOOR_HEIGHT}m：\n请紧贴航站楼层边缘点击放置廊桥。\n【PageUp/PageDown】切换接驳层。";
            }
            else if (currentState == BuildState.PlacingEnd)
            {
                tooltip = $"拖出飞机【推出路线】(Pushback Path)，靠近地表现有滑行道可自动垂向吸附。再次点击完成。";
            }
        }

        protected override void HandlePlacingStart()
        {
            Vector3? hitPos = GetMouseGroundPosition();
            if (hitPos.HasValue)
            {
                // 绘制幽灵模型跟随鼠标
                if (ghostGateObj == null)
                {
                    ghostGateObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    ghostGateObj.name = "Ghost_Gate_Module";
                    Destroy(ghostGateObj.GetComponent<BoxCollider>());
                    if (ghostGateMaterial != null) ghostGateObj.GetComponent<Renderer>().material = ghostGateMaterial;
                }

                // 机位尺寸 (预设) A320大概 36x36 占地
                Vector3 gateScale = new Vector3(36f, 0.2f, 36f);
                if (currentGateSize == GateSize.Large) gateScale = new Vector3(60f, 0.2f, 60f);

                float yPos = 0.1f + (currentFloorLayer * FLOOR_HEIGHT);

                ghostGateObj.transform.position = new Vector3(hitPos.Value.x, yPos, hitPos.Value.z);
                ghostGateObj.transform.localScale = gateScale;

                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                {
                    gateNodePos = ghostGateObj.transform.position;
                    startPoint = gateNodePos; // 用于基类逻辑
                    
                    currentState = BuildState.PlacingEnd;
                    UpdateFloorTooltip();

                    ghostPushbackLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    ghostPushbackLine.name = "Ghost_Pushback_Line";
                    Destroy(ghostPushbackLine.GetComponent<BoxCollider>());
                    if (ghostPushbackMaterial != null) ghostPushbackLine.GetComponent<Renderer>().material = ghostPushbackMaterial;
                }
            }
        }

        protected override void HandlePlacingEnd()
        {
            Vector3? hitPos = GetMouseGroundPosition();
            if (hitPos.HasValue)
            {
                Vector3 endPos = hitPos.Value;
                snappedPushbackNode = null;

                // 尝试吸附到距离最近的已存滑行道节点
                if (RunwayNetworkManager.Instance != null && RunwayNetworkManager.Instance.allRunways.Count > 0)
                {
                    float minDistance = snapRadius;
                    PathNode closestNode = null;

                    foreach (var runway in RunwayNetworkManager.Instance.allRunways)
                    {
                        foreach (var node in runway.centerlineNodes)
                        {
                            // 如果是普通滑行道节点才允许被推车推出吸附 (跑道上不能直接拉出机位，太危险)
                            if (node.type == NodeType.TaxiwayPoint)
                            {
                                float dist = Vector3.Distance(endPos, node.position);
                                if (dist < minDistance)
                                {
                                    minDistance = dist;
                                    closestNode = node;
                                }
                            }
                        }
                    }

                    if (closestNode != null)
                    {
                        endPos = closestNode.position;
                        snappedPushbackNode = closestNode;
                    }
                }

                // 绘制推出路线的视觉虚线/黄线预演
                UpdatePushbackLine(ghostPushbackLine.transform, gateNodePos, endPos);

                // 让停机坪的朝向背对推出方向 (机头朝向航站楼)
                Vector3 pushDirection = endPos - gateNodePos;
                if (pushDirection.sqrMagnitude > 1f && ghostGateObj != null)
                {
                    ghostGateObj.transform.rotation = Quaternion.LookRotation(-pushDirection);
                }

                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                {
                    FinalizeGate(gateNodePos, endPos, snappedPushbackNode);
                }
            }
        }

        private void UpdatePushbackLine(Transform tf, Vector3 p1, Vector3 p2)
        {
            if (tf == null) return;
            Vector3 diff = p2 - p1;
            float dist = diff.magnitude;

            tf.position = (p1 + p2) / 2f + new Vector3(0, 0.15f, 0); // 在机坪上方一点点
            tf.localScale = new Vector3(1f, 0.1f, dist);
            if (dist > 0.01f)
            {
                tf.rotation = Quaternion.LookRotation(diff);
            }
        }

        private void FinalizeGate(Vector3 gatePos, Vector3 pushbackEndPos, PathNode snapNode)
        {
            // 1. 视觉实体生成
            GameObject finalGate = Instantiate(ghostGateObj);
            finalGate.name = $"Gate_{currentGateSize}_{System.Guid.NewGuid().ToString().Substring(0, 5)}";
            if (placedGateMaterial != null) finalGate.GetComponent<Renderer>().material = placedGateMaterial;

            // 2. 数据结构生成
            string gateName = "Gate " + Random.Range(10, 99);
            GateData newGate = new GateData(gateName, currentGateSize);
            
            // 机位节点本身
            PathNode gateNode = new PathNode($"{gateName}_Node", gatePos, NodeType.GateNode);
            newGate.gateNode = gateNode;

            // 推出节点配置
            if (snapNode != null)
            {
                // 吸附成功：推出路点直接使用滑行网络里的节点！这就并网了！
                newGate.pushbackEndNode = snapNode;
            }
            else
            {
                // 未吸附：暂时做个孤岛节点 (未来补了滑行道还能连上)
                newGate.pushbackEndNode = new PathNode($"{gateName}_PushEndpoint", pushbackEndPos, NodeType.TaxiwayPoint);
            }

            // 3. 执行连线双向接驳
            float pushDist = Vector3.Distance(gatePos, pushbackEndPos);
            newGate.ConnectPushbackRoute(pushDist);

            // 4. 注册到大管家 (需要在 Manager 中添加此方法)
            if (RunwayNetworkManager.Instance != null)
            {
                RunwayNetworkManager.Instance.RegisterGate(newGate);
            }

            // 清理与重置
            if (ghostGateObj != null) Destroy(ghostGateObj);
            if (ghostPushbackLine != null) Destroy(ghostPushbackLine);
            ghostGateObj = null;
            ghostPushbackLine = null;
            
            ExitBuildMode();
            tooltip = $"停机位 {gateName} 建造完成！{(snapNode != null ? "[已吸附并入滑路网]" : "[未接入路网]")}";
        }

        protected override void CancelBuild()
        {
            if (ghostGateObj != null) Destroy(ghostGateObj);
            if (ghostPushbackLine != null) Destroy(ghostPushbackLine);
            ghostGateObj = null;
            ghostPushbackLine = null;
            
            base.CancelBuild();
            tooltip = "已取消停机位建造。";
        }

        protected override void DrawFloatingTooltip()
        {
            if (currentState == BuildState.PlacingEnd)
            {
                Vector2 mousePos = Mouse.current.position.ReadValue();
                float guiY = Screen.height - mousePos.y; 

                string floatText = "Pushback Route\n";
                if (snappedPushbackNode != null)
                {
                    floatText += "<color=green>[Snapped to Taxiway]</color>";
                }
                else
                {
                    floatText += "<color=red>[Not Snapped]</color>";
                }

                GUIStyle floatStyle = new GUIStyle();
                floatStyle.fontSize = 20;
                floatStyle.fontStyle = FontStyle.Bold;
                floatStyle.richText = true;
                floatStyle.normal.textColor = snappedPushbackNode != null ? Color.green : Color.yellow; 
                
                GUIStyle shadowStyle = new GUIStyle(floatStyle);
                shadowStyle.normal.textColor = Color.black;
                shadowStyle.richText = false; // 阴影不解析富文本

                Rect shadowRect = new Rect(mousePos.x + 22, guiY + 22, 200, 120);
                Rect labelRect = new Rect(mousePos.x + 20, guiY + 20, 200, 120);

                GUI.Label(shadowRect, "Pushback Route\n" + (snappedPushbackNode != null ? "[Snapped to Taxiway]" : "[Not Snapped]"), shadowStyle);
                GUI.Label(labelRect, floatText, floatStyle);
            }
        }
    }
}
