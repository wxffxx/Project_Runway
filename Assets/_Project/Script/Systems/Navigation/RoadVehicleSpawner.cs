using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PP_RY.Core.Navigation;
using PP_RY.Systems.Building;

namespace PP_RY.Systems.Navigation
{
    /// <summary>
    /// 陆侧车辆管理器 (Landside Vehicle Logic)
    /// 完全不使用 NavMesh 或是 Graph A* 搜寻，纯粹基于路标序列(Waypoints)进行顺滑位移插值(Lerp)
    /// 展现了 "Half-Pathfinding" 的终极降级性能妥协方案。
    /// </summary>
    public class RoadVehicleSpawner : MonoBehaviour
    {
        public static RoadVehicleSpawner Instance;

        [Header("设置")]
        public GameObject carPrefab; // 汽车的简单的低模预制体
        public float spawnInterval = 3f; // 每几秒刷一辆车
        public float vehicleSpeed = 15f; 

        // 全局正在路上运行的车，用于前车雷达侦测防追尾
        private List<RoadVehicle> activeVehicles = new List<RoadVehicle>();

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        void Start()
        {
            // 开启无限刷车循环
            StartCoroutine(SpawnRoutine());
        }

        IEnumerator SpawnRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(spawnInterval);

                if (RoadBuilder.Instance != null && RoadBuilder.Instance.allBuiltRoads.Count > 0)
                {
                    // 随机找一条建好的高架路
                    RoadData randomRoad = RoadBuilder.Instance.allBuiltRoads[Random.Range(0, RoadBuilder.Instance.allBuiltRoads.Count)];

                    if (randomRoad.waypoints.Count >= 2)
                    {
                        SpawnVehicle(randomRoad);
                    }
                }
            }
        }

        private void SpawnVehicle(RoadData targetRoad)
        {
            GameObject carObj;
            if (carPrefab != null)
            {
                carObj = Instantiate(carPrefab, targetRoad.waypoints[0], Quaternion.identity);
            }
            else
            {
                // 如果用户没配置预制体，咱们就临时画个方块代替汽车
                carObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                carObj.transform.localScale = new Vector3(2f, 1.5f, 4f);
                carObj.GetComponent<Renderer>().material.color = new Color(Random.value, Random.value, Random.value);
                carObj.transform.position = targetRoad.waypoints[0];
            }

            carObj.name = $"RoadVehicle_{System.Guid.NewGuid().ToString().Substring(0,4)}";

            RoadVehicle logic = carObj.AddComponent<RoadVehicle>();
            logic.Initialize(targetRoad, vehicleSpeed);
            
            activeVehicles.Add(logic);
        }
        
        public void RemoveVehicle(RoadVehicle vehicle)
        {
            if (activeVehicles.Contains(vehicle))
            {
                activeVehicles.Remove(vehicle);
            }
            Destroy(vehicle.gameObject);
        }
    }

    /// <summary>
    /// 挂载在每一辆生成出的高架车上的纯插值引擎
    /// </summary>
    public class RoadVehicle : MonoBehaviour
    {
        private RoadData assignedRoad;
        private int currentWaypointIndex = 0;
        private float speed = 10f;

        public void Initialize(RoadData road, float speed)
        {
            this.assignedRoad = road;
            this.speed = speed;
            this.currentWaypointIndex = 0; // 起点

            if (road.waypoints.Count > 1)
            {
                transform.rotation = Quaternion.LookRotation(road.waypoints[1] - road.waypoints[0]);
            }
        }

        void Update()
        {
            if (assignedRoad == null || currentWaypointIndex >= assignedRoad.waypoints.Count) return;

            Vector3 targetPoint = assignedRoad.waypoints[currentWaypointIndex];

            // 使用纯数学直接移动过去，不要 NavMeshAgent！没有任何开销！
            transform.position = Vector3.MoveTowards(transform.position, targetPoint, speed * Time.deltaTime);

            // 转向
            Vector3 direction = targetPoint - transform.position;
            if (direction.sqrMagnitude > 0.05f)
            {
                Quaternion lookRot = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * 5f);
            }

            // 是否到达了这个节点
            if (Vector3.Distance(transform.position, targetPoint) < 0.5f)
            {
                currentWaypointIndex++;
                
                // 如果到达了最后一个节点 (比如航站楼大门或者出图点)
                if (currentWaypointIndex >= assignedRoad.waypoints.Count)
                {
                    // 这里未来可以写：如果这是航站楼落客区，实例化 3 个旅客小人
                    Debug.Log($"[{gameObject.name}] 到达公路终点，乘客已落客进入大厅，车辆销毁。");

                    if (RoadVehicleSpawner.Instance != null)
                    {
                        RoadVehicleSpawner.Instance.RemoveVehicle(this);
                    }
                    else
                    {
                        Destroy(gameObject);
                    }
                }
            }
            
            // TODO: 未来只需要从 RoadVehicleSpawner.activeVehicles 中寻找是否有距离在 5 米内的其它车，
            // 动态调节 speed = 0 来实现排队防追尾堵车现象。
        }
    }
}
