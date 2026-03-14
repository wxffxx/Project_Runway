using System.Collections.Generic;
using UnityEngine;
using PP_RY.Core.Navigation;

namespace PP_RY.Systems.Management
{
    /// <summary>
    /// 全局机场管理器 (Singleton)
    /// 负责统筹和存储玩家建造的所有关键设施数据（跑道、机位、滑行道网络等）。
    /// 它是 UI 面板和 ATC 系统的唯一数据事实来源。
    /// </summary>
    public class AirportManager : MonoBehaviour
    {
        public static AirportManager Instance { get; private set; }

        [Header("机场设施注册表")]
        public List<RunwayData> activeRunways = new List<RunwayData>();
        public List<GateData> activeGates = new List<GateData>();

        [Header("机场基础属性")]
        public string airportName = "Player International Airport";
        public string icaoCode = "ZBAA";

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        #region 设施注册 API (供建造系统调用)

        public void RegisterRunway(RunwayData runway)
        {
            if (!activeRunways.Contains(runway))
            {
                activeRunways.Add(runway);
                Debug.Log($"[AirportManager] 跑道 {runway.runwayName} 注册成功。当前总数: {activeRunways.Count}");
            }
        }

        public void RegisterGate(GateData gate)
        {
            if (!activeGates.Contains(gate))
            {
                activeGates.Add(gate);
                Debug.Log($"[AirportManager] 机位 {gate.gateName} 注册成功。当前总数: {activeGates.Count}");
            }
        }

        #endregion

        #region 设施查询 API (供 UI 和 ATC 调用)

        /// <summary>
        /// 获取当前所有处于空闲状态的机位
        /// </summary>
        public List<GateData> GetAvailableGates(GateSize minimumSize)
        {
            List<GateData> available = new List<GateData>();
            foreach (var gate in activeGates)
            {
                // TODO: 需要在 GateData 中加入 isOccupied 字段判断
                // 这里暂时假设能匹配尺寸即可
                if (gate.supportedSize >= minimumSize) 
                {
                    available.Add(gate);
                }
            }
            return available;
        }

        #endregion
    }
}
