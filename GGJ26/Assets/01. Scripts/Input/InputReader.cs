using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName = "InputReader", menuName = "Input/Input Reader")]
public class InputReader : DescriptionSO, GameInput.IGameplayActions, GameInput.IUIActions
{
    // Gameplay
    public event UnityAction PauseEvent = delegate { };
    
    // UI
    public event UnityAction CancelEvent = delegate { };
    
    private GameInput gameInput;
    
    private void OnEnable()
    {
        if (gameInput == null)
        {
            gameInput = new GameInput();
            gameInput.Gameplay.SetCallbacks(this);
            gameInput.UI.SetCallbacks(this);
            DisableAllInput();
        }
    }

    private void OnDisable()
    {
        DisableAllInput();
    }

    public void EnableGameplayInput()
    {
        gameInput.Gameplay.Enable();
        gameInput.UI.Disable();
    }
    
    public void EnableUIInput()
    {
        DisableGameInput();
        gameInput.UI.Enable();
    }

    private void DisableGameInput()
    {
        gameInput.Gameplay.Disable();
    }

    public void DisableAllInput()
    {
        DisableGameInput();
        gameInput.UI.Disable();
    }
    
    /* Gameplay Inputs */
    
    public void OnMove(InputAction.CallbackContext context)
    {
        
    }
    public void OnLook(InputAction.CallbackContext context)
    {
        
    }
    public void OnAttack(InputAction.CallbackContext context)
    {
        
    }
    public void OnInteract(InputAction.CallbackContext context)
    {
        
    }
    public void OnCrouch(InputAction.CallbackContext context)
    {
        
    }
    public void OnJump(InputAction.CallbackContext context)
    {
        
    }
    public void OnPrevious(InputAction.CallbackContext context)
    {
        
    }
    public void OnNext(InputAction.CallbackContext context)
    {
        
    }
    public void OnSprint(InputAction.CallbackContext context)
    {
        
    }
    public void OnPause(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            PauseEvent.Invoke();
        }
    }

    /* UI Inputs */
    
    public void OnNavigate(InputAction.CallbackContext context)
    {
        
    }
    public void OnSubmit(InputAction.CallbackContext context)
    {
        
    }
    public void OnCancel(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            CancelEvent.Invoke();
        }
    }
    public void OnPoint(InputAction.CallbackContext context)
    {
        
    }
    public void OnClick(InputAction.CallbackContext context)
    {
        
    }
    public void OnRightClick(InputAction.CallbackContext context)
    {
        
    }
    public void OnMiddleClick(InputAction.CallbackContext context)
    {
        
    }
    public void OnScrollWheel(InputAction.CallbackContext context)
    {
        
    }
    public void OnTrackedDevicePosition(InputAction.CallbackContext context)
    {
        
    }
    public void OnTrackedDeviceOrientation(InputAction.CallbackContext context)
    {
        
    }
}
