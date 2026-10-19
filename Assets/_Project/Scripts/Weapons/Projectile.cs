using Unity.Netcode;
using UnityEngine;

namespace EyesOff.Weapons
{
    /// <summary>
    /// Server-simulated bullet. The server moves it and checks for hits with a raycast each frame;
    /// clients only see it through its NetworkTransform (Authority Mode = Server).
    /// </summary>
    public class Projectile : NetworkBehaviour
    {
        [SerializeField] private float speed = 25f;
        [SerializeField] private float lifetime = 3f;

        /// <summary>Set by the server before spawning, so the bullet ignores its own shooter.</summary>
        public ulong ShooterClientId { get; set; }

        private float _despawnTime;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
                _despawnTime = Time.time + lifetime;
        }

        private void Update()
        {
            if (!IsServer || !IsSpawned)
                return;

            float step = speed * Time.deltaTime;

            // A raycast over the distance travelled this frame, so fast bullets can't skip through walls.
            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, step) && !IsShooter(hit.collider))
            {
                NetworkObject.Despawn();
                return;
            }

            transform.position += transform.forward * step;

            if (Time.time >= _despawnTime)
                NetworkObject.Despawn();
        }

        private bool IsShooter(Collider other)
        {
            NetworkObject owner = other.GetComponentInParent<NetworkObject>();
            return owner != null && owner.IsPlayerObject && owner.OwnerClientId == ShooterClientId;
        }
    }
}
