using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace PP_RY.Systems.Building
{
    public class TerminalFloorBuilder : BaseBuilder
    {
        [Header("Materials")]
        public Material ghostFloorMaterial;
        public Material placedFloorMaterial;

        public static TerminalFloorBuilder Instance;

        public int currentFloorLayer = 0;
        public const float FLOOR_HEIGHT = 6f; // 固定层高：6米

        private GameObject ghostFloorObj;
        
        // 我们将建造好的地块暂时存放在普通列表中，未来如果需要真正的内部网格寻路，这里可以改成二维数组网格注册。
        public List<GameObject> builtTerminalFloors = new List<GameObject>();

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void StartBuildingTerminalFloor()
        {
            currentState = BuildState.PlacingStart;
            tooltip = $"建造航站楼地块 (当前楼层: {currentFloorLayer}F)：\n点击并按住拖拽以绘制一个矩形。\n【PageUp/PageDown】切换楼层高度。";

            if (ghostFloorObj != null) Destroy(ghostFloorObj);
            ghostFloorObj = null;
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
                tooltip = $"建造航站楼地块 (当前楼层: {currentFloorLayer}F)：\n点击并按住拖拽以绘制一个矩形。\n【PageUp/PageDown】切换楼层高度。";
            }
            else if (currentState == BuildState.PlacingEnd)
            {
                tooltip = $"拖动鼠标改变航站楼地块大小 (当前: {currentFloorLayer}F)。再次点击完成建设。";
            }
        }

        protected override void HandlePlacingStart()
        {
            Vector3? hitPos = GetMouseGroundPosition();
            if (hitPos.HasValue && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                startPoint = hitPos.Value;
                // 强制吸附到 5m 网格，模拟瓷砖感
                startPoint.x = Mathf.Round(startPoint.x / 5f) * 5f;
                startPoint.z = Mathf.Round(startPoint.z / 5f) * 5f;
                // 将 startPoint 的 y 限定到当前楼层的空中
                startPoint.y = 0f;

                currentState = BuildState.PlacingEnd;
                UpdateFloorTooltip();

                ghostFloorObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ghostFloorObj.name = "Ghost_Terminal_Floor";
                Destroy(ghostFloorObj.GetComponent<BoxCollider>());
                if (ghostFloorMaterial != null) ghostFloorObj.GetComponent<Renderer>().material = ghostFloorMaterial;
            }
        }

        protected override void HandlePlacingEnd()
        {
            Vector3? hitPos = GetMouseGroundPosition();
            if (hitPos.HasValue)
            {
                Vector3 endPos = hitPos.Value;
                // 强制吸附到 5m 网格
                endPos.x = Mathf.Round(endPos.x / 5f) * 5f;
                endPos.z = Mathf.Round(endPos.z / 5f) * 5f;

                // 为了防止起点终点重合，强制至少有 5x5 的大小
                if (Mathf.Abs(endPos.x - startPoint.x) < 5f) endPos.x += (endPos.x >= startPoint.x ? 5f : -5f);
                if (Mathf.Abs(endPos.z - startPoint.z) < 5f) endPos.z += (endPos.z >= startPoint.z ? 5f : -5f);

                UpdateTransform(ghostFloorObj.transform, startPoint, endPos);

                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                {
                    FinalizeTerminalFloor(startPoint, endPos);
                }
            }
        }

        private void UpdateTransform(Transform tf, Vector3 p1, Vector3 p2)
        {
            if (tf == null) return;

            Vector3 min = Vector3.Min(p1, p2);
            Vector3 max = Vector3.Max(p1, p2);

            Vector3 center = (min + max) / 2f;
            center.y = 0.5f + (currentFloorLayer * FLOOR_HEIGHT); // 提升到固定的空中指定高度

            Vector3 size = new Vector3(max.x - min.x, 1f, max.z - min.z);

            tf.position = center;
            tf.localScale = size;
        }

        private void FinalizeTerminalFloor(Vector3 p1, Vector3 p2)
        {
            GameObject finalFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            finalFloor.name = $"TerminalFloor_{System.Guid.NewGuid().ToString().Substring(0, 5)}";
            
            UpdateTransform(finalFloor.transform, p1, p2);
            
            if (placedFloorMaterial != null) finalFloor.GetComponent<Renderer>().material = placedFloorMaterial;
            
            builtTerminalFloors.Add(finalFloor);

            if (ghostFloorObj != null) Destroy(ghostFloorObj);
            ghostFloorObj = null;

            ExitBuildMode();
            tooltip = "航站楼地块建设完成！";
        }

        protected override void CancelBuild()
        {
            if (ghostFloorObj != null) Destroy(ghostFloorObj);
            ghostFloorObj = null;
            
            base.CancelBuild();
            tooltip = "已取消航站楼建造。";
        }

        protected override void DrawFloatingTooltip()
        {
            if (currentState == BuildState.PlacingEnd)
            {
                Vector2 mousePos = Mouse.current.position.ReadValue();
                float guiY = Screen.height - mousePos.y; 

                Vector3? hitPos = GetMouseGroundPosition();
                if (hitPos.HasValue)
                {
                    Vector3 endPos = hitPos.Value;
                    endPos.x = Mathf.Round(endPos.x / 5f) * 5f;
                    endPos.z = Mathf.Round(endPos.z / 5f) * 5f;

                    float width = Mathf.Max(5f, Mathf.Abs(endPos.x - startPoint.x));
                    float length = Mathf.Max(5f, Mathf.Abs(endPos.z - startPoint.z));

                    string floatText = $"Terminal F{currentFloorLayer}\n{width}m x {length}m\nY: {currentFloorLayer * FLOOR_HEIGHT}m";

                    GUIStyle floatStyle = new GUIStyle();
                    floatStyle.fontSize = 22;
                    floatStyle.fontStyle = FontStyle.Bold;
                    floatStyle.normal.textColor = new Color(0.2f, 0.8f, 1f); 
                    
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
}
