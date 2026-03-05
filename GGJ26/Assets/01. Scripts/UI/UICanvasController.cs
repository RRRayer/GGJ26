using System;
using UnityEngine;
using UnityEngine.Serialization;

public class UICanvasController : MonoBehaviour
{
    [SerializeField] private UIMenuManager menuCanvas;
    [SerializeField] private UIManager gameplayCanvas;
    
    [Header("Listening to")]
    [SerializeField] private GameStateEventChannelSO onGameStateChanged;

    private void Awake()
    {
        // Canvas 비활성화
        menuCanvas.gameObject.SetActive(false);
        gameplayCanvas.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        onGameStateChanged.OnEventRaised += SetCanvas;
    }

    private void OnDisable()
    {
        onGameStateChanged.OnEventRaised -= SetCanvas;
    }

    /// <summary>
    /// 현재 게임 상태에 맞게 Canvas 활성화/비활성화
    /// </summary>
    private void SetCanvas(GameState currentGameState)
    {
        bool isGameplayState = currentGameState == GameState.Gameplay || currentGameState == GameState.Pause;
        bool isMenuState = currentGameState == GameState.Menu;
        
        gameplayCanvas.gameObject.SetActive(isGameplayState);
        menuCanvas.gameObject.SetActive(isMenuState);
    }
}
