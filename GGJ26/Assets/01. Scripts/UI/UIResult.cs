using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement; // Added
using UnityEngine.UI; // Added

public class UIResult : MonoBehaviour
{
    [Header("Listening on")]
    [SerializeField] private GameResultEventChannelSO onGameResult;

    [Header("UI")]
    [SerializeField] private Canvas ResultCanvas;
    [SerializeField] private TextMeshProUGUI txtResult;
    [SerializeField] private Button confirmButton; // Added
    [SerializeField] private Canvas[] notResultCanvas;
    
    [Header("Game Data")]
    [SerializeField] private PlayerStateManager playerStateManager;


    private void OnEnable()
    {
        if (onGameResult != null)
        {
            onGameResult.OnEventRaised += OnGameResult;
        }
        if (confirmButton != null) // Added listener
        {
            confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        }
    }

    private void OnDisable()
    {
        if (onGameResult != null)
        {
            onGameResult.OnEventRaised -= OnGameResult;
        }
        if (confirmButton != null) // Removed listener
        {
            confirmButton.onClick.RemoveListener(OnConfirmButtonClicked);
        }
    }

    public void HideResult()
    {
        ResultCanvas.enabled = false;
        foreach (var c in notResultCanvas) c.enabled = true;
    }

    public void ShowResult()
    {
        Debug.Log("Showing Result UI");
        foreach (var c in notResultCanvas) c.enabled = false;
        ResultCanvas.enabled = true;

        // Make mouse cursor visible and unlock its state
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void OnGameResult(GameResultData data)
    {
        ShowResult();
        if (txtResult != null)
        {
            txtResult.text = data.LocalPlayerWin ? "You Win!" : "You Lose!";
        }
    }

    private void OnConfirmButtonClicked() // Added handler
    {
        var launcher = FindFirstObjectByType<FusionLauncher>();
        if (launcher != null)
        {
            launcher.ShutdownRunner();
        }

        SceneManager.LoadScene("Lobby");
    }
}
