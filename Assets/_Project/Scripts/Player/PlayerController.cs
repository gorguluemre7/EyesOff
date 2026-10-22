using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EyesOff.Player
{
    /// <summary>
    /// First-person movement and looking. Only the owner reads input and moves;
    /// the NetworkTransform (Authority Mode = Owner) replicates position and yaw to everyone,
    /// and the pitch is shared through a NetworkVariable so others can see where we look.
    /// </summary>
    [RequireComponent(typeof(CharacterController), typeof(NetworkTransform))]
    public class PlayerController : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private Camera playerCamera;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 3.5f;
        [SerializeField] private float gravity = -20f;

        [Header("Look")]
        [SerializeField] private float lookSensitivity = 0.1f;
        [SerializeField] private float minPitch = -70f;
        [SerializeField] private float maxPitch = 70f;

        // Owner writes, everyone reads. Used later by the aim rig on remote players.
        public readonly NetworkVariable<float> LookPitch =
            new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        /// <summary>When true the player can still look around but cannot move (Panic Time).</summary>
        public bool MovementLocked { get; set; }

        /// <summary>When true mouse look is ignored too (match over, result screen open).</summary>
        public bool LookLocked { get; set; }

        private CharacterController _characterController;
        private InputAction _moveAction;
        private InputAction _lookAction;
        private Camera _sceneCamera;
        private float _verticalVelocity;
        private float _pitch;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            playerCamera.gameObject.SetActive(false);
        }

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
                return;

            // Project-wide actions (Project Settings > Input System Package).
            _moveAction = InputSystem.actions.FindAction("Player/Move");
            _lookAction = InputSystem.actions.FindAction("Player/Look");

            // Swap the menu camera for our own first-person camera.
            _sceneCamera = Camera.main;
            if (_sceneCamera != null)
                _sceneCamera.gameObject.SetActive(false);
            playerCamera.gameObject.SetActive(true);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // Runs after every NetworkBehaviour on this object has spawned, so the NetworkTransform is ready.
        protected override void OnNetworkPostSpawn()
        {
            if (IsOwner)
                MoveToSpawnPoint();
        }

        public override void OnNetworkDespawn()
        {
            if (!IsOwner)
                return;

            playerCamera.gameObject.SetActive(false);
            if (_sceneCamera != null)
                _sceneCamera.gameObject.SetActive(true);
        }

        private void Update()
        {
            if (!IsSpawned)
                return;

            if (IsOwner)
            {
                Look();
                Move();
            }
            else
            {
                // Remote players: apply the replicated pitch so their aim rig (head, torso, gun) follows it.
                Quaternion target = Quaternion.Euler(LookPitch.Value, 0f, 0f);
                cameraPivot.localRotation = Quaternion.Slerp(cameraPivot.localRotation, target, 15f * Time.deltaTime);
            }
        }

        private void Look()
        {
            if (LookLocked)
                return;

            Vector2 delta = _lookAction.ReadValue<Vector2>() * lookSensitivity;

            transform.Rotate(0f, delta.x, 0f);  // yaw turns the whole body

            _pitch = Mathf.Clamp(_pitch - delta.y, minPitch, maxPitch);
            cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            LookPitch.Value = _pitch;
        }

        private void Move()
        {
            Vector2 input = MovementLocked ? Vector2.zero : _moveAction.ReadValue<Vector2>();
            Vector3 horizontal = (transform.right * input.x + transform.forward * input.y) * moveSpeed;

            _verticalVelocity = _characterController.isGrounded ? -1f : _verticalVelocity + gravity * Time.deltaTime;

            _characterController.Move((horizontal + Vector3.up * _verticalVelocity) * Time.deltaTime);
        }

        private void MoveToSpawnPoint()
        {
            SpawnPoint spawn = SpawnPoint.Get(OwnerClientId);
            if (spawn == null)
                return;

            // CharacterController overrides transform changes while enabled.
            _characterController.enabled = false;
            GetComponent<NetworkTransform>().Teleport(spawn.transform.position, spawn.transform.rotation, transform.localScale);
            _characterController.enabled = true;
        }
    }
}
