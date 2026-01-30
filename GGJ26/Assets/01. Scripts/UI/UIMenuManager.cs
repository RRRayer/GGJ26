using System.Collections;
using UnityEngine;

public class UIMenuManager : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private UIGenericButton startGameButton;
    [SerializeField] private UIGenericButton exitButton;

    [Header("Listening to")]
    [SerializeField] private GameStateEventChannelSO onGameStateChanged;

    private void OnEnable()
    {
        startGameButton.Clicked += StartGame;
        exitButton.Clicked += ExitGame;
    }

    private void OnDisable()
    {
        startGameButton.Clicked -= StartGame;
        exitButton.Clicked -= ExitGame;
    }
    
    /// <summary>
    /// Binding to 게임 시작 버튼
    /// </summary>
    private void StartGame()
    {
        // 1. 게임 메뉴 -> 게임플레이 상태 전환
        GameManager.Instance.UpdateGameState(GameState.Gameplay);
    }

    /// <summary>
    /// Binding to 게임 종료 버튼
    /// </summary>
    private void ExitGame()
    {
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        Application.Quit();  
    #endif
    }
}