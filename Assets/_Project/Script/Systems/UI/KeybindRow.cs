using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class KeybindRow : MonoBehaviour
{
    [Header("UI 引用")]
    public TMP_Text actionNameText; // 显示操作名称 (如 "向前移动")
    public TMP_Text bindingNameText; // 显示当前绑定的按键名 (如 "W")
    public Button rebindButton;      // 点击开始重新绑定的按钮
    public GameObject waitingOverlay; // 等待输入时的遮罩/提示文字 (如 "请按任意键...")

    private InputAction actionToRebind;
    private int bindingIndex;
    private InputActionRebindingExtensions.RebindingOperation rebindingOperation;

    /// <summary>
    ///  初始化这个 UI 行
    /// </summary>
    /// <param name="action">对应的 Input Action</param>
    /// <param name="bIndex">需要修改的绑定索引 (比如复合键的 Up 是 1)</param>
    /// <param name="displayName">面板上人类可读的名字</param>
    public void Initialize(InputAction action, int bIndex, string displayName)
    {
        actionToRebind = action;
        bindingIndex = bIndex;

        if (actionNameText != null) actionNameText.text = displayName;
        
        UpdateBindingDisplay();

        if (rebindButton != null)
        {
            rebindButton.onClick.RemoveAllListeners();
            rebindButton.onClick.AddListener(StartRebinding);
        }

        if (waitingOverlay != null) waitingOverlay.SetActive(false);
    }

    private void UpdateBindingDisplay()
    {
        if (actionToRebind == null) return;
        
        // 获取按键的友好人类可读名字
        string displayString = InputControlPath.ToHumanReadableString(
            actionToRebind.bindings[bindingIndex].effectivePath,
            InputControlPath.HumanReadableStringOptions.OmitDevice);
            
        if (bindingNameText != null) bindingNameText.text = displayString;
    }

    private void StartRebinding()
    {
        if (actionToRebind == null) return;

        // 绑定按键前必须禁用 action
        actionToRebind.Disable();

        if (waitingOverlay != null) waitingOverlay.SetActive(true);
        if (rebindButton != null) rebindButton.interactable = false;

        // 开始录制新按键
        rebindingOperation = actionToRebind.PerformInteractiveRebinding(bindingIndex)
            // 过滤掉鼠标移动，防止一晃鼠标就绑定给了鼠标位移
            .WithControlsExcluding("Mouse/position")
            .WithControlsExcluding("Mouse/delta")
            .OnMatchWaitForAnother(0.1f) // 稍微等一下防抖
            .OnComplete(operation => RebindComplete())
            .OnCancel(operation => RebindCanceled())
            .Start();
    }

    private void RebindComplete()
    {
        rebindingOperation.Dispose();
        rebindingOperation = null;

        if (waitingOverlay != null) waitingOverlay.SetActive(false);
        if (rebindButton != null) rebindButton.interactable = true;

        actionToRebind.Enable();
        UpdateBindingDisplay();

        // 将新的绑定字符串保存到 PlayerPrefs
        SaveBindingOverride();
    }

    private void RebindCanceled()
    {
        rebindingOperation.Dispose();
        rebindingOperation = null;

        if (waitingOverlay != null) waitingOverlay.SetActive(false);
        if (rebindButton != null) rebindButton.interactable = true;

        actionToRebind.Enable();
    }

    private void SaveBindingOverride()
    {
        string overridePath = actionToRebind.bindings[bindingIndex].overridePath;
        if (!string.IsNullOrEmpty(overridePath))
        {
            // 例如把键命名为: "Move_1_Override"
            string prefKey = $"{actionToRebind.name}_{bindingIndex}_Override";
            PlayerPrefs.SetString(prefKey, overridePath);
            PlayerPrefs.Save();
        }
    }

    public static void LoadBindingOverride(InputAction action, int index)
    {
        string prefKey = $"{action.name}_{index}_Override";
        if (PlayerPrefs.HasKey(prefKey))
        {
            string overridePath = PlayerPrefs.GetString(prefKey);
            action.ApplyBindingOverride(index, overridePath);
        }
    }
}
