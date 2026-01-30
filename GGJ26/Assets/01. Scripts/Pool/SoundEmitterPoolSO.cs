using UnityEngine;

[CreateAssetMenu(fileName = "NewSoundEmitterPool", menuName = "Pool/Sound Emitter")]
public class SoundEmitterPoolSO : ComponentPoolSO<SoundEmitter>
{
    [SerializeField] private SoundEmitterFactorySO factory;

    public override IFactory<SoundEmitter> Factory => factory;
}
