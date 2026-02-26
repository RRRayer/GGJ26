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
        Clicked.Invoke();
    }

    public void SetButton(string newText)
    {
        buttonText.text = newText;
    }
}
