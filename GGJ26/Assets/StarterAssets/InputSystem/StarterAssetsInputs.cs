using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
	public class StarterAssetsInputs : MonoBehaviour
	{
		[Header("Character Input Values")]
		public Vector2 move;
		public Vector2 look;
		public bool jump;
		public bool sprint;
		public bool dance1;
		public bool dance2;
		public bool dance3;
		public bool dance4;

		[Header("Movement Settings")]
		public bool analogMovement;

		[Header("Mouse Cursor Settings")]
		public bool cursorLocked = true;
		public bool cursorInputForLook = true;

		private void OnEnable()
		{
			ResetDanceInputs();
		}

		private void OnDisable()
		{
			ResetDanceInputs();
		}

#if ENABLE_INPUT_SYSTEM
		public void OnMove(InputValue value)
		{
			MoveInput(value.Get<Vector2>());
		}

		public void OnLook(InputValue value)
		{
			if(cursorInputForLook)
			{
				LookInput(value.Get<Vector2>());
			}
		}

		public void OnJump(InputValue value)
		{
			JumpInput(value.isPressed);
		}

		public void OnSprint(InputValue value)
		{
			SprintInput(value.isPressed);
		}

		public void OnDance1(InputValue value)
		{
			dance1 = value.isPressed;
		}
		
		public void OnDance2(InputValue value)
		{
			dance2 = value.isPressed;
		}

		public void OnDance3(InputValue value)
		{
			dance3 = value.isPressed;
		}

		public void OnDance4(InputValue value)
		{
			dance4 = value.isPressed;
		}

#endif


		public void MoveInput(Vector2 newMoveDirection)
		{
			move = newMoveDirection;
		} 

		public void LookInput(Vector2 newLookDirection)
		{
			look = newLookDirection;
		}

		public void JumpInput(bool newJumpState)
		{
			jump = newJumpState;
		}

		public void SprintInput(bool newSprintState)
		{
			sprint = newSprintState;
		}

		private void ResetDanceInputs()
		{
			dance1 = false;
			dance2 = false;
			dance3 = false;
			dance4 = false;
		}

	private void OnApplicationFocus(bool hasFocus)
	{
		if (hasFocus == false)
		{
			return;
		}

		SetCursorState(cursorLocked);
	}

	private void SetCursorState(bool newState)
	{
		Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
	}

	public void ForceCursorUnlocked()
	{
		cursorLocked = false;
		cursorInputForLook = false;
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
	}

	public void ForceCursorLocked()
	{
		cursorLocked = true;
		cursorInputForLook = true;
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	}
	}
	
}
