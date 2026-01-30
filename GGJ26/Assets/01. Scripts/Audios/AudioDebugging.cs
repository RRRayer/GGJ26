using System;
using UnityEngine;

public class AudioDebugging : MonoBehaviour
{
    [TextArea] public string description;
    
    public AudioCueEventChannelSO sfxEventChannel;
    [SerializeField] private AudioConfigurationSO sfxAudioConfiguration;
    public bool state = false;
    public AudioCueSO audioCue;
    public Transform audioTransform;
    
    private void Update()
    {
        if (state)
        {
            Log.D("AudioDebugging::Update()");
            state = false;
            sfxEventChannel.RaisePlayEvent(audioCue, sfxAudioConfiguration, audioTransform.position);
        }
    }
}
