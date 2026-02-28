using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PP_RY.Systems.UI
{
    public class BuildingMenuManager : MonoBehaviour
    {
        [Header("Main Categories (主分类)")]
        [Tooltip("飞行区按钮")] public Button flightZoneButton;
        [Tooltip("乘客区按钮")] public Button passengerZoneButton;
        [Tooltip("后勤区按钮")] public Button logisticsZoneButton;

        [Header("Main Category Panels (主分类面板)")]
        [Tooltip("飞行区面板")] public GameObject flightZonePanel;
        [Tooltip("乘客区面板")] public GameObject passengerZonePanel;
        [Tooltip("后勤区面板")] public GameObject logisticsZonePanel;
        
        [Header("Flight Zone Sub-Categories (飞行区子分类)")]
        [Tooltip("跑道按钮")] public Button runwayButton;
        [Tooltip("滑行道按钮")] public Button taxiwayButton;
        [Tooltip("机库/停机位按钮")] public Button hangarButton;
        [Tooltip("助航设施按钮")] public Button navAidButton;
        [Tooltip("装饰资产按钮")] public Button decorationButton;

        [Header("Flight Zone Sub-Panels (飞行区子面板)")]
        [Tooltip("跑道内容面板")] public GameObject runwayPanel;
        [Tooltip("滑行道内容面板")] public GameObject taxiwayPanel;
        [Tooltip("机库/停机位内容面板")] public GameObject hangarPanel;
        [Tooltip("助航设施内容面板")] public GameObject navAidPanel;
        [Tooltip("装饰资产内容面板")] public GameObject decorationPanel;

        private void Start()
        {
            // Register Main Category Button Clicks
            if (flightZoneButton != null) flightZoneButton.onClick.AddListener(ShowFlightZone);
            if (passengerZoneButton != null) passengerZoneButton.onClick.AddListener(ShowPassengerZone);
            if (logisticsZoneButton != null) logisticsZoneButton.onClick.AddListener(ShowLogisticsZone);

            // Register Flight Zone Sub-Category Button Clicks
            if (runwayButton != null) runwayButton.onClick.AddListener(ShowRunwayPanel);
            if (taxiwayButton != null) taxiwayButton.onClick.AddListener(ShowTaxiwayPanel);
            if (hangarButton != null) hangarButton.onClick.AddListener(ShowHangarPanel);
            if (navAidButton != null) navAidButton.onClick.AddListener(ShowNavAidPanel);
            if (decorationButton != null) decorationButton.onClick.AddListener(ShowDecorationPanel);
            
            // Initialize Default State
            ShowFlightZone();
        }

        // ======================= Main Categories =======================

        public void ShowFlightZone()
        {
            SetMainPanelsActive(true, false, false);
            SetButtonVisualState(flightZoneButton, true);
            SetButtonVisualState(passengerZoneButton, false);
            SetButtonVisualState(logisticsZoneButton, false);
            
            // Default to showing the first sub-category when opening Flight Zone
            ShowRunwayPanel();
        }

        public void ShowPassengerZone()
        {
            SetMainPanelsActive(false, true, false);
            SetButtonVisualState(flightZoneButton, false);
            SetButtonVisualState(passengerZoneButton, true);
            SetButtonVisualState(logisticsZoneButton, false);
        }

        public void ShowLogisticsZone()
        {
            SetMainPanelsActive(false, false, true);
            SetButtonVisualState(flightZoneButton, false);
            SetButtonVisualState(passengerZoneButton, false);
            SetButtonVisualState(logisticsZoneButton, true);
        }

        private void SetMainPanelsActive(bool flight, bool passenger, bool logistics)
        {
            if (flightZonePanel != null) flightZonePanel.SetActive(flight);
            if (passengerZonePanel != null) passengerZonePanel.SetActive(passenger);
            if (logisticsZonePanel != null) logisticsZonePanel.SetActive(logistics);
        }

        // ======================= Flight Zone Sub-Categories =======================

        public void ShowRunwayPanel()
        {
            SetFlightSubPanelsActive(true, false, false, false, false);
        }

        public void ShowTaxiwayPanel()
        {
            SetFlightSubPanelsActive(false, true, false, false, false);
        }

        public void ShowHangarPanel()
        {
            SetFlightSubPanelsActive(false, false, true, false, false);
        }

        public void ShowNavAidPanel()
        {
            SetFlightSubPanelsActive(false, false, false, true, false);
        }

        public void ShowDecorationPanel()
        {
            SetFlightSubPanelsActive(false, false, false, false, true);
        }

        private void SetFlightSubPanelsActive(bool run, bool taxi, bool hangar, bool nav, bool decor)
        {
            if (runwayPanel != null) runwayPanel.SetActive(run);
            if (taxiwayPanel != null) taxiwayPanel.SetActive(taxi);
            if (hangarPanel != null) hangarPanel.SetActive(hangar);
            if (navAidPanel != null) navAidPanel.SetActive(nav);
            if (decorationPanel != null) decorationPanel.SetActive(decor);
        }

        // ======================= Helper Methods =======================

        /// <summary>
        /// Update Button Visual State (e.g., interactable state or color to show it's active)
        /// </summary>
        private void SetButtonVisualState(Button btn, bool isActive)
        {
            if (btn == null) return;
            
            // Simple visual cue: disable interactability if it's the currently active tab.
            // You can change this to swap sprites or colors instead.
            btn.interactable = !isActive;
        }
    }
}
