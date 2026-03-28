using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using PP_RY.Core.Navigation;

namespace PP_RY.Systems.Building
{
    public class RoadBuilder : BaseBuilder
    {
        [Header("Materials")]
        public Material ghostRoadMaterial;
        public Material placedRoadMaterial;

        public float roadWidth = 8f; // 典型双向车道宽度

        public static RoadBuilder Instance;

        private List<Vector3> currentWaypoints = new List<Vector3>();
        private List<GameObject> ghostSegments = new List<GameObject>();
        
        // 允许玩家拉出一长串路网络，最后可以并入大管家中
        public List<RoadData> allBuiltRoads = new List<RoadData>();

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void StartBuildingRoad()
        {
            currentState = BuildState.PlacingStart;
            tooltip = "修建高架路：点击第一下确定公路起点。";
            
            ClearGhosts();
            currentWaypoints.Clear();
        }

        protected override void HandlePlacingStart()
        {
            Vector3? hitPos = GetMouseGroundPosition();
            if (hitPos.HasValue && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                // 吸附到偶数坐标，让直线更好画
                Vector3 start = hitPos.Value;
                start.x = Mathf.Round(start.x);
                start.z = Mathf.Round(start.z);

                currentWaypoints.Add(start);
                startPoint = start;
                currentState = BuildState.PlacingEnd;
                
                tooltip = "移动鼠标拉出路线。再次【左键】打下新节点，连画直至完成。【右键】确认并修路。";
                
                CreateGhostSegment();
            }
        }

        protected override void HandlePlacingEnd()
        {
            Vector3? hitPos = GetMouseGroundPosition();
            if (!hitPos.HasValue) return;

            Vector3 currentPos = hitPos.Value;
            
            // 加入 Shift 键强制吸附：45 度角度
            if (Keyboard.current != null && Keyboard.current.shiftKey.isPressed)
            {
                currentPos = SnapToAngle(currentWaypoints[currentWaypoints.Count-1], currentPos);
            }

            // 更新最后一段幽灵预览线的位置 (倒数第一个 Ghost 是预演还没点下去的路线)
            GameObject lastGhost = ghostSegments[ghostSegments.Count - 1];
            UpdateSegmentTransform(lastGhost.transform, currentWaypoints[currentWaypoints.Count - 1], currentPos);

            // 当玩家点左键时，固化当前的航点，并添加新一轮的幽灵预览
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                // 不要跟上一点太近
                if (Vector3.Distance(currentWaypoints[currentWaypoints.Count - 1], currentPos) > 1f)
                {
                    currentWaypoints.Add(currentPos);
                    CreateGhostSegment();
                }
            }

            // 当玩家点右键时，结束修建
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            {
                if (currentWaypoints.Count > 1) 
                {
                    FinalizeRoad();
                }
                else
                {
                    CancelBuild();
                }
            }
        }

        private void CreateGhostSegment()
        {
            GameObject seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seg.name = $"Ghost_Road_Segment_{ghostSegments.Count}";
            Destroy(seg.GetComponent<BoxCollider>());
            if (ghostRoadMaterial != null) seg.GetComponent<Renderer>().material = ghostRoadMaterial;
            ghostSegments.Add(seg);
        }

        private void UpdateSegmentTransform(Transform tf, Vector3 p1, Vector3 p2)
        {
            if (tf == null) return;
            Vector3 diff = p2 - p1;
            float dist = diff.magnitude;

            tf.position = (p1 + p2) / 2f + new Vector3(0, 0.05f, 0); // 在地面上一点点，但低于跑道和机坪
            tf.localScale = new Vector3(roadWidth, 0.1f, dist);
            if (dist > 0.01f)
            {
                tf.rotation = Quaternion.LookRotation(diff);
            }
        }

        private void FinalizeRoad()
        {
            RoadData newRoad = new RoadData($"Road_{System.Guid.NewGuid().ToString().Substring(0,5)}");

            // 将所有固化的路段换成实物
            for (int i = 0; i < currentWaypoints.Count; i++)
            {
                newRoad.AddWaypoint(currentWaypoints[i]);

                if (i < currentWaypoints.Count - 1)
                {
                    GameObject finalSeg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    finalSeg.name = $"Placed_Road_Seg_{i}";
                    UpdateSegmentTransform(finalSeg.transform, currentWaypoints[i], currentWaypoints[i+1]);
                    if (placedRoadMaterial != null) finalSeg.GetComponent<Renderer>().material = placedRoadMaterial;
                }
            }

            allBuiltRoads.Add(newRoad);
            Debug.Log($"【半寻路网络】 已注册新道路: {newRoad.id}, 全长: {newRoad.totalLength:F1}m, 包含 {newRoad.waypoints.Count} 个主航点。");

            ClearGhosts();
            currentWaypoints.Clear();
            
            ExitBuildMode();
            tooltip = "陆侧路网建造完成！";
        }

        private Vector3 SnapToAngle(Vector3 start, Vector3 end)
        {
            Vector3 direction = end - start;
            float distance = direction.magnitude;
            if (distance < 0.1f) return end;

            float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float snappedAngle = Mathf.Round(angle / 45f) * 45f;
            
            Vector3 snappedDirection = Quaternion.Euler(0, snappedAngle, 0) * Vector3.forward;
            return start + snappedDirection * distance;
        }

        private void ClearGhosts()
        {
            foreach (var g in ghostSegments)
            {
                if (g != null) Destroy(g);
            }
            ghostSegments.Clear();
        }

        protected override void CancelBuild()
        {
            ClearGhosts();
            currentWaypoints.Clear();
            
            base.CancelBuild();
            tooltip = "已取消修路。";
        }

        // 把右键取消覆写掉，因为右键在路网建设中是“确认保存当前进度并退出”的意思
        protected override void Update()
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
        }

        protected override void DrawFloatingTooltip()
        {
            if (currentState == BuildState.PlacingEnd && currentWaypoints.Count > 0)
            {
                Vector2 mousePos = Mouse.current.position.ReadValue();
                float guiY = Screen.height - mousePos.y; 

                Vector3? hitPos = GetMouseGroundPosition();
                if (hitPos.HasValue)
                {
                    Vector3 currentPos = hitPos.Value;
                    if (Keyboard.current != null && Keyboard.current.shiftKey.isPressed)
                    {
                        currentPos = SnapToAngle(currentWaypoints[currentWaypoints.Count-1], currentPos);
                    }
                    
                    float length = Vector3.Distance(currentWaypoints[currentWaypoints.Count - 1], currentPos);
                    string floatText = $"Segment Length: {length:F1}m\nContinuous Dots: {currentWaypoints.Count}";

                    GUIStyle floatStyle = new GUIStyle();
                    floatStyle.fontSize = 20;
                    floatStyle.fontStyle = FontStyle.Bold;
                    floatStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f); 
                    
                    GUIStyle shadowStyle = new GUIStyle(floatStyle);
                    shadowStyle.normal.textColor = Color.black;

                    Rect shadowRect = new Rect(mousePos.x + 22, guiY + 22, 200, 120);
                    Rect labelRect = new Rect(mousePos.x + 20, guiY + 20, 200, 120);

                    GUI.Label(shadowRect, floatText, shadowStyle);
                    GUI.Label(labelRect, floatText, floatStyle);
                }
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // 在编辑器里画出航点贝塞尔/折线，这代表了半寻路系统的物理基础
            if (allBuiltRoads == null) return;
            foreach (var road in allBuiltRoads)
            {
                Gizmos.color = Color.cyan;
                for (int i = 0; i < road.waypoints.Count - 1; i++)
                {
                    Gizmos.DrawLine(road.waypoints[i], road.waypoints[i+1]);
                    Gizmos.DrawSphere(road.waypoints[i], 0.8f);
                }
                if (road.waypoints.Count > 0) 
                    Gizmos.DrawSphere(road.waypoints[road.waypoints.Count-1], 0.8f);
            }
        }
#endif
    }
}
