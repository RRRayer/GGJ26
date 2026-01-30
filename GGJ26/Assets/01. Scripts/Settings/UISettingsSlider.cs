using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UISettingsSlider : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private TextMeshProUGUI text;

    public UnityAction<float> ValueChanged;
    
    private void Awake()
    {
        slider.onValueChanged.AddListener(SliderValueChanged);
    }

    private void SliderValueChanged(float value)
    {
        ValueChanged?.Invoke(value);
    }

    public void SetSlider(float value)
    {
        slider.value = value;
    }

    public float GetValue()
    {
        return slider.value;
    }
}
