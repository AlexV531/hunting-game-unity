using UnityEngine;
using TMPro;
using Cinemachine;
using Unity.Netcode;
using Unity.Collections;
using System.Collections.Generic;
using System;
using System.Xml.Serialization;
using Unity.VisualScripting;
using NUnit.Framework;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM
[RequireComponent(typeof(PlayerInput))]
#endif

public class FirstPersonController : NetworkBehaviour
{
	[Header("Player")]
	[Tooltip("Move speed of the character in m/s")]
	public float MoveSpeed = 4.0f;
	[Tooltip("Sprint speed of the character in m/s")]
	public float SprintSpeed = 6.0f;
	[Tooltip("Rotation speed of the character")]
	public float RotationSpeed = 1.0f;
	[Tooltip("Acceleration and deceleration")]
	public float SpeedChangeRate = 10.0f;

	[Space(10)]
	[Tooltip("The height the player can jump")]
	public float JumpHeight = 1.2f;
	[Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
	public float Gravity = -15.0f;

	[Space(10)]
	[Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
	public float JumpTimeout = 0.1f;
	[Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
	public float FallTimeout = 0.15f;

	[Header("Player Grounded")]
	[Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
	public bool Grounded = true;
	[Tooltip("Useful for rough ground")]
	public float GroundedOffset = -0.14f;
	[Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
	public float GroundedRadius = 0.5f;
	[Tooltip("What layers the character uses as ground")]
	public LayerMask GroundLayers;

	[Header("Cinemachine")]
	[Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
	public GameObject CinemachineCameraTarget;
	public CinemachineVirtualCamera vCam;
	[Tooltip("How far in degrees can you move the camera up")]
	public float TopClamp = 90.0f;
	[Tooltip("How far in degrees can you move the camera down")]
	public float BottomClamp = -90.0f;

	[Header("Interact")]
	public float interactRange = 3f;
	public LayerMask interactableLayer;

	[Header("Crouch")]
	[Tooltip("Normal standing height of the character controller")]
	public float StandHeight = 2.0f;

	[Tooltip("Crouched height of the character controller")]
	public float CrouchHeight = 1.0f;

	[Tooltip("How much the camera lowers when crouching")]
	public float CameraCrouchOffset = 0.5f;

	[Tooltip("Movement speed while crouching")]
	public float CrouchSpeed = 2.0f;

	[Header("Shoulder Carry")]
	public Transform shoulderCarryPoint;

	[Header("Cart Pulling")]
	public Transform grabPoint;

	[Header("Multiplayer")]
	public NetworkVariable<FixedString64Bytes> PlayerName = new NetworkVariable<FixedString64Bytes>(
		"Player",
		NetworkVariableReadPermission.Everyone,
		NetworkVariableWritePermission.Server);

	public NetworkVariable<int> KillCount = new NetworkVariable<int>(
		0,
		NetworkVariableReadPermission.Everyone,
		NetworkVariableWritePermission.Server);

	// Local player reference
	public static FirstPersonController LocalPlayer { get; private set; }
	public static event Action<FirstPersonController> OnLocalPlayerSpawned;

	// Inventory
	private int money;
    public int Money
    {
        get => money;
        set
        {
            money = value;
            if (IsLocalPlayer)
                OnMoneyChanged?.Invoke(money); // Only local player triggers UI
        }
    }

    public event Action<int> OnMoneyChanged;

	public List<string> Inventory;

	// interactables
	private InteractableBase currentInteractable;

	// shoulder carry
	private Animal carriedAnimal;
	public NetworkVariable<bool> IsCarryingAnimal = new NetworkVariable<bool>(false,
		NetworkVariableReadPermission.Everyone,
		NetworkVariableWritePermission.Server);

	// hand cart carry
	public HandCart attachedCart;

	// cinemachine
	private float _cinemachineTargetPitch;

	// player
	private float _speed;
	private float _rotationVelocity;
	private float _verticalVelocity;
	private float _terminalVelocity = 53.0f;

	// crouch
	private bool _isCrouching = false;
	private Vector3 _cameraStandPos;

	// footsteps
	private float timeSinceLastStep;
	private float timeBetweenSteps = 0.75f;
	private float crouchTimeBetweenModifier = 0.5f;
	private float sprintTimeBetweenModfier = 2f;
	private float footstepLoudness = 17f;
	private float crouchLoudnessModifier = 0.4f;
	private float sprintLoudnessModfier = 1.75f;

	// timeout deltatime
	private float _jumpTimeoutDelta;
	private float _fallTimeoutDelta;

	// pause
	private PauseMenu _pauseMenu;
	private LoadoutManager _loadoutManager;
	private ShopUI _shopUI;
	private AnimalInspectUI _inspectUI;

	// weapon manager
	private WeaponManager _weaponManager;
	public Transform weaponContainer;


#if ENABLE_INPUT_SYSTEM
	private PlayerInput _playerInput;
#endif
	private CharacterController _controller;
	private PlayerInputs _input;
	private GameObject _mainCamera;
	private TextMeshProUGUI _interactText;

	private const float _threshold = 0f;

	private bool IsCurrentDeviceMouse
	{
		get
		{
#if ENABLE_INPUT_SYSTEM
			return _playerInput.currentControlScheme == "KeyboardMouse";
#else
			return false;
#endif
		}
	}

	private void Awake()
	{
		if (_mainCamera == null)
		{
			_mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
		}
		if (vCam != null)
		{
			vCam.m_Lens.FieldOfView = GlobalVariables.cameraFOV;
		}
		if (_pauseMenu == null)
		{
			_pauseMenu = GameObject.FindGameObjectWithTag("UserInterface").GetComponent<PauseMenu>();
		}
		if (_loadoutManager == null)
		{
			_loadoutManager = GameObject.FindGameObjectWithTag("UserInterface").GetComponent<LoadoutManager>();
		}
		if (_shopUI == null)
		{
			_shopUI = GameObject.FindGameObjectWithTag("UserInterface").GetComponent<ShopUI>();
		}
		if (_inspectUI == null)
		{
			_inspectUI = GameObject.FindGameObjectWithTag("UserInterface").GetComponent<AnimalInspectUI>();
		}
		if (_interactText == null)
		{
			_interactText = GameObject.FindGameObjectWithTag("InteractText").GetComponent<TextMeshProUGUI>();
		}
		GroundLayers = LayerMask.GetMask("Terrain", "Default", "Interactable");
	}

	private void Start()
	{
		_controller = GetComponent<CharacterController>();
		_input = GetComponent<PlayerInputs>();
		_inspectUI.SetPlayerInput(_input);
		// GlobalVariables.RegisterPlayerInputs(_input);
#if ENABLE_INPUT_SYSTEM
		_playerInput = GetComponent<PlayerInput>();
#else
		Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif
		_weaponManager = GetComponent<WeaponManager>();

		// crouch setup
		_cameraStandPos = CinemachineCameraTarget.transform.localPosition;

		// reset our timeouts on start
		_jumpTimeoutDelta = JumpTimeout;
		_fallTimeoutDelta = FallTimeout;
	}

	public override void OnNetworkSpawn()
	{
		if (IsOwner && IsClient)
		{
			string savedName = PlayerPrefs.GetString("PlayerName", "Player");
			SubmitNameServerRpc(savedName);

			LoadPlayer();
		}
		if (IsLocalPlayer)
		{
			LocalPlayer = this;
			OnLocalPlayerSpawned?.Invoke(this);
			Debug.Log("Local player assigned via OnNetworkSpawn");
		}
	}

	private void Update()
	{
		if (!IsOwner)
			return;

		JumpAndGravity();
		HandleMenus();

		if (IsPlayerInMenu())
			return;

		GroundedCheck();
		HandleCrouch();
		Move();
		DetectInteractable();

		if (carriedAnimal != null)
		{
			carriedAnimal.transform.position = shoulderCarryPoint.position;
			carriedAnimal.transform.rotation = shoulderCarryPoint.rotation;
		}

		if (currentInteractable != null && currentInteractable.IsInteractionEnabled() && _input.interact)
		{
			currentInteractable.Interact(this);
		}
		else if (IsCarryingAnimal.Value && _input.interact)
		{
			DropAnimalServerRpc();
		}
		else if (attachedCart != null && _input.interact)
        {
			attachedCart.ReleaseCartServerRpc(OwnerClientId);
			// attachedCart = null;
        }
		_input.interact = false;
	}

	private void LateUpdate()
	{
		if (IsPlayerInMenu())
			return;

		CameraRotation();
	}

	private void GroundedCheck()
	{
		// set sphere position, with offset
		Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
		Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
	}

	private void CameraRotation()
	{
		// if there is an input
		if (_input.look.sqrMagnitude >= _threshold)
		{
			//Don't multiply mouse input by Time.deltaTime
			float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

			_cinemachineTargetPitch += _input.look.y * RotationSpeed * deltaTimeMultiplier;
			_rotationVelocity = _input.look.x * RotationSpeed * deltaTimeMultiplier;

			// clamp our pitch rotation
			_cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

			// Update Cinemachine camera target pitch
			CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0.0f, 0.0f);

			// rotate the player left and right
			transform.Rotate(Vector3.up * _rotationVelocity);
		}
	}

	void DetectInteractable()
	{
		Ray ray = new Ray(_mainCamera.transform.position, _mainCamera.transform.forward);

		if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactableLayer))
		{
			InteractableBase interactable = hit.collider.GetComponent<InteractableBase>();

			if (interactable != null && interactable.IsInteractionEnabled())
			{
				if (interactable != currentInteractable)
				{
					// Debug.Log("Changing current interactable");
					currentInteractable = interactable;
					_interactText.text = interactable.GetPrompt(this);
					_interactText.gameObject.SetActive(true);
				}
				return;
			}
		}

		// No interactable found
		currentInteractable = null;
		_interactText.gameObject.SetActive(false);
	}

	private void HandleMenus()
	{
		if (_loadoutManager.IsLoadoutOpen()) // Loadout menu open
		{
			if (_input.loadout || _input.pause)
			{
				_loadoutManager.CloseLoadoutScreen();
				_input.pause = false;
				_input.loadout = false;
			}
		}
		else if (_shopUI.IsShopOpen())
		{
			if (_input.loadout || _input.pause)
			{
				_shopUI.CloseShopScreen();
				_input.pause = false;
				_input.loadout = false;
			}
		}
		else if (_inspectUI.IsInspectOpen())
		{
            if (_input.loadout || _input.pause)
			{
				_inspectUI.CloseInspectScreen();
				_input.pause = false;
				_input.loadout = false;
			}
        }
		else if (PauseMenu.IsPaused()) // Pause menu open
		{
			if (_input.pause)
			{
				_pauseMenu.Resume();
				_input.pause = false;
			}
		}
		else // No menus open
		{
			if (_input.pause)
			{
				_pauseMenu.Pause();
				_input.pause = false;
			}
			else if (_input.loadout)
			{
				// _loadoutManager.OpenLoadoutScreen();
				_inspectUI.OpenInspectScreen();
				_input.loadout = false;
			}
		}
		
	}

	private void Move()
	{
		// set target speed based on move speed, sprint speed and if sprint is pressed
		float targetSpeed = _isCrouching ? CrouchSpeed : (_input.sprint ? SprintSpeed : MoveSpeed);

		// a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

		// note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
		// if there is no input, set the target speed to 0
		if (_input.move == Vector2.zero) targetSpeed = 0.0f;

		// a reference to the players current horizontal velocity
		float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

		float speedOffset = 0.1f;
		float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

		// accelerate or decelerate to target speed
		if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
		{
			// creates curved result rather than a linear one giving a more organic speed change
			// note T in Lerp is clamped, so we don't need to clamp our speed
			_speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);

			// round speed to 3 decimal places
			_speed = Mathf.Round(_speed * 1000f) / 1000f;
		}
		else
		{
			_speed = targetSpeed;
		}

		// normalise input direction
		Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

		// note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
		// if there is a move input rotate player when the player is moving
		if (_input.move != Vector2.zero)
		{
			// move
			inputDirection = transform.right * _input.move.x + transform.forward * _input.move.y;
		}

		// move the player
		_controller.Move(inputDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

		float loudnessModifier = 1f;
		float timeBetweenStepsModifier = 1f;
		if (_isCrouching)
		{
			loudnessModifier *= crouchLoudnessModifier;
			timeBetweenStepsModifier *= crouchTimeBetweenModifier;
		}
		if (_input.sprint)
        {
        	loudnessModifier *= sprintLoudnessModfier;
			timeBetweenStepsModifier *= sprintTimeBetweenModfier;
        }
		float trueFootstepLoudness = footstepLoudness * loudnessModifier;
		float trueTimeBetweenSteps = timeBetweenSteps * timeBetweenStepsModifier;

		// increment footstep timer if player is moving
		if (targetSpeed != 0)
		{
			timeSinceLastStep += Time.deltaTime;
		}
		// emit noise based on footstep timing and whether player is crouching, sprinting etc.
		if (timeSinceLastStep >= trueTimeBetweenSteps)
        {
			timeSinceLastStep %= trueTimeBetweenSteps;
			EmitNoiseServerRpc(transform.position, trueFootstepLoudness, "player " + OwnerClientId + " footstep");
        }

	}

	[ServerRpc(RequireOwnership = false)]
    void EmitNoiseServerRpc(Vector3 position, float loudness, string name)
    {
        var noiseEvent = new NoiseEvent(position, loudness, name);
        NoiseManager.Instance.EmitNoise(noiseEvent);
    }

	private void JumpAndGravity()
	{
		if (Grounded)
		{
			// reset the fall timeout timer
			_fallTimeoutDelta = FallTimeout;

			// stop our velocity dropping infinitely when grounded
			if (_verticalVelocity < 0.0f)
			{
				_verticalVelocity = -2f;
			}

			// Jump
			if (_input.jump && _jumpTimeoutDelta <= 0.0f)
			{
				// the square root of H * -2 * G = how much velocity needed to reach desired height
				_verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
			}

			// jump timeout
			if (_jumpTimeoutDelta >= 0.0f)
			{
				_jumpTimeoutDelta -= Time.deltaTime;
			}
		}
		else
		{
			// reset the jump timeout timer
			_jumpTimeoutDelta = JumpTimeout;

			// fall timeout
			if (_fallTimeoutDelta >= 0.0f)
			{
				_fallTimeoutDelta -= Time.deltaTime;
			}

			// if we are not grounded, do not jump
			_input.jump = false;
		}

		// apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
		if (_verticalVelocity < _terminalVelocity)
		{
			_verticalVelocity += Gravity * Time.deltaTime;
		}
	}

	private void HandleCrouch()
	{
		if (_input.crouch)
		{
			if (!_isCrouching)
			{
				_isCrouching = true;
				_controller.height = CrouchHeight;
				_controller.center = new Vector3(0, CrouchHeight / 2f, 0);

				Vector3 camPos = _cameraStandPos;
				camPos.y -= CameraCrouchOffset;
				CinemachineCameraTarget.transform.localPosition = camPos;
			}
		}
		else
		{
			if (_isCrouching)
			{
				_isCrouching = false;
				// _controller.enabled = false;
				_controller.height = StandHeight;
				_controller.center = new Vector3(0, StandHeight / 2f, 0);
				// _controller.enabled = true;

				CinemachineCameraTarget.transform.localPosition = _cameraStandPos;
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void PickUpAnimalServerRpc(NetworkObjectReference animalRef)
	{
		if (!animalRef.TryGet(out NetworkObject netObj)) return;
		if (!netObj.TryGetComponent<Animal>(out var animal)) return;

		// Pickup logic
		animal.corpseInteractable.SetInteractionEnabled(false);

		animal.DisableInternalCollidersClientRpc(); // CHANGE THIS SO PLAYER DOESN'T COLLIDE WITH INTERNALS, THIS ONLY AFFECTS MAIN ANIMAL COLLIDER FOR PLAYERS

		animal.NetworkObject.ChangeOwnership(OwnerClientId);

		BalloonAttach animalAttach = animal.GetComponent<BalloonAttach>();
		if (animalAttach != null)
			animalAttach.Release();

		if (animal.animalAI != null)
				animal.animalAI.animator.SetTrigger("carry");

		IsCarryingAnimal.Value = true;

		OnPickupAnimalClientRpc(animalRef);
	}

	[ClientRpc]
	private void OnPickupAnimalClientRpc(NetworkObjectReference animalRef)
	{
		if (!animalRef.TryGet(out NetworkObject netObj)) return;
		if (!netObj.TryGetComponent<Animal>(out var animal)) return;

		if (animal.animalAI != null)
			animal.animalAI.animator.SetTrigger("carry");

		carriedAnimal = animal; // client stores the local reference
	}

	[ServerRpc(RequireOwnership = false)]
	public void DropAnimalServerRpc(bool enableCollidersOnDrop = true)
	{
		// Make sure the player is carrying an animal
		if (carriedAnimal == null) return;

		var animal = carriedAnimal;

		// Release ownership
		animal.NetworkObject.RemoveOwnership();

		// Enable colliders and corpse state
		if (enableCollidersOnDrop)
			animal.EnableInternalCollidersClientRpc();
		animal.corpseInteractable.SetInteractionEnabled(true);

		// Drop slightly below player
		animal.transform.position -= new Vector3(0f, 0.75f, 0f);

		// Trigger drop animation if AI exists
		if (animal.animalAI != null)
			animal.animalAI.animator.SetTrigger("drop");

		// Update player state
		IsCarryingAnimal.Value = false;
		carriedAnimal = null;

		// Notify clients
		OnDropAnimalClientRpc(animal.NetworkObject);
	}

	[ClientRpc]
	private void OnDropAnimalClientRpc(NetworkObjectReference animalRef)
	{
		carriedAnimal = null;
	}

	[ServerRpc(RequireOwnership = false)]
	public void PlaceAnimalServerRpc(NetworkObjectReference tableRef)
	{
		// Make sure player is carrying an animal
		if (carriedAnimal == null) return;

		var animal = carriedAnimal;

		if (!tableRef.TryGet(out NetworkObject tableObj)) return;
		if (!tableObj.TryGetComponent<AnimalStoringInteractableBase>(out var table)) return;

		// Release ownership from player
		animal.NetworkObject.RemoveOwnership();

		// Place animal (NetworkTransform handles syncing position/rotation)
		animal.transform.SetPositionAndRotation(table.placementPoint.position, table.placementPoint.rotation);

		// Track animal server-side
		table.SetPlacedAnimal(animal);

		if (animal.animalAI != null)
			animal.animalAI.animator.SetTrigger("drop");

		IsCarryingAnimal.Value = false;

		// Clear carried animal state
		carriedAnimal = null;

		// Tell clients to update
		OnPlaceAnimalClientRpc(animal.NetworkObject, tableRef);
	}

	[ClientRpc]
	private void OnPlaceAnimalClientRpc(NetworkObjectReference animalRef, NetworkObjectReference tableRef)
	{
		if (!animalRef.TryGet(out NetworkObject netObj)) return;
		if (!netObj.TryGetComponent<Animal>(out var animal)) return;

		if (!tableRef.TryGet(out NetworkObject tableObj)) return;
		if (!tableObj.TryGetComponent<AnimalStoringInteractableBase>(out var table)) return;

		table.SetPlacedAnimal(animal);
		carriedAnimal = null;
	}

	public Animal GetCarriedAnimal()
	{
		if (IsOwner)
		{
			return carriedAnimal;
		}
		return null;
	}

	public InteractableBase GetCurrentInteractable()
	{
		return currentInteractable;
	}

	[ServerRpc(RequireOwnership = false)]
	private void SubmitNameServerRpc(string newName)
	{
		PlayerName.Value = newName;
	}

	[ServerRpc(RequireOwnership = false)]
	public void AddKillServerRpc()
	{
		// Only the server can update authoritative stats
		KillCount.Value += 1;
	}

	public void SavePlayer()
	{
		Debug.Log("Saved player data");
		PlayerSaveData data = new PlayerSaveData();
		data.money = Money;
		data.unlockedWeaponKeys = _weaponManager.GetUnlockedWeaponKeys();
		data.loadout = _loadoutManager.GetCurrentLoadout();
		data.equippedWeaponKey = _weaponManager.GetEquippedWeaponKey();
		SaveSystem.SavePlayer(data);
	}

	public void LoadPlayer()
	{
		PlayerSaveData data = SaveSystem.LoadPlayer();
		if (data == null) return;

		Money = data.money;
		Inventory.Clear();
	}

	public bool IsPlayerInMenu()
	{
		return PauseMenu.IsPaused() || _loadoutManager.IsLoadoutOpen() || _shopUI.IsShopOpen() || _inspectUI.IsInspectOpen();
	}

	public WeaponManager GetWeaponManager()
	{
		return _weaponManager;
	}

	public LoadoutManager GetLoadoutManager()
	{
		return _loadoutManager;
	}

	public ShopUI GetShopUI()
	{
		return _shopUI;
	}

	private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
	{
		if (lfAngle < -360f) lfAngle += 360f;
		if (lfAngle > 360f) lfAngle -= 360f;
		return Mathf.Clamp(lfAngle, lfMin, lfMax);
	}

	private void OnDrawGizmosSelected()
	{
		Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
		Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

		if (Grounded) Gizmos.color = transparentGreen;
		else Gizmos.color = transparentRed;

		// when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
		Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
	}
	
	public override void OnDestroy()
    {
		base.OnDestroy();
        if (LocalPlayer == this)
			LocalPlayer = null;
    }
}
