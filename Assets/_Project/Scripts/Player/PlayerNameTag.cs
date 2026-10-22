using EyesOff.Game;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace EyesOff.Player
{
    /// <summary>
    /// World Space UI above the player's head. Always faces the viewer (billboard).
    /// Hidden on our own player, and hidden in the dark so it can't give away the opponent's position.
    /// </summary>
    public class PlayerNameTag : NetworkBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private TMP_Text label;

        public override void OnNetworkSpawn()
        {
            label.text = PlayerNames.For(OwnerClientId);
            canvas.enabled = false;
        }

        private void LateUpdate()
        {
            if (!IsSpawned || IsOwner)
                return;

            PhaseManager phases = PhaseManager.Instance;
            canvas.enabled = phases != null && phases.IsSpawned && phases.IsLitPhase;

            Camera viewer = Camera.main;
            if (canvas.enabled && viewer != null)
                canvas.transform.rotation = viewer.transform.rotation;
        }
    }
}
