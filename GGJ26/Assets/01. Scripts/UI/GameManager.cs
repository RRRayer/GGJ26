using System.Collections;
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
    
    [Header("Broadcasting on")]
    [SerializeField] private GameStateEventChannelSO onGameStateChanged;

    private GameObject player;
    private GameState currentGameState = GameState.None;

    // Debugging을 위해 public 설정. 
    public GameState InitialGameState = GameState.Gameplay;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        
        // 세이브 데이터 로드
        if (saveLoadSystem != null)
        {
            hasData = saveLoadSystem.LoadSaveDataFromDisk();
        }
        
        // 플레이어 Transform Anchor 설정
        player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            if (hasData)
            {
                
            }
            else
            {
                player.transform.position = defaultSpawnPoint.position;
            }
            playerTransformAnchor.Provide(player.transform);
        }
        else
        {
            Log.W("플레이어 태그가 존재하지 않습니다.");
        }
        
        Application.targetFrameRate = 60;
    }

    private IEnumerator Start()
    {
        UpdateGameState(InitialGameState);

        while (true)
        {
            yield return new WaitForSeconds(30f);
            saveLoadSystem.SaveDataToDisk();
        }
    }

    /// <summary>
    /// 타 스크립트에서 게임 상태 변경 시 사용 
    /// </summary>
    public void UpdateGameState(GameState newGameState)
    {
        if (newGameState == CurrentGameState)
            return;

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
    
    // private void OnApplicationFocus(bool hasFocus)
    // {
    //     if (!hasFocus)
    //     {
    //         onGameClosing?.RaiseEvent();
    //         saveLoadSystem.SaveDataToDisk(); 
    //     }
    // }
}

public enum GameState
{
    None,     // 초기화 값
    Gameplay, // 실제 땅 파는 게임 로직 
    Pause,    // ESC 클릭 시 켜지는 퍼즈창(설정, 조작키, 볼륨 등)
    Menu,     // Menu
    CutScene,
    Ending    // 엔딩 시퀀스
}