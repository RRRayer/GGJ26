    using UnityEngine;

[CreateAssetMenu(fileName = "NewAudioCueSO", menuName = "Audio/Audio Cue")]
public class AudioCueSO : DescriptionSO
{
    public bool Looping = false;
    [SerializeField] private AudioClip audioClip;

    public AudioClip GetClip()
    {
        return audioClip;
    }
}