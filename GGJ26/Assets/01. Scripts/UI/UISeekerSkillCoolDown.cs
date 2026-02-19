using UnityEngine;
using UnityEngine.UI;
using Fusion;

public class UISeekerSkillCoolDown : MonoBehaviour
{
    [SerializeField]
    private Image _skillImage;

    [SerializeField]
    private SeekerNpcDanceCommandSettingsSO _seekerSkillSettings;

    private FusionThirdPersonMotor _localPlayerMotor;
    private float _cooldownDuration;

    private void Start()
    {
        if (_skillImage == null)
        {
            _skillImage = GetComponent<Image>();
        }

        if (_skillImage != null)
        {
            // The user wants the image to be filled, so we ensure it is.
            _skillImage.type = Image.Type.Filled;
            // And it should be ready at the start.
            _skillImage.fillAmount = 1f;
        }
        else
        {
            Debug.LogError("UISeekerSkillCoolDown: Image component is not assigned and could not be found.", this);
            this.enabled = false;
            return;
        }


        if (_seekerSkillSettings != null)
        {
            _cooldownDuration = _seekerSkillSettings.cooldown;
        }
        else
        {
            Debug.LogError("SeekerNpcDanceCommandSettingsSO is not assigned in the inspector!", this);
            // Disable the component if settings are missing.
            this.enabled = false;
            return;
        }
    }

    private void Update()
    {
        // If we haven't found the local player's motor yet, try to find it.
        if (_localPlayerMotor == null)
        {
            // This isn't the most performant method, but it keeps the script self-contained
            // as requested, and will stop searching once the player is found.
            var motors = FindObjectsOfType<FusionThirdPersonMotor>();
            foreach (var motor in motors)
            {
                // We only care about the motor that belongs to the local player (has state authority).
                if (motor.Object != null && motor.Object.HasStateAuthority)
                {
                    _localPlayerMotor = motor;
                    break;
                }
            }
        }
        
        // If we still don't have a motor, or the simulation isn't running, do nothing.
        // Assume the skill is ready.
        if (_localPlayerMotor == null || _localPlayerMotor.Runner == null || !_localPlayerMotor.Runner.IsRunning)
        {
            if(_skillImage != null) _skillImage.fillAmount = 1f;
            return;
        }

        float nextUseTime = _localPlayerMotor.NextNpcDanceCommandTime;
        float currentTime = _localPlayerMotor.Runner.SimulationTime;

        if (nextUseTime > currentTime)
        {
            // Skill is on cooldown.
            float remainingTime = nextUseTime - currentTime;
            // The fill amount should go from 0 (just used) to 1 (ready).
            _skillImage.fillAmount = 1f - Mathf.Clamp01(remainingTime / _cooldownDuration);
        }
        else
        {
            // Skill is ready.
            _skillImage.fillAmount = 1f;
        }
    }
}
