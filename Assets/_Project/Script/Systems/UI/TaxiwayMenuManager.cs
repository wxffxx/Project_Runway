using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PP_RY.Systems.UI
{
    public enum ICAOTaxiwayCategory
    {
        A, B, C, D, E, F
    }

    public class TaxiwayMenuManager : MonoBehaviour
    {
        [Header("建造分类按钮")]
        public Button btnCategoryA;
        public Button btnCategoryB;
        public Button btnCategoryC;
        public Button btnCategoryD;
        public Button btnCategoryE;
        public Button btnCategoryF;

        // 滑行道核心主道面宽度 (m)
        private Dictionary<ICAOTaxiwayCategory, float> coreWidths = new Dictionary<ICAOTaxiwayCategory, float>()
        {
            { ICAOTaxiwayCategory.A, 7.5f },
            { ICAOTaxiwayCategory.B, 10.5f },
            { ICAOTaxiwayCategory.C, 15f },  // 取主流 15m (某些情况是18)
            { ICAOTaxiwayCategory.D, 18f },
            { ICAOTaxiwayCategory.E, 23f },
            { ICAOTaxiwayCategory.F, 25f }
        };

        // 包含道肩在内的总宽度 (m) - 仅 D, E, F 类有强制要求，A, B, C 为了视觉也可以给一点点，或者干脆没有（设为等于核心宽度）
        private Dictionary<ICAOTaxiwayCategory, float> totalWidths = new Dictionary<ICAOTaxiwayCategory, float>()
        {
            { ICAOTaxiwayCategory.A, 7.5f },
            { ICAOTaxiwayCategory.B, 10.5f },
            { ICAOTaxiwayCategory.C, 15f },
            { ICAOTaxiwayCategory.D, 38f },
            { ICAOTaxiwayCategory.E, 38f },
            { ICAOTaxiwayCategory.F, 44f }
        };

        private void Start()
        {
            if (btnCategoryA != null) btnCategoryA.onClick.AddListener(() => StartBuilding(ICAOTaxiwayCategory.A));
            if (btnCategoryB != null) btnCategoryB.onClick.AddListener(() => StartBuilding(ICAOTaxiwayCategory.B));
            if (btnCategoryC != null) btnCategoryC.onClick.AddListener(() => StartBuilding(ICAOTaxiwayCategory.C));
            if (btnCategoryD != null) btnCategoryD.onClick.AddListener(() => StartBuilding(ICAOTaxiwayCategory.D));
            if (btnCategoryE != null) btnCategoryE.onClick.AddListener(() => StartBuilding(ICAOTaxiwayCategory.E));
            if (btnCategoryF != null) btnCategoryF.onClick.AddListener(() => StartBuilding(ICAOTaxiwayCategory.F));
        }

        private void StartBuilding(ICAOTaxiwayCategory category)
        {
            float coreWidth = coreWidths[category];
            float totalWidth = totalWidths[category];
            string catName = category.ToString();

            Debug.Log($"【建造指令】 开始建造滑行道, ICAO等级: {category}, 核心宽度: {coreWidth}m, 总宽度(含道肩): {totalWidth}m");

            if (TaxiwayBuilder.Instance != null)
            {
                // 传给建造器：等级名称，中间核心宽度，总宽度(用于画底座道肩)
                TaxiwayBuilder.Instance.StartBuildingTaxiway(coreWidth, totalWidth, catName);
                
                // 自动关闭整个 UI 面板，让玩家专心划线
                gameObject.SetActive(false);
            }
            else
            {
                Debug.LogError("场景中找不到 TaxiwayBuilder，请确保已挂载该脚本！");
            }
        }
    }
}
