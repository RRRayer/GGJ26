using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 사운드 소스 필요
/// 뮤직, 효과음 다르게 처리
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class SoundEmitter : MonoBehaviour
{
    // 자기 자신을 인자로 넘겨 종료한다.
    public UnityAction<SoundEmitter> OnSoundFinishedPlaying;
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    /// <summary>
    /// 실제 Clip 재생
    /// </summary>
    public void PlayAudioClip(AudioClip clip, AudioConfigurationSO settings, bool isLoop, Vector3 position = default)
    {
        audioSource.clip = clip;
        settings.ApplyTo(audioSource);
        audioSource.loop = isLoop;
        audioSource.transform.position = position;
        audioSource.time = 0f;
        audioSource.Play();

        if (!isLoop)
        {
            StartCoroutine(FinishingPlaying(clip.length));
        }
    }

    /// <summary>
    /// SoundEmitter가 플레이 중인지 여부를 반환
    /// </summary>
    public bool IsPlaying()
    {
        return audioSource.isPlaying;
    }

    /// <summary>
    /// SoundEmitter가 Loop 인지 여부를 반환
    /// </summary>
    public bool IsLooping()
    {
        return audioSource.loop;
    }

    /// <summary>
    /// AudioSource 종료
    /// </summary>
    public void Stop()
    {
        audioSource.Stop();
    }

    /// <summary>
    /// 오디오 클립이 끝나면 종료 이벤트 실행
    /// 종료 이벤트는 AudioManager에서 정의함
    /// </summary>
    private IEnumerator FinishingPlaying(float length)
    {
        yield return new WaitForSeconds(length);
        OnSoundFinishedPlaying.Invoke(this);
    }
}
