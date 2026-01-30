using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Serialization;

public class AudioManager : MonoBehaviour
{
    [Header("SoundEmitters Pool")]
    [SerializeField] private SoundEmitterPoolSO pool;
    [SerializeField] private int initialSize = 10;
    
    [Header("Audio Control")]
    [SerializeField] private AudioMixer audioMixer;
    [Range(0f, 1f)][SerializeField] private float masterVolume = 1f;
    [Range(0f, 1f)][SerializeField] private float musicVolume = 0.8f;
    [Range(0f, 1f)][SerializeField] private float sfxVolume = 1f;
    
    [Header("Listening to")]
    // Audio Cue 실행 이벤트
    [SerializeField] private AudioCueEventChannelSO musicEventChannel;
    [SerializeField] private AudioCueEventChannelSO sfxEventChannel;
    // Volume 조절 이벤트
    [SerializeField] private FloatEventChannelSO changeMasterVolumeEvent;
    [SerializeField] private FloatEventChannelSO changeMusicVolumeEvent;
    [SerializeField] private FloatEventChannelSO changeSfxVolumeEvent;
    
    private SoundEmitterVault soundEmitterVault; // 활성화된 Sound Emitters 관리
    private SoundEmitter musicSoundEmitter;      // BGM Sound Emitter
    
    private void Awake()
    {
        soundEmitterVault = new SoundEmitterVault();
        
        pool.Prewarm(initialSize);
        pool.SetParent(this.transform);
    }

    private void OnEnable()
    {
        musicEventChannel.OnAudioCuePlayRequested += PlayMusicTrack;
        musicEventChannel.OnAudioCueStopRequested += StopMusic;
        sfxEventChannel.OnAudioCuePlayRequested   += PlayAudioCue;
        sfxEventChannel.OnAudioCueStopRequested   += StopAudioCue;

        changeMasterVolumeEvent.OnEventRaised += ChangeMasterVolume;
        changeMusicVolumeEvent.OnEventRaised  += ChangeMusicVolume;
        changeSfxVolumeEvent.OnEventRaised    += ChangeSfxVolume;
    }

    private void OnDisable()
    {
        musicEventChannel.OnAudioCuePlayRequested -= PlayMusicTrack;
        musicEventChannel.OnAudioCueStopRequested -= StopMusic;
        sfxEventChannel.OnAudioCuePlayRequested   -= PlayAudioCue;
        sfxEventChannel.OnAudioCueStopRequested   -= StopAudioCue;
        
        changeMasterVolumeEvent.OnEventRaised -= ChangeMasterVolume;
        changeMusicVolumeEvent.OnEventRaised  -= ChangeMusicVolume;
        changeSfxVolumeEvent.OnEventRaised    -= ChangeSfxVolume;
    }
    
    #region VOLUME

    private void ChangeMasterVolume(float value)
    {
        masterVolume = Mathf.Clamp01(value);
        SetGroupVolume("MasterVolume", NormalizedToMixerValue(masterVolume));
    }
    
    private void ChangeMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);
        SetGroupVolume("MusicVolume", NormalizedToMixerValue(musicVolume));
    }
    
    private void ChangeSfxVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
        SetGroupVolume("SFXVolume", NormalizedToMixerValue(sfxVolume));
    }

    private void SetGroupVolume(string parameterName, float normalizedVolume)
    {
        bool volumeSet = audioMixer.SetFloat(parameterName, normalizedVolume);
        if (!volumeSet)
            Log.E("오디오 믹서의 파라미터를 찾을 수 없습니다.");
    }

    private float NormalizedToMixerValue(float normalizedValue)
    {
        // -80dB~0dB로 볼륨 조절
        float ret = (normalizedValue - 1f) * 80f;
        return ret;
    }
    
    #endregion

    /// <summary>
    /// 오디오 재생 이벤트 정의
    /// </summary>
    /// <param name="audioCue">AudioClip을 담는 SO</param>
    /// <param name="position">AudioSource가 재생될 월드 좌표</param>
    private AudioCueKey PlayAudioCue(AudioCueSO audioCue, AudioConfigurationSO settings, Vector3 position)
    {
        AudioClip clip = audioCue.GetClip();
        SoundEmitter soundEmitter = pool.Request();
        soundEmitter.PlayAudioClip(clip, settings, audioCue.Looping, position);
        soundEmitter.OnSoundFinishedPlaying += OnSoundEmitterFinishedPlaying;
        
        return soundEmitterVault.Add(audioCue, soundEmitter);
    }

    /// <summary>
    /// 오디오 종료 이벤트 정의
    /// </summary>
    /// <param name="audioCueKey">AudioCue Key에 해당하는 SoundEmitter 종료</param>
    private bool StopAudioCue(AudioCueKey audioCueKey)
    {
        bool isFound = soundEmitterVault.Get(audioCueKey, out SoundEmitter soundEmitter);
        // 만약 현재 Key에 대한 SoundEmitter가 존재한다면, 종료
        if (isFound)
        {
            StopAndCleanEmitter(soundEmitter);
        }
        else
        {
            //Log.W("현재 Key에 대한 SoundEmitter가 존재하지 않습니다.");
        }
        return isFound;
    }

    /// <summary>
    /// SoundEmitter가 오디오 클립을 끝냈을 때의 콜백 함수
    /// </summary>
    /// <param name="soundEmitter">콜백 함수를 실행한 자기 자신</param>
    private void OnSoundEmitterFinishedPlaying(SoundEmitter soundEmitter)
    {
        soundEmitter.OnSoundFinishedPlaying -= OnSoundEmitterFinishedPlaying;

        StopAndCleanEmitter(soundEmitter);
    }

    /// <summary>
    /// 종료된 SoundEmitter를 제거한다.
    /// </summary>
    private void StopAndCleanEmitter(SoundEmitter soundEmitter)
    {
        pool.Return(soundEmitter);
    }

    /// <summary>
    /// BGM 재생
    /// </summary>
    private AudioCueKey PlayMusicTrack(AudioCueSO audioCue, AudioConfigurationSO audioConfiguration, Vector3 position)
    {
        // 이미 플레이 중이라면 무시
        if (musicSoundEmitter != null && musicSoundEmitter.IsPlaying())
            return AudioCueKey.Invalid;
        
        AudioClip clip = audioCue.GetClip();
        musicSoundEmitter = pool.Request();
        musicSoundEmitter.PlayAudioClip(clip, audioConfiguration, audioCue.Looping, position);
        musicSoundEmitter.OnSoundFinishedPlaying += StopMusicEmitter;
        
        soundEmitterVault.Add(audioCue, musicSoundEmitter);
        return AudioCueKey.Invalid;
    }

    /// <summary>
    /// BGM 종료
    /// </summary>
    private bool StopMusic(AudioCueKey key)
    {
        if (musicSoundEmitter != null && musicSoundEmitter.IsPlaying())
        {
            musicSoundEmitter.Stop();
            StopMusicEmitter(musicSoundEmitter);
            return true;    
        }
        return false;
    }
    
    private void StopMusicEmitter(SoundEmitter soundEmitter)
    {
        soundEmitter.OnSoundFinishedPlaying -= StopMusicEmitter;
        pool.Return(soundEmitter);
    }
}
