using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EyesOff.Weapons
{
    /// <summary>
    /// The owner presses Attack -> asks the server to fire -> the server spawns a networked Projectile.
    /// </summary>
    public class WeaponShooter : NetworkBehaviour
    {
        [SerializeField] private Projectile projectilePrefab;
        [Tooltip("Where the bullet appears (tip of the gun barrel).")]
        [SerializeField] private Transform muzzle;
        [Tooltip("The player camera. Bullets fly from the muzzle to whatever is under the screen center (crosshair).")]
        [SerializeField] private Transform aim;
        [SerializeField] private float maxAimDistance = 100f;
        [SerializeField] private float cooldown = 0.4f;

        /// <summary>Server-only: raised with the shooter's client id. Used by the phase flow (Panic Time).</summary>
        public static event Action<ulong> ServerShotFired;

        /// <summary>How many times this player fired (shown on the result screen).</summary>
        public readonly NetworkVariable<int> ShotsFired = new();

        /// <summary>Set by the phase flow to block shooting.</summary>
        public bool CanShoot { get; set; } = true;

        private InputAction _attackAction;
        private float _nextShotTime;

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
                _attackAction = InputSystem.actions.FindAction("Player/Attack");
        }

        private void Update()
        {
            if (!IsSpawned || !IsOwner || !CanShoot)
                return;

            // Ignore clicks while the cursor is free (e.g. a menu is open).
            if (Cursor.lockState != CursorLockMode.Locked)
                return;

            if (_attackAction.WasPressedThisFrame() && Time.time >= _nextShotTime)
            {
                _nextShotTime = Time.time + cooldown;
                Vector3 direction = (GetAimPoint() - muzzle.position).normalized;
                ShootRpc(muzzle.position, direction);
            }
        }

        /// <summary>
        /// The point under the crosshair: the first thing the camera's center ray hits (skipping our own body).
        /// The muzzle sits below and to the right of the camera, so shooting along the camera's forward
        /// from the muzzle would land off the crosshair.
        /// </summary>
        private Vector3 GetAimPoint()
        {
            Vector3 point = aim.position + aim.forward * maxAimDistance;
            float closest = maxAimDistance;

            foreach (RaycastHit hit in Physics.RaycastAll(aim.position, aim.forward, maxAimDistance,
                         Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (hit.distance < closest && !hit.transform.IsChildOf(transform))
                {
                    closest = hit.distance;
                    point = hit.point;
                }
            }

            return point;
        }

        [Rpc(SendTo.Server)]
        private void ShootRpc(Vector3 position, Vector3 direction)
        {
            Projectile projectile = Instantiate(projectilePrefab, position, Quaternion.LookRotation(direction));
            projectile.ShooterClientId = OwnerClientId;
            projectile.NetworkObject.Spawn();

            ShotsFired.Value++;
            ServerShotFired?.Invoke(OwnerClientId);
        }
    }
}
