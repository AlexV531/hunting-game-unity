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
	public bool steadyAim;
	public bool fire;
	public bool reload;
	public bool equip1;
	public bool equip2;
	public bool equip3;
	public bool equip4;
	public bool equip5;
	public bool equip6;
	public bool equip7;
	public bool equip8;
	public bool interact;
	public bool inspect;
	public bool pause;
	public bool loadout;
	public bool inventory;
	public bool map;
	public bool dropItem;
	public bool toggleShader;
	public bool debug1;
	public bool debug2;
	public bool leftMouseHeld;
	public Vector2 dragInUI;

	[Header("Crouch Settings")]
	[Tooltip("If true, crouch is toggled on/off. If false, crouch is hold-to-crouch.")]
	public bool crouchToggleMode = true;

	[Header("Steady Aim Settings")]
	[Tooltip("If true, steady aim is toggled on/off. If false, steady aim is hold-to-steady.")]
	public bool steadyAimToggleMode = false;

	[Header("Movement Settings")]
	public bool analogMovement;

	[Header("Mouse Cursor Settings")]
	public bool cursorLocked = true;
	public bool cursorInputForLook = true;

	private bool crouchHeld;
	private bool steadyAimHeld;

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

	public void OnSteadyAim(InputValue value)
	{
		SteadyAimInput(value.isPressed);
	}

	public void OnFire(InputValue value)
	{
		FireInput(value.isPressed);
	}

	public void OnReload(InputValue value)
	{
		ReloadInput(value.isPressed);
	}

	public void OnEquip1(InputValue value)
	{
		Equip1Input(value.isPressed);
	}

	public void OnEquip2(InputValue value)
	{
		Equip2Input(value.isPressed);
	}

	public void OnEquip3(InputValue value)
	{
		Equip3Input(value.isPressed);
	}

	public void OnEquip4(InputValue value)
	{
		Equip4Input(value.isPressed);
	}

	public void OnEquip5(InputValue value)
	{
		Equip5Input(value.isPressed);
	}

	public void OnEquip6(InputValue value)
	{
		Equip6Input(value.isPressed);
	}

	public void OnEquip7(InputValue value)
	{
		Equip7Input(value.isPressed);
	}

	public void OnEquip8(InputValue value)
	{
		Equip8Input(value.isPressed);
	}

	public void OnInteract(InputValue value)
	{
		InteractInput(value.isPressed);
	}

	public void OnInspect(InputValue value)
	{
		InspectInput(value.isPressed);
	}

	public void OnPause(InputValue value)
	{
		PauseInput(value.isPressed);
	}

	public void OnLoadout(InputValue value)
	{
		LoadoutInput(value.isPressed);
	}

	public void OnInventory(InputValue value)
	{
		InventoryInput(value.isPressed);
	}

	public void OnMap(InputValue value)
	{
		MapInput(value.isPressed);
	}

	public void OnDropItem(InputValue value)
	{
		DropItemInput(value.isPressed);
	}

	public void OnToggleShader(InputValue value)
	{
		ToggleShaderInput(value.isPressed);
	}

	public void OnDebug1(InputValue value)
	{
		Debug1Input(value.isPressed);
	}

	public void OnDebug2(InputValue value)
	{
		Debug2Input(value.isPressed);
	}

	public void OnLeftMouseHeld(InputValue value)
	{
		LeftMouseHeldInput(value.isPressed);
	}

	public void OnDragInUI(InputValue value)
	{
		DragInUIInput(value.Get<Vector2>());
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

	public void SteadyAimInput(bool pressed)
	{
		if (steadyAimToggleMode)
		{
			// toggle crouch on button down only
			if (pressed && !steadyAimHeld)
			{
				steadyAim = !steadyAim;
			}
			// remember button state
			steadyAimHeld = pressed;
		}
		else
		{
			// hold-to-crouch mode
			steadyAim = pressed;
		}
	}

	public void FireInput(bool newFireState)
	{
		fire = newFireState;
	}

	public void ReloadInput(bool newReloadState)
	{
		reload = newReloadState;
	}

	public void Equip1Input(bool newEquip1State)
	{
		equip1 = newEquip1State;
	}

	public void Equip2Input(bool newEquip2State)
	{
		equip2 = newEquip2State;
	}

	public void Equip3Input(bool newEquip3State)
	{
		equip3 = newEquip3State;
	}

	public void Equip4Input(bool newEquip4State)
	{
		equip4 = newEquip4State;
	}

	public void Equip5Input(bool newEquip5State)
	{
		equip5 = newEquip5State;
	}

	public void Equip6Input(bool newEquip6State)
	{
		equip6 = newEquip6State;
	}

	public void Equip7Input(bool newEquip7State)
	{
		equip7 = newEquip7State;
	}

	public void Equip8Input(bool newEquip8State)
	{
		equip8 = newEquip8State;
	}

	public void InteractInput(bool newInteractState)
	{
		interact = newInteractState;
	}

	public void InspectInput(bool newInspectState)
	{
		inspect = newInspectState;
	}

	public void PauseInput(bool newPauseState)
	{
		pause = newPauseState;
	}

	public void LoadoutInput(bool newLoadoutState)
	{
		loadout = newLoadoutState;
	}

	public void InventoryInput(bool newInventoryState)
	{
		inventory = newInventoryState;
	}

	public void MapInput(bool newMapState)
	{
		map = newMapState;
	}

	public void DropItemInput(bool newDropItemState)
	{
		dropItem = newDropItemState;
	}

	public void ToggleShaderInput(bool newToggleShaderState)
	{
		toggleShader = newToggleShaderState;
	}

	public void Debug1Input(bool newDebug1State)
	{
		debug1 = newDebug1State;
	}

	public void Debug2Input(bool newDebug2State)
	{
		debug2 = newDebug2State;
	}

	public void LeftMouseHeldInput(bool pressed)
	{
		leftMouseHeld = pressed;
	}

	public void DragInUIInput(Vector2 newDragInUIState)
	{
		dragInUI = newDragInUIState;
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