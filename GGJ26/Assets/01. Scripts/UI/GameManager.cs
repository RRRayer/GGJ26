using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState CurrentGameState
    {
        get => currentGameState;
        set => currentGameState = value;
    }

    private bool hasData = false;
    public bool HasData => hasData;

    [Header("Game Managing")]
    [SerializeField] private SaveLoadSystem saveLoadSystem;
    [SerializeField] private InputReader inputReader;
    [SerializeField] private TransformAnchor playerTransformAnchor;
    [SerializeField] private Transform defaultSpawnPoint;
    [SerializeField] private float totalGameSeconds = 180f;
    [SerializeField] private PlayerStateManager playerStateManager;
    [SerializeField] private StatsManager statsManager;

    [Header("Broadcasting on")]
    [SerializeField] private GameStateEventChannelSO onGameStateChanged;
    [SerializeField] private VoidEventChannelSO onGameEnded;
    [SerializeField] private GameResultEventChannelSO onGameResult;

    private GameObject player;
    private GameState currentGameState = GameState.None;
    private float remainingSeconds;
    private bool hasEnded;

    public GameState InitialGameState = GameState.Gameplay;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        if (saveLoadSystem != null)
        {
            hasData = saveLoadSystem.LoadSaveDataFromDisk();
        }

        Application.targetFrameRate = 60;
    }

    private IEnumerator Start()
    {
        remainingSeconds = totalGameSeconds;
        hasEnded = false;
        UpdateGameState(InitialGameState);

        for (int i = 0; i < 60; i++)
        {
            if (TrySetupPlayerAnchor())
            {
                break;
            }
            yield return null;
        }

        while (true)
        {
            yield return new WaitForSeconds(30f);
            saveLoadSystem.SaveDataToDisk();
        }
    }

    private void Update()
    {
        if (hasEnded || currentGameState != GameState.Gameplay)
        {
            return;
        }

        remainingSeconds = Mathf.Max(0f, remainingSeconds - Time.deltaTime);
        if (remainingSeconds <= 0f)
        {
            EndGame(seekerWin: false);
            return;
        }

        if (playerStateManager != null && playerStateManager.AreAllNonSeekersDead())
        {
            EndGame(seekerWin: true);
        }
    }

    public void UpdateGameState(GameState newGameState)
    {
        if (newGameState == CurrentGameState)
        {
            return;
        }

        currentGameState = newGameState;

        switch (currentGameState)
        {
            case GameState.Gameplay:
                inputReader.EnableGameplayInput();
                Time.timeScale = 1;
                break;
            case GameState.Menu:
                inputReader.EnableUIInput();
                Time.timeScale = 1;
                break;
            case GameState.Pause:
                inputReader.EnableUIInput();
                Time.timeScale = 0;
                break;
            case GameState.CutScene:
                inputReader.DisableAllInput();
                break;
            case GameState.Ending:
                inputReader.DisableAllInput();
                Time.timeScale = 1;
                break;
        }

        onGameStateChanged?.RaiseEvent(currentGameState);
    }

    private void OnApplicationPause(bool state)
    {
        if (state)
        {
            saveLoadSystem.SaveDataToDisk();
        }
    }

    private bool TrySetupPlayerAnchor()
    {
        if (playerTransformAnchor == null)
        {
            return false;
        }

        var players = GameObject.FindGameObjectsWithTag("Player");
        if (players == null || players.Length == 0)
        {
            return false;
        }

        player = FindLocalPlayer(players);
        if (player == null)
        {
            player = players[0];
        }

        if (player == null)
        {
            return false;
        }

        if (ShouldUseDefaultSpawn() && hasData == false)
        {
            player.transform.position = defaultSpawnPoint.position;
        }

        playerTransformAnchor.Provide(player.transform);
        return true;
    }

    private void EndGame(bool seekerWin)
    {
        if (hasEnded)
        {
            return;
        }

        hasEnded = true;
        UpdateGameState(GameState.Ending);

        bool localWin = seekerWin;
        if (playerStateManager != null && playerStateManager.TryGetLocalPlayer(out var localState))
        {
            localWin = localState.IsSeeker == seekerWin;
        }

        float avgReaction = statsManager != null ? statsManager.GetAverageReactionMs() : 0f;
        List<MaskColor> history = statsManager != null ? statsManager.GetMaskHistory() : new List<MaskColor>();
        var result = new GameResultData(seekerWin, localWin, remainingSeconds, avgReaction, history);

        onGameResult?.RaiseEvent(result);
        onGameEnded?.RaiseEvent();
    }

    private static GameObject FindLocalPlayer(GameObject[] players)
    {
        foreach (var candidate in players)
        {
            if (candidate == null)
            {
                continue;
            }

            var networkObject = candidate.GetComponent<NetworkObject>();
            if (networkObject != null && networkObject.HasInputAuthority)
            {
                return candidate;
            }
        }

        return null;
    }

    private static bool ShouldUseDefaultSpawn()
    {
        var runner = FindFirstObjectByType<NetworkRunner>();
        return runner == null || runner.IsRunning == false;
    }
}

public enum GameState
{
    None,
    Gameplay,
    Pause,
    Menu,
    CutScene,
    Ending
}
