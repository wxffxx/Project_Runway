using System.Collections.Generic;
using UnityEngine;
using PP_RY.Core.Navigation;

namespace PP_RY.Systems.Management
{
    /// <summary>
    /// 空中交通管制系统 (Air Traffic Control)
    /// 负责根据 AirportManager 提供的硬件资源，对进出港航班进行逻辑分配和跑道防冲突锁定。
    /// </summary>
    public class ATCManager : MonoBehaviour
    {
        public static ATCManager Instance { get; private set; }

        private AirportManager airportManager;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            airportManager = AirportManager.Instance;
        }

        /// <summary>
        /// 为进近航班分配一条合适的降落跑道
        /// </summary>
        public RunwayData RequestLandingRunway(string flightId)
        {
            if (airportManager == null || airportManager.activeRunways.Count == 0)
            {
                Debug.LogWarning($"[ATC] 无法为航班 {flightId} 分配降落跑道，机场无可用跑道！");
                return null;
            }

            // 简单逻辑：寻找第一条状态为 Free 的跑道
            foreach (var runway in airportManager.activeRunways)
            {
                if (runway.currentStatus == RunwayStatus.Free)
                {
                    // 锁定该跑道
                    runway.currentStatus = RunwayStatus.Landing;
                    runway.activeFlightId = flightId;
                    Debug.Log($"[ATC] 指派跑道 {runway.runwayName} 供航班 {flightId} 降落。跑到状态更新为 Landing。");
                    return runway;
                }
            }

            Debug.Log($"[ATC] 航班 {flightId} 申请降落被拒绝，所有跑道均在忙碌中。要求航班盘旋等待 (Hold in pattern)。");
            return null;
        }

        /// <summary>
        /// 航班脱离跑道后，向 ATC 报告释放资源
        /// </summary>
        public void ReportRunwayVacated(string runwayName, string flightId)
        {
            var runway = airportManager.activeRunways.Find(r => r.runwayName == runwayName);
            if (runway != null && runway.activeFlightId == flightId)
            {
                runway.currentStatus = RunwayStatus.Free;
                runway.activeFlightId = "";
                Debug.Log($"[ATC] 航班 {flightId} 已脱离跑道 {runwayName}，跑道重新开放为 Free 状态。");
            }
        }
    }
}
