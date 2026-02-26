using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UIGenericButton : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI buttonText;
    [SerializeField] private Button button;
    
    public UnityAction Clicked;

    public void Click()
    {
        if (Clicked == null)
        {
            Debug.LogWarning($"[UIGenericButton] Clicked listener is not assigned on {name}.", this);
            return;
        }

        Clicked.Invoke();
    }

    public void SetButton(string newText)
    {
        if (buttonText != null)
        {
            buttonText.text = newText;
        }
    }
}
