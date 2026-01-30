using UnityEngine;

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
    private bool isGroupDanceActive;
    private Coroutine fadeRoutine;

    private void OnEnable()
    {
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
        PlayNormalBgm();
    }

    private void OnDisable()
    {
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
    }

    private void OnStartDisco()
    {
        isGroupDanceActive = true;
        PlayGroupDanceBgm();
    }

    private void OnStopDisco()
    {
        isGroupDanceActive = false;
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

        if (groupBgmEmitter != null)
        {
            FadeOutAndPause(groupBgmEmitter);
        }

        if (normalBgmEmitter == null)
        {
            normalBgmEmitter = audioManager.PlayLoopingMusic(normalBgmCue, musicConfiguration, transform.position);
        }
        else
        {
            audioManager.ResumeEmitter(normalBgmEmitter);
        }

        FadeIn(normalBgmEmitter);
    }

    private void PlayGroupDanceBgm()
    {
        if (audioManager == null || groupDanceBgmCue == null)
        {
            return;
        }

        if (normalBgmEmitter != null)
        {
            FadeOutAndPause(normalBgmEmitter);
        }

        if (groupBgmEmitter == null)
        {
            groupBgmEmitter = audioManager.PlayLoopingMusic(groupDanceBgmCue, musicConfiguration, transform.position);
        }
        else
        {
            audioManager.ResumeEmitter(groupBgmEmitter);
        }

        FadeIn(groupBgmEmitter);
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
