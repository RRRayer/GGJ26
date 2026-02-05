using UnityEngine;
using UnityEngine.SceneManagement;

public class GameAudioController : MonoBehaviour
{
    [Header("Audio Channels")]
    [SerializeField] private AudioCueEventChannelSO sfxEventChannel;

    [Header("Audio Config")]
    [SerializeField] private AudioConfigurationSO musicConfiguration;
    [SerializeField] private AudioConfigurationSO sfxConfiguration;

    [Header("BGM Cues")]
    [SerializeField] private AudioCueSO normalBgmCue;
    [SerializeField] private AudioCueSO groupDanceBgmCue;

    [Header("SFX Cues")]
    [SerializeField] private AudioCueSO victorySfxCue;
    [SerializeField] private AudioCueSO defeatSfxCue;

    [Header("Events")]
    [SerializeField] private VoidEventChannelSO startDiscoEvent;
    [SerializeField] private VoidEventChannelSO stopDiscoEvent;
    [SerializeField] private GameResultEventChannelSO gameResultEvent;

    [Header("Fade")]
    [SerializeField] private float musicFadeSeconds = 0.5f;

    private AudioManager audioManager;
    private SoundEmitter normalBgmEmitter;
    private SoundEmitter groupBgmEmitter;
    private Coroutine fadeRoutine;

    private void OnEnable()
    {
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
        if (startDiscoEvent != null)
        {
            startDiscoEvent.OnEventRaised += OnStartDisco;
        }

        if (stopDiscoEvent != null)
        {
            stopDiscoEvent.OnEventRaised += OnStopDisco;
        }

        if (gameResultEvent != null)
        {
            gameResultEvent.OnEventRaised += OnGameResult;
        }
    }

    private void Start()
    {
        audioManager = FindFirstObjectByType<AudioManager>();
        LobbyAudioController.StopSharedLobbyBgm();
        PlayNormalBgm();
    }

    private void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        if (startDiscoEvent != null)
        {
            startDiscoEvent.OnEventRaised -= OnStartDisco;
        }

        if (stopDiscoEvent != null)
        {
            stopDiscoEvent.OnEventRaised -= OnStopDisco;
        }

        if (gameResultEvent != null)
        {
            gameResultEvent.OnEventRaised -= OnGameResult;
        }

        StopAllBgm();
    }

    private void OnStartDisco()
    {
        PlayGroupDanceBgm();
    }

    private void OnStopDisco()
    {
        PlayNormalBgm();
    }

    private void OnGameResult(GameResultData result)
    {
        if (result == null)
        {
            return;
        }

        if (result.LocalPlayerWin)
        {
            PlaySfx(victorySfxCue, transform.position);
        }
        else
        {
            PlaySfx(defeatSfxCue, transform.position);
        }
    }

    private void PlayNormalBgm()
    {
        if (audioManager == null || normalBgmCue == null)
        {
            return;
        }

        SwitchBgm(groupBgmEmitter, normalBgmCue, false);
    }

    private void PlayGroupDanceBgm()
    {
        if (audioManager == null || groupDanceBgmCue == null)
        {
            return;
        }

        SwitchBgm(normalBgmEmitter, groupDanceBgmCue, true);
    }

    private void PlaySfx(AudioCueSO cue, Vector3 position)
    {
        if (sfxEventChannel == null || cue == null)
        {
            return;
        }

        sfxEventChannel.RaisePlayEvent(cue, sfxConfiguration, position);
    }

    private void FadeOutAndPause(SoundEmitter emitter)
    {
        if (emitter == null || audioManager == null)
        {
            return;
        }

        StartFade(emitter, 0f, true);
    }

    private void FadeIn(SoundEmitter emitter)
    {
        if (emitter == null || audioManager == null)
        {
            return;
        }

        StartFade(emitter, 1f, false);
    }

    private void SwitchBgm(SoundEmitter fadeOutEmitter, AudioCueSO cue, bool isGroupTarget)
    {
        if (audioManager == null || cue == null)
        {
            return;
        }

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }

        fadeRoutine = StartCoroutine(SwitchBgmRoutine(fadeOutEmitter, cue, isGroupTarget));
    }

    private void OnActiveSceneChanged(Scene current, Scene next)
    {
        if (IsGameScene(next) == false)
        {
            StopAllBgm();
        }
    }

    private bool IsGameScene(Scene scene)
    {
        string path = scene.path;
        return path.EndsWith("GameScene.unity", System.StringComparison.OrdinalIgnoreCase)
               || path.EndsWith("Game.unity", System.StringComparison.OrdinalIgnoreCase);
    }

    private void StopAllBgm()
    {
        if (audioManager == null)
        {
            audioManager = FindFirstObjectByType<AudioManager>();
        }

        if (audioManager == null)
        {
            return;
        }

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        if (normalBgmEmitter != null)
        {
            audioManager.StopEmitter(normalBgmEmitter);
            normalBgmEmitter = null;
        }

        if (groupBgmEmitter != null)
        {
            audioManager.StopEmitter(groupBgmEmitter);
            groupBgmEmitter = null;
        }
    }

    private System.Collections.IEnumerator SwitchBgmRoutine(SoundEmitter fadeOutEmitter, AudioCueSO cue, bool isGroupTarget)
    {
        if (fadeOutEmitter != null)
        {
            yield return FadeRoutine(fadeOutEmitter, 0f, true);
        }

        SoundEmitter fadeInEmitter = isGroupTarget ? groupBgmEmitter : normalBgmEmitter;
        if (fadeInEmitter == null)
        {
            fadeInEmitter = audioManager.PlayLoopingMusic(cue, musicConfiguration, transform.position);
            if (isGroupTarget)
            {
                groupBgmEmitter = fadeInEmitter;
            }
            else
            {
                normalBgmEmitter = fadeInEmitter;
            }
        }
        else
        {
            audioManager.ResumeEmitter(fadeInEmitter);
        }

        audioManager.SetEmitterVolume(fadeInEmitter, 0f);
        yield return FadeRoutine(fadeInEmitter, 1f, false);
    }

    private void StartFade(SoundEmitter emitter, float targetVolume, bool pauseAfter)
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }

        fadeRoutine = StartCoroutine(FadeRoutine(emitter, targetVolume, pauseAfter));
    }

    private System.Collections.IEnumerator FadeRoutine(SoundEmitter emitter, float targetVolume, bool pauseAfter)
    {
        float startVolume = emitter.GetVolume();
        float duration = Mathf.Max(0.01f, musicFadeSeconds);
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            float volume = Mathf.Lerp(startVolume, targetVolume, t);
            audioManager.SetEmitterVolume(emitter, volume);
            yield return null;
        }

        audioManager.SetEmitterVolume(emitter, targetVolume);

        if (pauseAfter)
        {
            audioManager.PauseEmitter(emitter);
        }
    }
}
