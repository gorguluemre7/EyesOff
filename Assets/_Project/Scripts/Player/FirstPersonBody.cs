using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

namespace EyesOff.Player
{
    /// <summary>
    /// On the local player, the head (and things attached to it like hair and glasses) switch to
    /// "Shadows Only": our camera does not draw them, so we never see the inside of our own head,
    /// but they still cast a shadow, so the full body shadow is preserved. Other players are untouched.
    /// </summary>
    public class FirstPersonBody : NetworkBehaviour
    {
        [SerializeField] private Renderer[] headRenderers;

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
                return;

            foreach (Renderer r in headRenderers)
                r.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        }
    }
}
