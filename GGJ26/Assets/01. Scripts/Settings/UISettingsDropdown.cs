using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class UISettingsDropdown : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown dropdown;
    [SerializeField] private TextMeshProUGUI text;

    public UnityAction<int> ValueChanged;
    
    private void Awake()
    {
        dropdown.onValueChanged.AddListener(DropDownValueChanged);
    }

    private void DropDownValueChanged(int value)
    {
        ValueChanged?.Invoke(value);
    }

    public void SetValue(int value)
    {
        dropdown.value = value;
    }

    public float GetValue()
    {
        return dropdown.value;
    }

    public void ClearOptions()
    {
        dropdown.ClearOptions();   
    }

    public void AddOptions(List<string> option)
    {
        dropdown.AddOptions(option);
    }

    public void RefreshShownValue()
    {
        dropdown.RefreshShownValue();
    }
}
