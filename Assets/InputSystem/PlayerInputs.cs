using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerInputs : MonoBehaviour
{
	[Header("Character Input Values")]
	public Vector2 move;
	public Vector2 look;
	public bool jump;
	public bool sprint;
	public bool crouch;
	public bool aim;
	public bool fire;
	public bool reload;
	public bool interact;
	public bool toggleShader;

	[Header("Crouch Settings")]
	[Tooltip("If true, crouch is toggled on/off. If false, crouch is hold-to-crouch.")]
	public bool crouchToggleMode = false;

	[Header("Movement Settings")]
	public bool analogMovement;

	[Header("Mouse Cursor Settings")]
	public bool cursorLocked = true;
	public bool cursorInputForLook = true;

	private bool crouchHeld;

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

	public void OnCrouch(InputValue value)
	{
		CrouchInput(value.isPressed);
	}

	public void OnAim(InputValue value)
	{
		AimInput(value.isPressed);
	}

	public void OnFire(InputValue value)
	{
		FireInput(value.isPressed);
	}

	public void OnReload(InputValue value)
	{
		ReloadInput(value.isPressed);
	}

	public void OnInteract(InputValue value)
	{
		InteractInput(value.isPressed);
	}

	public void OnToggleShader(InputValue value)
	{
		ToggleShaderInput(value.isPressed);
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

	public void CrouchInput(bool pressed)
	{
		if (crouchToggleMode)
		{
			// toggle crouch on button down only
			if (pressed && !crouchHeld)
			{
				crouch = !crouch;
			}
			// remember button state
			crouchHeld = pressed;
		}
		else
		{
			// hold-to-crouch mode
			crouch = pressed;
		}
	}

	public void AimInput(bool newAimState)
	{
		aim = newAimState;
	}

	public void FireInput(bool newFireState)
	{
		fire = newFireState;
	}

	public void ReloadInput(bool newReloadState)
	{
		reload = newReloadState;
	}

	public void InteractInput(bool newInteractState)
	{
		interact = newInteractState;
	}

	public void ToggleShaderInput(bool newToggleShaderState)
	{
		toggleShader = newToggleShaderState;
	}
	
	private void OnApplicationFocus(bool hasFocus)
	{
		SetCursorState(cursorLocked);
	}

	private void SetCursorState(bool newState)
	{
		Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
	}
}