using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyAudioController : MonoBehaviour
{
    [Header("Audio Config")]
    [SerializeField] private AudioConfigurationSO musicConfiguration;

    [Header("Lobby BGM Cue")]
    [SerializeField] private AudioCueSO lobbyBgmCue;

    private AudioManager audioManager;
    private static SoundEmitter sharedLobbyEmitter;
    private static AudioManager sharedAudioManager;

    private void OnEnable()
    {
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    private void Start()
    {
        audioManager = AudioManager.Instance;
        sharedAudioManager = audioManager;
        if (audioManager == null || lobbyBgmCue == null || musicConfiguration == null)
        {
            return;
        }

        if (sharedLobbyEmitter == null || sharedLobbyEmitter.IsPlaying() == false)
        {
            sharedLobbyEmitter = audioManager.PlayLoopingMusic(lobbyBgmCue, musicConfiguration, transform.position);
        }
    }

    private void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    private void OnActiveSceneChanged(Scene current, Scene next)
    {
        if (audioManager == null)
        {
            audioManager = AudioManager.Instance;
        }

        if (audioManager == null || sharedLobbyEmitter == null)
        {
            return;
        }

        string nextPath = next.path;
        if (nextPath.EndsWith("GameScene.unity", System.StringComparison.OrdinalIgnoreCase) ||
            nextPath.EndsWith("Game.unity", System.StringComparison.OrdinalIgnoreCase))
        {
            StopSharedLobbyBgm();
        }
    }

    public static void StopSharedLobbyBgm()
    {
        if (sharedLobbyEmitter == null)
        {
            return;
        }

        if (sharedAudioManager == null)
        {
            sharedAudioManager = AudioManager.Instance;
        }

        if (sharedAudioManager != null)
        {
            sharedAudioManager.StopEmitter(sharedLobbyEmitter);
        }

        sharedLobbyEmitter = null;
    }
}
