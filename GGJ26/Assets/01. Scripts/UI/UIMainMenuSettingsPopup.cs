using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class UIMainMenuSettingsPopup : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button closeButton;
    [SerializeField] private KeyCode closeKey = KeyCode.Escape;

    private void Awake()
    {
        if (panelRoot == null)
        {
            panelRoot = gameObject;
        }

    }

    private void OnEnable()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Close);
        }
    }

    private void OnDisable()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);
        }
    }

    private void Update()
    {
        if (WasPressedThisFrame(closeKey))
        {
            Close();
        }
    }

    public void Close()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    private bool WasPressedThisFrame(KeyCode key)
    {
        if (Keyboard.current != null)
        {
            if (key == KeyCode.Escape)
            {
                return Keyboard.current.escapeKey.wasPressedThisFrame;
            }
        }

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(key);
#else
        return false;
#endif
    }
}
