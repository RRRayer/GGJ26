using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "NewAudioCueEventChannel", menuName = "Events/Audio/AudioCueEventChannel")]
public class AudioCueEventChannelSO : DescriptionSO
{
    public AudioCuePlayAction OnAudioCuePlayRequested;
    public AudioCueStopAction OnAudioCueStopRequested;

    public AudioCueKey RaisePlayEvent(AudioCueSO audioCue, AudioConfigurationSO audioConfiguration, Vector3 position)
    {
        AudioCueKey audioCueKey = AudioCueKey.Invalid;
        // Audio Source 플레이
        if (OnAudioCuePlayRequested != null)
        {
            audioCueKey = OnAudioCuePlayRequested.Invoke(audioCue, audioConfiguration, position);    
        }
        else
        {
            Log.W("AudioCue Play 요청 액션 할당이 없습니다.");
        }
        return audioCueKey;
    }
    
    public bool RaiseStopEvent(AudioCueKey audioCueKey)
    {
        bool requestSucceed = false;
        if (OnAudioCueStopRequested != null)
        {
            // Key에 맞는거 종료
            requestSucceed = OnAudioCueStopRequested.Invoke(audioCueKey);    
        }
        else
        {
            Log.W("AudioCue Stop 요청 액션 할당이 없습니다.");
        }
        return requestSucceed;
    }
}

public delegate AudioCueKey AudioCuePlayAction(AudioCueSO audioCue, AudioConfigurationSO audioConfiguration, Vector3 position);
public delegate bool AudioCueStopAction(AudioCueKey key);