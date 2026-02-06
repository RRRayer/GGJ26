using UnityEngine;

public class GameStateController : MonoBehaviour
{
    [SerializeField] private InputReader inputReader;
    [SerializeField] private GameStateEventChannelSO onGameStateChanged;

    public GameState CurrentState { get; private set; } = GameState.None;

    public void Configure(InputReader inputReader, GameStateEventChannelSO onGameStateChanged)
    {
        if (inputReader != null)
        {
            this.inputReader = inputReader;
        }

        if (onGameStateChanged != null)
        {
            this.onGameStateChanged = onGameStateChanged;
        }
    }

    public void SetState(GameState newState)
    {
        if (newState == CurrentState)
        {
            return;
        }

        CurrentState = newState;

        switch (CurrentState)
        {
            case GameState.Gameplay:
                if (inputReader != null) inputReader.EnableGameplayInput();
                Time.timeScale = 1;
                break;
            case GameState.Menu:
                if (inputReader != null) inputReader.EnableUIInput();
                Time.timeScale = 1;
                break;
            case GameState.Pause:
                if (inputReader != null) inputReader.EnableUIInput();
                Time.timeScale = 0;
                break;
            case GameState.CutScene:
                if (inputReader != null) inputReader.DisableAllInput();
                break;
            case GameState.Ending:
                if (inputReader != null) inputReader.DisableAllInput();
                Time.timeScale = 1;
                break;
        }

        onGameStateChanged?.RaiseEvent(CurrentState);
    }

    public bool IsGameplay()
    {
        return CurrentState == GameState.Gameplay;
    }

    public bool IsEnding()
    {
        return CurrentState == GameState.Ending;
    }
}
