using UnityEngine;
using DG.Tweening;
using System.Threading.Tasks;
using UnityEngine.VFX;

public class MaskEffect : MonoBehaviour
{
    [Header("구성 요소")]
    public GameObject maskObject; // 애니메이션 될 가면 모델
    public VisualEffect flashVFX; // 재생할 파티클 이펙트
    public Light flashLight; // 껐다 켤 라이트

    [Header("애니메이션 설정")]
    public int rotationCount = 2;
    public float yOffset = -0.5f;
    public float animationDuration = 0.8f;
    
    [Header("라이트 설정")] // New header
    public float lightFlashDuration = 0.3f;
    public float flashEndRangeMultiplier = 2.0f; // Range after the flash

    public float selfDestructDelay = 2f; // 파괴 전 대기 시간

    // 이 메서드가 연출의 모든 것을 책임지고, 끝날 때 Task를 완료합니다.
    public async Task PlayEffectSequence()
    {
        if (maskObject == null)
        {
            Debug.LogWarning("MaskObject가 할당되지 않았습니다.", this);
            await Task.CompletedTask;
            return;
        }

        // 1. 가면 회전 및 하강 애니메이션
        Sequence animSequence = DOTween.Sequence();
        animSequence.Join(maskObject.transform.DORotate(new Vector3(0, 360 * rotationCount, 0), animationDuration, RotateMode.LocalAxisAdd));
        maskObject.transform.position = new Vector3(maskObject.transform.position.x, maskObject.transform.position.y - yOffset, maskObject.transform.position.z); 
        animSequence.Join(maskObject.transform.DOLocalMoveY(yOffset, animationDuration).SetRelative(true));

        // DOTween 애니메이션이 끝날 때까지 기다립니다.
        await animSequence.AsyncWaitForCompletion();

        // 2. 파티클 및 라이트 점멸 효과
        if (flashVFX != null)
            flashVFX.Play();

        if (flashLight != null)
        {
            float originalIntensity = flashLight.intensity;
            float originalRange = flashLight.range; // 원본 range 값 저장

            flashLight.enabled = true;
            Sequence lightSequence = DOTween.Sequence();

            // Phase 1: One Flash (Intensity and Range to 0, then back to original)
            // Fade out
            lightSequence.Append(flashLight.DOIntensity(0, lightFlashDuration / 2))
                         .Join(DOTween.To(() => flashLight.range, x => flashLight.range = x, 0, lightFlashDuration / 2));
            // Fade in
            lightSequence.Append(flashLight.DOIntensity(originalIntensity, lightFlashDuration / 2))
                         .Join(DOTween.To(() => flashLight.range, x => flashLight.range = x, originalRange, lightFlashDuration / 2));

            // Phase 2: Range Increase
            // Animate range to a new, larger value (based on multiplier)
            lightSequence.Append(DOTween.To(() => flashLight.range, x => flashLight.range = x, originalRange * flashEndRangeMultiplier, lightFlashDuration / 2));
            
            // 라이트 애니메이션이 끝날 때까지 기다립니다.
            await lightSequence.AsyncWaitForCompletion();
            flashLight.enabled = false;
        }

        // 3. 스스로 파괴
        Destroy(gameObject, selfDestructDelay);

        // (선택) 파티클이 사라질 약간의 추가 시간을 기다립니다.
        await Task.Delay(500);
    }
}

// DOTween Pro가 없는 경우를 대비한 확장 메서드
public static class DotweenExtensions
{
    public static Task AsyncWaitForCompletion(this Tween tween)
    {
        var tcs = new TaskCompletionSource<bool>();
        tween.OnComplete(() => tcs.SetResult(true));
        return tcs.Task;
    }
}
