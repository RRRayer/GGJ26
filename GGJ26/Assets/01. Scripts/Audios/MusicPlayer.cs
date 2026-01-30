using UnityEngine;

public class MusicPlayer : MonoBehaviour
{
    [Header("Audio Setting")]
    [SerializeField] private AudioCueEventChannelSO musicEventChannel;
    [SerializeField] private AudioConfigurationSO musicConfiguration;
    [SerializeField] private AudioCueSO musicCue;

    private void Start()
    {
        musicEventChannel.RaisePlayEvent(musicCue, musicConfiguration, transform.position);
    }
}
