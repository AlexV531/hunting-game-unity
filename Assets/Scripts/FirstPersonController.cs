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

	[Header("Item dropping")]
	public ItemSpawner itemSpawner;

	[Header("Map menu")]
	public MapMenu mapMenu;

	[Header("Grass trampling")]
	public GrassTrampler trampler;

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

	// interactables
	private InteractableBase currentInteractable;

	// shoulder carry
	private Animal carriedAnimal;
	private WorldItem carriedWorldItem;
	public NetworkVariable<bool> IsShoulderCarrying = new NetworkVariable<bool>(false,
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
	private LoadoutMenu _loadoutManager;
	private ShopUI _shopUI;
	private AnimalInspectUI _inspectUI;
	private PlayerInventoryMenu _playerInventoryMenu;
	private StorageMenu _storageMenu;
	private ObjectiveMenu _objectiveMenu;

	// ammo UI
	private AmmoUI _ammoUI;

	// weapon manager
	private WeaponManager _weaponManager;
	public Transform weaponContainer;

	// inventory
	private Inventory inventory = new Inventory();
	private Inventory storageInventory = new Inventory();


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
			_loadoutManager = GameObject.FindGameObjectWithTag("UserInterface").GetComponent<LoadoutMenu>();
		}
		if (_shopUI == null)
		{
			_shopUI = GameObject.FindGameObjectWithTag("UserInterface").GetComponent<ShopUI>();
		}
		if (_inspectUI == null)
		{
			_inspectUI = GameObject.FindGameObjectWithTag("UserInterface").GetComponent<AnimalInspectUI>();
		}
		if (_playerInventoryMenu == null)
		{
			_playerInventoryMenu = GameObject.FindGameObjectWithTag("UserInterface").GetComponent<PlayerInventoryMenu>();
		}
		if (_storageMenu == null)
		{
			_storageMenu = GameObject.FindGameObjectWithTag("UserInterface").GetComponent<StorageMenu>();
		}
		if (_objectiveMenu == null)
		{
			_objectiveMenu = GameObject.FindGameObjectWithTag("UserInterface").GetComponent<ObjectiveMenu>();
		}
		if (_interactText == null)
		{
			_interactText = GameObject.FindGameObjectWithTag("InteractText").GetComponent<TextMeshProUGUI>();
		}
		if (_ammoUI == null)
		{
			_ammoUI = GameObject.FindGameObjectWithTag("UserInterface").GetComponent<AmmoUI>();
		}
		GroundLayers = LayerMask.GetMask("Terrain", "Default", "Interactable");
	}

	private void Start()
	{
		_controller = GetComponent<CharacterController>();
		_input = GetComponent<PlayerInputs>();
		_weaponManager = GetComponent<WeaponManager>();
		_inspectUI.SetPlayerInput(_input);
		inventory.SetWeaponManager(_weaponManager);
		inventory.SetCanHoldLargeItems(false);
		_playerInventoryMenu.SetPlayerInput(_input);
#if ENABLE_INPUT_SYSTEM
		_playerInput = GetComponent<PlayerInput>();
#else
		Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

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

		// All clients should run this
		base.OnNetworkSpawn();
    	MapManager.Instance?.RegisterPlayer(gameObject, IsOwner);
	}

	public override void OnNetworkDespawn()
	{
		MapManager.Instance?.UnregisterPlayer(gameObject);
		base.OnNetworkDespawn();
	}

	private void Update()
	{
		if (!IsOwner)
			return;

		JumpAndGravity();
		HandleMenus();
		GroundedCheck();

		if (!IsPlayerInMenu())
			HandleCrouch();

		Move();

		if (IsPlayerInMenu())
			return;

		DetectInteractable();

		if (carriedAnimal != null)
		{
			carriedAnimal.transform.position = shoulderCarryPoint.position;
			carriedAnimal.transform.rotation = shoulderCarryPoint.rotation;
		}

		if (carriedWorldItem != null)
		{
			carriedWorldItem.transform.position = shoulderCarryPoint.position;
			carriedWorldItem.transform.rotation = shoulderCarryPoint.rotation;
		}

		// If current interactable is an Animal and player presses inspect
		if (currentInteractable != null && currentInteractable.IsInteractionEnabled() && currentInteractable is Corpse && _input.inspect)
		{
			// _inspectUI.OpenInspectScreen(((Corpse)currentInteractable).animal.internalContainer.gameObject, ((Corpse)currentInteractable).animal.hits);
			// Debug.Log("Requesting open inspect");
			RequestOpenInspect(((Corpse)currentInteractable).animal.NetworkObjectId);
			_input.inspect = false;
			return;
		}

		if (currentInteractable != null && currentInteractable.IsInteractionEnabled() && _input.interact)
		{
			currentInteractable.Interact(this);
		}
		else if (IsShoulderCarrying.Value && _input.interact)
		{
			if (carriedAnimal != null)
				DropAnimalServerRpc();
			if (carriedWorldItem != null)
				DropWorldItemServerRpc(true);
		}
		else if (attachedCart != null && _input.interact)
		{
			attachedCart.ReleaseCartServerRpc(OwnerClientId);
		}
		else if (_input.interact)
		{
			// Put thing you want to debug here
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
			// Debug.Log(hit.collider.name);
			InteractableBase interactable = hit.collider.GetComponent<InteractableBase>();

			if (interactable == null)
            {
                interactable = hit.collider.GetComponentInParent<InteractableBase>();
            }

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
		if (_loadoutManager.IsMenuOpen()) // Loadout menu open
		{
			if (_input.loadout || _input.pause)
			{
				_loadoutManager.CloseMenu();
				_input.pause = false;
				_input.loadout = false;
			}
		}
		else if (_shopUI.IsMenuOpen())
		{
			if (_input.loadout || _input.pause)
			{
				_shopUI.CloseMenu();
				_input.pause = false;
				_input.loadout = false;
			}
		}
		else if (_inspectUI.IsMenuOpen())
		{
			if (_input.loadout || _input.pause)
			{
				_inspectUI.CloseMenu();
				_input.pause = false;
				_input.loadout = false;
			}
		}
		else if (_playerInventoryMenu.IsMenuOpen())
		{
			if (_input.loadout || _input.pause || _input.inventory)
			{
				_playerInventoryMenu.CloseMenu();
				_input.pause = false;
				_input.loadout = false;
				_input.inventory = false;
			}
		}
		else if (_storageMenu.IsMenuOpen())
		{
			if (_input.loadout || _input.pause || _input.inventory)
			{
				_storageMenu.CloseMenu();
				_input.pause = false;
				_input.loadout = false;
				_input.inventory = false;
			}
		}
		else if (_objectiveMenu.IsMenuOpen())
        {
            if (_input.loadout || _input.pause || _input.inventory)
			{
				_objectiveMenu.CloseMenu();
				_input.pause = false;
				_input.loadout = false;
				_input.inventory = false;
			}
        }
		else if (mapMenu.IsMenuOpen())
        {
            if (_input.loadout || _input.pause || _input.map)
			{
				mapMenu.CloseMenu();
				_input.pause = false;
				_input.loadout = false;
				_input.map = false;
			}
        }
		else if (_pauseMenu.IsMenuOpen()) // Pause menu open
		{
			if (_input.pause)
			{
				_pauseMenu.CloseMenu();
				_input.pause = false;
			}
		}
		else // No menus open
		{
			if (_input.pause)
			{
				_pauseMenu.OpenMenu();
				_input.pause = false;
			}
			else if (_input.loadout)
			{
				_loadoutManager.OpenMenu();
				_input.loadout = false;
			}
			else if (_input.inventory)
			{
				_playerInventoryMenu.OpenMenu();
				_input.inventory = false;
			}
			else if (_input.map)
			{
				mapMenu.OpenMenu();
				_input.map = false;
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

	public void TestAddPeltToInventory()
	{
		ItemInstance pelt = new ItemInstance
		{
			key = 20,
			customData = new ItemCustomData
			{
				quality = 0.85f,
				color = Color.brown
			}
		};
		inventory.AddItem(pelt);
	}

	[ServerRpc(RequireOwnership = false)]
	public void PickUpWorldItemServerRpc(NetworkObjectReference worldItemRef)
	{
		// if (carriedAnimal != null) return;
		if (!worldItemRef.TryGet(out NetworkObject netObj)) return;
		if (!netObj.TryGetComponent<WorldItem>(out var worldItem)) return;

		worldItem.SetInteractionEnabled(false);

		worldItem.DisableCollidersClientRpc();

		worldItem.NetworkObject.ChangeOwnership(OwnerClientId);

		Debug.Log("Hello");

		BalloonAttach animalAttach = worldItem.GetComponent<BalloonAttach>();
		if (animalAttach != null)
			animalAttach.Release();

		IsShoulderCarrying.Value = true;
		Debug.Log("Hello");

		OnPickupWorldItemClientRpc(worldItemRef);
	}

	public void PickUpWorldItem(WorldItem worldItem)
	{
		// if (!worldItemRef.TryGet(out NetworkObject netObj)) return;
		// if (!netObj.TryGetComponent<WorldItem>(out var worldItem)) return;
		// if (carriedAnimal != null) return;

		worldItem.SetInteractionEnabled(false);

		worldItem.DisableColliders();
		worldItem.DisableCollidersClientRpc();

		Debug.Log("Attempting to change ownership of world item");

		worldItem.NetworkObject.ChangeOwnership(OwnerClientId);

		Debug.Log("Hello");

		BalloonAttach animalAttach = worldItem.GetComponent<BalloonAttach>();
		if (animalAttach != null)
			animalAttach.Release();

		IsShoulderCarrying.Value = true;
		Debug.Log("Hello");

		carriedWorldItem = worldItem;

		OnPickupWorldItemClientRpc(worldItem.NetworkObject);
	}

	[ClientRpc]
	private void OnPickupWorldItemClientRpc(NetworkObjectReference worldItemRef)
	{
		if (!worldItemRef.TryGet(out NetworkObject netObj)) return;
		if (!netObj.TryGetComponent<WorldItem>(out var worldItem)) return;

		Debug.Log("Client registered world item pickup");

		carriedWorldItem = worldItem;
	}
	
	[ServerRpc(RequireOwnership = false)]
	public void DropWorldItemServerRpc(bool enableCollidersOnDrop = true)
	{
		// Make sure the player is carrying a world item
		if (carriedWorldItem == null) return;

		var worldItem = carriedWorldItem;

		worldItem.NetworkObject.RemoveOwnership();

		if (enableCollidersOnDrop)
        {
			worldItem.EnableColliders();
			worldItem.EnableCollidersClientRpc();
        }

		worldItem.SetInteractionEnabled(true);

		worldItem.transform.position -= new Vector3(0f, 0.75f, 0f);

		IsShoulderCarrying.Value = false;
		carriedWorldItem = null;

		OnDropWorldItemClientRpc(worldItem.NetworkObject);
	}

	[ClientRpc]
	private void OnDropWorldItemClientRpc(NetworkObjectReference worldItemRef)
	{
		Debug.Log("Client registered world item drop");

		carriedWorldItem = null;
	}

	[ServerRpc(RequireOwnership = false)]
	public void PickUpAnimalServerRpc(NetworkObjectReference animalRef)
	{
		// if (carriedWorldItem != null) return;
		if (!animalRef.TryGet(out NetworkObject netObj)) return;
		if (!netObj.TryGetComponent<Animal>(out var animal)) return;

		// Pickup logic
		animal.corpseInteractable.SetInteractionEnabled(false);

		animal.DisableInternalCollidersClientRpc(); // CHANGE THIS SO PLAYER DOESN'T COLLIDE WITH INTERNALS, THIS ONLY AFFECTS MAIN ANIMAL COLLIDER FOR PLAYERS

		animal.NetworkObject.ChangeOwnership(OwnerClientId);

		AnimalReward animalReward = animal.GetComponent<AnimalReward>();
		if (animalReward != null)
			animalReward.butcherable = false; // If picking up from butcher table it must not be butcherable anymore

		BalloonAttach animalAttach = animal.GetComponent<BalloonAttach>();
		if (animalAttach != null)
			animalAttach.Release();

		if (animal.animalAI != null)
				animal.animalAI.animator.SetTrigger("carry");

		IsShoulderCarrying.Value = true;

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
		IsShoulderCarrying.Value = false;
		carriedAnimal = null;

		// Notify clients
		OnDropAnimalClientRpc(animal.NetworkObject);
	}

	[ClientRpc]
	private void OnDropAnimalClientRpc(NetworkObjectReference animalRef)
	{
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

	public WorldItem GetCarriedWorldItem()
	{
		if (IsOwner)
		{
			return carriedWorldItem;
		}
		return null;
	}

	public InteractableBase GetCurrentInteractable() => currentInteractable;

	public Inventory GetInventory() => inventory;

	public Inventory GetStorageInventory() => storageInventory;

	[ServerRpc(RequireOwnership = false)]
	private void SubmitNameServerRpc(string newName)
	{
		PlayerName.Value = newName;
	}

	[ServerRpc(RequireOwnership = false)]
	public void AddKillServerRpc()
	{
		// Only the server can update authoritative stats
		Debug.Log(OwnerClientId + " got kill credit");
		KillCount.Value += 1;
	}

	public void SavePlayer()
	{
		Debug.Log("Saved player data");
		PlayerSaveData data = new PlayerSaveData();
		data.money = Money;
		data.inventory = inventory;
		data.storageInventory = storageInventory;
		data.loadout = _weaponManager.GetCurrentLoadout();
		data.equippedWeaponInstance = _weaponManager.GetEquippedWeaponInstance();
		SaveSystem.SavePlayer(data);
	}

	public void LoadPlayer()
	{
		PlayerSaveData data = SaveSystem.LoadPlayer();
		if (data == null) return;

		Money = data.money;
		inventory = data.inventory;
		storageInventory = data.storageInventory;
		// inventory = new Inventory();
	}

	public bool IsPlayerInMenu()
	{
		return _pauseMenu.IsMenuOpen() || _loadoutManager.IsMenuOpen() || _shopUI.IsMenuOpen() || _inspectUI.IsMenuOpen() || _playerInventoryMenu.IsMenuOpen() || _storageMenu.IsMenuOpen() || _objectiveMenu.IsMenuOpen() || mapMenu.IsMenuOpen();
	}

	public WeaponManager GetWeaponManager() => _weaponManager;

	public LoadoutMenu GetLoadoutManager() => _loadoutManager;

	public ShopUI GetShopUI() => _shopUI;

	public PlayerInventoryMenu GetPlayerInventoryMenu() => _playerInventoryMenu;

	public StorageMenu GetStorageMenu() => _storageMenu;

	public ObjectiveMenu GetObjectiveMenu() => _objectiveMenu;

	public AmmoUI GetAmmoUI() => _ammoUI;

    public void RequestOpenInspect(ulong animalId)
    {
        if (IsOwner)
		{
			// Debug.Log("Requesting hit data");
            RequestHitDataServerRpc(animalId, NetworkManager.Singleton.LocalClientId);
        }
    }

    [ServerRpc]
    private void RequestHitDataServerRpc(ulong animalId, ulong requesterClientId)
    {
        // Find the animal by its NetworkObjectId
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(animalId, out var animalObj))
        {
            var animal = animalObj.GetComponent<Animal>();
            if (animal != null)
            {
                // Get data from the animal
                List<HitDataStrings> hitData = animal.GetHitData();
                
                // Send data back to this client
                SendAnimalInfoClientRpc(hitData.ToArray(), new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new[] { requesterClientId }
                    }
                });
            }
        }
    }

	[ClientRpc]
	private void SendAnimalInfoClientRpc(HitDataStrings[] hitData, ClientRpcParams rpcParams = default)
	{
		Debug.Log("Received animal info from server: " + hitData);
		if (currentInteractable != null && currentInteractable is Corpse)
		{
			// Debug.Log("Going to open inspect UI");
			_inspectUI.OpenInspectScreen(((Corpse)currentInteractable).animal.internalContainer.gameObject, hitData);
		}
	}

	[ServerRpc(RequireOwnership = false)]
    public void DropItemServerRpc(ItemInstance droppedItem)
    {
		itemSpawner.DropItem(droppedItem, itemSpawner.transform.position, Vector3.zero);
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
