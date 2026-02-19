using UnityEngine;
using Mirror;
using UnityEngine.Rendering;
using UnityEngine.SoundManager;
using Player;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
	[RequireComponent(typeof(CharacterController))]
	[RequireComponent(typeof(PlayerVitals))]
	public class FirstPersonController : NetworkBehaviour
	{
		[Header("Player")]
		[Tooltip("Move speed of the character in m/s")]
		public float MoveSpeed = 4.0f;
		[Tooltip("Sprint speed of the character in m/s")]
		public float SprintSpeed = 8.0f;
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
		[Tooltip("How far in degrees can you move the camera up")]
		public float TopClamp = 90.0f;
		[Tooltip("How far in degrees can you move the camera down")]
		public float BottomClamp = -90.0f;

		// cinemachine
		private float _cinemachineTargetPitch;

		// player
		private float _speed;
		private float _rotationVelocity;
		private float _verticalVelocity;
		private float _terminalVelocity = 53.0f;

		// player state
		private bool _isSprinting;

		// camera stats
		private float _normalFOV = 62f;
		private float _sprintFOV = 65f;

		// timeout deltatime
		private float _jumpTimeoutDelta;
		private float _fallTimeoutDelta;

		// footsteps
		[Header("Footsteps")]
		[SerializeField] private float walkStepInterval = 0.5f;
		[SerializeField] private float sprintStepInterval = 0.35f;
		[SerializeField] private float footstepVolume = 1f;
		[SerializeField] private float footstepMinDistance = 1.5f;
		[SerializeField] private float footstepMaxDistance = 20f;

private float _footstepTimer;

	
#if ENABLE_INPUT_SYSTEM
		private PlayerInput _playerInput;
#endif
		private CharacterController _controller;
		private StarterAssetsInputs _input;
		private GameObject _mainCamera;
		private PlayerVitals _vitals;

		private const float _threshold = 0.01f;



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

        public override void OnStartClient()
		{
			base.OnStartClient();

			_input = GetComponent<StarterAssetsInputs>();

		#if ENABLE_INPUT_SYSTEM
			_playerInput = GetComponent<PlayerInput>();
		#endif

			Debug.Log($"FirstPersonController.OnStartClient netId={netId} isLocalPlayer={isLocalPlayer}");

			if (!isLocalPlayer)
			{
				SetInputEnabled(false);
			}
		}


        private void SetInputEnabled(bool enabled)
		{
		#if ENABLE_INPUT_SYSTEM
			if (_playerInput != null)
			{
				_playerInput.enabled = enabled;

				if (enabled)
				{
					_playerInput.neverAutoSwitchControlSchemes = true;
					_playerInput.ActivateInput();
				}
			}
		#endif

			if (_input != null)
			{
				_input.enabled = enabled;
			}
		}


		public override void OnStartLocalPlayer() {
			Debug.Log($"FirstPersonController.OnStartLocalPlayer netId={netId}");

			// Re-grab references just in case
			_input = GetComponent<StarterAssetsInputs>();
			_controller = GetComponent<CharacterController>();
			_vitals = GetComponent<PlayerVitals>();

			if (_mainCamera == null) {
				_mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
			}

			// Find Cinemachine and set follow target
			var vcam = FindAnyObjectByType<Unity.Cinemachine.CinemachineCamera>();
			if (vcam != null && CinemachineCameraTarget != null) {
				vcam.Follow = CinemachineCameraTarget.transform;
			}

#if ENABLE_INPUT_SYSTEM
			_playerInput = GetComponent<PlayerInput>();
#endif
			SetInputEnabled(true);

			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
		}

		public override void OnStartAuthority()
		{
			base.OnStartAuthority();

			_input = GetComponent<StarterAssetsInputs>();
	#if ENABLE_INPUT_SYSTEM
			_playerInput = GetComponent<PlayerInput>();
	#endif

			if (isLocalPlayer)
			{
				SetInputEnabled(true);
			}
		}

		public override void OnStopAuthority()
		{
			base.OnStopAuthority();
			SetInputEnabled(false);
		}

		public override void OnStopLocalPlayer()
		{
			base.OnStopLocalPlayer();
			SetInputEnabled(false);
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;
			Debug.Log($"FirstPersonController.OnStopLocalPlayer netId={netId}");
		}

		private void Start()
		{
			_controller = GetComponent<CharacterController>();
			_input = GetComponent<StarterAssetsInputs>();
			_vitals = GetComponent<PlayerVitals>();

            // reset our timeouts on start
            _jumpTimeoutDelta = JumpTimeout;
			_fallTimeoutDelta = FallTimeout;

            // Disable input by default, OnStartLocalPlayer will enable it

            // -------------------------------------------------------------------------------------------------------
            // TODO: This improves performance by like <1% probably by limiting unnecessary input tracking, but it's causing an
			// inconsistent problem (1 in 4 runs on editor) where player movement gets disabled incorrectly so I'm leaving for future investigation.
			// This is already blocked in Update(), so not worth fixing rn

            //#if ENABLE_INPUT_SYSTEM
            //            _playerInput = GetComponent<PlayerInput>();
            //            if (_playerInput != null && !isLocalPlayer)
            //                _playerInput.enabled = false;
            //#endif
            // -------------------------------------------------------------------------------------------------------
        }

        [Client]
		private void Update()
		{
            if (!isLocalPlayer) return;
			if (_input == null) return;
            // Debug.Log($"_input.move: {_input.move} _playerInput.enabled: {_playerInput.enabled} isLocalPlayer: {isLocalPlayer}");
            JumpAndGravity();
			GroundedCheck();
			Move();
			HandleFootsteps();

		}

		private void LateUpdate()
		{
            if (!isLocalPlayer) return;
            if (_input == null) return;
            CameraRotation();
			CameraFOV();
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
				if (CinemachineCameraTarget != null) {
                    CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0.0f, 0.0f);
                }

				// rotate the player left and right
				transform.Rotate(Vector3.up * _rotationVelocity);
			}
		}

		private void CameraFOV()
		{
            var vcam = FindAnyObjectByType<Unity.Cinemachine.CinemachineCamera>();
            if (vcam == null)
				return;
			float targetFOV = _isSprinting ? _sprintFOV : _normalFOV;
			
			vcam.Lens.FieldOfView = Mathf.Lerp(vcam.Lens.FieldOfView, targetFOV, Time.deltaTime * 10f);
        }

        private void Move()
		{
			bool hasMoveInput = _input.move != Vector2.zero;
			bool sprintActive = _input.sprint && hasMoveInput;
			if (_vitals != null)
			{
				 sprintActive = _vitals.ResolveSprint(Time.deltaTime, _input.sprint, hasMoveInput);
			}

			_isSprinting = sprintActive;
			float targetSpeed = sprintActive ? SprintSpeed : MoveSpeed;

			// a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

			// note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
			// if there is no input, set the target speed to 0
			if (!hasMoveInput) targetSpeed = 0.0f;

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

		// Handle footstep sounds
		private void HandleFootsteps()
		{
			if (!Grounded) return;

			// Only count steps when actually moving on the ground
			Vector3 horizontalVel = new Vector3(_controller.velocity.x, 0f, _controller.velocity.z);
			if (horizontalVel.sqrMagnitude < 0.05f) return;

			float interval;
			if (_isSprinting)
				interval = sprintStepInterval;
			else
				interval = walkStepInterval;

			_footstepTimer -= Time.deltaTime;
			if (_footstepTimer > 0f) return;

			PlayFootstepLocalAndNetwork();
			_footstepTimer = interval;
		}

		// Play footstep sound locally and on network
		private void PlayFootstepLocalAndNetwork()
		{
			Vector3 pos = transform.position;

			// Play instantly for the local player to avoid latency
			SoundManager.Play3D(
				SoundType.PLAYERFOOTSTEP,
				pos,
				footstepVolume,
				minDistance: footstepMinDistance,
				maxDistance: footstepMaxDistance
			);

			// Call server to play for other clients
			CmdPlayFootstep(pos);
		}

		[Command(channel = Channels.Unreliable)]
		private void CmdPlayFootstep(Vector3 worldPos)
		{
			RpcPlayFootstep(worldPos);
		}

		[ClientRpc(channel = Channels.Unreliable)]
		private void RpcPlayFootstep(Vector3 worldPos)
		{
			// Don't play on local player, already played instantly
			if (isLocalPlayer) return;

			SoundManager.Play3D(
				SoundType.PLAYERFOOTSTEP,
				worldPos,
				footstepVolume,
				minDistance: footstepMinDistance,
				maxDistance: footstepMaxDistance
			);
		}

	}
}



