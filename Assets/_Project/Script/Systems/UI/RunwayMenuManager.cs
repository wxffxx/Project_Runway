using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PP_RY.Systems.UI
{
    public class RunwayMenuManager : MonoBehaviour
    {
        public enum ICAORunwayCategory
        {
            None,
            A, // 15m
            B, // 23m
            C, // 30m
            D, // 45m
            E, // 45m
            F  // 60m
        }

        [Header("ICAO Category Selection Buttons")]
        public Button catAButton;
        public Button catBButton;
        public Button catCButton;
        public Button catDButton;
        public Button catEButton;
        public Button catFButton;

        [Header("Runway Number Input")]
        [Tooltip("输入 01 到 36 之间的数字")]
        public TMP_InputField numberInputField;

        [Header("Position Suffix (L, R, C, or Empty)")]
        [Tooltip("可选下拉菜单 (L=Left, R=Right, C=Center, 无=留空)")]
        public TMP_Dropdown positionSuffixDropdown;

        [Header("Confirmation")]
        public Button buildButton;
        [Tooltip("用于显示当前选择的提示信息或错误")]
        public TextMeshProUGUI infoText;

        // Current Selections
        private ICAORunwayCategory _selectedCategory = ICAORunwayCategory.None;
        private string _selectedNumber = "";

        // ICAO Width Mapping
        private readonly Dictionary<ICAORunwayCategory, int> categoryWidths = new Dictionary<ICAORunwayCategory, int>
        {
            { ICAORunwayCategory.A, 15 },
            { ICAORunwayCategory.B, 23 },
            { ICAORunwayCategory.C, 30 },
            { ICAORunwayCategory.D, 45 },
            { ICAORunwayCategory.E, 45 },
            { ICAORunwayCategory.F, 60 }
        };

        private void Start()
        {
            // Category Buttons now immediately start the building process
            if (catAButton != null) catAButton.onClick.AddListener(() => StartBuilding(ICAORunwayCategory.A));
            if (catBButton != null) catBButton.onClick.AddListener(() => StartBuilding(ICAORunwayCategory.B));
            if (catCButton != null) catCButton.onClick.AddListener(() => StartBuilding(ICAORunwayCategory.C));
            if (catDButton != null) catDButton.onClick.AddListener(() => StartBuilding(ICAORunwayCategory.D));
            if (catEButton != null) catEButton.onClick.AddListener(() => StartBuilding(ICAORunwayCategory.E));
            if (catFButton != null) catFButton.onClick.AddListener(() => StartBuilding(ICAORunwayCategory.F));
            // Set up input validation
            if (numberInputField != null)
            {
                numberInputField.contentType = TMP_InputField.ContentType.IntegerNumber;
                numberInputField.characterLimit = 2;
                numberInputField.onValueChanged.AddListener(ValidateNumberInput);
            }

            UpdateUIState();
        }

        private void StartBuilding(ICAORunwayCategory category)
        {
            int width = categoryWidths[category];
            string catName = category.ToString(); // 获取类别字母，比如 A, B, C 等
            Debug.Log($"【建造指令】 开始建造跑道白模, ICAO等级: {category}, 宽度: {width}m");

            if (RunwayBuilder.Instance != null)
            {
                // 立刻进入建造模式，同时把跑道等级字符串传给建造器用于UI显示
                RunwayBuilder.Instance.StartBuildingRunway(width, catName);
                
                // 自动关闭整个 UI 面板，让玩家专心画跑道
                gameObject.SetActive(false);
            }
            else
            {
                Debug.LogError("场景中找不到 RunwayBuilder，请确保已把脚本挂载到任意常驻物体上！");
            }
        }

        private void ValidateNumberInput(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                _selectedNumber = "";
            }
            else
            {
                // Ensure it's a valid number between 1 and 36
                if (int.TryParse(input, out int num))
                {
                    if (num < 1) num = 1;
                    if (num > 36) num = 36;
                    
                    // Format to two digits (e.g., "01", "09", "36")
                    _selectedNumber = num.ToString("D2");
                    
                    // We only want to force update the text if they finish editing or typed invalid stuff, 
                    // but for smooth typing, doing it instantly can sometimes be annoying if they are trying to type "3".
                    // Let's keep the internal state as D2, but leave the input box alone until they confirm.
                }
                else
                {
                    _selectedNumber = "";
                }
            }
            
            UpdateUIState();
        }

        private void UpdateButtonVisuals()
        {
            if (catAButton != null) catAButton.interactable = _selectedCategory != ICAORunwayCategory.A;
            if (catBButton != null) catBButton.interactable = _selectedCategory != ICAORunwayCategory.B;
            if (catCButton != null) catCButton.interactable = _selectedCategory != ICAORunwayCategory.C;
            if (catDButton != null) catDButton.interactable = _selectedCategory != ICAORunwayCategory.D;
            if (catEButton != null) catEButton.interactable = _selectedCategory != ICAORunwayCategory.E;
            if (catFButton != null) catFButton.interactable = _selectedCategory != ICAORunwayCategory.F;
        }

        // UI State Update Logic (No longer strictly needed for building, 
        // but kept to prevent null references if you still have the text box active)
        private void UpdateUIState()
        {
            // We removed the forced requirement logic, so the Build button isn't needed here.
            if (infoText != null)
            {
                infoText.text = "点击跑道规格 (A-F) 立刻开始建造白模。跑道编号和后缀将在日后系统自动分配或手动编辑。";
            }
        }
    }
}
