using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 생성된 SoundEmitter들 저장소
/// </summary>
public class SoundEmitterVault
{
    private int uniqueKey = 0;
    private List<AudioCueKey> emitterKeys;
    private List<SoundEmitter> emitters;

    public SoundEmitterVault()
    {
        emitterKeys = new List<AudioCueKey>();
        emitters = new List<SoundEmitter>();
    }

    /// <summary>
    /// 새로운 AudioCue에 대한 AudioCueKey를 반환
    /// </summary>
    /// <param name="cue"></param>
    /// <returns></returns>
    public AudioCueKey GetKey(AudioCueSO cue)
    {
        return new AudioCueKey(uniqueKey++, cue);
    }

    /// <summary>
    /// 새로운 AudioCueSO를 추가
    /// </summary>
    public AudioCueKey Add(AudioCueSO cue, SoundEmitter emitter)
    {
        AudioCueKey key = GetKey(cue);
        emitterKeys.Add(key);
        emitters.Add(emitter);
        return key;
    }

    /// <summary>
    /// Key에 대한 SoundEmitter를 찾아서 할당함
    /// 탐색 여부를 Boolean으로 반환
    /// </summary>
    public bool Get(AudioCueKey key, out SoundEmitter emitter)
    {
        int index = emitterKeys.FindIndex(x => x == key);
        if (index < 0)
        {
            emitter = null;
            return false;
        }
        emitter = emitters[index];
        return true;
    }
}
